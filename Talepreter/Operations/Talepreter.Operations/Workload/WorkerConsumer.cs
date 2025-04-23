using Talepreter.Contracts.Messaging;
using Talepreter.Common.RabbitMQ.Interfaces;
using CMD = Talepreter.Data.BaseTypes.Command;
using CMDRAW = Talepreter.Data.BaseTypes.CommandData;
using NP = Talepreter.Data.BaseTypes.NamedParameter;
using NPT = Talepreter.Data.BaseTypes.NamedParameterType;
using Talepreter.Exceptions;
using Microsoft.Extensions.Logging;

namespace Talepreter.Operations.Workload;

public class WorkerConsumer :
    IConsumer<ProcessPageRequest>,
    IConsumer<ExecutePageRequest>,
    IConsumer<CancelPageOperationRequest>
{
    private readonly IWorkTaskManager _workManager;

    public WorkerConsumer(IWorkTaskManager workManager)
    {
        _workManager = workManager;
    }

    public async Task Consume(ProcessPageRequest message, IReadContext context, CancellationToken token)
    {
        var arg = MapArgument(message);

        if (_workManager.DoesExist<ProcessTask, ProcessTaskArgument>(t =>
            t.Type == WorkTaskType.Process &&
            t.Argument.TaleId == message.TaleId &&
            t.Argument.TaleVersionId == message.TaleVersionId &&
            t.Argument.Chapter == message.Chapter &&
            t.Argument.Page == message.Page))
        {
            context.Logger.LogWarning($"Duplicate request for processing: {message.TaleId}\\{message.TaleVersionId}.{message.Chapter}#{message.Page}");
            await context.Reject(false, token);
            return;
        }

        context.Logger.LogDebug($"Start for processing: {message.TaleId}\\{message.TaleVersionId}.{message.Chapter}#{message.Page}");
        _workManager.StartTask<ProcessTask,ProcessTaskArgument>(arg);
        await context.Success(token);
    }

    public async Task Consume(ExecutePageRequest message, IReadContext context, CancellationToken token)
    {
        var arg = MapArgument(message);

        if (_workManager.DoesExist<ExecuteTask, ExecuteTaskArgument>(t =>
            t.Type == WorkTaskType.Execute &&
            t.Argument.TaleId == message.TaleId &&
            t.Argument.TaleVersionId == message.TaleVersionId &&
            t.Argument.Chapter == message.Chapter &&
            t.Argument.Page == message.Page))
        {
            context.Logger.LogWarning($"Duplicate request for executing: {message.TaleId}\\{message.TaleVersionId}.{message.Chapter}#{message.Page}");
            await context.Reject(false, token);
            return;
        }

        context.Logger.LogDebug($"Start for executing: {message.TaleId}\\{message.TaleVersionId}.{message.Chapter}#{message.Page}");
        _workManager.StartTask<ExecuteTask, ExecuteTaskArgument>(arg);
        await context.Success(token);
    }

    public async Task Consume(CancelPageOperationRequest message, IReadContext context, CancellationToken token)
    {
        _workManager.CancelTasks(worker =>
        {
            if (worker is ProcessTask t)
            {
                if (t.Argument.TaleId == message.TaleId && t.Argument.TaleVersionId == message.TaleVersionId) return true;
            }
            else if (worker is ExecuteTask e)
            {
                if (e.Argument.TaleId == message.TaleId && e.Argument.TaleVersionId == message.TaleVersionId) return true;
            }
            return false;
        });
        await context.Success(token);
    }

    // --

    private ExecuteTaskArgument MapArgument(ExecutePageRequest message)
    {
        return new ExecuteTaskArgument
        {
            TaleId = message.TaleId,
            TaleVersionId = message.TaleVersionId,
            GrainLogId = message.GrainLogId,
            Chapter = message.Chapter,
            Page = message.Page
        };
    }

    private ProcessTaskArgument MapArgument(ProcessPageRequest message)
    {
        return new ProcessTaskArgument
        {
            TaleId = message.TaleId,
            TaleVersionId = message.TaleVersionId,
            GrainLogId = message.GrainLogId,
            Chapter = message.Chapter,
            Page = message.Page,
            Commands = message.Commands.Select(x => new CMD
            {
                OperationTime = DateTime.UtcNow,
                TaleId = message.TaleId,
                TaleVersionId = message.TaleVersionId,
                ChapterId = message.Chapter,
                PageId = message.Page,
                Phase = x.Phase,
                Index = x.Index,
                Tag = x.Tag,
                Target = x.Target,
                RawData = new CMDRAW
                {
                    Tag = x.Tag,
                    Target = x.Target,
                    Parent = x.Parent,
                    Comment = x.Comment,
                    ArrayParameters = x.ArrayParameters,
                    NamedParameters = x.NamedParameters?.Select(p => new NP
                    {
                        Name = p.Name,
                        Value = p.Value,
                        Type = p.Type switch
                        {
                            NamedParameterType.Set => NPT.Set,
                            NamedParameterType.Add => NPT.Add,
                            NamedParameterType.Reset => NPT.Reset,
                            NamedParameterType.Remove => NPT.Remove,
                            _ => throw MissingMapperException.Fault<NamedParameterType, NPT>(p.Type)
                        }
                    }).ToArray() ?? []
                }
            }).ToArray()
        };
    }
}
