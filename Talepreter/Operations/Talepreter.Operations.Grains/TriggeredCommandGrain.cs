using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Talepreter.Contracts.Orleans;
using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Data.BaseTypes;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Exceptions;

namespace Talepreter.Operations.Grains;

[GenerateSerializer]
public abstract class TriggeredCommandGrain : CommandGrain, ITriggeredCommandGrain
{
    protected TriggeredCommandGrain(ILogger logger, IDocumentDbContext documentDbContext) : base(logger, documentDbContext) { }

    public async Task<ExecuteCommandResponse> ExecuteTrigger(ExecuteTriggerContext triggerInfo)
    {
        ArgumentNullException.ThrowIfNull(nameof(triggerInfo));
        var ctx = Validate(Id, nameof(ExecuteTrigger)).TaleId(triggerInfo.TaleId).TaleVersionId(triggerInfo.TaleVersionId)
            .IsNull(triggerInfo.Trigger, nameof(triggerInfo.Trigger)).Chapter(triggerInfo.Chapter).Page(triggerInfo.PageInChapter);

        Stopwatch sw = Stopwatch.StartNew();

        using var taskDbContext = _scope.ServiceProvider.GetRequiredService<ITaskDbContext>() ?? throw new GrainOperationException($"{typeof(ITaskDbContext).Name} initialization failed");

        try
        {
            // actual execute, may take a little long
            try
            {
                var result = await ExecuteTriggerAsync(triggerInfo, taskDbContext, GrainToken);
                var triggerResult = result switch
                {
                    // there is a special case for teleportation memory check. that trigger has to shift itself continuously so actually Set is also a success for us
                    TriggerState.Set or TriggerState.Triggered => ExecuteResult.Success,
                    _ => ExecuteResult.Faulted
                };

                GrainToken.ThrowIfCancellationRequested();

                if (result != TriggerState.Set) await taskDbContext.UpdateTriggerAsync(triggerInfo.TaleId, triggerInfo.TaleVersionId, triggerInfo.Trigger.Id!, result, GrainToken);
                return new ExecuteCommandResponse { Status = triggerResult };
            }
            catch
            {
                await taskDbContext.UpdateTriggerAsync(triggerInfo.TaleId, triggerInfo.TaleVersionId, triggerInfo.Trigger.Id!, TriggerState.Faulted, GrainToken);
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            ctx.Error($"Command execution timed out for {triggerInfo.Trigger}");
            return new ExecuteCommandResponse { Status = ExecuteResult.Timedout, Command = triggerInfo.Trigger.ToCommand() };
        }
        catch (CommandExecutionException ex)
        {
            ctx.Error($"Command execution failed for {triggerInfo.Trigger}: {ex.Message}");
            return new ExecuteCommandResponse
            {
                Status = ExecuteResult.Faulted,
                Command = triggerInfo.Trigger.ToCommand(),
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
            ctx.Error($"Command validation failed for {triggerInfo.Trigger}: {ex.Message}");
            return new ExecuteCommandResponse
            {
                Status = ExecuteResult.Blocked,
                Command = triggerInfo.Trigger.ToCommand(),
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
            ctx.Error(ex, $"Command execution got unexpected error for {triggerInfo.Trigger}: {ex.Message}");
            return new ExecuteCommandResponse
            {
                Status = ExecuteResult.Faulted,
                Command = triggerInfo.Trigger.ToCommand(),
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
            ctx.Debug($"Trigger {triggerInfo.Trigger.Id} executed in {sw.ElapsedMilliseconds} ms");
        }
    }

    // --

    protected abstract Task<TriggerState> ExecuteTriggerAsync(ExecuteTriggerContext triggerInfo, ITaskDbContext taskDbContext, CancellationToken token);
}
