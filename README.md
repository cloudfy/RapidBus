# RapidBus

## Lightwight setup
```csharp
var builder = Host.CreateApplicationBuilder();
builder.Services.AddRapidBus(busBuilder =>
{
    busBuilder
        .UseRabbitMQ("..", "exchange", "queue");
});
var host = builder.Build();
await host.RunAsync();

...

// in application code use

var bus = Services.GetRequiredService<IRapidBus>();
bus.Publish(new SampleEvent());

```