namespace RapidBus.AzureStorageQueues;

public static class StartupExtensions
{
    public static RapidBusOptions UseAzureStorageQueues(this RapidBusOptions options)
    {
        return options;
    }
}
