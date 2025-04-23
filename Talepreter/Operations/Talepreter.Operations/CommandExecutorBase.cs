using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Data.BaseTypes;
using Talepreter.Exceptions;
using Talepreter.Model.Command;
using Talepreter.Model.Document;

namespace Talepreter.Operations;

public abstract class CommandExecutorBase : ICommandExecutorBase
{
    protected CancellationToken Token = CancellationToken.None;
    protected IExecuteContext Context { get; private set; } = default!;
    protected IDocumentDbContext DbContext { get; private set; } = default!;
    protected ITaskDbContext TaskDbContext { get; private set; } = default!;

    public void Initialize(IDocumentDbContext dbContext, ITaskDbContext taskDbContext, CancellationToken token)
    {
        DbContext = dbContext;
        TaskDbContext = taskDbContext;
        Token = token;
        Token.ThrowIfCancellationRequested();
    }

    // --

    protected void Setup(ExecuteCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(nameof(context));
        Context = context;
        Token.ThrowIfCancellationRequested();
    }
    protected void Setup(ExecuteTriggerContext context)
    {
        ArgumentNullException.ThrowIfNull(nameof(context));
        Context = context;
        Token.ThrowIfCancellationRequested();
    }
    protected async Task Finalize<T>(T document) where T : DocumentBase
    {
        PreFinalize(document);
        document.LastUpdatedChapter = Context.Chapter;
        document.LastUpdatedPageInChapter = Context.PageInChapter;
        document.LastUpdate = DateTime.UtcNow;
        Token.ThrowIfCancellationRequested();
        await OverwriteAsync(document);
        Token.ThrowIfCancellationRequested();

        if (TaskDbContext != null) await TaskDbContext.SaveChangesAsync(Token);
        Token.ThrowIfCancellationRequested();
    }
    protected async Task FinalizeExtension<T>(T document) where T : DocumentBase<string>
    {
        PreFinalizeExtension(document);
        document.LastUpdatedChapter = Context.Chapter;
        document.LastUpdatedPageInChapter = Context.PageInChapter;
        document.LastUpdate = DateTime.UtcNow;
        Token.ThrowIfCancellationRequested();
        await OverwriteExtensionAsync(document);
        Token.ThrowIfCancellationRequested();

        if (TaskDbContext != null) await TaskDbContext.SaveChangesAsync(Token);
        Token.ThrowIfCancellationRequested();
    }

    // --

    protected virtual void PreFinalize<T>(T document) where T : DocumentBase
    {
    }
    protected virtual void PreFinalizeExtension<T>(T document) where T : DocumentBase<string>
    {
    }

    protected async Task<T?> FetchAsync<T>(string name, bool required, DocumentState state = DocumentState.Any) where T : DocumentBase<string>
    {
        var dbId = $"{typeof(T).Name}:{name}";
        var document = await DbContext.GetAsync<T>(Context.TaleId, Context.TaleVersionId, dbId, state, Token);
        if (document == null && required) throw new CommandExecutionException($"{typeof(T).Name} {name} not found");
        Token.ThrowIfCancellationRequested();
        return document;
    }
    protected async Task<T?> FetchAsync<T>(long id, bool required, DocumentState state = DocumentState.Any) where T : DocumentBase<long>
    {
        var dbId = $"{typeof(T).Name}:{id}";
        var document = await DbContext.GetAsync<T>(Context.TaleId, Context.TaleVersionId, dbId, state, Token);
        if (document == null && required) throw new CommandExecutionException($"{typeof(T).Name} #{id} not found");
        Token.ThrowIfCancellationRequested();
        return document;
    }
    protected async Task<E?> FetchExtensionAsync<E>(string docType, string name, bool required) where E : DocumentBase<string>
    {
        var dbId = $"{docType}:{name}";
        var document = await DbContext.GetAsync<E>(Context.TaleId, Context.TaleVersionId, dbId, DocumentState.Any, Token);
        if (document == null && required) throw new CommandExecutionException($"{typeof(E).Name} {name} not found");
        return document;
    }
    protected async Task OverwriteAsync<T>(T document) where T : DocumentBase
    => await DbContext.OverwriteAsync(Context.TaleId, Context.TaleVersionId, document, Token);
    protected async Task OverwriteExtensionAsync<E>(E document) where E : DocumentBase<string>
        => await DbContext.OverwriteAsync(Context.TaleId, Context.TaleVersionId, document, Token);
    protected async Task<long> CountAsync<T>(string dbId, int limit = 1) where T : DocumentBase
        => await DbContext.CountAsync(Context.TaleId, Context.TaleVersionId, Builders<T>.Filter.Eq(x => x.Id, dbId), limit, Token);
    protected async Task<long> CountAsync<T>(string docType, string name, int limit = 1) where T : DocumentBase
        => await DbContext.CountAsync(Context.TaleId, Context.TaleVersionId, Builders<T>.Filter.Eq(x => x.Id, $"{docType}:{name}"), limit, Token);
    protected async Task ValidateTimeline<E>(PageTimelineInfo record, string documentType, bool isExtension)
        where E : DocumentBase<string>
    {
        if (record.TravelTo != null)
        {
            var filter1 = Builders<E>.Filter.Or(
                Builders<E>.Filter.Eq(x => x.DocumentId, record.StartLocation.Settlement),
                Builders<E>.Filter.Eq(x => x.DocumentId, record.TravelTo.Settlement));
            var filter2 = Builders<E>.Filter.Eq(x => x.DocumentType, documentType);
            var filter3 = Builders<E>.Filter.Eq(x => x.IsExtension, isExtension);
            var filter4 = Builders<E>.Filter.Eq(x => x.State, DocumentState.Active);
            var filter = Builders<E>.Filter.And(filter1, filter2, filter3, filter4);
            var res = (await DbContext.GetManyAsync(Context.TaleId, Context.TaleVersionId, filter, DocumentState.Active, Token))?.Select(x => x.DocumentId) ?? [];
            if (!res.Contains(record.StartLocation.Settlement)) throw new CommandExecutionException($"Invalid timeline information, local settlement {record.StartLocation.Settlement} does not match");
            if (!res.Contains(record.TravelTo.Settlement)) throw new CommandExecutionException($"Invalid timeline information, travel settlement {record.TravelTo.Settlement} does not match");
        }
        else
        {
            var filter1 = Builders<E>.Filter.Eq(x => x.DocumentId, record.StartLocation.Settlement);
            var filter2 = Builders<E>.Filter.Eq(x => x.DocumentType, documentType);
            var filter3 = Builders<E>.Filter.Eq(x => x.IsExtension, isExtension);
            var filter4 = Builders<E>.Filter.Eq(x => x.State, DocumentState.Active);
            var filter = Builders<E>.Filter.And(filter1, filter2, filter3, filter4);
            var res = await DbContext.CountAsync(Context.TaleId, Context.TaleVersionId, filter, 1, Token);
            if (res < 1) throw new CommandExecutionException($"Invalid timeline information, local settlement {record.StartLocation.Settlement} does not match");
        }
    }

    // -- 

    protected T[] Append<T>(T[] source, T entry) => [.. source, .. new[] { entry }];

    protected void ApplyDirect<T>(Parameter<T>? par, Action<T>? set = null, Action<T>? reset = null, Action<T>? add = null, Action<T>? remove = null)
    {
        if (par == null) return;
        switch (par.Type)
        {
            case ParameterType.Set: if (set != null) set.Invoke(par.Value); else throw new CommandValidationException("Command does not accept set action"); break;
            case ParameterType.Reset: if (reset != null) reset.Invoke(par.Value); else throw new CommandValidationException("Command does not accept reset action"); break;
            case ParameterType.Add: if (add != null) add.Invoke(par.Value); else throw new CommandValidationException("Command does not accept add action"); break;
            case ParameterType.Remove: if (remove != null) remove.Invoke(par.Value); else throw new CommandValidationException("Command does not accept remove action"); break;
            default: throw new InvalidOperationException($"Parameter type {par.Type} is unrecognized");
        };
    }
    protected T Apply<T>(T @default, Parameter<T>? par, Func<T, T>? set = null, Func<T, T>? reset = null, Func<T, T>? add = null, Func<T, T>? remove = null)
    {
        if (par == null) return @default;
        switch (par.Type)
        {
            case ParameterType.Set: if (set != null) return set.Invoke(par.Value); else throw new CommandValidationException("Command does not accept set action");
            case ParameterType.Reset: if (reset != null) return reset.Invoke(par.Value); else throw new CommandValidationException("Command does not accept reset action");
            case ParameterType.Add: if (add != null) return add.Invoke(par.Value); else throw new CommandValidationException("Command does not accept add action");
            case ParameterType.Remove: if (remove != null) return remove.Invoke(par.Value); else throw new CommandValidationException("Command does not accept remove action");
            default: throw new InvalidOperationException($"Parameter type {par.Type} is unrecognized");
        };
    }
}
