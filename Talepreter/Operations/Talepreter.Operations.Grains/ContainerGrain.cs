using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Exceptions;

namespace Talepreter.Operations.Grains;

[StatelessWorker]
public abstract class ContainerGrain<TSelf> : GrainBase, IContainerGrain
{
    private readonly IDocumentDbContext _documentDbContext;

    private string Id(Guid taleId, Guid taleVersionId) => $"{taleId}\\{taleVersionId}:{GetType().Name}";

    protected ContainerGrain(ILogger logger, IDocumentDbContext documentDbContext) : base(logger)
    {
        _documentDbContext = documentDbContext;
    }

    public async Task InitializePublish(Guid taleId, Guid taleVersionId)
    {
        var ctx = Validate(Id(taleId, taleVersionId), nameof(InitializePublish)).TaleId(taleId).TaleVersionId(taleVersionId);

        try
        {
            await InitializePublishExtension(taleId, taleVersionId, _documentDbContext, GrainToken);

            ctx.Debug($"initialized collection entries");
        }
        catch (Exception ex)
        {
            ctx.Error(ex, $"Container grain could not initialize publish");
            throw new GrainOperationException(ctx.Id, ctx.MethodName, ex.Message); // TODO: do not hide hierarchy
        }
    }

    public async Task Purge(Guid taleId, Guid taleVersionId)
    {
        var ctx = Validate(Id(taleId, taleVersionId), nameof(Purge)).TaleId(taleId).TaleVersionId(taleVersionId);

        try
        {
            using var taskDbContext = _scope.ServiceProvider.GetRequiredService<Data.BaseTypes.ITaskDbContext>()
                ?? throw new GrainOperationException(ctx.Id, ctx.MethodName, $"{typeof(Data.BaseTypes.ITaskDbContext).Name} initialization failed");
            await taskDbContext.PurgeEntitiesAsync(taleId, taleVersionId, GrainToken);

            ctx.Debug($"purged data");
        }
        catch (Exception ex)
        {
            ctx.Error(ex, $"Container grain could not purge");
            throw new GrainOperationException(ctx.Id, ctx.MethodName, ex.Message); // TODO: do not hide hierarchy
        }
    }

    public async Task BackupTo(Guid taleId, Guid taleVersionId, Guid newVersionId)
    {
        var ctx = Validate(Id(taleId, taleVersionId), nameof(BackupTo)).TaleId(taleId).TaleVersionId(taleVersionId).TaleVersionId(newVersionId);

        try
        {
            using var taskDbContext = _scope.ServiceProvider.GetRequiredService<Data.BaseTypes.ITaskDbContext>()
                ?? throw new GrainOperationException(ctx.Id, ctx.MethodName, $"{typeof(Data.BaseTypes.ITaskDbContext).Name} initialization failed");
            await taskDbContext.BackupToAsync(taleId, taleVersionId, newVersionId, GrainToken);

            ctx.Debug($"backuped data");
        }
        catch (Exception ex)
        {
            ctx.Error(ex, $"Container grain could not backup");
            throw new GrainOperationException(ctx.Id, ctx.MethodName, ex.Message); // TODO: do not hide hierarchy
        }
    }

    protected virtual Task InitializePublishExtension(Guid taleId, Guid taleVersionId, IDocumentDbContext dbContext, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}
