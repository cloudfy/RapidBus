using Azure.Storage.Queues;
using RabidBus.Abstractions;
using System.Text.Json;
using System.Text;
using Azure;

namespace RapidBus.AzureStorageQueues;

internal sealed class AzureQueueBus : IRapidBus
{
    private readonly QueueServiceClient _queueClient;
    private readonly string _queueName = "example";

    internal AzureQueueBus(QueueServiceClient queueClient)
    {
        _queueClient = queueClient;
    }

    public void Publish<TEvent>(TEvent @event) where TEvent : IIntegrationEvent
    {
        var queueClient = _queueClient.GetQueueClient(_queueName);
        
        var message = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(message);
        try
        {
            var resp = queueClient.SendMessage(new BinaryData(body));
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "QueueNotFound")
        {
            _queueClient.CreateQueue(_queueName);
            var resp = queueClient.SendMessage(new BinaryData(body));
        }
    }
}
