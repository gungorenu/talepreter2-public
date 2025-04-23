using Talepreter.Common.RabbitMQ.Interfaces;
using TPT = Talepreter.Common.RabbitMQ.TalepreterTopology;

namespace Talepreter.TaleSvc;

public class ExchangeSetupService : IHostedService
{
    private readonly IRabbitMQConnectionFactory _connFactory;
    public ExchangeSetupService(IRabbitMQConnectionFactory connFactory)
    {
        _connFactory = connFactory;
    }

    public async Task StartAsync(CancellationToken token)
    {
        using var channel = await _connFactory.Connection.CreateChannelAsync(cancellationToken: token);

        var exchangeArgs = new Dictionary<string, object?> { { "x-delayed-type", "fanout" } };
        await channel.ExchangeDeclareAsync(TPT.Exchanges.WorkExchange, "x-delayed-message", true, false, exchangeArgs, cancellationToken: token);
        exchangeArgs = new Dictionary<string, object?> { { "x-delayed-type", "topic" } };
        await channel.ExchangeDeclareAsync(TPT.Exchanges.EventExchange, "x-delayed-message", true, false, exchangeArgs, cancellationToken: token);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
