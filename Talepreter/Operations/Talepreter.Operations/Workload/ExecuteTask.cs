using Microsoft.Extensions.Logging;
using Talepreter.Common.RabbitMQ.Interfaces;
using Talepreter.Common;
using Talepreter.Extensions;
using System.Diagnostics;
using Talepreter.Exceptions;
using Talepreter.Model.Command;
using DataCommand = Talepreter.Data.BaseTypes.Command;
using ExecuteCommand = Talepreter.Contracts.Orleans.RawCommand;
using ExecuteTrigger = Talepreter.Contracts.Orleans.RawTrigger;
using NP = Talepreter.Contracts.Orleans.NamedParameter;
using NPT = Talepreter.Contracts.Orleans.NamedParameterType;
using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Data.BaseTypes;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Common.RabbitMQ;
using Microsoft.EntityFrameworkCore;

namespace Talepreter.Operations.Workload;

public class ExecuteTask : WorkTask<ExecuteTaskArgument>
{
    public override WorkTaskType Type => WorkTaskType.Execute;

    private readonly string _serviceName;
    private readonly IPublisher _publisher;
    private readonly IGrainFactory _grainFactory;
    private readonly Contracts.Messaging.ResponsibleService _serviceId;
    private readonly ICommandValidator _validatorFactory;
    private readonly IExecuteTaskGrainFetcher _customGrainFetcher;
    private ResultTaskManager<ExecuteCommandResponse> _taskMgr;
    private ITaskDbContext _taskDbContext;
    private int _taskCount = 0;

    public ExecuteTask(ILogger<ProcessTask> logger,
        IPublisher publisher,
        IGrainFactory grainFactory,
        ITalepreterServiceIdentifier serviceId,
        ICommandValidator validatorFactory,
        ITaskDbContext taskDbContext,
        IExecuteTaskGrainFetcher customGrainFetcher) : base(logger)
    {
        _serviceName = serviceId.ServiceId.ToString();
        _publisher = publisher;
        _grainFactory = grainFactory;
        _serviceId = serviceId.ServiceId.Map();
        _validatorFactory = validatorFactory;
        _taskDbContext = taskDbContext;
        _customGrainFetcher = customGrainFetcher;
        _taskMgr = new ResultTaskManager<ExecuteCommandResponse>();
    }

    protected async override Task WorkAsync()
    {
        try
        {
            var timer = new Stopwatch();
            timer.Start();

            // phases are groups of commands that are executed together. main intention of phases is to solve issues like dependencies and parent-child commands
            // commands that rely on each other must be in different phases. 
            // phase 0 and -1 are special. phase 0 is first ever phase and -1 is last ever phase. commands can only be sent to phase -1 during execution
            var results = await ExecutePhaseAsync(0, Token);
            var maxPhase = await _taskDbContext.ExecuteAwaitingCommandsMaxPhase(_arg.TaleId, _arg.TaleVersionId, _arg.Chapter, _arg.Page, Token);
            if (results == ExecuteResult.Success)
            {
                for (int phase = 1; phase <= maxPhase; phase++)
                {
                    results = await ExecutePhaseAsync(phase, Token);
                    if (results!= ExecuteResult.Success) break;
                }
            }
            // these are last and dynamic created commands, they are not supposed to be visible at beginning
            if (results == ExecuteResult.Success) results = await ExecutePhaseAsync(-1, Token);

            ExecuteResult result = results;
            await ReportBackResultAsync(result);
            timer.Stop();

            await _taskDbContext.SaveChangesAsync(Token);

            _logger.LogInformation($"{_arg.GrainLogId} Command executing finalized for page {_arg.Chapter}#{_arg.Page} with result {result}, with {_taskMgr.SuccessfullTaskCount}/{_taskMgr.FaultedTaskCount}/{_taskMgr.TimedoutTaskCount} completed/faulted/timedout commands, took {timer.ElapsedMilliseconds} ms");
        }
        catch (OperationCanceledException)
        {
            using var excToken = new CancellationTokenSource(30 * 1000);
            try
            {
                _logger.LogError($"{_arg.GrainLogId} Command executing got time out for page {_arg.Chapter}#{_arg.Page}");
                var allErrors = _taskMgr.Errors;
                await PublishErrorsAsync(null, allErrors, excToken.Token);
                await ReportBackResultAsync(ExecuteResult.Timedout);
            }
            catch (Exception cex)
            {
                _logger.LogCritical(cex, $"{_arg.GrainLogId} Command execution recovery failed for page {_arg.Chapter}#{_arg.Page}: {cex.Message}");
            }
        }
        catch (Exception ex)
        {
            Cancel();
            await Task.Delay(100);
            try
            {
                _logger.LogError(ex, $"{_arg.GrainLogId} Command executing got an error for page {_arg.Chapter}#{_arg.Page}");
                var allErrors = _taskMgr.Errors;

                using var excToken = new CancellationTokenSource(30 * 1000);

                var response = new Contracts.Messaging.ExecuteCommandResponse
                {
                    TaleId = _arg.TaleId,
                    OperationTime = DateTime.UtcNow,
                    TaleVersionId = _arg.TaleVersionId,
                    Chapter = _arg.Chapter,
                    Page = _arg.Page,
                    Status = Contracts.Messaging.ExecuteStatus.Faulted,
                    Error = new Contracts.Messaging.ErrorInfo
                    {
                        Message = ex.Message,
                        Stacktrace = ex.StackTrace,
                        Type = ex.GetType().Name,
                    },
                    Service = _serviceId,
                    CommandData = "Error during executing"
                };

                await _publisher.PublishAsync(response, TalepreterTopology.Exchanges.EventExchange, TalepreterTopology.RoutingKeys.ExecuteCommandResultRoutingKey(_arg.TaleId), excToken.Token);

                await PublishErrorsAsync(null, allErrors, excToken.Token);
                await ReportBackResultAsync(ExecuteResult.Faulted);
            }
            catch (OperationCanceledException) { }
            catch (Exception cex)
            {
                _logger.LogCritical(cex, $"{_arg.GrainLogId} Command execution recovery failed for page {_arg.Chapter}#{_arg.Page}: {cex.Message}");
            }
        }
    }

    private async Task<ExecuteResult> ExecutePhaseAsync(int phase, CancellationToken? token)
    {
        var commands = await _taskDbContext.ExecuteAwaitingCommands(_arg.TaleId, _arg.TaleVersionId, _arg.Chapter, _arg.Page, phase).ToArrayAsync(Token);
        if (commands == null || commands.Length == 0) return ExecuteResult.Success;

        _taskCount += commands.Length;
        foreach (var command in commands) _taskMgr.AppendTasks((token) => ExecuteAsync(command));

        var results = _taskMgr.Start(Token);
        var allErrors = _taskMgr.Errors;
        await PublishErrorsAsync(results, allErrors, token ?? Token);
        Token.ThrowIfCancellationRequested();

        ExecuteResult res;
        if (_taskMgr.FaultedTaskCount > 0) res = ExecuteResult.Faulted;
        else if (_taskMgr.TimedoutTaskCount > 0) res = ExecuteResult.Timedout;
        else if (_taskMgr.SuccessfullTaskCount == _taskCount)
        {
            if (results == null) res = ExecuteResult.Faulted;
            else if (!results.All(x => x.Status == ExecuteResult.Success)) res = results.First(x => x.Status != ExecuteResult.Success).Status;
            else res = ExecuteResult.Success;
        }
        else res = ExecuteResult.Blocked; // edge case, it should not happen

        return res;
    }

    private async Task ReportBackResultAsync(ExecuteResult result)
    {
        var pageGrain = _grainFactory.FetchPage(_arg.TaleId, _arg.TaleVersionId, _arg.Chapter, _arg.Page);
        await pageGrain.OnExecuteComplete(_serviceName, result);
    }

    private async Task PublishErrorsAsync(ExecuteCommandResponse[]? results, Exception[]? errors, CancellationToken? token)
    {
        if (results != null)
        {
            foreach (var res in results.Where(x => x.Status != ExecuteResult.Success))
            {
                var response = new Contracts.Messaging.ExecuteCommandResponse
                {
                    TaleId = _arg.TaleId,
                    OperationTime = DateTime.UtcNow,
                    TaleVersionId = _arg.TaleVersionId,
                    Chapter = _arg.Chapter,
                    Page = _arg.Page,
                    Status = res.Status switch
                    {
                        ExecuteResult.Cancelled => Contracts.Messaging.ExecuteStatus.Timeout,
                        ExecuteResult.Timedout => Contracts.Messaging.ExecuteStatus.Timeout,
                        _ => Contracts.Messaging.ExecuteStatus.Faulted
                    },
                    Error = new Contracts.Messaging.ErrorInfo
                    {
                        Message = res.Error?.Message ?? "No error message available",
                        Stacktrace = res.Error?.Stacktrace,
                        Type = res.Error?.Type ?? "No error message available",
                    },
                    Service = _serviceId,
                    CommandData = res.Command?.ToString() ?? "No command info available"
                };

                await _publisher.PublishAsync(response, TalepreterTopology.Exchanges.EventExchange, TalepreterTopology.RoutingKeys.ExecuteCommandResultRoutingKey(_arg.TaleId), token ?? Token);
            }
        }

        if (errors != null)
        {
            foreach (var err in errors.OfType<CommandException>())
            {
                var response = new Contracts.Messaging.ExecuteCommandResponse
                {
                    TaleId = _arg.TaleId,
                    OperationTime = DateTime.UtcNow,
                    TaleVersionId = _arg.TaleVersionId,
                    Chapter = _arg.Chapter,
                    Page = _arg.Page,
                    Status = Contracts.Messaging.ExecuteStatus.Faulted,
                    Error = new Contracts.Messaging.ErrorInfo
                    {
                        Message = err.Message,
                        Stacktrace = err.StackTrace,
                        Type = err.GetType().Name,
                    },
                    Service = _serviceId,
                    CommandData = err.CommandData ?? "No command info available"
                };
            }
        }
    }

    private async Task<ExecuteCommandResponse> ExecuteAsync(DataCommand command)
    {
        ArgumentNullException.ThrowIfNull(command, nameof(command));
        Stopwatch sw = new();
        sw.Start();

        Token.ThrowIfCancellationRequested();
        try
        {
            _validatorFactory.ValidatePreExecute(command.RawData); // may throw validation exception

            // depends on type, trigger and commands are executed on same entity grain
            ExecuteCommandResponse? result = default;
            if (command.Tag == CommandIds.Trigger)
            {
                var grainType = command.RawData.NamedParameters?.FirstOrDefault(x => x.Name == CommandIds.TriggerCommand.Grain)?.Value ??
                    throw new CommandExecutionException(command.RawData.ToString()!, "Trigger has no grain type set");
                var grain = _customGrainFetcher.FetchTriggerGrain(_arg, _grainFactory, command.Target, grainType);
                result = await grain.ExecuteTrigger(new ExecuteTriggerContext
                {
                    TaleId = _arg.TaleId,
                    TaleVersionId = _arg.TaleVersionId,
                    Chapter = command.ChapterId,
                    PageInChapter = command.PageId,
                    Trigger = MapToTrigger(command)
                });
            }
            else
            {
                var grain = _customGrainFetcher.FetchCommandGrain(_arg, _grainFactory, command.Tag, command.Target);
                result = await grain.ExecuteCommand(new ExecuteCommandContext
                {
                    TaleId = _arg.TaleId,
                    TaleVersionId = _arg.TaleVersionId,
                    Chapter = command.ChapterId,
                    PageInChapter = command.PageId,
                    Command = MapToCommand(command)
                });
            }

            Token.ThrowIfCancellationRequested();
            if (result == null)
            {
                command.Result = CommandExecuteResult.Error;
                command.Error = "Operation result is empty";
            }
            else
            {
                command.Result = result.Status switch
                {
                    ExecuteResult.Success => CommandExecuteResult.Success,
                    ExecuteResult.None => CommandExecuteResult.None,
                    _ => CommandExecuteResult.Error
                };
                command.Error = result.Error != null ? $"[{result.Error.Type}]: {result.Error.Message}\r\n{result.Error.Stacktrace}" : default!;
            }
            command.OperationTime = DateTime.UtcNow;
            return result ?? new ExecuteCommandResponse { Status = ExecuteResult.None };
        }
        catch (Exception ex)
        {
            command.Result = CommandExecuteResult.Error;
            command.Error = $"[{ex.GetType().Name}]: {ex.Message}";
            command.OperationTime = DateTime.UtcNow;
            throw new CommandExecutionException(command.RawData.ToString()!, ex.Message, ex);
        }
        finally
        {
            sw.Stop();
            command.Duration = sw.ElapsedMilliseconds;
        }
    }

    private ExecuteTrigger MapToTrigger(DataCommand cmd)
    {
        var id = cmd.RawData.NamedParameters!.First(x => x.Name == CommandIds.TriggerCommand.Id).Value;
        var type = cmd.RawData.NamedParameters!.First(x => x.Name == CommandIds.TriggerCommand.Type).Value;
        var parameter = cmd.RawData.NamedParameters!.First(x => x.Name == CommandIds.TriggerCommand.Parameter).Value;
        var grain = cmd.RawData.NamedParameters!.First(x => x.Name == CommandIds.TriggerCommand.Grain).Value;
        var triggerAt = cmd.RawData.NamedParameters!.First(x => x.Name == CommandIds.TriggerCommand.At).Value.ToLong();

        return new ExecuteTrigger
        {
            Id = id,
            Parameter = parameter,
            Type = type,
            Target = cmd.Target,
            Grain = grain,
            TriggerAt = triggerAt
        };
    }

    private ExecuteCommand MapToCommand(DataCommand cmd)
    {
        return new ExecuteCommand
        {
            Tag = cmd.Tag,
            Target = cmd.Target,
            Comment = cmd.RawData.Comment,
            Parent = cmd.RawData.Parent,
            ArrayParameters = cmd.RawData.ArrayParameters,
            NamedParameters = cmd.RawData.NamedParameters?.Select(x =>
            new NP
            {
                Name = x.Name,
                Value = x.Value,
                Type = x.Type switch
                {
                    NamedParameterType.Set => NPT.Set,
                    NamedParameterType.Reset => NPT.Reset,
                    NamedParameterType.Add => NPT.Add,
                    NamedParameterType.Remove => NPT.Remove,
                    _ => throw MissingMapperException.Fault<NamedParameterType, NPT>(x.Type)
                }
            }).ToArray() ?? []
        };
    }

    // --

    protected override void DisposeCustom()
    {
        _taskMgr?.Dispose();
        _taskMgr = null!;
        _taskDbContext?.Dispose();
        _taskDbContext = null!;
    }

    public override string ToString()
    {
        return $"{_arg.TaleId}\\{_arg.TaleVersionId}.{_arg.Chapter}#{_arg.Page}:{_serviceName}: Execute task {Id}";
    }
}