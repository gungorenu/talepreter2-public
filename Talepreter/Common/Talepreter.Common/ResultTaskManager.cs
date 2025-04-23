using System.Collections.Concurrent;

namespace Talepreter.Common;

public class ResultTaskManager<TResult> : IDisposable
{
    private readonly int _parallelCount = 8;
    private readonly ConcurrentQueue<WorkItem> _workToDo = [];
    private readonly Func<Exception, bool>? _retryErrorHandler = null!;
    private readonly Func<TResult, bool>? _retryHandler = null!;
    private readonly ConcurrentBag<Exception> _errors = [];
    private readonly ConcurrentBag<TResult> _results = [];
    private bool _isDisposed = false;
    private int _ongoingTaskCount = 0;
    private int _successfullTaskCount = 0;
    private int _timedoutTaskCount = 0;
    private int _faultedTaskCount = 0;
    private CountdownEvent _countDown = null!;
    private CancellationToken _token;

    public ResultTaskManager(Func<Exception, bool>? retryErrorHandler = null, Func<TResult, bool>? retryHandler = null)
    {
        var v = EnvironmentVariableHandler.TryReadEnvVar("TaskManagerParallelTaskCount");
        if (!string.IsNullOrEmpty(v)) _parallelCount = v.ToInt();
        _retryErrorHandler = retryErrorHandler;
        _retryHandler = retryHandler;
    }

    public Exception[]? Errors => [.. _errors];
    public TResult[]? Results => [.. _results];
    public int SuccessfullTaskCount => _successfullTaskCount;
    public int FaultedTaskCount => _faultedTaskCount;
    public int TimedoutTaskCount => _timedoutTaskCount;

    public void AppendTasks(params Func<CancellationToken, Task<TResult>>[] tasks)
    {
        foreach (var t in tasks) _workToDo.Enqueue(new WorkItem(t));
    }

    public TResult[]? Start(CancellationToken token)
    {
        if (_countDown != null)
        {
            _countDown.Dispose();
            _countDown = null!;
        }

        _token = token;
        _token.ThrowIfCancellationRequested();
        if (_workToDo.IsEmpty) return [];
        _countDown = new CountdownEvent(_workToDo.Count);

        for (int i = 0; i < _parallelCount; i++)
        {
            _token.ThrowIfCancellationRequested();
            if (!_workToDo.TryDequeue(out var work)) break; // that was all, we did not even reach limit
            Task.Run(() => DoWorkAsync(work, _token), token).ContinueWith(OnComplete, work, token);
        }
        _token.ThrowIfCancellationRequested();

        while (true)
        {
            var set = _countDown.Wait(15, _token);
            if (set) return [.. _results];
            _token.ThrowIfCancellationRequested();
        }
    }

    private async Task<TResult> DoWorkAsync(WorkItem w, CancellationToken token)
    {
        Interlocked.Increment(ref _ongoingTaskCount);
        try
        {
            if (w.Retry > 0) await Task.Delay(w.Retry * Timeouts.TaskManagerDelayTimeout, token); // we delay a little
            return await w.DoWork(token);
        }
        finally
        {
            Interlocked.Decrement(ref _ongoingTaskCount);
        }
    }

    private void OnComplete(Task<TResult> task, object? obj)
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
                var shouldRetry = _retryErrorHandler?.Invoke(task.Exception.InnerException);
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
        // successfull
        else if (task.IsCompletedSuccessfully)
        {
            // but is it accepted? or should it be retried?
            var shouldRetry = _retryHandler?.Invoke(task.Result) ?? false;
            if (shouldRetry) // not accepted, we will retry
            {
                if (completedWork.Retry >= 10) // tried enough times, that is end
                {
                    _results.Add(task.Result);
                    Interlocked.Increment(ref _faultedTaskCount);
                    signalCountDown = true;
                }
                else
                {
                    // in this case we do not take task as completed, we will retry, we put the item back normally
                    completedWork.Retry++;
                    _workToDo.Enqueue(completedWork);
                }
            }
            else // accepted
            {
                Interlocked.Increment(ref _successfullTaskCount);
                _results.Add(task.Result);
                signalCountDown = true;
            }
        }
        else if (task.IsCanceled)
        {
            Interlocked.Increment(ref _timedoutTaskCount);
            signalCountDown = true;
        }
        else
        {
            Interlocked.Increment(ref _faultedTaskCount);
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
        public WorkItem(Func<CancellationToken, Task<TResult>> work)
        {
            Work = work;
        }

        public Func<CancellationToken, Task<TResult>> Work { get; init; }
        public int Retry { get; set; } = 0;

        public async Task<TResult> DoWork(CancellationToken token)
        {
            return await Work(token);
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
