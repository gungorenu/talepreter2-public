using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Talepreter.Common.RabbitMQ.Interfaces;
using Talepreter.Extensions;

namespace Talepreter.Common.RabbitMQ.Consumer;

public abstract class RabbitMQMessageReaderService : RabbitMQMessageReader, IHostedService
{
    protected RabbitMQMessageReaderService(IRabbitMQConnectionFactory connFactory,
        ILogger<RabbitMQMessageReaderService> logger,
        IServiceScopeFactory scopeFactory,
        ITalepreterServiceIdentifier serviceId)
        : base(connFactory, logger, scopeFactory, serviceId)
    {
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Starting reader service [{this}]");
        await InitAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Stopping reader service [{this}]");
        await Task.CompletedTask;
    }
}
