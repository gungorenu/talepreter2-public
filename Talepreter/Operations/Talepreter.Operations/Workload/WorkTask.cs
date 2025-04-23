using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Talepreter.Common;

namespace Talepreter.Operations.Workload;

public abstract class WorkTask : IWorkTask
{
    private readonly Guid _id = Guid.NewGuid();
    protected readonly ILogger _logger;
    private bool _isDisposed;
    private CancellationTokenSource _tokenSource = null!;

    public WorkTask(ILogger logger) { _logger = logger; }

    public abstract WorkTaskType Type { get; }

    public Guid Id => _id;
    public DateTime Started { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; private set; } = default!;
    public long CompletedWithin { get; private set; } = default!;
    public CancellationToken Token { get => _tokenSource?.Token ?? CancellationToken.None; }
    public void Cancel() => _tokenSource?.Cancel();

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                if (_tokenSource != null)
                {
                    _tokenSource.Cancel();
                    _tokenSource.Dispose();
                    _tokenSource = null!;
                }
                DisposeCustom();
            }
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void DisposeCustom() { }

    protected void OnStart()
    {
        _tokenSource = new CancellationTokenSource();
        _tokenSource.CancelAfter(Timeouts.WorktaskTimeout * 1000);
        Started = DateTime.UtcNow;
    }

    protected void OnComplete(long completedWithin)
    {
        CompletedWithin = completedWithin;
        CompletedAt = DateTime.UtcNow;
    }
}

public abstract class WorkTask<TArg> : WorkTask where TArg : WorkTaskArgument
{
    protected TArg _arg = default!;

    public WorkTask(ILogger logger) : base(logger) { }

    protected abstract Task WorkAsync();
    public TArg Argument => _arg;

    public void Assign(TArg arg)
    {
        _arg = arg;
    }

    public async Task Operate()
    {
        // can only start onces
        Stopwatch sw = new();
        sw.Start();
        try
        {
            OnStart();
            await WorkAsync();
        }
        finally
        {
            sw.Stop();
            OnComplete(sw.ElapsedMilliseconds);
        }
    }
}
