using System;
using System.Threading.Tasks;

namespace hosted_service_rabbit.Services
{
    public interface IMessageService
    {
        void CreateConsumer<TEntity>(string queue, int retryInSeconds, Func<TEntity, Task> method);
    }
}