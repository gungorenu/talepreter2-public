using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Talepreter.Operations.Workload;

public interface IWorkTaskManager
{
    void CancelTasks(Func<WorkTask, bool> predicate);
    void StartTask<T, A>(A arg) where T : WorkTask<A> where A : WorkTaskArgument;
    bool DoesExist<T,A>(Func<T, bool> predicate) where T : WorkTask<A> where A : WorkTaskArgument;
}

public class WorkTaskManager : IWorkTaskManager
{
    private readonly ConcurrentDictionary<Guid, WorkTask> _workTasks = [];
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public WorkTaskManager(IServiceScopeFactory scopeFactory, ILogger<WorkTaskManager> logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    private void OnWorkTaskComplete(Task t, object? o)
    {
        var workTask = (WorkTask)o!;
        _workTasks.TryRemove(workTask.Id, out _);
    }

    // --

    public void CancelTasks(Func<WorkTask, bool> predicate)
    {
        foreach (var task in _workTasks.Values.ToArray())
        {
            if (task.CompletedAt.HasValue) continue;
            var fits = predicate(task);
            if (!fits) continue;
            task.Cancel();
        }
    }

    public bool DoesExist<T, A>(Func<T, bool> predicate) where T : WorkTask<A> where A : WorkTaskArgument
    {
        foreach (var task in _workTasks.Values.ToArray().Where(x => x is T))
            if (predicate((T)task)) return true;

        return false;
    }

    public void StartTask<T, A>(A arg)
        where T : WorkTask<A>
        where A : WorkTaskArgument
    {
        Task.Run(async () =>
        {
            using var taskScope = _scopeFactory.CreateScope();
            var workTask = taskScope.ServiceProvider.GetRequiredService<T>() ?? throw new InvalidOperationException($"System could not instantiate worktask {typeof(T).Name}");
            workTask.Assign(arg);
            try
            {
                _workTasks.GetOrAdd(workTask.Id, workTask);
                _logger.LogDebug($"WorkloadMgr: Starting {workTask}, total {_workTasks.Count} tasks in system");
                await workTask.Operate().ContinueWith(OnWorkTaskComplete, workTask);
                _logger.LogDebug($"WorkloadMgr: Completed {workTask} in {workTask.CompletedWithin} ms");
            }
            catch
            {
                OnWorkTaskComplete(null!, workTask);
                _logger.LogWarning($"WorkloadMgr: Faulted {workTask}, total {_workTasks.Count} tasks in system");
            }
        });
    }
}
