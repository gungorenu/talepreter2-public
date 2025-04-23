namespace Talepreter.Common.RabbitMQ.Interfaces;

public interface IPublisher
{
    Task PublishAsync<TMessage>(TMessage message, string exchange, string routing, CancellationToken token);
}
