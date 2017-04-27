using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Miinto.Bus.Core;
using Miinto.ProductCentre.LocationStatus.Worker.Messaging;
using RabbitMQ.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Miinto.ProductCentre.LocationStatus.Worker
{
    public class Worker
    {
        public void Start()
        {
            try
            {
                const string queueName = "processed-pcr.generic";
                const string exchangeName = "Miinto.ProductCentre.Context.Product.Events:ProductCreationRequestProcessed";

                var numberOfConsumers = WorkerConfiguration.Config.GetValue<uint>("Worker:NumberOfConsumers", 1);
                var busConfig = WorkerConfiguration.Config.GetSection("BusSettings");
                var settings = new BusSettings()
                {
                    HostName = busConfig.GetValue<string>("HostName"),
                    VirtualHost = busConfig.GetValue<string>("VirtualHost"),
                    UserName = busConfig.GetValue<string>("UserName"),
                    Password = busConfig.GetValue<string>("Password"),
                    PrefetchCount = busConfig.GetValue<ushort>("PrefetchCount")
                };

                Log.Logger.Information($"Starting consuming events with {numberOfConsumers} consumers, with buffer size: {settings.PrefetchCount}");

                using (var bus = new ServiceBus(settings))
                using (RunAndWait.WithMessage())
                {
                    bus.SetupExchangeWithQueue(exchangeName, queueName);
                    for (int i = 0; i < numberOfConsumers; i++)
                    {
                        var consumer = new Consumer { Id = i + 1 };
                        bus.ConsumeBuffered<MessageEnvelope<ProductCreationRequestProcessed>>(queueName, TimeSpan.FromSeconds(10), consumer.Consume);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Worker unhandled exception occured.");
                throw;
            }
            
        }

        class Consumer
        {
            public int Id { get; set; }

            public void Consume(IModel model, IList<MessageEnvelope<ProductCreationRequestProcessed>> messages)
            {
                Log.Logger.Debug($"Consumer {Id}: consuming {messages.Count} messages");
                var aggregatedEvents = messages.Select(m => m.Message).ToList();
                new LocationStatusUpdater().HandleEvent(aggregatedEvents);
                Log.Logger.Debug($"Consumer {Id}: finished");
            }
        }
    }
}
