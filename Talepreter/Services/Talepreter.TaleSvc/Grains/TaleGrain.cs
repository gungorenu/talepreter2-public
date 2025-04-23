using Orleans.Runtime;
using Talepreter.Common.RabbitMQ;
using Talepreter.Common.RabbitMQ.Interfaces;
using Talepreter.Contracts.Messaging;
using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Contracts.Orleans.Process;
using Talepreter.Exceptions;
using Talepreter.Operations.Grains;
using Talepreter.TaleSvc.Grains.GrainStates;

namespace Talepreter.TaleSvc.Grains;

[GenerateSerializer]
public class TaleGrain : GrainWithStateBase<TaleGrainState>, ITaleGrain
{
    private readonly IPublisher _publisher;

    public TaleGrain([PersistentState("persistentState", "TaleSvcStorage")] IPersistentState<TaleGrainState> persistentState,
        ILogger<TaleGrain> logger,
        IPublisher publisher)
        : base(persistentState, logger)
    {
        _publisher = publisher;
    }

    private Guid TaleId => Guid.Parse(GrainReference.GrainId.Key.ToString()![5..]);


    public Task<Guid[]> GetVersions()
    {
        return Task.FromResult(State.VersionTracker.ToArray());
    }

    public async Task Initialize(Guid taleVersionId, Guid? backupOfVersionId = null)
    {
        var ctx = Validate(TaleId, nameof(Initialize)).TaleId(TaleId).TaleVersionId(taleVersionId)
            .Custom(backupOfVersionId.HasValue && backupOfVersionId == Guid.Empty, $"Null/Empty argument {nameof(backupOfVersionId)}")
            .Custom(State.VersionTracker.Contains(taleVersionId), "Tale publish id already exists");

        if (backupOfVersionId != null)
        {
            var backupGrain = GrainFactory.FetchPublish(TaleId, backupOfVersionId.Value);
            var stateOfBackup = await backupGrain.GetStatus();
            if (stateOfBackup != Contracts.Orleans.System.ControllerGrainStatus.Executed)
                throw new GrainOperationException(ctx.Id, ctx.MethodName, $"Backup publish is faulty, using that version as backup source is not accepted");

            await backupGrain.BackupTo(taleVersionId);

            await SaveStateAsync((state) =>
            {
                state.VersionTracker.Add(taleVersionId);
                state.LastUpdate = DateTime.UtcNow;
                return Task.CompletedTask;
            });

            ctx.Information($"initialized {taleVersionId} as backup of {backupOfVersionId}");
        }
        else
        {
            await SaveStateAsync(async (state) =>
            {
                var grain = GrainFactory.FetchPublish(TaleId, taleVersionId);
                await grain.Initialize(TaleId, taleVersionId);
                state.VersionTracker.Add(taleVersionId);
                state.LastUpdate = DateTime.UtcNow;
            });
            ctx.Information($"initialized {taleVersionId}");
        }
    }

    public async Task<bool> AddChapterPage(Guid taleVersionId, int chapter, int page)
    {
        var ctx = Validate(TaleId, nameof(AddChapterPage)).TaleId(TaleId).TaleVersionId(taleVersionId).Chapter(chapter).Page(page)
            .Custom(!State.VersionTracker.Contains(taleVersionId), $"Tale publish does not exist");

        var grain = GrainFactory.FetchPublish(TaleId, taleVersionId);
        var result = await grain.AddChapterPage(chapter, page);
        ctx.Information($"added new page {chapter}#{page} to tale publish {taleVersionId}");
        return result;
    }

    public async Task BeginProcess(Guid taleVersionId, int chapter, int page, ProcessCommand[] pageCommands)
    {
        var ctx = Validate(TaleId, nameof(BeginProcess)).TaleId(TaleId).TaleVersionId(taleVersionId).Chapter(chapter).Page(page)
            .Custom(!State.VersionTracker.Contains(taleVersionId), $"Tale publish does not exist").IsNull(pageCommands, nameof(pageCommands));

        var grain = GrainFactory.FetchPublish(TaleId, taleVersionId);
        await grain.BeginProcess(chapter, page, pageCommands);
        ctx.Information($"initiated processing for tale publish {taleVersionId}");
    }

    public async Task BeginExecute(Guid taleVersionId)
    {
        var ctx = Validate(TaleId, nameof(BeginExecute)).TaleId(TaleId).TaleVersionId(taleVersionId).Custom(!State.VersionTracker.Contains(taleVersionId), $"Tale publish does not exist");

        var grain = GrainFactory.FetchPublish(TaleId, taleVersionId);
        await grain.BeginExecute();
        ctx.Information($"initiated execution for tale version {taleVersionId}");
    }

    public async Task PurgePublish(Guid taleVersionId)
    {
        var ctx = Validate(TaleId, nameof(PurgePublish)).TaleId(TaleId).TaleVersionId(taleVersionId).Custom(!State.VersionTracker.Contains(taleVersionId), $"Tale publish does not exist");

        await SaveStateAsync(async (state) =>
        {
            var grain = GrainFactory.FetchPublish(TaleId, taleVersionId);
            await grain.Purge();
            state.VersionTracker.Remove(taleVersionId);
            state.LastUpdate = DateTime.UtcNow;
        });
        ctx.Information($"purged tale version {taleVersionId}");
    }

    public async Task Purge()
    {
        var ctx = Validate(TaleId, nameof(Purge)).TaleId(TaleId);

        await SaveStateAsync(async (state) =>
        {
            async Task purge(Guid taleVersionId)
            {
                var grain = GrainFactory.FetchPublish(TaleId, taleVersionId);
                await grain.Purge();
            }

            ParallelRunner runner = new();
            await runner.RunInParallel(state.VersionTracker.Select(x => purge(x)));
            if (runner.FailCount > 0) throw new GrainOperationException($"Purging tale failed: {runner.Errors.FirstOrDefault()!.Message}");

            state.VersionTracker.Clear();
            state.LastUpdate = DateTime.UtcNow;
        });
        ctx.Information($"purged all versions of tale");
    }

    public async Task Stop(Guid taleVersionId)
    {
        var ctx = Validate(TaleId, nameof(Stop)).TaleId(TaleId).TaleVersionId(taleVersionId).Custom(!State.VersionTracker.Contains(taleVersionId), $"Tale publish does not exist");

        await SaveStateAsync(async (state) =>
        {
            var grain = GrainFactory.FetchPublish(TaleId, taleVersionId);
            await grain.Stop();
            state.LastUpdate = DateTime.UtcNow;
        });
        ctx.Information($"stopped tale version {taleVersionId}");
    }

    // --

    public async Task OnProcessComplete(Guid taleVersionId, int callerChapter, int callerPage, ProcessResult result)
    {
        var ctx = Validate(TaleId, nameof(OnProcessComplete)).TaleId(TaleId).TaleVersionId(taleVersionId).Chapter(callerChapter).Page(callerPage);
        if (!State.VersionTracker.Contains(taleVersionId))
        {
            ctx.Debug($"tale version {taleVersionId} with {callerChapter}#{callerPage} completed processing with {result} but tale version is not known so doing nothing");
            return;
        }

        ProcessStatus processResult;
        if (result.HasFlag(ProcessResult.Faulted) ||
            result.HasFlag(ProcessResult.Cancelled) ||
            result.HasFlag(ProcessResult.Blocked)) processResult = ProcessStatus.Faulted;
        else if (result.HasFlag(ProcessResult.Timedout)) processResult = ProcessStatus.Timeout;
        else if (result == ProcessResult.Success) processResult = ProcessStatus.Success;
        else throw new GrainOperationException(ctx.Id, ctx.MethodName, $"Operation result {result} is not recognized");

        // publish message, dont call grains due to deadlock potential
        await _publisher.PublishAsync(new ProcessOperationResponse
        {
            TaleId = TaleId,
            Status = processResult,
            TaleVersionId = taleVersionId,
            Chapter = callerChapter,
            Page = callerPage,
            OperationTime = DateTime.UtcNow
        }, TalepreterTopology.Exchanges.EventExchange, TalepreterTopology.RoutingKeys.StatusUpdateRoutingKey(TaleId), GrainToken);

        ctx.Information($"tale version {taleVersionId} with {callerChapter}#{callerPage} completed processing with {result}");
    }

    public async Task OnExecuteComplete(Guid taleVersionId, int callerChapter, int callerPage, ExecuteResult result)
    {
        var ctx = Validate(TaleId, nameof(OnExecuteComplete)).TaleId(TaleId).TaleVersionId(taleVersionId).Chapter(callerChapter).Page(callerPage);
        if (!State.VersionTracker.Contains(taleVersionId))
        {
            ctx.Debug($"tale version {taleVersionId} with {callerChapter}#{callerPage} completed executing with {result} but tale version is not known so doing nothing");
            return;
        }

        ExecuteStatus executeResult;
        if (result.HasFlag(ExecuteResult.Faulted) ||
            result.HasFlag(ExecuteResult.Cancelled) ||
            result.HasFlag(ExecuteResult.Blocked)) executeResult = ExecuteStatus.Faulted;
        else if (result.HasFlag(ExecuteResult.Timedout)) executeResult = ExecuteStatus.Timeout;
        else if (result == ExecuteResult.Success) executeResult = ExecuteStatus.Success;
        else throw new GrainOperationException(ctx.Id, ctx.MethodName, $"Operation result {result} is not recognized");

        // publish message, dont call grains due to deadlock potential
        await _publisher.PublishAsync(new ExecuteOperationResponse
        {
            TaleId = TaleId,
            Status = executeResult,
            TaleVersionId = taleVersionId,
            Chapter = callerChapter,
            Page = callerPage,
            OperationTime = DateTime.UtcNow
        }, TalepreterTopology.Exchanges.EventExchange, TalepreterTopology.RoutingKeys.StatusUpdateRoutingKey(TaleId), GrainToken);

        ctx.Information($"tale version {taleVersionId} with {callerChapter}#{callerPage} completed executing with {result}");
    }
}
