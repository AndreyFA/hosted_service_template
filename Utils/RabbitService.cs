using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace hosted_service_rabbit.Services
{
    public class RabbitService : IRabbitService
    {
        private readonly IConfiguration _configuration;
        private IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;

        public RabbitService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IConnectionFactory GetConnectionFactory()
        {
            if (Equals(this._connectionFactory, null))
            {
                this._connectionFactory = new ConnectionFactory()
                {
                    HostName = this._configuration["RabbitMQ:Hostname"],
                    UserName = this._configuration["RabbitMQ:UserName"],
                    Password = this._configuration["RabbitMQ:Password"]
                };
            }

            return this._connectionFactory;
        }

        private IConnection GetConnection()
        {
            if (Equals(this._connection, null))
            {
                this._connection = this.GetConnectionFactory().CreateConnection();
            }

            return this._connection;
        }

        public IModel Channel
        {
            get
            {
                if (Equals(this._channel, null) || this._channel.IsClosed)
                {
                    this._channel = this.GetConnection().CreateModel();
                }

                return this._channel;
            }
        }

        public void Dispose()
        {
            this._channel.Dispose();
            this._connection.Dispose();
        }
    }
}