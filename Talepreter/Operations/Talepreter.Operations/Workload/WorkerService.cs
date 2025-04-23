using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Talepreter.Common.RabbitMQ.Consumer;
using Talepreter.Common.RabbitMQ.Interfaces;
using Talepreter.Extensions;
using Talepreter.Contracts.Messaging;
using TPT = Talepreter.Common.RabbitMQ.TalepreterTopology;

namespace Talepreter.Operations.Workload;

public class WorkerService : RabbitMQMessageReaderService
{
    public WorkerService(IRabbitMQConnectionFactory connFactory,
        ILogger<WorkerService> logger,
        IServiceScopeFactory scopeFactory,
        ITalepreterServiceIdentifier serviceId) : base(connFactory, logger, scopeFactory, serviceId)
    {
        RegisterConsumer<ProcessPageRequest>();
        RegisterConsumer<ExecutePageRequest>();
        RegisterConsumer<CancelPageOperationRequest>();
    }

    protected async override Task<string> SetupAsync(CancellationToken token)
    {
        var exchangeArgs = new Dictionary<string, object?> { { "x-delayed-type", "fanout" } };
        await Channel.ExchangeDeclareAsync(TPT.Exchanges.WorkExchange, "x-delayed-message", true, false, exchangeArgs, cancellationToken: token);
        exchangeArgs = new Dictionary<string, object?> { { "x-delayed-type", "topic" } };
        await Channel.ExchangeDeclareAsync(TPT.Exchanges.EventExchange, "x-delayed-message", true, false, exchangeArgs, cancellationToken: token);

        var queue = TPT.Queues.WorkQueue(ServiceId);
        var queueArgs = new Dictionary<string, object?> { { "x-queue-type", "quorum" } };
        await Channel.QueueDeclareAsync(queue, true, false, false, queueArgs, cancellationToken: token);

        // queue has two bindings to allow multiple message types
        // note: exchange is fanout so these are not needed actually, regardless the message will come
        await Channel.QueueBindAsync(queue, TPT.Exchanges.WorkExchange, TPT.RoutingKeys.WorkRoutingKey(ServiceId), null, cancellationToken: token);
        await Channel.QueueBindAsync(queue, TPT.Exchanges.WorkExchange, TPT.RoutingKeys.CancelWorkRoutingKey, null, cancellationToken: token);

        return queue;
    }
}
