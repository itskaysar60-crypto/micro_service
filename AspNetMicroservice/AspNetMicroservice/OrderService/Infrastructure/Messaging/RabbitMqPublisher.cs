using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace OrderService.Infrastructure.Messaging;

/// <summary>
/// Singleton publisher that maintains one long-lived RabbitMQ connection and channel.
/// Declares the exchange on first use so the broker is topology-idempotent.
/// </summary>
public sealed class RabbitMqPublisher : IDisposable
{
    private readonly RabbitMqSettings          _settings;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private          IConnection?               _connection;
    private          IModel?                    _channel;
    private readonly object                     _lock = new();

    public RabbitMqPublisher(
        RabbitMqSettings               settings,
        ILogger<RabbitMqPublisher>     logger)
    {
        _settings = settings;
        _logger   = logger;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Publishes a raw JSON string to the configured exchange.
    /// Thread-safe — uses a lock to guard the channel.
    /// </summary>
    public void Publish(string jsonPayload)
    {
        EnsureConnected();

        var body = Encoding.UTF8.GetBytes(jsonPayload);

        lock (_lock)
        {
            var props = _channel!.CreateBasicProperties();
            props.Persistent    = true;          // survive broker restart
            props.ContentType   = "application/json";
            props.DeliveryMode  = 2;

            _channel.BasicPublish(
                exchange:   RabbitMqSettings.ExchangeName,
                routingKey: RabbitMqSettings.RoutingKey,
                basicProperties: props,
                body:        body);
        }

        _logger.LogDebug("Published message to exchange '{Exchange}'.", RabbitMqSettings.ExchangeName);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void EnsureConnected()
    {
        if (_connection?.IsOpen == true && _channel?.IsOpen == true)
            return;

        lock (_lock)
        {
            if (_connection?.IsOpen == true && _channel?.IsOpen == true)
                return;

            _logger.LogInformation(
                "Connecting to RabbitMQ at {Host}:{Port}...", _settings.Host, _settings.Port);

            var factory = new ConnectionFactory
            {
                HostName    = _settings.Host,
                Port        = _settings.Port,
                UserName    = _settings.Username,
                Password    = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                // Automatic recovery reconnects if the broker drops
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval  = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection("OrderService-Publisher");
            _channel    = _connection.CreateModel();

            // Declare durable direct exchange (idempotent)
            _channel.ExchangeDeclare(
                exchange:   RabbitMqSettings.ExchangeName,
                type:       ExchangeType.Direct,
                durable:    true,
                autoDelete: false);

            _logger.LogInformation("RabbitMQ publisher connected.");
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
