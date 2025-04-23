using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Talepreter.Common.RabbitMQ.Interfaces;
using Talepreter.Exceptions;
using Talepreter.Extensions;

namespace Talepreter.Common.RabbitMQ.Consumer;

public abstract class RabbitMQMessageReader : IDisposable, IAsyncDisposable, IConsumerDescription
{
    internal const string T_MESSAGE_TYPE = "talepreter-message-type";
    internal const string T_DELAY_COUNT = "talepreter-delay-count";
    internal const string T_SERVICE_ID = "talepreter-service-id";
    internal const string X_DELAY = "x-delay";

    private bool isDisposed = false;

    public RabbitMQMessageReader(IRabbitMQConnectionFactory connFactory,
        ILogger logger,
        IServiceScopeFactory scopeFactory, // sadly needed for consumer locating
        ITalepreterServiceIdentifier serviceId)
    {
        ScopeFactory = scopeFactory;
        Logger = logger;
        Factory = connFactory;
        ServiceId = serviceId.ServiceId;
    }

    private readonly List<IConsumerTypePair> _consumerTypePairs = [];

    protected ILogger Logger { get; private init; }
    protected IServiceScopeFactory ScopeFactory { get; private init; }

    public ServiceId ServiceId { get; private init; }
    public IChannel Channel { get; private set; } = default!;
    public IRabbitMQConnectionFactory Factory { get; private init; }
    public virtual int MaxDelayCount => 10;
    public virtual int ExecuteTimeout => Factory.ExecuteTimeout;

    protected async Task InitAsync(CancellationToken token)
    {
        Channel = await Factory.Connection.CreateChannelAsync(cancellationToken: token);

        Logger.LogDebug($"Initializing reader {this}");
        var queueName = await SetupAsync(token);

        if (!string.IsNullOrEmpty(queueName))
        {
            var consumer = new AsyncEventingBasicConsumer(Channel);
            consumer.ReceivedAsync += HandleOnReceivedAsync;
            await Channel.BasicConsumeAsync(queueName, false, consumer, token);
        }
    }

    protected async Task HandleOnReceivedAsync(object sender, BasicDeliverEventArgs @event)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        var description = "";
        try
        {
            var body = Encoding.UTF8.GetString(@event.Body.ToArray());
            var headers = @event.BasicProperties.Headers;
            if (headers == null)
            {
                Logger.LogWarning($"Reader-{this}: Unknown type message, headers is not set");
                await Channel!.BasicNackAsync(@event.DeliveryTag, false, false, @event.CancellationToken);
                return;
            }
            if (@event.BasicProperties.Headers == null)
            {
                Logger.LogWarning($"Reader-{this}: Unknown type message, type info is missing");
                await Channel!.BasicNackAsync(@event.DeliveryTag, false, false);
                return;
            }

            if (!@event.BasicProperties.Headers!.TryGetValue(T_MESSAGE_TYPE, out var typeName))
            {
                Logger.LogWarning($"Reader-{this}: Unknown type message, type info is missing");
                await Channel!.BasicNackAsync(@event.DeliveryTag, false, false, @event.CancellationToken);
                return;
            }
            if (typeName == null || string.IsNullOrEmpty(typeName.ToString()))
            {
                Logger.LogWarning($"Reader-{this}: Unknown type message, type info is null or empty");
                await Channel!.BasicNackAsync(@event.DeliveryTag, false, false, @event.CancellationToken);
                return;
            }

            var typeNameString = Encoding.UTF8.GetString((byte[])typeName);
            Type? type = AppDomain.CurrentDomain.GetAssemblies().Reverse().Select(a => a.GetType(typeNameString)).FirstOrDefault(t => t != null);
            if (type == null)
            {
                Logger.LogWarning($"Reader-{this}: Unknown type message, type is not recognized");
                await Channel!.BasicNackAsync(@event.DeliveryTag, false, false, @event.CancellationToken);
                return;
            }

            var pair = _consumerTypePairs.FirstOrDefault(x => x.MessageType == type);
            if (pair == null)
            {
                Logger.LogCritical($"Reader-{this}: Reader setup has no consumer registered for this message");
                await Channel!.BasicNackAsync(@event.DeliveryTag, false, false, @event.CancellationToken);
                return;
            }

            var message = System.Text.Json.JsonSerializer.Deserialize(body, type);
            if (message == null)
            {
                Logger.LogWarning($"Reader-{this}: Null Message of type {type.FullName}");
                await Channel!.BasicNackAsync(@event.DeliveryTag, false, false, @event.CancellationToken);
                return;
            }
            description = message.ToString();

            using var consumerContext = new RabbitMQMessageReaderContext(Logger, this, @event, ScopeFactory.CreateScope(), message, pair.ConsumerType);
            await consumerContext.ConsumeMessageAsync(@event.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning($"Reader-{this}: Timeout >> {description}");
            await Channel!.BasicNackAsync(@event.DeliveryTag, false, false, @event.CancellationToken);
        }
        catch (ConsumerJobException cex) // catch known exceptions
        {
            Logger.LogWarning(cex, $"Reader-{this}: Failed >> {description}");
            await Channel!.BasicNackAsync(@event.DeliveryTag, false, false, @event.CancellationToken);
        }
        catch (Exception ex) // catch any exception
        {
            Logger.LogError(ex, $"Reader-{this}: Faulted >> {description}");
            await Channel!.BasicNackAsync(@event.DeliveryTag, false, false, @event.CancellationToken);
        }
    }

    /// <summary>
    /// Must declare queue, exchange if necessary and bindings
    /// </summary>
    /// <returns>queue name</returns>
    protected abstract Task<string> SetupAsync(CancellationToken token);

    public void RegisterConsumer<TMessage>()
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        _consumerTypePairs.Add(new ConsumerTypePair<TMessage, IConsumer<TMessage>>());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool isDisposing)
    {
        if (isDisposed) return;

        try
        {
            if (Channel != null)
            {
                Channel?.Dispose();
                Channel = null!;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Reader {this} could not dispose RabbitMQ channel");
        }
        finally
        {
            isDisposed = true;
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (Channel is not null)
        {
            await Channel.CloseAsync().ConfigureAwait(false);
            await Channel.DisposeAsync().ConfigureAwait(false);
        }

        Channel = null!;
    }

    public override string ToString() => $"{GetType().Name}";

    private interface IConsumerTypePair
    {
        Type MessageType { get; }
        Type ConsumerType { get; }
    }

    private class ConsumerTypePair<TMessage, TConsumer> : IConsumerTypePair
        where TConsumer : IConsumer<TMessage>
    {
        public ConsumerTypePair()
        {
            MessageType = typeof(TMessage);
            ConsumerType = typeof(TConsumer);
        }
        public Type MessageType { get; init; }
        public Type ConsumerType { get; init; }
    }
}
