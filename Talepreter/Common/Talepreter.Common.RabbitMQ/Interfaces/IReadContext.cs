using Microsoft.Extensions.Logging;

namespace Talepreter.Common.RabbitMQ.Interfaces;

public interface IReadContext
{
    Task Respond<TResponse>(TResponse response, string exchange, string routing, CancellationToken token = default);
    Task Respond<TResponse>(TResponse response, string routing, CancellationToken token = default);
    Task Success(CancellationToken token = default);
    Task Delete(CancellationToken token = default);
    Task Reject(bool requeue = false, CancellationToken token = default);
    Task Duplicate(CancellationToken token = default);

    ILogger Logger { get; }
    IServiceProvider Provider { get; }

    int DelayCount { get; }
}
