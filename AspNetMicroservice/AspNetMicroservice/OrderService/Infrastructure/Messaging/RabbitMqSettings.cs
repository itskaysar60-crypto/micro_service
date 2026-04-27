namespace OrderService.Infrastructure.Messaging;

/// <summary>
/// Strongly-typed binding for the "RabbitMq" section in appsettings.json.
/// </summary>
public sealed class RabbitMqSettings
{
    public string Host        { get; set; } = "localhost";
    public int    Port        { get; set; } = 5672;
    public string Username    { get; set; } = "guest";
    public string Password    { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    // Exchange / routing key constants shared by publisher & consumer
    public const string ExchangeName = "order.created";
    public const string RoutingKey   = "order.created";
}
