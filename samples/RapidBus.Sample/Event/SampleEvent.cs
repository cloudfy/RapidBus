using RabidBus.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RapidBus.Sample.Event;

public class SampleEvent : IIntegrationEvent
{
    public string Messagge = "Hello, World!";
}
public class SampleEventHandler : IIntegrationEventHandler<SampleEvent>
{
    public Task Handle(SampleEvent integrationEvent, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
