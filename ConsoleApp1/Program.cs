using Miinto.Bus.Core;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var settings = new BusSettings()
            {
                HostName = "localhost",
                VirtualHost = "/",
                UserName = "guest",
                Password = "guest"
            };
            using (var bus = new ServiceBus(settings))
            {
                var date = DateTime.Now;
                var sessionId = Guid.NewGuid();
                var locationId = Guid.NewGuid();
                Random rnd = new Random();
                for (int i = 0; i < 1000; i++)
                {
                    bus.Send("processed-pcr.generic", new MessageEnvelope<ProductCreationRequestProcessed>
                    {
                        Message = new ProductCreationRequestProcessed
                        {
                            SessionDate = date,
                            LocationId = locationId,
                            SessionId = sessionId,
                            WithSuccess = (rnd.Next(10) % 2) == 0 ? false : true
                        }
                    });
                }
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
    }
}