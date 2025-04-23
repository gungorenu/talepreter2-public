using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using System.Text;
using Talepreter.Common.RabbitMQ.Interfaces;
using Talepreter.Exceptions;
using Talepreter.Extensions;

namespace Talepreter.Common.RabbitMQ.Publisher;

public class PublisherWithChannelPool : IPublisher
{
    private readonly ObjectPool<IChannel> _channelPool;
    private readonly ILogger<IPublisher> _logger;
    private readonly ServiceId _serviceId;

    public PublisherWithChannelPool(IRabbitMQConnectionFactory factory, IPooledObjectPolicy<IChannel> channelPolicy, ILogger<IPublisher> logger, ITalepreterServiceIdentifier serviceId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(factory.MaxPublishChannel, 1, "MaxPublishChannel");

        _serviceId = serviceId.ServiceId;
        _logger = logger;
        var provider = new DefaultObjectPoolProvider
        {
            MaximumRetained = factory.MaxPublishChannel
        };
        _channelPool = provider.Create(channelPolicy);
    }

    public async Task PublishAsync<TMessage>(TMessage message, string exchange, string routing, CancellationToken token)
    {
        var channel = _channelPool.Get();
        try
        {
            if (channel == null) throw new PublisherJobException("Channel pool could not instantiate new channel to use");

            _logger.LogDebug($"Publishing message {message} to {exchange}:{routing}");
            var props = channel.TalepreterMessageProperties(typeof(TMessage), _serviceId);
            var body = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message));

            await channel.BasicPublishAsync(exchange, routing, false, props, body, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Publishing message {message} to {exchange}:{routing} failed!");
            throw;
        }
        finally
        {
            _channelPool.Return(channel);
        }
    }
}
