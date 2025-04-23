using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Talepreter.Extensions;

public class BaseHostedService : IHostedService
{
    private readonly ILogger<BaseHostedService> _logger;
    private readonly ITalepreterServiceIdentifier _svcIdentifier;

    public BaseHostedService(ILogger<BaseHostedService> logger, ITalepreterServiceIdentifier svcIdentifier)
    {
        _logger = logger;
        _svcIdentifier = svcIdentifier;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Starting [{_svcIdentifier.Name}] service");
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Stopping [{_svcIdentifier.Name}] service");
        await Task.CompletedTask;
    }
}
