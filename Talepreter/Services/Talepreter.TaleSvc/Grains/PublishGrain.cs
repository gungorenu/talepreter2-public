using Orleans.Runtime;
using Talepreter.Common.RabbitMQ;
using Talepreter.Common.RabbitMQ.Interfaces;
using Talepreter.Contracts.Messaging;
using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Contracts.Orleans.Process;
using Talepreter.Contracts.Orleans.System;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Exceptions;
using Talepreter.Operations.Grains;
using Talepreter.TaleSvc.Grains.GrainStates;

namespace Talepreter.TaleSvc.Grains;

[GenerateSerializer]
public class PublishGrain : GrainWithStateBase<PublishGrainState>, IPublishGrain
{
    private readonly IPublisher _publisher;
    private readonly IDocumentDbContext _documentDbContext;
    private string Id(Guid taleId, Guid taleVersionId) => GrainFetcher.FetchPublish(taleId, taleVersionId);

    public PublishGrain(
        [PersistentState("persistentState", "TaleSvcStorage")] IPersistentState<PublishGrainState> persistentState,
        ILogger<PublishGrain> logger,
        IPublisher publisher,
        IDocumentDbContext documentDbContext)
        : base(persistentState, logger)
    {
        _publisher = publisher;
        _documentDbContext = documentDbContext;
    }

    public Task<ControllerGrainStatus> GetStatus() => Task.FromResult(State.Status);

    public Task<ChapterPagePair> LastExecutedPage()
    {
        _ = Validate(Id(State.TaleId, State.TaleVersionId), nameof(LastExecutedPage)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId);
        return Task.FromResult(State.LastExecutedPage.Clone());
    }

    public async Task Initialize(Guid taleId, Guid taleVersionId)
    {
        var ctx = Validate(Id(taleId, taleVersionId), nameof(Initialize)).TaleId(taleId).TaleVersionId(taleVersionId).IsHealthy(State.Status, ControllerGrainStatus.Idle);

        await SaveStateAsync(async (state) =>
        {
            async Task initializeGrain(IContainerGrain grain) { await grain.InitializePublish(taleId, taleVersionId); }
            async Task initializeCollection()
            {
                await _documentDbContext.InitializeTaleVersionAsync(taleId, taleVersionId, GrainToken);
            }

            var tasks = new List<Task>(AllContainerGrains(taleId, taleVersionId).Select(x => initializeGrain(x)))
            {
                initializeCollection()
            };
            ParallelRunner runner = new();
            var result = await runner.RunInParallel(tasks);
            if (runner.FailCount > 0) throw new GrainOperationException($"Initializing tale version {taleVersionId} failed: {runner.Errors.FirstOrDefault()!.Message}");

            state.TaleId = taleId;
            state.TaleVersionId = taleVersionId;
            state.LastUpdate = DateTime.UtcNow;
        });
        ctx.Debug($"is initialized");
    }

    public async Task<bool> AddChapterPage(int chapter, int page)
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId), nameof(AddChapterPage)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(chapter).Page(page)
            .IsHealthy(State.Status,
                ControllerGrainStatus.Idle, // creation from scratch
                ControllerGrainStatus.Executed); // a middle place, when there are multiple pages to add and in the middle of those pages

        bool chapterExists = false;
        // chapter exists already? check if healthy
        if (State.ExecuteResults.TryGetValue(chapter, out var execResult))
        {
            chapterExists = true;
            switch (execResult)
            {
                case ExecuteResult.Faulted:
                case ExecuteResult.Blocked:
                case ExecuteResult.Timedout:
                    throw new GrainOperationException(ctx.Id, ctx.MethodName, $"Publish grain already has chapter in fault execute state");
                default: break;
            }
        }
        else if (State.ProcessResults.TryGetValue(chapter, out var procResult))
        {
            chapterExists = true;
            switch (procResult)
            {
                case ProcessResult.Faulted:
                case ProcessResult.Blocked:
                case ProcessResult.Timedout:
                    throw new GrainOperationException(ctx.Id, ctx.MethodName, $"Publish grain already has chapter in fault process state");
                default: break;
            }
        }

        var chapterGrain = GrainFactory.FetchChapter(State.TaleId, State.TaleVersionId, chapter);
        if (!chapterExists) await chapterGrain.Initialize(State.TaleId, State.TaleVersionId, chapter);
        var pageAdded = await chapterGrain.AddPage(page);
        if (!pageAdded)
        {
            if (!chapterExists) ctx.Fatal($"page {chapter}#{page} already exists in healthy state but chapter did not exist");
            else ctx.Debug($"page {chapter}#{page} already exists in healthy state, so not added");
            return false;
        }

        await SaveStateAsync((state) =>
        {
            if (!chapterExists)
            {
                state.ProcessResults[chapter] = ProcessResult.None;
                state.ExecuteResults[chapter] = ExecuteResult.None;
            }
            else if (pageAdded)
            {
                state.ProcessResults[chapter] = ProcessResult.None;
                state.ExecuteResults[chapter] = ExecuteResult.None;
            }

            state.LastUpdate = DateTime.UtcNow;
            state.Status = ControllerGrainStatus.Idle; // this may overwrite existing status like Executed or Published
            return Task.CompletedTask;
        });
        ctx.Debug($"added {chapter}#{page} page");
        return true;
    }

    public async Task BeginProcess(int chapter, int page, ProcessCommand[] pageCommands)
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId), nameof(BeginProcess)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(chapter).Page(page)
            .IsHealthy(State.Status, ControllerGrainStatus.Idle).IsNull(pageCommands, nameof(pageCommands));

        await SaveStateAsync(async (state) =>
        {
            var grain = GrainFactory.FetchChapter(State.TaleId, State.TaleVersionId, chapter);
            await grain.BeginProcess(chapter, page, pageCommands);
            state.Status = ControllerGrainStatus.Processing;
            state.LastUpdate = DateTime.UtcNow;
        });
        ctx.Debug($"initiated process operation");
    }

    public async Task OnProcessComplete(int callerChapter, int callerPage, ProcessResult result)
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId), nameof(OnProcessComplete)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(callerChapter).Page(callerPage);

        if (result == ProcessResult.Success && State.Status != ControllerGrainStatus.Processing)
        {
            ctx.Debug($"page {callerChapter}#{callerPage} completed processing with {result} but state is {State.Status} so skipping");
            return; // duplicate success messages
        }
        else if (State.Status == ControllerGrainStatus.Cancelled ||
            State.Status == ControllerGrainStatus.Timedout ||
            State.Status == ControllerGrainStatus.Purged)
        {
            ctx.Debug($"page {callerChapter}#{callerPage} completed processing with {result} but tale version is at a fault state {State.Status} so skipping");
            return; // we ignore if cancel called / timed out / purged
        }

        if (!State.ProcessResults.ContainsKey(callerChapter)) throw new GrainOperationException(ctx.Id, ctx.MethodName, $"Publish grain does not recognize the chapter");
        await SaveStateAsync(async (state) =>
        {
            state.ProcessResults[callerChapter] = result;

            if (await ReportOnProcessComplete(state, ctx, ControllerGrainStatus.Cancelled, callerChapter, callerPage, ProcessResult.Cancelled))
                return; // cancelled already? we inform back
            if (await ReportOnProcessComplete(state, ctx, ControllerGrainStatus.Timedout, callerChapter, callerPage, ProcessResult.Timedout))
                return; // timed out already? we inform back
            if (await ReportOnProcessComplete(state, ctx, ControllerGrainStatus.Purged, callerChapter, callerPage, ProcessResult.Cancelled)) // same state as cancel
                return; // purged already? we inform back

            if (state.ProcessResults.Values.All(x => x != ProcessResult.None))
            {
                var result = state.ProcessResults.Values.Aggregate(ProcessResult.None, (a, b) => a | b);
                // precedence: Faulted > Cancelled > Timeout > Success
                if (result.HasFlag(ProcessResult.Faulted))
                {
                    result = ProcessResult.Faulted;
                    state.Status = ControllerGrainStatus.Faulted;
                }
                else if (result.HasFlag(ProcessResult.Cancelled))
                {
                    result = ProcessResult.Cancelled;
                    state.Status = ControllerGrainStatus.Cancelled;
                }
                else if (result.HasFlag(ProcessResult.Timedout)
                    || result.HasFlag(ProcessResult.Blocked)) // special case, blocked is internal use status, it should not come here
                {
                    result = ProcessResult.Timedout;
                    state.Status = ControllerGrainStatus.Timedout;
                }
                else if (result != ProcessResult.Success)
                {
                    ctx.Fatal($"Process result had to be success but it gave another value {result}");
                    return;
                }
                else state.Status = ControllerGrainStatus.Processed;

                var taleGrain = GrainFactory.FetchTale(state.TaleId);
                await taleGrain.OnProcessComplete(state.TaleVersionId, callerChapter, callerPage, result);
                ctx.Debug($"all chapters processed, final result for processing {result}");
            }
        });

        ctx.Debug($"page {callerChapter}#{callerPage} completed processing with {result}");
    }

    public async Task BeginExecute()
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId), nameof(BeginExecute)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).IsHealthy(State.Status, ControllerGrainStatus.Processed);

        // we have results already? this can happen when we run same version but with minor command changes only
        await SaveStateAsync(async (state) =>
        {
            state.LastUpdate = DateTime.UtcNow;
            state.Status = ControllerGrainStatus.Executing;

            for (int chapterId = 0; chapterId < state.ChapterCount(); chapterId++)
            {
                var processResult = state.ProcessResults[chapterId];
                if (processResult != ProcessResult.Success) throw new GrainOperationException(ctx.Id, ctx.MethodName, "Chapter within publish version has a failure in processing so execution cannot proceed");

                var executeResult = state.ExecuteResults[chapterId];
                if (executeResult != ExecuteResult.Success && executeResult != ExecuteResult.None) throw new GrainOperationException(ctx.Id, ctx.MethodName, "Chapter within publish version has a failure in executing so execution cannot proceed");

                if (executeResult == ExecuteResult.None)
                {
                    var chapterGrain = GrainFactory.FetchChapter(state.TaleId, state.TaleVersionId, chapterId);
                    await chapterGrain.BeginExecute();
                    ctx.Debug($"initiated execute operation");
                    // TODO: start reminder
                    return;
                }
            }

            ctx.Debug($"could not execute because all chapters are executed");
        });
    }

    public async Task OnExecuteComplete(int callerChapter, int callerPage, ExecuteResult result)
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId), nameof(OnExecuteComplete)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(callerChapter).Page(callerPage)
            .Custom(!State.ExecuteResults.ContainsKey(callerChapter), "Publish grain does not have the chapter");

        if (result == ExecuteResult.Success && State.Status != ControllerGrainStatus.Executing)
        {
            ctx.Debug($"page {callerChapter}#{callerPage} completed executing with {result} but tale version is at state {State.Status} so skipping");
            return; // duplicate success messages
        }
        else if (State.Status == ControllerGrainStatus.Cancelled ||
            State.Status == ControllerGrainStatus.Timedout ||
            State.Status == ControllerGrainStatus.Purged)
        {
            ctx.Debug($"page {callerChapter}#{callerPage} completed executing with {result} but tale version is at a fault state {State.Status} so skipping");
            return; // we ignore if cancel called / timed out / purged
        }

        await SaveStateAsync(async (state) =>
        {
            state.ExecuteResults[callerChapter] = result;

            if (await ReportOnExecuteComplete(state, ctx, ControllerGrainStatus.Cancelled, callerChapter, callerPage, ExecuteResult.Cancelled))
                return; // cancelled already? we inform back
            if (await ReportOnExecuteComplete(state, ctx, ControllerGrainStatus.Timedout, callerChapter, callerPage, ExecuteResult.Timedout))
                return; // timed out already? we inform back
            if (await ReportOnExecuteComplete(state, ctx, ControllerGrainStatus.Purged, callerChapter, callerPage, ExecuteResult.Cancelled)) // same state as cancel
                return; // purged already? we inform back

            if (state.ExecuteResults.Values.All(x => x != ExecuteResult.None))
            {
                var result = state.ExecuteResults.Values.Aggregate(ExecuteResult.None, (a, b) => a | b);
                // precedence: Faulted > Cancelled > Timeout > Success
                if (result.HasFlag(ExecuteResult.Faulted))
                {
                    result = ExecuteResult.Faulted;
                    state.Status = ControllerGrainStatus.Faulted;
                }
                else if (result.HasFlag(ExecuteResult.Cancelled))
                {
                    result = ExecuteResult.Cancelled;
                    state.Status = ControllerGrainStatus.Cancelled;
                }
                else if (result.HasFlag(ExecuteResult.Timedout)
                    || result.HasFlag(ExecuteResult.Blocked)) // special case, blocked is internal use status, it should not come here
                {
                    result = ExecuteResult.Timedout;
                    state.Status = ControllerGrainStatus.Timedout;
                }
                else if (result != ExecuteResult.Success)
                {
                    ctx.Fatal($"Execute result had to be success but it gave another value {result}");
                    return;
                }
                else
                {
                    state.Status = ControllerGrainStatus.Executed;
                    state.LastExecutedPage = new ChapterPagePair { Chapter = callerChapter, Page = callerPage };
                }

                var taleGrain = GrainFactory.FetchTale(state.TaleId);
                await taleGrain.OnExecuteComplete(state.TaleVersionId, callerChapter, callerPage, result);
                ctx.Debug($"all chapters executed, final result for executing {result}");
                state.LastUpdate = DateTime.UtcNow;
            }
        });
        ctx.Debug($"page {callerChapter}#{callerPage} completed executing with {result}");
    }

    public async Task Purge()
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId), nameof(Purge)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId);

        await SaveStateAsync(async (state) =>
        {
            await _publisher.PublishAsync(new CancelPageOperationRequest
            {
                TaleId = state.TaleId,
                TaleVersionId = state.TaleVersionId
            }, TalepreterTopology.Exchanges.WorkExchange, TalepreterTopology.RoutingKeys.CancelWorkRoutingKey, GrainToken);

            async Task purge(IContainerGrain grain) { await grain.Purge(state.TaleId, state.TaleVersionId); }
            async Task purgeCollection()
            {
                await _documentDbContext.PurgeTaleVersionAsync(state.TaleId, state.TaleVersionId, GrainToken);
            }

            ParallelRunner runner = new();
            List<Task> tasks = new(AllContainerGrains(state.TaleId, state.TaleVersionId).Select(x => purge(x)))
            {
                purgeCollection()
            };

            var result = await runner.RunInParallel([.. tasks]);
            if (runner.FailCount > 0) throw new GrainOperationException($"Purge tale version {state.TaleVersionId} failed: {runner.Errors.FirstOrDefault()!.Message}");

            state.LastUpdate = DateTime.UtcNow;
            state.Status = ControllerGrainStatus.Purged;
        });
        ctx.Debug($"purged data");
    }

    public async Task Stop()
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId), nameof(Stop)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId);

        await SaveStateAsync(async (state) =>
        {
            await _publisher.PublishAsync(new CancelPageOperationRequest
            {
                TaleId = state.TaleId,
                TaleVersionId = state.TaleVersionId
            }, TalepreterTopology.Exchanges.WorkExchange, TalepreterTopology.RoutingKeys.CancelWorkRoutingKey, GrainToken);

            async Task stop(Guid taleId, Guid taleVersionId, int chapter)
            {
                var chapterGrain = GrainFactory.FetchChapter(state.TaleId, state.TaleVersionId, chapter);
                await chapterGrain.Stop();
            }
            ParallelRunner runner = new();
            await runner.RunInParallel(state.ProcessResults.Keys.Select(x => stop(state.TaleId, state.TaleVersionId, x)));
            if (runner.FailCount > 0) throw new GrainOperationException($"Stopping tale version {state.TaleVersionId} failed: {runner.Errors.FirstOrDefault()!.Message}");

            state.LastUpdate = DateTime.UtcNow;
            state.Status = ControllerGrainStatus.Cancelled;
        });
        ctx.Debug($"stopped operations");
    }

    public async Task BackupTo(Guid newVersionId)
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId), nameof(BackupTo)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Custom(newVersionId == Guid.Empty, "Publish grain got empty new version id")
            .IsHealthy(State.Status, ControllerGrainStatus.Executed);

        var target = GrainFactory.FetchPublish(State.TaleId, newVersionId);
        async Task backupCollection()
        {
            await _documentDbContext.BackupTaleVersionAsync(State.TaleId, State.TaleVersionId, newVersionId, GrainToken);
        }
        async Task backupInitialize()
        {
            await target.BackupFrom(State.TaleId, newVersionId, State.LastExecutedPage.Clone());
        }

        async Task backupTo(IContainerGrain grain) { await grain.BackupTo(State.TaleId, State.TaleVersionId, newVersionId); }

        ParallelRunner runner = new();
        List<Task> tasks = new(AllContainerGrains(State.TaleId, State.TaleVersionId).Select(x => backupTo(x)))
        {
            backupCollection(),
            backupInitialize()
        };

        var result = await runner.RunInParallel([.. tasks]);
        if (runner.FailCount > 0) throw new GrainOperationException($"Backup tale version {State.TaleVersionId} to {newVersionId} failed: {runner.Errors.FirstOrDefault()!.Message}");

        ctx.Debug($"used version data to backup over {newVersionId}");
    }

    public async Task BackupFrom(Guid taleId, Guid taleVersionId, ChapterPagePair lastExecuted)
    {
        var ctx = Validate(Id(taleId, taleVersionId), nameof(BackupFrom)).TaleId(taleId).TaleVersionId(taleVersionId).IsNull(lastExecuted, nameof(lastExecuted));

        await SaveStateAsync(async (state) =>
        {
            for (int c = 0; c <= lastExecuted.Chapter; c++)
            {
                state.ProcessResults[c] = ProcessResult.Success;
                state.ExecuteResults[c] = ExecuteResult.Success;
            }

            var chapterGrain = GrainFactory.FetchChapter(taleId, taleVersionId, lastExecuted.Chapter ?? 0);
            await chapterGrain.Initialize(taleId, taleVersionId, lastExecuted.Chapter ?? 0);

            state.TaleId = taleId;
            state.TaleVersionId = taleVersionId;
            state.LastUpdate = DateTime.UtcNow;
            state.Status = ControllerGrainStatus.Executed;
            state.LastExecutedPage = lastExecuted.Clone();
        });
        ctx.Debug($"initialized from backup");
    }

    // --

    private async Task<bool> ReportOnProcessComplete(PublishGrainState state, ValidationContext ctx, ControllerGrainStatus status, int callerChapter, int callerPage, ProcessResult result)
    {
        if (state.Status == status)
        {
            var taleGrain = GrainFactory.FetchTale(state.TaleId);
            await taleGrain.OnProcessComplete(state.TaleVersionId, callerChapter, callerPage, result);
            ctx.Debug($"page {callerChapter}#{callerPage} completed processing with {result} which is a fault so reporting back");
            return true;
        }
        return false;
    }

    private async Task<bool> ReportOnExecuteComplete(PublishGrainState state, ValidationContext ctx, ControllerGrainStatus status, int callerChapter, int callerPage, ExecuteResult result)
    {
        if (state.Status == status)
        {
            var taleGrain = GrainFactory.FetchTale(state.TaleId);
            await taleGrain.OnExecuteComplete(state.TaleVersionId, callerChapter, callerPage, result);
            ctx.Debug($"page {callerChapter}#{callerPage} completed executing with {result} which is a fault so reporting back");
            return true;
        }
        return false;
    }

    private IContainerGrain[] AllContainerGrains(Guid taleId, Guid taleVersionId) => [
        GrainFactory.FetchActorContainer(taleId, taleVersionId),
        GrainFactory.FetchAnecdoteContainer(taleId, taleVersionId),
        GrainFactory.FetchPersonContainer(taleId, taleVersionId),
        GrainFactory.FetchWorldContainer(taleId, taleVersionId)];
}
