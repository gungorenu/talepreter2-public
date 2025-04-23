using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Talepreter.Common;

namespace Talepreter.Operations.Grains;

public abstract class GrainBase : Grain, IGrain, IIncomingGrainCallFilter
{
    protected IServiceScope _scope = null!;
    protected readonly ILogger _logger;
    protected CancellationTokenSource? _tokenSource;

    protected GrainBase(ILogger logger)
    {
        _logger = logger;
    }

    protected ValidationContext Validate(string id, string methodName) => ValidationContext.Validate(id, _logger, GetType().Name, methodName);
    protected ValidationContext Validate(Guid id, string methodName) => ValidationContext.Validate(id.ToString(), _logger, GetType().Name, methodName);

    protected CancellationToken GrainToken => _tokenSource?.Token ?? CancellationToken.None;

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        try
        {
            _scope = ServiceProvider.CreateScope();
            if (_tokenSource == null)
            {
                _tokenSource = new CancellationTokenSource();
                _tokenSource.CancelAfter(Timeouts.GrainOperationTimeout * 1000);
            }
            await context.Invoke();
        }
        finally
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
            _tokenSource = null;
            _scope?.Dispose();
            _scope = null!;
        }
    }
}