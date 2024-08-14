using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RapidBus.RabbitMQ;

public static class StartupExtensions
{
    public static RapidBusOptions UseRabbitMQ(this RapidBusOptions options, string connectionString)
    {
        //options.AddIntegrationEventBus<RabbitMQIntegrationEventBus>();
        //options.Services.Configure<RabbitMQOptions>(options =>
        //{
        //    options.ConnectionString = connectionString;
        //});
        return options;
    }
}
