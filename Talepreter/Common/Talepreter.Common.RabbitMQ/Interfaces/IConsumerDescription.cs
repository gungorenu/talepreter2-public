using RabbitMQ.Client;
using Talepreter.Extensions;

namespace Talepreter.Common.RabbitMQ.Interfaces;

public interface IConsumerDescription
{
    IChannel Channel { get; }
    int MaxDelayCount { get; }
    ServiceId ServiceId { get; }
}
