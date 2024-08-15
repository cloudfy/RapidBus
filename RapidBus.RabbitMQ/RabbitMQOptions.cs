
namespace RapidBus.RabbitMQ;

public class RabbitMQOptions
{
    public string ConnectionString { get; init; } = null!;
    public string? QueueName { get; init; }
}