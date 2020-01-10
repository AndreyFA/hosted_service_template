using System;
using RabbitMQ.Client;

namespace hosted_service_rabbit.Services
{
    public interface IRabbitService: IDisposable
    {
        IModel Channel { get; }
    }
}