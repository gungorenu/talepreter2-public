// source: https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/march/async-programming-patterns-for-asynchronous-mvvm-applications-data-binding
using System.Threading.Tasks;

namespace Talepreter.GUI.Common
{
    public class NotifyTaskCompletion<TResult> : Notifier
    {
        private readonly TResult _initialValue;
        private readonly Action<Task<TResult>>? _callback = default!;

        public NotifyTaskCompletion(Task<TResult> task, TResult initialValue = default!)
        {
            _initialValue = initialValue;
            Task = task;
            if (!task.IsCompleted) _ = WatchTaskAsync(task);
        }

        public NotifyTaskCompletion(Task<TResult> task, Action<Task<TResult>> callback, TResult initialValue = default!)
        {
            _initialValue = initialValue;
            _callback = callback;
            Task = task;
            if (!task.IsCompleted) _ = WatchTaskAsync(task);
        }

        private async Task WatchTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch { } // empty, normal, but exception is not lost, it can be read via properties

            TriggerPropertyChanges(nameof(Status), nameof(IsCompleted), nameof(IsNotCompleted));
            if (task.IsCanceled) TriggerPropertyChange(nameof(IsCanceled));
            else if (task.IsFaulted) TriggerPropertyChanges(nameof(IsFaulted), nameof(Exception), nameof(InnerException), nameof(ErrorMessage));
            else TriggerPropertyChanges(nameof(IsSuccessfullyCompleted), nameof(Result));

            _callback?.Invoke(Task);
        }

        public Task<TResult> Task { get; init; }
        public TResult Result => (Task.Status == TaskStatus.RanToCompletion) ? Task.Result : _initialValue;
        public TaskStatus Status => Task.Status;

        public bool IsCompleted => Task.IsCompleted;
        public bool IsNotCompleted => !Task.IsCompleted;
        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;
        public bool IsCanceled => Task.IsCanceled;
        public bool IsFaulted => Task.IsFaulted;

        public AggregateException? Exception => Task.Exception;
        public Exception? InnerException => Task.Exception?.InnerException ?? default!;
        public string? ErrorMessage => Task.Exception?.InnerException?.Message ?? default!;
    }

    public class NotifyTaskCompletion : Notifier
    {
        private readonly Action<Task>? _callback = default!;

        public NotifyTaskCompletion(Task task)
        {
            Task = task;
            if (!task.IsCompleted) _ = WatchTaskAsync(task);
            else ExecutePostTask(task); // already completed before it came here
        }

        public NotifyTaskCompletion(Task task, Action<Task> callback)
        {
            _callback = callback;
            Task = task;
            if (!task.IsCompleted) _ = WatchTaskAsync(task);
            else ExecutePostTask(task); // already completed before it came here
        }

        private async Task WatchTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch { } // empty, normal, but exception is not lost, it can be read via properties

            ExecutePostTask(task);
        }

        private void ExecutePostTask(Task task)
        {
            TriggerPropertyChanges(nameof(Status), nameof(IsCompleted), nameof(IsNotCompleted));
            if (task.IsCanceled) TriggerPropertyChange(nameof(IsCanceled));
            else if (task.IsFaulted) TriggerPropertyChanges(nameof(IsFaulted), nameof(Exception), nameof(InnerException), nameof(ErrorMessage));
            else TriggerPropertyChanges(nameof(IsSuccessfullyCompleted));

            _callback?.Invoke(Task);
        }

        public Task Task { get; init; }
        public TaskStatus Status => Task.Status;

        public bool IsCompleted => Task.IsCompleted;
        public bool IsNotCompleted => !Task.IsCompleted;
        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;
        public bool IsCanceled => Task.IsCanceled;
        public bool IsFaulted => Task.IsFaulted;

        public AggregateException? Exception => Task.Exception;
        public Exception? InnerException => Task.Exception?.InnerException ?? default!;
        public string? ErrorMessage => Task.Exception?.InnerException?.Message ?? default!;
    }
}
