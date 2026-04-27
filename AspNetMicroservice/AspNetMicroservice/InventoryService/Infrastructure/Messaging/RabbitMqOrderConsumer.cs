using System.Text;
using System.Text.Json;
using InventoryService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;

namespace InventoryService.Infrastructure.Messaging;

/// <summary>
/// Background service that connects to RabbitMQ and consumes OrderCreatedEvents
/// published by OrderService.
/// 
/// Topology (idempotently declared on startup):
///   Exchange : "order.created"  (direct, durable)
///   Queue    : "order.created.inventory" (durable)
///   Binding  : exchange → queue via routing key "order.created"
/// </summary>
public sealed class RabbitMqOrderConsumer : BackgroundService
{
    private readonly RabbitMqSettings              _settings;
    private readonly IServiceScopeFactory          _scopeFactory;
    private readonly ILogger<RabbitMqOrderConsumer> _logger;

    private IConnection? _connection;
    private IModel?      _channel;

    public RabbitMqOrderConsumer(
        RabbitMqSettings                settings,
        IServiceScopeFactory            scopeFactory,
        ILogger<RabbitMqOrderConsumer>  logger)
    {
        _settings     = settings;
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // RabbitMQ consumer is event-driven — we register a callback and return.
        // The BackgroundService host keeps us alive.
        Connect();
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }

    // ── Connection & topology ──────────────────────────────────────────────

    private void Connect()
    {
        _logger.LogInformation(
            "RabbitMqOrderConsumer connecting to {Host}:{Port}...",
            _settings.Host, _settings.Port);

        var factory = new ConnectionFactory
        {
            HostName    = _settings.Host,
            Port        = _settings.Port,
            UserName    = _settings.Username,
            Password    = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            // Automatically recover connection after broker restart
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval  = TimeSpan.FromSeconds(10),
            DispatchConsumersAsync   = true   // async consumer callbacks
        };

        _connection = factory.CreateConnection("InventoryService-Consumer");
        _channel    = _connection.CreateModel();

        // Declare exchange (idempotent — matches what OrderService declared)
        _channel.ExchangeDeclare(
            exchange:   RabbitMqSettings.ExchangeName,
            type:       ExchangeType.Direct,
            durable:    true,
            autoDelete: false);

        // Declare durable queue
        _channel.QueueDeclare(
            queue:      RabbitMqSettings.QueueName,
            durable:    true,
            exclusive:  false,
            autoDelete: false,
            arguments:  null);

        // Bind queue → exchange
        _channel.QueueBind(
            queue:      RabbitMqSettings.QueueName,
            exchange:   RabbitMqSettings.ExchangeName,
            routingKey: RabbitMqSettings.RoutingKey);

        // Process one message at a time — do not pre-fetch more than 1
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;

        _channel.BasicConsume(
            queue:       RabbitMqSettings.QueueName,
            autoAck:     false,      // manual ack — ack only after successful processing
            consumer:    consumer);

        _logger.LogInformation(
            "RabbitMqOrderConsumer listening on queue '{Queue}'.",
            RabbitMqSettings.QueueName);
    }

    // ── Message handler ────────────────────────────────────────────────────

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var deliveryTag = ea.DeliveryTag;

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            _logger.LogInformation(
                "Received message from RabbitMQ on queue '{Queue}'.",
                RabbitMqSettings.QueueName);

            var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (orderEvent is null)
            {
                _logger.LogWarning("Received null/unparseable OrderCreatedEvent. Discarding.");
                _channel!.BasicNack(deliveryTag, multiple: false, requeue: false);
                return;
            }

            // Use a fresh DI scope — consumer is Singleton, service is Scoped
            using var scope = _scopeFactory.CreateScope();
            var inventoryService = scope.ServiceProvider
                .GetRequiredService<IInventoryService>();

            await inventoryService.ProcessSyncedOrderAsync(orderEvent);

            // Acknowledge — remove from queue
            _channel!.BasicAck(deliveryTag, multiple: false);

            _logger.LogInformation(
                "Order {OrderId} processed successfully from RabbitMQ.",
                orderEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RabbitMQ message. Discarding (no requeue).");
            // Nack without requeue to avoid poison-message loops
            _channel?.BasicNack(deliveryTag, multiple: false, requeue: false);
        }
    }
}
