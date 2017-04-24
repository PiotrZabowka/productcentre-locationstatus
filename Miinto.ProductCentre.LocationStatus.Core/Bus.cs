using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace Miinto.Bus.Core
{
    public class ServiceBus : IDisposable, IBus
    {

        IConnection conn;
        IModel channel;

        public ServiceBus(BusSettings options)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.UserName = options.UserName;
            factory.Password = options.Password;
            factory.VirtualHost = options.VirtualHost;
            factory.HostName = options.HostName;

            this.conn = factory.CreateConnection();
            this.channel = conn.CreateModel();
            this.channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        }

        public void Send<T>(string queueName, T message)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            var basicProperties = this.channel.CreateBasicProperties();
            basicProperties.Persistent = true;
            channel.BasicPublish("", queueName, basicProperties: basicProperties, body: bytes);
        }

        public void Publish<T>(string exchange, T message)
        {
            this.channel.ExchangeDeclare(exchange, type: ExchangeType.Fanout);
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            channel.BasicPublish(exchange, "", basicProperties: null, body: bytes);
        }

        public void Consume<T>(string queueName, Action<IModel, T> handler)
        {
            this.channel.QueueDeclare(queueName, true, false, false, null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (m, ea) =>
            {
                try
                {
                    var body = ea.Body;
                    var message = Encoding.UTF8.GetString(body);
                    var feedUrlMessage = JsonConvert.DeserializeObject<T>(message);
                    handler(this.channel, feedUrlMessage);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {

                }
            };
            channel.BasicConsume(queue: queueName,
                                 noAck: false,
                                 consumer: consumer);
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
            channel.Dispose();
            conn.Dispose();
        }
    }
}
