using Miinto.Bus.Core;
using System;
using System.Threading;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            const string queueName = "processed-pcr.generic";
            const string exchangeName = "Miinto.ProductCentre.Context.Product.Events:ProductCreationRequestProcessed";

            var settings = new BusSettings()
            {
                //HostName = "10.200.36.54",
                HostName = "localhost",
                VirtualHost = "product_centre",
                UserName = "product_center_worker",
                Password = "pr0duct_c3nt3r_w0rk3r",
            };
            using (var bus = new ServiceBus(settings))
            {
                bus.SetupExchangeWithQueue(exchangeName, queueName);
                var date = DateTime.Now;
                var sessionId = Guid.NewGuid();
                var locationId = Guid.Parse("6590ca1e-b1f9-4b8b-8277-466962c9a2a2");
                //var locationId = Guid.Parse("737e6f34-8880-4413-99c5-a75500ea0e83");
                Random rnd = new Random();
                var messagesToSend = 1003;
                for (int i = 1; i <= messagesToSend; i++)
                {
                    bus.Publish(exchangeName, new MessageEnvelope<ProductCreationRequestProcessed>
                    {
                        Message = new ProductCreationRequestProcessed
                        {
                            SessionDate = date,
                            LocationId = locationId,
                            SessionId = sessionId,
                            WithSuccess = (i % 2) == 0 ? false : true,
                            LocationStatusNotChanged = false
                        }
                    });
                    if ((i % 100) == 0)
                        Console.WriteLine($"Sent {i} messages");
                }

                Console.WriteLine($"Finished sending {messagesToSend} messages.");
            }

            Console.ReadLine();
        }
    }


    public class MessageEnvelope<T>
    {
        public T Message { get; set; }
    }
    public class ProductCreationRequestProcessed
    {
        public Guid SessionId { get; set; }
        public DateTime SessionDate { get; set; }
        public Guid LocationId { get; set; }
        public bool WithSuccess { get; set; }
        public bool LocationStatusNotChanged { get; set; }
    }
}