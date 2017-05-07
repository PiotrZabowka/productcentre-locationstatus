using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using System.Threading;

namespace Miinto.Bus.Core
{
    public class ServiceBus : IDisposable, IBus
    {
        private readonly BusSettings busSettings;
        IConnection conn;
        IModel channel;

        CountdownEvent exitEvent = null;
        int backgroundTasks = 0;
        object locker = new object();


        public ServiceBus(BusSettings options)
        {
            this.busSettings = options;
            ConnectionFactory factory = new ConnectionFactory();
            factory.UserName = options.UserName;
            factory.Password = options.Password;
            factory.VirtualHost = options.VirtualHost;
            factory.HostName = options.HostName;
            
            this.conn = factory.CreateConnection();
            this.channel = conn.CreateModel();
            this.channel.BasicQos(prefetchSize: 0, prefetchCount: options.PrefetchCount, global: false);

        }

        public void SetupQueue(string queueName)
        {
            this.channel.QueueDeclare(queueName, true, false, false, null);
        }

        public void SetupExchange(string exchangeName)
        {
            this.channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout, true);
        }

        public void SetupExchangeWithQueue(string exchangeName, string queueName)
        {
            this.channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout, true);
            this.channel.QueueBind(queue: queueName,
                                 exchange: exchangeName,
                                 routingKey: "");
        }


        public void Send<T>(string queueName, T message)
        {
            PerformSend(queueName, message);
        }

        private void PerformSend<T>(string queueName, T message, string errorMessage = null)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            var basicProperties = this.channel.CreateBasicProperties();
            basicProperties.Persistent = true;
            if (errorMessage != null)
            {
                basicProperties.Headers = new Dictionary<string, object>();
                basicProperties.Headers.Add("ErrorMessage", errorMessage);
            }
            channel.BasicPublish("", queueName, basicProperties: basicProperties, body: bytes);
        }
        public void Publish<T>(string exchange, T message)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            channel.BasicPublish(exchange, "", basicProperties: null, body: bytes);
        }

        public void Consume<T>(string queueName, Action<IModel, T> handler)
        {
            SetupQueue(queueName);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (m, ea) =>
            {
                T feedUrlMessage = default(T);

                try
                {
                    feedUrlMessage = CreateMessage<T>(ea);
                    handler(this.channel, feedUrlMessage);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Error when consuming message.");
                    if (feedUrlMessage != null)
                    {
                        this.channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                        SendMessagesToErrorQueue(queueName, new List<T> { feedUrlMessage }, ex.ToString());
                    }
                }
            };
            channel.BasicConsume(queue: queueName,
                                 noAck: false,
                                 consumer: consumer);
        }


        private static T CreateMessage<T>(BasicDeliverEventArgs ea)
        {
            var body = ea.Body;
            var message = Encoding.UTF8.GetString(body);
            var feedUrlMessage = JsonConvert.DeserializeObject<T>(message);
            return feedUrlMessage;
        }

        public void ConsumeBuffered<T>(string queueName, TimeSpan timeSpan,  Action<IModel, IList<T>> handler)
        {
            try
            {
                SetupQueue(queueName);
                var consumer = new EventingBasicConsumer(channel);

                //IObservable<EventPattern<BasicDeliverEventArgs>> messages = Observable.FromEventPattern<BasicDeliverEventArgs>(consumer, "Received");
                IObservable<BasicDeliverEventArgs> messages = Observable.Create<BasicDeliverEventArgs>(observer =>
                {
                    consumer.Received += (m, ea) =>
                    {
                        observer.OnNext(ea);
                    };
                    return () => { };
                });

                messages.Buffer(timeSpan, busSettings.PrefetchCount)
                        .Where(x => x.Count > 0)
                        .Subscribe(
                            (evList) =>
                            {
                                Task.Run(() => ProcessBufferedMessages(queueName, handler, evList));
                            });

                channel.BasicConsume(queue: queueName,
                                     noAck: false,
                                     consumer: consumer);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Bus.ConsumeBuffered unhandled exception.");
                throw;
            }

        }

        private void ProcessBufferedMessages<T>(string queueName, Action<IModel, IList<T>> handler, IList<BasicDeliverEventArgs> evList)
        {
            lock (locker)
            {
                if (exitEvent != null)
                    return;
                backgroundTasks++;
            }

            IList<T> messagePack = null;

            try
            {
                messagePack = evList.Select(ev => CreateMessage<T>(ev)).ToList();
                handler(this.channel, messagePack);
                foreach (var ev in evList)
                {
                    this.channel.BasicAck(ev.DeliveryTag, multiple: false);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error when consuming buffered messages.");
                if (messagePack != null)
                {
                    SendMessagesToErrorQueue(queueName, messagePack, ex.ToString());
                    foreach (var ev in evList)
                    {
                        this.channel.BasicNack(ev.DeliveryTag, multiple: false, requeue: false);
                    }
                }
            }
            finally
            {
                lock (locker)
                {
                    backgroundTasks--;
                    if (exitEvent != null)
                    {
                        exitEvent.Signal();
                    }
                }
            }
        }


        private void SendMessagesToErrorQueue<T>(string originQueueName, IList<T> messages, string errorMessage)
        {
            var errorQueue = $"{originQueueName}.error";
            SetupQueue(errorQueue);

            foreach (var em in messages)
            {
                PerformSend(errorQueue, em, errorMessage);
            }
        }

        public void Subscribe<T>(string exchange, Action<IModel, T> handler)
        {
            var queueName = this.channel.QueueDeclare().QueueName;
            this.channel.ExchangeDeclare(exchange, type: ExchangeType.Fanout);
            this.channel.QueueBind(queue: queueName,
                                 exchange: exchange,
                                 routingKey: "");

            var consumer = new EventingBasicConsumer(channel);
            
            consumer.Received += (m, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                var feedUrlMessage = JsonConvert.DeserializeObject<T>(message);
                handler(this.channel, feedUrlMessage);
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };
            channel.BasicConsume(queue: queueName,
                                 noAck: false,
                                 consumer: consumer);
        }

        public void Dispose()
        {
            Log.Logger.Debug("Bus dispose started.");
            lock (locker)
            {
                exitEvent = new CountdownEvent(backgroundTasks);
            }
            exitEvent.Wait();
            exitEvent.Dispose();
            channel.Dispose();
            conn.Dispose();
            Log.Logger.Debug("Bus dispose finished.");
        }
    }
}
