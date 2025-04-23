using RabbitMQ.Client;
using Talepreter.Common.RabbitMQ.Interfaces;

namespace Talepreter.Common.RabbitMQ;

public class RabbitMQConnectionFactory : IRabbitMQConnectionFactory, IAsyncDisposable, IDisposable
{
    private IConnection _connection = default!;

    public RabbitMQConnectionFactory()
    {
        var user = EnvironmentVariableHandler.ReadEnvVar("RabbitMQUser");
        var pwd = EnvironmentVariableHandler.ReadEnvVar("RabbitMQPwd");
        var server = EnvironmentVariableHandler.ReadEnvVar("RabbitMQServer");
        var virtualHost = EnvironmentVariableHandler.ReadEnvVar("RabbitMQVirtualHost");
        var concurrentConsumerCount = EnvironmentVariableHandler.ReadEnvVar("RabbitMQConcurrentConsumerCount").ToUShort();
        ExecuteTimeout = Timeouts.RabbitMQExecuteTimeout;
        MaxPublishChannel = EnvironmentVariableHandler.ReadEnvVar("RabbitMQMaxPublishChannel").ToInt();

        Factory = new ConnectionFactory()
        {
            HostName = server,
            UserName = user,
            Password = pwd,
            VirtualHost = virtualHost,
            ConsumerDispatchConcurrency = concurrentConsumerCount
        };
    }

    public IConnectionFactory Factory { get; private set; }
    public int ExecuteTimeout { get; private set; }
    public int MaxPublishChannel { get; private set; }
    public IConnection Connection => _connection;

    public async Task InitConnectionAsync(CancellationToken token)
    {
        if (_connection != null) return;
        _connection = await Factory.CreateConnectionAsync(token);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_connection is IDisposable disposable)
            {
                disposable.Dispose();
                _connection = null!;
            }
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync().ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        _connection = null!;
    }
}
