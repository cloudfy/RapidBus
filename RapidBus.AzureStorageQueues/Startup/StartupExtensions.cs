using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace RapidBus.AzureStorageQueues;

public static class StartupExtensions
{
    public static RapidBusBuilder UseAzureStorageQueues(
        this RapidBusBuilder builder
        , string connectionString)
    {
        // add dependency services
        builder.Services.AddAzureClients(builder => {
            builder.AddQueueServiceClient(connectionString).WithName("rapidbus");
        });

        // add the bus
        builder.SetFactory((sp) => {
            var azureClientFactory = sp.GetRequiredService<IAzureClientFactory<QueueServiceClient>>();
            var queueClient = azureClientFactory.CreateClient("rapidbus");

            return new AzureQueueBus(queueClient); 
        });
        return builder;
    }
}