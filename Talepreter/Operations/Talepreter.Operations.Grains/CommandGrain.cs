using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Exceptions;
using Talepreter.Contracts.Orleans;
using System.Diagnostics;
using Talepreter.Data.DocumentDbContext;

namespace Talepreter.Operations.Grains;

[GenerateSerializer]
public abstract class CommandGrain : GrainBase, ICommandGrain
{
    protected readonly IDocumentDbContext _documentDbContext;
    protected string Id => GrainReference.GrainId.ToString();

    protected CommandGrain(ILogger logger, IDocumentDbContext documentDbContext) : base(logger)
    {
        _documentDbContext = documentDbContext;
    }

    public async Task<ExecuteCommandResponse> ExecuteCommand(ExecuteCommandContext commandInfo)
    {
        ArgumentNullException.ThrowIfNull(nameof(commandInfo));
        var ctx = Validate(Id, nameof(ExecuteCommand)).TaleId(commandInfo.TaleId).TaleVersionId(commandInfo.TaleVersionId)
            .IsNull(commandInfo.Command, nameof(commandInfo.Command)).Chapter(commandInfo.Chapter).Page(commandInfo.PageInChapter);

        Stopwatch sw = Stopwatch.StartNew();

        try
        {
            // actual execute, may take a little long
            await ExecuteCommandAsync(commandInfo, GrainToken);
            GrainToken.ThrowIfCancellationRequested();
            return new ExecuteCommandResponse { Status = ExecuteResult.Success };
        }
        catch (OperationCanceledException)
        {
            ctx.Error($"Command execution timed out for {commandInfo.Command}");
            return new ExecuteCommandResponse { Status = ExecuteResult.Timedout, Command = commandInfo.Command };
        }
        catch (CommandExecutionException ex)
        {
            ctx.Error($"Command execution failed for {commandInfo.Command}: {ex.Message}");
            return new ExecuteCommandResponse
            {
                Status = ExecuteResult.Faulted,
                Command = commandInfo.Command,
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Stacktrace = ex.StackTrace,
                    Type = ex.GetType().Name
                }
            };
        }
        catch (CommandValidationException ex)
        {
            ctx.Error($"Command validation failed for {commandInfo.Command}: {ex.Message}");
            return new ExecuteCommandResponse
            {
                Status = ExecuteResult.Blocked,
                Command = commandInfo.Command,
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Stacktrace = ex.StackTrace,
                    Type = ex.GetType().Name
                }
            };
        }
        catch (Exception ex)
        {
            ctx.Error(ex, $"Command execution got unexpected error for {commandInfo.Command}: {ex.Message}");
            return new ExecuteCommandResponse
            {
                Status = ExecuteResult.Faulted,
                Command = commandInfo.Command,
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Stacktrace = ex.StackTrace,
                    Type = ex.GetType().Name
                }
            };
        }
        finally
        {
            ctx.Debug($"Command {commandInfo.Command.Tag} executed in {sw.ElapsedMilliseconds} ms");
        }
    }

    // --

    protected abstract Task ExecuteCommandAsync(ExecuteCommandContext commandInfo, CancellationToken token);
}