using System;
using RabbitMQ.Client;

namespace Miinto.Bus.Core
{
    public interface IBus
    {
        void Consume<T>(string queueName, Action<IModel, T> handler);
        void Dispose();
        void Publish<T>(string exchange, T message);
        void Send<T>(string queueName, T message);
        void Subscribe<T>(string exchange, Action<IModel, T> handler);
    }
}