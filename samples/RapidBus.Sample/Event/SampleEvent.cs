using RabidBus.Abstractions;

namespace RapidBus.Sample.Event;

[IntegrationEvent("SampleEvent")]
public class SampleEvent : IIntegrationEvent
{
    public string Message = "Hello, World!";
}
