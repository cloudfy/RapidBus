using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RapidBus.Consumer;

internal sealed class RapidBusConsumer : BackgroundService
{
    private readonly ILogger<RapidBusConsumer> _logger;

    public RapidBusConsumer(ILogger<RapidBusConsumer> logger) 
    {
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested == false)
        {
            _logger.LogInformation("check");
            // do something
            cancellationToken.WaitHandle.WaitOne(1000);
        }

        return Task.CompletedTask;
    }
}