using Orleans.Runtime;
using Talepreter.Common.RabbitMQ;
using Talepreter.Common.RabbitMQ.Interfaces;
using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Contracts.Orleans.Process;
using Talepreter.Contracts.Orleans.System;
using Talepreter.Exceptions;
using Talepreter.Contracts.Orleans;
using Talepreter.Operations.Grains;
using Talepreter.TaleSvc.Grains.GrainStates;

namespace Talepreter.TaleSvc.Grains;

[GenerateSerializer]
public class PageGrain : GrainWithStateBase<PageGrainState>, IPageGrain
{
    private readonly IPublisher _publisher;

    private string Id(Guid taleId, Guid taleVersionId, int chapter, int page) => GrainFetcher.FetchPage(taleId, taleVersionId, chapter, page);

    public PageGrain([PersistentState("persistentState", "TaleSvcStorage")] IPersistentState<PageGrainState> persistentState,
        ILogger<PageGrain> logger,
        IPublisher publisher)
        : base(persistentState, logger)
    {
        _publisher = publisher;
    }

    public Task<ControllerGrainStatus> GetStatus() => Task.FromResult(State.Status);

    public async Task Initialize(Guid taleId, Guid taleVersionId, int chapter, int page)
    {
        var ctx = Validate(Id(taleId, taleVersionId, chapter, page), nameof(Initialize)).TaleId(taleId).TaleVersionId(taleVersionId).Chapter(chapter).Page(page)
            .IsHealthy(State.Status, ControllerGrainStatus.Idle);

        await SaveStateAsync((state) =>
        {
            state.TaleId = taleId;
            state.TaleVersionId = taleVersionId;
            state.ChapterId = chapter;
            state.PageId = page;
            state.LastUpdate = DateTime.UtcNow;
            return Task.CompletedTask;
        });
        ctx.Debug($"is initialized");
    }

    public async Task BeginProcess(int chapter, int page, ProcessCommand[] pageCommands)
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId, State.ChapterId, State.PageId), nameof(BeginProcess))
            .TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(chapter).Page(page).IsNull(pageCommands, nameof(pageCommands))
            .IsHealthy(State.Status, ControllerGrainStatus.Idle);

        await SaveStateAsync(async (state) =>
        {
            await _publisher.PublishAsync(new Contracts.Messaging.ProcessPageRequest
            {
                TaleId = state.TaleId,
                TaleVersionId = state.TaleVersionId,
                Chapter = chapter,
                Page = page,
                GrainLogId = ctx.Id,
                Commands = MapCommands(pageCommands)
            }, TalepreterTopology.Exchanges.WorkExchange, "*", GrainToken);

            state.LastUpdate = DateTime.UtcNow;
            state.Status = ControllerGrainStatus.Processing;

            // TODO: start reminder
        });

        ctx.Debug($"initiated process operation");
    }

    public async Task OnProcessComplete(string callerContainer, ProcessResult result)
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId, State.ChapterId, State.PageId), nameof(OnProcessComplete))
            .TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(State.ChapterId).Page(State.PageId).IsEmpty(callerContainer, nameof(callerContainer))
            .Custom(!State.ProcessResults.TryGetValue(callerContainer, out var value), "Page grain does not recognize the caller");

        if (result == ProcessResult.Success && State.Status != ControllerGrainStatus.Processing)
        {
            ctx.Debug($"container {callerContainer} completed processing with {result} but state is {State.Status} so skipping");
            return; // duplicate success messages
        }
        else if (State.Status == ControllerGrainStatus.Cancelled ||
            State.Status == ControllerGrainStatus.Timedout ||
            State.Status == ControllerGrainStatus.Purged)
        {
            ctx.Debug($"container {callerContainer} completed processing with {result} but state is at a fault state {State.Status} so skipping");
            return; // we ignore if cancel called / timed out / purged
        }

        await SaveStateAsync(async (state) =>
        {
            state.ProcessResults[callerContainer] = result;
            if (await ReportOnProcessComplete(state, ctx, ControllerGrainStatus.Cancelled, callerContainer, ProcessResult.Cancelled))
                return; // cancelled already? we inform back
            if (await ReportOnProcessComplete(state, ctx, ControllerGrainStatus.Timedout, callerContainer, ProcessResult.Timedout))
                return; // timed out already? we inform back
            if (await ReportOnProcessComplete(state, ctx, ControllerGrainStatus.Purged, callerContainer, ProcessResult.Cancelled)) // same state as cancel
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

                var chapterGrain = GrainFactory.FetchChapter(state.TaleId, state.TaleVersionId, state.ChapterId);
                await chapterGrain.OnProcessComplete(state.PageId, result);
                ctx.Debug($"all containers processed, final result for processing {result}");
            }
        });
        ctx.Debug($"container {callerContainer} completed processing with {result}");
    }

    public async Task BeginExecute()
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId, State.ChapterId, State.PageId), nameof(BeginExecute))
            .TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(State.ChapterId).Page(State.PageId).IsHealthy(State.Status, ControllerGrainStatus.Processed);

        await SaveStateAsync(async (state) =>
        {
            await _publisher.PublishAsync(new Contracts.Messaging.ExecutePageRequest
            {
                TaleId = state.TaleId,
                TaleVersionId = state.TaleVersionId,
                Chapter = state.ChapterId,
                Page = state.PageId,
                GrainLogId = ctx.Id,
            }, TalepreterTopology.Exchanges.WorkExchange, "*", GrainToken);

            state.LastUpdate = DateTime.UtcNow;
            state.Status = ControllerGrainStatus.Executing;

            // TODO: start reminder
        });
        ctx.Debug($"initiated execute operation");
    }

    public async Task OnExecuteComplete(string callerContainer, ExecuteResult result)
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId, State.ChapterId, State.PageId), nameof(OnExecuteComplete))
            .TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(State.ChapterId).Page(State.PageId).IsEmpty(callerContainer, nameof(callerContainer))
            .Custom(!State.ProcessResults.TryGetValue(callerContainer, out _), "Page grain does not recognize the caller");

        if (result == ExecuteResult.Success && State.Status != ControllerGrainStatus.Executing)
        {
            ctx.Debug($"container {callerContainer} completed executing with {result} but state is {State.Status} so skipping");
            return; // duplicate success messages
        }
        else if (State.Status == ControllerGrainStatus.Cancelled ||
            State.Status == ControllerGrainStatus.Timedout ||
            State.Status == ControllerGrainStatus.Purged)
        {
            ctx.Debug($"container {callerContainer} completed executing with {result} but state is at a fault state {State.Status} so skipping");
            return; // we ignore if cancel called / timed out / purged
        }

        await SaveStateAsync(async (state) =>
        {
            state.ExecuteResults[callerContainer] = result;
            if (await ReportOnExecuteComplete(state, ctx, ControllerGrainStatus.Cancelled, callerContainer, ExecuteResult.Cancelled))
                return; // cancelled already? we inform back
            if (await ReportOnExecuteComplete(state, ctx, ControllerGrainStatus.Timedout, callerContainer, ExecuteResult.Timedout))
                return; // timed out already? we inform back
            if (await ReportOnExecuteComplete(state, ctx, ControllerGrainStatus.Purged, callerContainer, ExecuteResult.Cancelled)) // same state as cancel
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
                    ctx.Fatal("Execute result had to be success but it gave another value");
                    return;
                }
                else state.Status = ControllerGrainStatus.Executed;

                var chapterGrain = GrainFactory.FetchChapter(state.TaleId, state.TaleVersionId, state.ChapterId);
                await chapterGrain.OnExecuteComplete(state.PageId, result);
                ctx.Debug($"all containers executed, final result for executing {result}");
            }
        });
        ctx.Debug($"container {callerContainer} completed executing with {result}");
    }

    public async Task Stop()
    {
        var ctx = Validate(Id(State.TaleId, State.TaleVersionId, State.ChapterId, State.PageId), nameof(Stop))
            .TaleId(State.TaleId).TaleVersionId(State.TaleVersionId).Chapter(State.ChapterId).Page(State.PageId);

        await SaveStateAsync((state) =>
        {
            state.LastUpdate = DateTime.UtcNow;
            state.Status = ControllerGrainStatus.Cancelled;
            return Task.CompletedTask;
        });
        ctx.Debug($"stopped operations");
    }

    // --

    private async Task<bool> ReportOnProcessComplete(PageGrainState state, ValidationContext ctx, ControllerGrainStatus status, string callerContainer, ProcessResult result)
    {
        if (state.Status == status)
        {
            var chapterGrain = GrainFactory.FetchChapter(state.TaleId, state.TaleVersionId, state.ChapterId);
            await chapterGrain.OnProcessComplete(state.PageId, result);
            ctx.Debug($"container {callerContainer} completed processing with {result} which is a fault so reporting back");
            return true;
        }
        return false;
    }

    private async Task<bool> ReportOnExecuteComplete(PageGrainState state, ValidationContext ctx, ControllerGrainStatus status, string callerContainer, ExecuteResult result)
    {
        if (state.Status == status)
        {
            var chapterGrain = GrainFactory.FetchChapter(state.TaleId, state.TaleVersionId, state.ChapterId);
            await chapterGrain.OnExecuteComplete(state.PageId, result);
            ctx.Debug($"container {callerContainer} completed executing with {result} which is a fault so reporting back");
            return true;
        }
        return false;
    }

    private IContainerGrain[] AllContainerGrains(Guid taleId, Guid taleVersionId) => [
        GrainFactory.FetchActorContainer(taleId, taleVersionId),
        GrainFactory.FetchAnecdoteContainer(taleId, taleVersionId),
        GrainFactory.FetchPersonContainer(taleId, taleVersionId),
        GrainFactory.FetchWorldContainer(taleId, taleVersionId)];

    private Contracts.Messaging.Command[] MapCommands(ProcessCommand[] cmds)
    {
        return cmds?.Select(x =>
            new Contracts.Messaging.Command
            {
                Phase = x.Phase,
                Index = x.Index,
                Tag = x.Tag,
                Target = x.Target,
                Parent = x.Parent,
                Comment = x.Comment,
                ArrayParameters = x.ArrayParameters,
                NamedParameters = x.NamedParameters?.Select(n => new Contracts.Messaging.NamedParameter
                {
                    Name = n.Name,
                    Value = n.Value,
                    Type = n.Type switch
                    {
                        NamedParameterType.Set => Contracts.Messaging.NamedParameterType.Set,
                        NamedParameterType.Add => Contracts.Messaging.NamedParameterType.Add,
                        NamedParameterType.Reset => Contracts.Messaging.NamedParameterType.Reset,
                        NamedParameterType.Remove => Contracts.Messaging.NamedParameterType.Remove,
                        _ => throw MissingMapperException.Fault<NamedParameterType, Data.BaseTypes.NamedParameterType>(n.Type)
                    }
                }).ToArray() ?? []
            }
        ).ToArray() ?? [];
    }
}
