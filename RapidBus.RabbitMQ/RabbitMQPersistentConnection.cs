using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace RapidBus.RabbitMQ;

internal sealed class RabbitMQPersistentConnection
    : IPersistentConnection
{
    private readonly ConnectionFactory _connectionFactory;
    private readonly object _locker = new();
    private readonly TimeSpan _timeoutBeforeReconnecting;
    private readonly ILogger<RabbitMQPersistentConnection> _logger;

    private bool _connectionFailed = false;
    private IConnection? _connection;
    private bool _disposed = false;

    internal RabbitMQPersistentConnection(
        ConnectionFactory connectionFactory
        , int timeoutBeforeReconnecting
        , ILogger<RabbitMQPersistentConnection> logger)
    {
        _connectionFactory = connectionFactory;
        _timeoutBeforeReconnecting = TimeSpan.FromSeconds(timeoutBeforeReconnecting);
        _logger = logger;
    }

    public bool IsConnected => (_connection is not null) && (_connection.IsOpen) && (!_disposed);

    public event EventHandler<bool>? ReconnectedAfterConnectionFailure;
    
    public bool TryConnect()
    {
     //   _logger.LogInformation("Trying to connect to RabbitMQ...");

        lock (_locker)
        {
            // Creates a policy to retry connecting to message broker until it succeds.
            var policy = Policy
                .Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetryForever((duration) => _timeoutBeforeReconnecting, (ex, time) => {
                    _logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut} seconds ({ExceptionMessage}). Waiting to try again...", $"{(int)time.TotalSeconds}", ex.Message);
                });

            policy.Execute(() => {
                _connection = _connectionFactory.CreateConnection();
            });

            if (!IsConnected || _connection is null)
            {
                _logger.LogCritical("ERROR: could not connect to RabbitMQ.");
                _connectionFailed = true;
                return false;
            }

            // These event handlers hadle situations where the connection is lost by any reason. They try to reconnect the client.
            _connection.ConnectionShutdown += OnConnectionShutdown;
            _connection.CallbackException += OnCallbackException;
            _connection.ConnectionBlocked += OnConnectionBlocked;
            _connection.ConnectionUnblocked += OnConnectionUnblocked;

            _logger.LogTrace("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.Endpoint.HostName);

            // If the connection has failed previously because of a RabbitMQ shutdown or something similar, we need to guarantee that the exchange and queues exist again.
            // It's also necessary to rebind all application event handlers. We use this event handler below to do this.
            if (_connectionFailed)
            {
                ReconnectedAfterConnectionFailure?.Invoke(this, true);
                _connectionFailed = false;
            }

            return true;
        }
    }

    public IModel CreateModel()
    {
        if (!IsConnected || _connection is null)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to perform this action.");
        }

        return _connection.CreateModel();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _connection?.Dispose();
        }
        catch (IOException ex)
        {
            _logger.LogCritical(ex.ToString());
        }
    }

    private void OnCallbackException(object? sender, CallbackExceptionEventArgs args)
    {
        _connectionFailed = true;

        _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");
        TryConnectIfNotDisposed();
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs args)
    {
        _connectionFailed = true;

        _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");
        TryConnectIfNotDisposed();
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs args)
    {
        _connectionFailed = true;

        _logger.LogWarning("A RabbitMQ connection is blocked. Trying to re-connect...");
        TryConnectIfNotDisposed();
    }

    private void OnConnectionUnblocked(object? sender, EventArgs args)
    {
        _connectionFailed = true;

        _logger.LogWarning("A RabbitMQ connection is unblocked. Trying to re-connect...");
        TryConnectIfNotDisposed();
    }

    private void TryConnectIfNotDisposed()
    {
        if (_disposed)
        {
            _logger.LogInformation("RabbitMQ client is disposed. No action will be taken.");
            return;
        }

        TryConnect();
    }
}