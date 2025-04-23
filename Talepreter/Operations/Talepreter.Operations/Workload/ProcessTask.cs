using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Talepreter.Common;
using Talepreter.Common.RabbitMQ;
using Talepreter.Common.RabbitMQ.Interfaces;
using Talepreter.Contracts.Orleans.Process;
using Talepreter.Data.BaseTypes;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Exceptions;
using Talepreter.Extensions;
using Talepreter.Contracts.Orleans.Grains;

namespace Talepreter.Operations.Workload;

public class ProcessTask : WorkTask<ProcessTaskArgument>
{
    public override WorkTaskType Type => WorkTaskType.Process;

    private readonly string _serviceName;
    private readonly IPublisher _publisher;
    private readonly IGrainFactory _grainFactory;
    private readonly Contracts.Messaging.ResponsibleService _serviceId;
    private readonly ICommandValidator _validatorFactory;
    private readonly IDocumentDbContext _dbContext;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IBatchCommandProcessor? _batchCommandProcessor;
    private ResultTaskManager<Command[]> _taskMgr;
    private ITaskDbContext _taskDbContext;

    public ProcessTask(ILogger<ProcessTask> logger,
        IPublisher publisher,
        IGrainFactory grainFactory,
        ITalepreterServiceIdentifier serviceId,
        ICommandValidator validatorFactory,
        ITaskDbContext taskDbContext,
        IDocumentDbContext dbContext,
        ICommandProcessor commandProcessor,
        IBatchCommandProcessor? batchCommandProcessor) : base(logger)
    {
        _serviceName = serviceId.ServiceId.ToString();
        _publisher = publisher;
        _grainFactory = grainFactory;
        _serviceId = serviceId.ServiceId.Map();
        _validatorFactory = validatorFactory;
        _taskDbContext = taskDbContext;
        _dbContext = dbContext;
        _commandProcessor = commandProcessor;
        _batchCommandProcessor = batchCommandProcessor;
        _taskMgr = new ResultTaskManager<Command[]>();
    }

    protected async override Task WorkAsync()
    {
        try
        {
            var timer = new Stopwatch();
            timer.Start();
            // in (max) parallel process all commands, some of them might be filtered out
            foreach (var command in _arg.Commands) _taskMgr.AppendTasks(_ => ProcessCommandAsync(command));

            await _taskDbContext.Commands.Where(x => x.TaleId == _arg.TaleId && x.TaleVersionId == _arg.TaleVersionId && x.ChapterId == _arg.Chapter && x.PageId == _arg.Page).ExecuteDeleteAsync(Token);

            var results = _taskMgr.Start(Token);
            var allErrors = _taskMgr.Errors;
            await PublishErrorsAsync(allErrors, Token);

            ProcessResult res = ProcessResult.None;
            if (_taskMgr.FaultedTaskCount > 0) res = ProcessResult.Faulted;
            else if (_taskMgr.TimedoutTaskCount > 0) res = ProcessResult.Timedout;
            else if (_taskMgr.SuccessfullTaskCount == _arg.Commands.Length) res = ProcessResult.Success;
            else res = ProcessResult.Blocked; // edge case, it should not happen

            Token.ThrowIfCancellationRequested();

            // a final time but this time with a global approach, not every service has this, no need
            if (_batchCommandProcessor != null)
            {
                var globalRes = await _batchCommandProcessor.BatchProcess(_arg.Commands, _dbContext, _taskDbContext, Token);
                if (results == null) results = [globalRes];
                else if (globalRes != null) results = [.. results, globalRes];
            }
            await SaveCommandsAsync(results);

            await ReportBackResultAsync(res);
            Token.ThrowIfCancellationRequested();

            timer.Stop();
            _logger.LogInformation($"{_arg.GrainLogId} Command processing finalized for page {_arg.Chapter}#{_arg.Page} with result {res}, with {_taskMgr.SuccessfullTaskCount}/{_taskMgr.FaultedTaskCount}/{_taskMgr.TimedoutTaskCount} completed/faulted/timedout commands, took {timer.ElapsedMilliseconds} ms");
        }
        catch (OperationCanceledException)
        {
            using var excToken = new CancellationTokenSource(30 * 1000);

            try
            {
                var allErrors = _taskMgr.Errors;
                await PublishErrorsAsync(allErrors, excToken.Token);
                await ReportBackResultAsync(ProcessResult.Timedout);
                _logger.LogError($"{_arg.GrainLogId} Command processing got time out for page {_arg.Chapter}#{_arg.Page}");
            }
            catch (Exception cex)
            {
                _logger.LogCritical(cex, $"{_arg.GrainLogId} Command processing got timeout for page {_arg.Chapter}#{_arg.Page} but also recovery failed with error: {cex.Message}");
            }
        }
        catch (Exception ex)
        {
            Cancel();
            await Task.Delay(100);
            try
            {
                var allErrors = _taskMgr.Errors;

                using var excToken = new CancellationTokenSource(30 * 1000);

                var response = new Contracts.Messaging.ProcessCommandResponse
                {
                    TaleId = _arg.TaleId,
                    OperationTime = DateTime.UtcNow,
                    TaleVersionId = _arg.TaleVersionId,
                    Chapter = _arg.Chapter,
                    Page = _arg.Page,
                    Status = Contracts.Messaging.ProcessStatus.Faulted,
                    Error = new Contracts.Messaging.ErrorInfo
                    {
                        Message = ex.Message,
                        Stacktrace = ex.StackTrace,
                        Type = ex.GetType().Name,
                    },
                    Service = _serviceId,
                    CommandData = "Error during processing"
                };

                await PublishErrorsAsync(allErrors, excToken.Token);
                await ReportBackResultAsync(ProcessResult.Faulted);

                await _publisher.PublishAsync(response, TalepreterTopology.Exchanges.EventExchange, TalepreterTopology.RoutingKeys.ProcessCommandResultRoutingKey(_arg.TaleId), excToken.Token);

                _logger.LogError(ex, $"{_arg.GrainLogId} Command processing got an error for page {_arg.Chapter}#{_arg.Page}");
            }
            catch (OperationCanceledException) { }
            catch (Exception cex)
            {
                _logger.LogCritical(cex, $"{_arg.GrainLogId} Command processing got an error for page {_arg.Chapter}#{_arg.Page} but also recovery failed with error: {cex.Message}");
            }
        }
    }

    private async Task<Command[]> ProcessCommandAsync(Command? cmd)
    {
        if (cmd == null) return [];
        try
        {
            _validatorFactory.ValidatePreProcess(cmd.RawData); // may throw validation exception
            return await _commandProcessor.Process(cmd, _taskDbContext, Token);
        }
        catch (Exception ex)
        {
            throw new CommandValidationException(cmd.RawData.ToString()!, ex.Message, ex);
        }
    }

    private async Task ReportBackResultAsync(ProcessResult result)
    {
        var pageGrain = _grainFactory.FetchPage(_arg.TaleId, _arg.TaleVersionId, _arg.Chapter, _arg.Page);
        await pageGrain.OnProcessComplete(_serviceName, result);
    }

    private async Task SaveCommandsAsync(Command[]?[]? results)
    {
        if (results == null) return;

        List<Command> commands = [];
        //now we have a list of command list so we should store them in DB
        foreach (var processResult in results)
        {
            if (processResult == null || processResult.Length == 0) continue; // some commands can return empty list, normal, it means service is not interested in the commands
            foreach (var processCommand in processResult) commands.Add(processCommand);
        }
        if (commands.Count == 0) return;

        // shifting phases. the system goes to DB for every phase during execution. it is although unlikely, sometimes some middle phases will be empty, so phase 0,1,6,7 will exist. system should actually skip the 2345 and start from 6 after 1 OR 67 should be renamed to 23.
        // since every service is isolated, the phase changing does not have a problem
        // this block will map phases 1+ and shrink them only if possible. everything is done in memory so overhead should be minimal
        var max = commands.Max(x => x.Phase);
        var grouped = commands.GroupBy(x => x.Phase);

        var toAdd = new List<Command>();
        var phase0 = grouped.FirstOrDefault(x => x.Key == 0);
        if (phase0 != null) toAdd.AddRange(phase0);
        var phaseS = grouped.FirstOrDefault(x => x.Key == -1);
        if (phaseS != null) toAdd.AddRange(phaseS);

        int i = 1, p = 1;
        for (; i <= max; i++)
        {
            var phaseX = grouped.FirstOrDefault(x => x.Key == i)?.ToArray() ?? [];
            if (phaseX.Length > 0)
            {
                foreach (var e in phaseX) e.Phase = p;
                toAdd.AddRange(phaseX);
                p++;
            }
        }

        if (toAdd.Count != commands.Count) throw new InvalidOperationException($"Something failed during ophase shifting, numbers of commands became org {commands.Count} to shifted {toAdd.Count}");
        if (i != p) _logger.LogDebug($"{_arg.GrainLogId} Command processing shrinked phases from max {i} to {p}, this does not affect processing");

        await _taskDbContext.Commands.AddRangeAsync(toAdd, Token);
        await _taskDbContext.SaveChangesAsync(Token);
        Token.ThrowIfCancellationRequested();
    }

    private async Task PublishErrorsAsync(Exception[]? errors, CancellationToken? token)
    {
        if (errors == null || errors.Length == 0) return;

        foreach (var err in errors.OfType<CommandException>())
        {
            var response = new Contracts.Messaging.ProcessCommandResponse
            {
                TaleId = _arg.TaleId,
                OperationTime = DateTime.UtcNow,
                TaleVersionId = _arg.TaleVersionId,
                Chapter = _arg.Chapter,
                Page = _arg.Page,
                Status = Contracts.Messaging.ProcessStatus.Faulted,
                Error = new Contracts.Messaging.ErrorInfo
                {
                    Message = err.Message,
                    Stacktrace = err.StackTrace,
                    Type = err.GetType().Name,
                },
                Service = _serviceId,
                CommandData = err.CommandData ?? "No command data available"
            };

            await _publisher.PublishAsync(response, TalepreterTopology.Exchanges.EventExchange, TalepreterTopology.RoutingKeys.ProcessCommandResultRoutingKey(_arg.TaleId), token ?? Token);
        }
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
        return $"{_arg.TaleId}\\{_arg.TaleVersionId}.{_arg.Chapter}#{_arg.Page}:{_serviceName}: Process task {Id}";
    }
}