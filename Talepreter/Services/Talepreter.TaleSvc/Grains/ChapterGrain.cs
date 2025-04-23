using Orleans.Runtime;
using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Contracts.Orleans.Process;
using Talepreter.Contracts.Orleans.System;
using Talepreter.Exceptions;
using Talepreter.Operations.Grains;
using Talepreter.TaleSvc.Grains.GrainStates;

namespace Talepreter.TaleSvc.Grains;

[GenerateSerializer]
public class ChapterGrain : GrainWithStateBase<ChapterGrainState>, IChapterGrain
{
    private string Id(Guid taleId, Guid taleVersionId, int chapter) => GrainFetcher.FetchChapter(taleId, taleVersionId, chapter);

    public ChapterGrain([PersistentState("persistentState", "TaleSvcStorage")] IPersistentState<ChapterGrainState> persistentState, ILogger<ChapterGrain> logger)
        : base(persistentState, logger)
    {
    }

    public Task<ControllerGrainStatus> GetStatus() => Task.FromResult(State.Status);

    public Task<int> LastExecutedPage()
    {
        _ = Validate(Id(State.TaleId, State.TaleVersionId, State.ChapterId), nameof(LastExecutedPage)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId);
        if (State.Status != ControllerGrainStatus.Executed &&
            State.Status != ControllerGrainStatus.Idle) return Task.FromResult(-1);

        for (int p = State.ExecuteResults.Count - 1; p >= 0; p--)
        {
            var pageResult = State.ExecuteResults[p];
            if (pageResult == ExecuteResult.Success) return Task.FromResult(p);
        }
        return Task.FromResult(-1);
    }

    public async Task Initialize(Guid taleId, Guid taleVersionId, int chapter, int? pageFromBackup = null)
    {
        var ctx = Validate(Id(taleId, taleVersionId, chapter), nameof(Initialize)).TaleId(taleId).TaleVersionId(taleVersionId).Chapter(chapter).IsHealthy(State.Status, ControllerGrainStatus.Idle);

        await SaveStateAsync((state) =>
        {
            if (pageFromBackup != null)
            {
                for (int p = 0; p <= pageFromBackup.Value; p++)
                {
                    state.ProcessResults[p] = ProcessResult.Success;
                    state.ExecuteResults[p] = ExecuteResult.Success;
                }
            }

            state.TaleId = taleId;
            state.TaleVersionId = taleVersionId;
            state.ChapterId = chapter;
            state.LastUpdate = DateTime.UtcNow;
            return Task.CompletedTask;
        });
        ctx.Debug($"is initialized");
    }

    public async Task<bool> AddPage(int page)
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId, State.ChapterId), nameof(AddPage)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(State.ChapterId)
            .IsHealthy(State.Status,
                ControllerGrainStatus.Idle, // creation from scratch
                ControllerGrainStatus.Executed) // a middle place, when there are multiple pages to add and in the middle of those pages
            .Page(page).Custom(State.ProcessResults.ContainsKey(page), $"Chapter already has page {page}");

        bool pageExists = false;
        // chapter exists already? check if healthy
        if (State.ExecuteResults.TryGetValue(page, out var execResult))
        {
            pageExists = true;
            switch (execResult)
            {
                case ExecuteResult.Faulted:
                case ExecuteResult.Blocked:
                case ExecuteResult.Timedout:
                    throw new GrainOperationException(ctx.Id, ctx.MethodName, $"Chapter grain already has page in fault execute state");
                default: break;
            }
        }
        else if (State.ProcessResults.TryGetValue(page, out var procResult))
        {
            pageExists = true;
            switch (procResult)
            {
                case ProcessResult.Faulted:
                case ProcessResult.Blocked:
                case ProcessResult.Timedout:
                    throw new GrainOperationException(ctx.Id, ctx.MethodName, $"Chapter grain already has page in fault process state");
                default: break;
            }
        }

        var pageGrain = GrainFactory.FetchPage(State.TaleId, State.TaleVersionId, State.ChapterId, page);
        var pageStatus = await pageGrain.GetStatus();
        switch (pageStatus)
        {
            case ControllerGrainStatus.Idle:
            case ControllerGrainStatus.Executed: break;
            default: throw new GrainOperationException(ctx.Id, ctx.MethodName, $"Page grain exists and is in a fault state");
        }

        if (pageExists)
        {
            ctx.Debug($"page {page} already exists in healthy state, so not added");
            return false;
        }

        await SaveStateAsync(async (state) =>
        {
            await pageGrain.Initialize(state.TaleId, state.TaleVersionId, state.ChapterId, page);
            state.ProcessResults[page] = ProcessResult.None;
            state.ExecuteResults[page] = ExecuteResult.None;
            state.LastUpdate = DateTime.UtcNow;
        });
        ctx.Debug($"added {page} page");
        return true;
    }

    public async Task BeginProcess(int chapter, int page, ProcessCommand[] pageCommands)
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId, State.ChapterId), nameof(BeginProcess)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(State.ChapterId).Chapter(chapter).Page(page)
            .IsHealthy(State.Status, ControllerGrainStatus.Idle, ControllerGrainStatus.Executed).IsNull(pageCommands, nameof(pageCommands));

        await SaveStateAsync(async (state) =>
        {
            state.Status = ControllerGrainStatus.Processing;
            var grain = GrainFactory.FetchPage(State.TaleId, State.TaleVersionId, chapter, page);
            await grain.BeginProcess(chapter, page, pageCommands);
        });
        ctx.Debug($"initiated process operation");
    }

    public async Task OnProcessComplete(int callerPage, ProcessResult result)
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId, State.ChapterId), nameof(OnProcessComplete)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(State.ChapterId).Page(callerPage);

        if (result == ProcessResult.Success && State.Status != ControllerGrainStatus.Processing)
        {
            ctx.Debug($"page {callerPage} completed processing with {result} but state is {State.Status} so skipping");
            return; // duplicate success messages
        }
        else if (State.Status == ControllerGrainStatus.Cancelled ||
            State.Status == ControllerGrainStatus.Timedout ||
            State.Status == ControllerGrainStatus.Purged)
        {
            ctx.Debug($"page {callerPage} completed processing with {result} but state is at a fault state {State.Status} so skipping");
            return; // we ignore if cancel called / timed out / purged
        }

        if (!State.ProcessResults.ContainsKey(callerPage)) throw new GrainOperationException(ctx.Id, ctx.MethodName, $"Chapter grain does not recognize the page");
        await SaveStateAsync(async (state) =>
        {
            state.ProcessResults[callerPage] = result;
            if (await ReportOnProcessComplete(state, ctx, ControllerGrainStatus.Cancelled, callerPage, ProcessResult.Cancelled))
                return; // cancelled already? we inform back
            if (await ReportOnProcessComplete(state, ctx, ControllerGrainStatus.Timedout, callerPage, ProcessResult.Timedout))
                return; // timed out already? we inform back
            if (await ReportOnProcessComplete(state, ctx, ControllerGrainStatus.Purged, callerPage, ProcessResult.Cancelled)) // same state as cancel
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
                    ctx.Fatal("Process result had to be success but it gave another value");
                    return;
                }
                else state.Status = ControllerGrainStatus.Processed;

                var publishGrain = GrainFactory.FetchPublish(state.TaleId, state.TaleVersionId);
                await publishGrain.OnProcessComplete(state.ChapterId, callerPage, result);
                ctx.Debug($"all pages processed, final result for processing {result}");
            }
        });
        ctx.Debug($"page {callerPage} completed processing with {result}");
    }

    public async Task BeginExecute()
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId, State.ChapterId), nameof(BeginExecute)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(State.ChapterId).IsHealthy(State.Status, ControllerGrainStatus.Processed);

        // we have results already? this can happen when we run same version but with minor command changes only
        await SaveStateAsync(async (state) =>
        {
            state.LastUpdate = DateTime.UtcNow;
            state.Status = ControllerGrainStatus.Executing;

            for (int pageId = 0; pageId < state.PageCount(); pageId++)
            {
                var processResult = state.ProcessResults[pageId];
                if (processResult != ProcessResult.Success) throw new GrainOperationException(ctx.Id, ctx.MethodName, "Page within chapter has a failure in processing so execution cannot proceed");

                var executeResult = state.ExecuteResults[pageId];
                if (executeResult != ExecuteResult.Success && executeResult != ExecuteResult.None) throw new GrainOperationException(ctx.Id, ctx.MethodName, "Page within chapter has a failure in executing so execution cannot proceed");

                if (executeResult == ExecuteResult.None)
                {
                    var pageGrain = GrainFactory.FetchPage(state.TaleId, state.TaleVersionId, state.ChapterId, pageId);
                    await pageGrain.BeginExecute();
                    ctx.Debug($"initiated execute operation");
                    // TODO: start reminder
                    return;
                }
            }
            ctx.Debug($"could not execute because all chapters are executed");
        });
    }

    public async Task OnExecuteComplete(int callerPage, ExecuteResult result)
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId, State.ChapterId), nameof(OnExecuteComplete)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(State.ChapterId).Page(callerPage).Custom(!State.ExecuteResults.ContainsKey(callerPage), "Chapter grain does not have the page");

        if (result == ExecuteResult.Success && State.Status != ControllerGrainStatus.Executing)
        {
            ctx.Debug($"page {callerPage} completed executing with {result} but state is {State.Status} so skipping");
            return; // duplicate success messages
        }
        else if (State.Status == ControllerGrainStatus.Cancelled ||
            State.Status == ControllerGrainStatus.Timedout ||
            State.Status == ControllerGrainStatus.Purged)
        {
            ctx.Debug($"page {callerPage} completed executing with {result} but state is at a fault state {State.Status} so skipping");
            return; // we ignore if cancel called / timed out / purged
        }

        await SaveStateAsync(async (state) =>
        {
            state.ExecuteResults[callerPage] = result;
            if (await ReportOnExecuteComplete(state, ctx, ControllerGrainStatus.Cancelled, callerPage, ExecuteResult.Cancelled))
                return; // cancelled already? we inform back
            if (await ReportOnExecuteComplete(state, ctx, ControllerGrainStatus.Timedout, callerPage, ExecuteResult.Timedout))
                return; // timed out already? we inform back
            if (await ReportOnExecuteComplete(state, ctx, ControllerGrainStatus.Purged, callerPage, ExecuteResult.Cancelled)) // same state as cancel
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
                else state.Status = ControllerGrainStatus.Executed;

                var publishGrain = GrainFactory.FetchPublish(state.TaleId, state.TaleVersionId);
                await publishGrain.OnExecuteComplete(state.ChapterId, callerPage, result);
                ctx.Debug($"all pages executed, final result for executing {result}");
            }
        });
        ctx.Debug($"page {callerPage} completed executing with {result}");
    }

    public async Task Stop()
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId, State.ChapterId), nameof(Stop)).TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(State.ChapterId);

        await SaveStateAsync(async (state) =>
        {
            async Task stop(Guid taleId, Guid taleVersionId, int chapter, int page)
            {
                var pageGrain = GrainFactory.FetchPage(state.TaleId, state.TaleVersionId, state.ChapterId, page);
                await pageGrain.Stop();
            }
            ParallelRunner runner = new();
            await runner.RunInParallel(state.ProcessResults.Keys.Select(x => stop(state.TaleId, state.TaleVersionId, state.ChapterId, x)));
            if (runner.FailCount > 0) throw new GrainOperationException($"Stopping tale version {state.TaleVersionId} failed: {runner.Errors.FirstOrDefault()!.Message}");
            state.LastUpdate = DateTime.UtcNow;
            state.Status = ControllerGrainStatus.Cancelled;
        });
        ctx.Debug($"stopped operations");
    }

    // --

    private async Task<bool> ReportOnProcessComplete(ChapterGrainState state, ValidationContext ctx, ControllerGrainStatus status, int callerPage, ProcessResult result)
    {
        if (state.Status == status)
        {
            var publishGrain = GrainFactory.FetchPublish(state.TaleId, state.TaleVersionId);
            await publishGrain.OnProcessComplete(state.ChapterId, callerPage, result);
            ctx.Debug($"page {callerPage} completed processing with {result} which is a fault so reporting back");
            return true;
        }
        return false;
    }

    private async Task<bool> ReportOnExecuteComplete(ChapterGrainState state, ValidationContext ctx, ControllerGrainStatus status, int callerPage, ExecuteResult result)
    {
        if (state.Status == status)
        {
            var publishGrain = GrainFactory.FetchPublish(state.TaleId, state.TaleVersionId);
            await publishGrain.OnExecuteComplete(state.ChapterId, callerPage, result);
            ctx.Debug($"page {callerPage} completed executing with {result} which is a fault so reporting back");
            return true;
        }
        return false;
    }
}
