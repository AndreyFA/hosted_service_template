using System.Threading;
using System.Threading.Tasks;
using hosted_service_rabbit.Models;
using hosted_service_rabbit.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Refit;

namespace hosted_service_rabbit.Consumers
{
    public class SampleConsumer : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IMessageService _messageService;

        public SampleConsumer(ILogger<SampleConsumer> logger,
            IMessageService messageService)
        {
            this._logger = logger;
            this._messageService = messageService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("runnig...");

            this._messageService.CreateConsumer<Model>("produtos_carga", 60000, async (sampleModel) =>
            {
                var service = RestService.For<IDestinationApi>("http://localhost:5555/api/values");
                await service.Post(sampleModel);
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("stoped...");
            return Task.CompletedTask;
        }
    }
}