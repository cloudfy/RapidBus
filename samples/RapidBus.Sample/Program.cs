using Microsoft.Extensions.Hosting;

using RapidBus;
using RapidBus.RabbitMQ;

var builder = Host.CreateApplicationBuilder();
builder.Services.AddRapidBus(builder => {
    builder.UseRabbitMQ("amqp://guest:guest@localhost:5672");
});

var host = builder.Build();
await host.RunAsync();
