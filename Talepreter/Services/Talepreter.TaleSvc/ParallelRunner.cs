namespace Talepreter.TaleSvc;

public class ParallelRunner
{
    private readonly List<Task<bool>> _runners = [];
    private readonly List<Exception> _errors = [];
    private int _failCounter = 0;
    private int _successCounter = 0;

    public int FailCount => _failCounter;
    public int SuccessCount => _successCounter;
    public ICollection<Exception> Errors => [.. _errors];

    public async Task<bool> RunInParallel(IEnumerable<Task> tasks)
    {
        _failCounter = 0;
        foreach (var t in tasks)
        {
            async Task<bool> task1()
            {
                try
                {
                    await t;
                    Interlocked.Increment(ref _successCounter);
                    return true;
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _failCounter);
                    _errors.Add(ex);
                    return false;
                }
            }
            _runners.Add(task1());
        }
        await Task.WhenAll(_runners);
        return _successCounter != _runners.Count;
    }
}