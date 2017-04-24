using Miinto.Bus.Core;
using StackExchange.Redis;
using System;
using System.Threading;

namespace Miinto.ProductCentre.LocationStatus.Worker
{
    class Program
    {
        static void Main(string[] args)
        {
            var settings = new BusSettings()
            {
                HostName="localhost",
                VirtualHost="/",
                UserName="guest",
                Password="guest"
            };
            using (var redis = ConnectionMultiplexer.Connect("localhost"))
            using (var bus = new ServiceBus(settings))
            using (RunAndWait.WithMessage())
            {
                bus.Consume<MessageEnvelope<ProductCreationRequestProcessed>>("processed-pcr.generic", (model, message)=>
                {
                    var db = redis.GetDatabase();
                    var fieldName = message.Message.WithSuccess ? "success" : "fail";
                    db.HashIncrement($"location:{message.Message.LocationId}:{message.Message.SessionId}", fieldName, 1);
                    db.SetAdd($"location", $"{message.Message.LocationId}:{message.Message.SessionId}");
                    db.SortedSetAdd($"location:sessionDate", $"{message.Message.LocationId}:{message.Message.SessionId}", (message.Message.SessionDate-new DateTime(2016,1,1)).TotalSeconds);
                });
            }
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
    public class RunAndWait : IDisposable
    {
        public static RunAndWait WithMessage(string message = "Press [Ctrl+C] to exit.") => new RunAndWait(message);
        ManualResetEvent exitEvent;
        private string message;

        public RunAndWait(string message)
        {
            this.message = message;
            exitEvent = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };
        }
        public void Dispose()
        {
            Console.WriteLine(this.message);
            exitEvent.WaitOne();
        }
    }
}