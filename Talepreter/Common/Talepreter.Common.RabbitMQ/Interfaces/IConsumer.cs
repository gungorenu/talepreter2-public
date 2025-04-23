namespace Talepreter.Common.RabbitMQ.Interfaces;

public interface IConsumer<TMessage>
{
    Task Consume(TMessage message, IReadContext context, CancellationToken token);
}
