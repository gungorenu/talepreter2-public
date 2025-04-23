using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;
using Talepreter.Common.RabbitMQ.Interfaces;

namespace Talepreter.Common.RabbitMQ.Publisher;

public class PooledPublishChannelPolicy : IPooledObjectPolicy<IChannel>
{
    private readonly IRabbitMQConnectionFactory _connFactory;

    public PooledPublishChannelPolicy(IRabbitMQConnectionFactory connFactory)
    {
        _connFactory = connFactory;
    }

    // TODO: RabbitMQ creates this as Async now so we need a new policy that accepts async stuff, until then we use this hack
    public IChannel Create() => _connFactory.Connection.CreateChannelAsync().Result;

    public bool Return(IChannel obj)
    {
        if (obj == null) return false;
        if (obj.IsOpen) return true;
        obj.Dispose();
        return false;
    }
}
