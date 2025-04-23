using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using Talepreter.Common.RabbitMQ.Interfaces;
using Talepreter.Common.RabbitMQ.Publisher;
using Talepreter.Common.RabbitMQ.Consumer;
using Talepreter.Extensions;
using HostInitActions;

namespace Talepreter.Common.RabbitMQ;

public static class Extensions
{
    public static BasicProperties TalepreterMessageProperties(this IChannel _, Type messageType, ServiceId serviceId = ServiceId.None)
    {
        var props = new BasicProperties
        {
            Persistent = true,
            DeliveryMode = DeliveryModes.Persistent,
            ContentType = "application/json"
        };
        props.Headers ??= new Dictionary<string, object?>();
        props.Headers[RabbitMQMessageReader.T_MESSAGE_TYPE] = messageType.FullName;
        props.Headers[RabbitMQMessageReader.T_DELAY_COUNT] = 0;
        props.Headers[RabbitMQMessageReader.T_SERVICE_ID] = serviceId.ToString();

        return props;
    }

    /// <summary>
    /// Registers all RabbitMQ needed services
    /// </summary>
    public static IServiceCollection RegisterRabbitMQ(this IServiceCollection services)
    {
        services.AddSingleton<IRabbitMQConnectionFactory, RabbitMQConnectionFactory>()
            .AddSingleton<IPooledObjectPolicy<IChannel>, PooledPublishChannelPolicy>()
            .AddSingleton<IPublisher, PublisherWithChannelPool>()
            .AddAsyncServiceInitialization()
                .AddInitAction<IRabbitMQConnectionFactory>(async (factory, token) => await factory.InitConnectionAsync(token));
        return services;
    }
}
