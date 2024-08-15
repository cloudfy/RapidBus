using RabidBus.Abstractions;

namespace RapidBus.Sample.Event;

public class SampleEventHandler : IIntegrationEventHandler<SampleEvent>
{
    public Task Handle(SampleEvent integrationEvent, CancellationToken cancellationToken)
    {
        Console.WriteLine(integrationEvent.Message);

        return Task.CompletedTask;
    }
}
