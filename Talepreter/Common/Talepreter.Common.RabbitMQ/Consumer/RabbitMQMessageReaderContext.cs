using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Talepreter.Common.RabbitMQ.Interfaces;
using Talepreter.Exceptions;

namespace Talepreter.Common.RabbitMQ.Consumer;

public class RabbitMQMessageReaderContext : IDisposable, IReadContext
{
    private IServiceScope _scope;
    private bool isDisposed = false;
    private readonly object _message;
    private readonly IConsumerDescription _reader;
    private readonly Type _consumerType;

    public RabbitMQMessageReaderContext(ILogger logger,
        IConsumerDescription consumer,
        BasicDeliverEventArgs @event,
        IServiceScope scope, // sadly needed for consumer locating
        object message,
        Type consumerType)
    {
        _reader = consumer;
        Context = @event;
        _scope = scope;
        Logger = logger;
        _message = message;
        _consumerType = consumerType;
    }

    public BasicDeliverEventArgs Context { get; init; }
    public ILogger Logger { get; init; }
    public IServiceProvider Provider => _scope.ServiceProvider;

    public ulong DeliveryTag => Context.DeliveryTag;
    public int DelayCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);
            if (Context.BasicProperties.Headers == null) return 0;
            if (Context.BasicProperties.Headers.TryGetValue(RabbitMQMessageReader.T_DELAY_COUNT, out object? value))
                return $"{value}".ToInt();
            return 0;
        }
    }

    public async Task ConsumeMessageAsync(CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        if (DelayCount >= _reader.MaxDelayCount) throw new ConsumerJobException($"Retry limit exceeded for message: {_message}");
        var msgType = _message.GetType();

        var consumer = _scope.ServiceProvider.GetRequiredService(_consumerType) ??
            throw new ConsumerJobException($"Consumer for message could not be created: {_message}");
        var method = consumer.GetType().GetMethod("Consume", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            [msgType, typeof(IReadContext), typeof(CancellationToken)]) ??
            throw new ConsumerJobException($"Consumer has not method matching for interface, reflection error: {_message}");

        var task = (Task)method.Invoke(consumer, [_message, this, token])!;
        await task;
    }

    public async Task Respond<TResponse>(TResponse response, string routing, CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        var body = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response));
        await PublishInternalAsync(typeof(TResponse), body, Context.Exchange, routing, token);
        Logger.LogDebug($"Reader-{_reader}: Responds {typeof(TResponse).Name} >> {_message}");
    }
    public async Task Respond<TResponse>(TResponse response, string exchange, string routing, CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        var body = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response));
        await PublishInternalAsync(typeof(TResponse), body, exchange, routing, token);
        Logger.LogDebug($"Reader-{_reader}: Responds {typeof(TResponse).Name} >> {_message}");
    }
    public async Task Success(CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        await _reader.Channel.BasicAckAsync(DeliveryTag, false, token);
        Logger.LogDebug($"Reader-{_reader}: Success >> {_message}");
    }
    public async Task Delete(CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        await _reader.Channel.BasicNackAsync(DeliveryTag, false, false, token);
        Logger.LogInformation($"Reader-{_reader}: Deletes >> {_message}");
    }
    public async Task Reject(bool requeue = false, CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        await _reader.Channel.BasicRejectAsync(DeliveryTag, requeue, token);
        Logger.LogInformation($"Reader-{_reader}: Rejects >> {_message}");
    }
    public async Task Duplicate(CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        await _reader.Channel.BasicRejectAsync(DeliveryTag, false, token);
        Logger.LogDebug($"Reader-{_reader}: Duplicate >> {_message}");
    }

    private async Task PublishInternalAsync(Type type, byte[] body, string exchange, string routing, CancellationToken token = default)
    {
        var props = _reader.Channel.TalepreterMessageProperties(type, _reader.ServiceId);
        props.AppId = Context.BasicProperties.AppId;

        await _reader.Channel.BasicPublishAsync(exchange, routing, false, props, body, token);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool isDisposing)
    {
        if (isDisposed) return;

        if (_scope != null)
        {
            _scope.Dispose();
            _scope = null!;
        }
        isDisposed = true;
    }
}
