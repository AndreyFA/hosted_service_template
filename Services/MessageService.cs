using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace hosted_service_rabbit.Services
{
    public class MessageService : IMessageService
    {
        private readonly ILogger _logger;
        private readonly IRabbitService _rabbitService;

        public MessageService(ILogger<MessageService> logger,
            IRabbitService rabbitService)
        {
            this._logger = logger;
            this._rabbitService = rabbitService;
        }

        private void CreateQueue(string queueName)
        {
            string deadLetterQueueName = $"{queueName}.dead.letter";
            string deadLetterExchangeName = $"exchange.{deadLetterQueueName}";

            this._rabbitService.Channel.ExchangeDeclare(deadLetterExchangeName, ExchangeType.Fanout);
            this._rabbitService.Channel.QueueDeclare(deadLetterQueueName, true, false, false);
            this._rabbitService.Channel.QueueBind(deadLetterQueueName, deadLetterExchangeName, deadLetterQueueName);

            var args = new Dictionary<string, object>();
            args.Add("x-dead-letter-exchange", deadLetterExchangeName);

            string messageExchangeName = $"exchange.{queueName}";

            this._rabbitService.Channel.ExchangeDeclare(messageExchangeName, ExchangeType.Direct);
            this._rabbitService.Channel.QueueDeclare(queueName, true, false, false, args);
            this._rabbitService.Channel.QueueBind(queueName, messageExchangeName, queueName);
        }

        private int GetNumberOfAttempts(BasicDeliverEventArgs args)
        {
            return args.BasicProperties.Headers.TryGetValue("NumberOfAttempts", out object numberOfAttempts)
                ? Convert.ToInt32(numberOfAttempts)
                : 1;
        }

        private int GetMaxNumberOfAttempts(BasicDeliverEventArgs args)
        {
            return args.BasicProperties.Headers.TryGetValue("MaxNumberOfAttempts", out object numberOfAttempts)
                ? Convert.ToInt32(numberOfAttempts)
                : 1;
        }

        private int GetIntervalBetweenAttempts(BasicDeliverEventArgs args)
        {
            return args.BasicProperties.Headers.TryGetValue("IntervalBetweenAttempts", out object intervalBetweenAttempts)
                ? Convert.ToInt32(intervalBetweenAttempts)
                : 30;
        }

        public void CreateConsumer<TEntity>(string queue, int retryInMilliseconds, Func<TEntity, Task> method)
        {
            var consumer = new EventingBasicConsumer(this._rabbitService.Channel);
            this.CreateQueue(queue);

            consumer.Received += async (model, e) =>
            {
                var message = Encoding.UTF8.GetString(e.Body);
                var tryUntil = DateTime.Now.AddMilliseconds(-1);

                if (!Equals(e.BasicProperties.Headers, null) && e.BasicProperties.Headers.ContainsKey("try-util"))
                {
                    var tryUntilReceived = Encoding.Default.GetString((byte[])e.BasicProperties.Headers["try-util"]);
                    tryUntil = DateTime.ParseExact(tryUntilReceived, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                }

                var entity = JsonConvert.DeserializeObject<TEntity>(message);

                try
                {
                    await method(entity);
                    this._rabbitService.Channel.BasicAck(e.DeliveryTag, false);
                }
                catch (Exception exception)
                {
                    this._logger.LogError(exception.Message, exception);
                    e.BasicProperties.Headers.Add("Error", exception);

                    // var maxNumberOfAttempts = this.GetMaxNumberOfAttempts(e);
                    // var numberOfAttempts = this.GetNumberOfAttempts(e);

                    // if (numberOfAttempts == maxNumberOfAttempts)
                    // {
                    //     this._rabbitService.Channel.BasicNack(e.DeliveryTag, false, false);
                    //     return;
                    // }
                    // else
                    // {

                    // }

                    if (DateTime.Now > tryUntil)
                    {
                        this._rabbitService.Channel.BasicNack(e.DeliveryTag, false, false);
                    }
                    else
                    {
                        new Task(() =>
                        {
                            Task.Delay(retryInMilliseconds, CancellationToken.None);
                            this._rabbitService.Channel.BasicNack(e.DeliveryTag, false, true);
                        });
                    }
                }
            };

            this._rabbitService.Channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);
        }
    }
}