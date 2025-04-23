using System.Collections.Concurrent;

namespace Talepreter.Common;

public class TaskManager : IDisposable
{
    private readonly int _parallelCount = 8;
    private readonly ConcurrentQueue<WorkItem> _workToDo = [];
    private readonly Func<Exception, bool>? _retryHandler = null!;
    private readonly ConcurrentBag<Exception> _errors = [];
    private bool _isDisposed = false;
    private int _ongoingTaskCount = 0;
    private int _successfullTaskCount = 0;
    private int _timedoutTaskCount = 0;
    private int _faultedTaskCount = 0;
    private CountdownEvent _countDown = null!;
    private CancellationToken _token;

    public TaskManager(Func<Exception, bool>? retryHandler = null)
    {
        var v = EnvironmentVariableHandler.TryReadEnvVar("TaskManagerParallelTaskCount");
        if (!string.IsNullOrEmpty(v)) _parallelCount = v.ToInt();
        _retryHandler = retryHandler;
    }

    public Exception[]? Errors => [.. _errors];
    public int SuccessfullTaskCount => _successfullTaskCount;
    public int FaultedTaskCount => _faultedTaskCount;
    public int TimedoutTaskCount => _timedoutTaskCount;

    public void AppendTasks(params Func<CancellationToken, Task>[] tasks)
    {
        foreach (var t in tasks) _workToDo.Enqueue(new WorkItem(t));
    }

    public void Start(CancellationToken token)
    {
        if (_countDown != null)
        {
            _countDown.Dispose();
            _countDown = null!;
        }

        _token = token;
        _token.ThrowIfCancellationRequested();
        if (_workToDo.IsEmpty) return;
        _countDown = new CountdownEvent(_workToDo.Count);
        for (int i = 0; i < _parallelCount; i++)
        {
            _token.ThrowIfCancellationRequested();
            if (!_workToDo.TryDequeue(out var work)) break; // that was all, we did not even reach limit
            Task.Run(() => DoWorkAsync(work, _token), token).ContinueWith(OnComplete, work, _token);
        }
        _token.ThrowIfCancellationRequested();

        while (true)
        {
            var set = _countDown.Wait(100, _token);
            if (set) return;
            _token.ThrowIfCancellationRequested();
        }
    }

    private async Task DoWorkAsync(WorkItem w, CancellationToken token)
    {
        Interlocked.Increment(ref _ongoingTaskCount);
        try
        {
            if (w.Retry > 0) await Task.Delay(w.Retry * Timeouts.TaskManagerDelayTimeout, token); // we delay a little
            await w.DoWork(token);
        }
        finally
        {
            Interlocked.Decrement(ref _ongoingTaskCount);
        }
    }

    private void OnComplete(Task task, object? obj)
    {
        WorkItem completedWork = (obj as WorkItem)!;
        bool signalCountDown = false;

        // task faulted, but it may be retried, we need to understand if it should retried though
        if (task.IsFaulted && task.Exception != null && task.Exception.InnerException != null)
        {
            // exceeded 10 retries, which is hardcoded limit, it will be considered as done and faulted
            if (completedWork.Retry >= 10)
            {
                _errors.Add(task.Exception.InnerException);
                Interlocked.Increment(ref _faultedTaskCount);
                signalCountDown = true;
            }
            else
            {
                // we ask the handler, if it says yes then we will retry
                var shouldRetry = _retryHandler?.Invoke(task.Exception.InnerException);
                if (shouldRetry != null && shouldRetry.HasValue && shouldRetry.Value)
                {
                    // in this case we do not take task as completed, we will retry, we put the item back normally
                    completedWork.Retry++;
                    _workToDo.Enqueue(completedWork);
                }
                // retry handler said no, so we do not retry
                else
                {
                    _errors.Add(task.Exception.InnerException);
                    Interlocked.Increment(ref _faultedTaskCount);
                    signalCountDown = true;
                }
            }
        }
        // we do not know the error or it was successful
        else
        {
            if (task.IsCompletedSuccessfully) Interlocked.Increment(ref _successfullTaskCount);
            else if (task.IsCanceled) Interlocked.Increment(ref _timedoutTaskCount);
            else Interlocked.Increment(ref _faultedTaskCount);
            signalCountDown = true;
        }

        // we will pick a new task now, but we should check if we have reached parallel limit as well
        if (_ongoingTaskCount < _parallelCount && !_token.IsCancellationRequested)
        {
            if (_workToDo.TryDequeue(out var newWork)) Task.Run(() => DoWorkAsync(newWork, _token)).ContinueWith(OnComplete, newWork);
        }

        if (signalCountDown) _countDown?.Signal();
    }

    private class WorkItem
    {
        public WorkItem(Func<CancellationToken, Task> work)
        {
            Work = work;
        }

        public Func<CancellationToken, Task> Work { get; init; }
        public int Retry { get; set; } = 0;

        public async Task DoWork(CancellationToken token)
        {
            await Work(token);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _countDown?.Dispose();
                _countDown = null!;
            }
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
