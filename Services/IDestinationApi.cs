using System.Threading.Tasks;
using hosted_service_rabbit.Models;
using Refit;

namespace hosted_service_rabbit.Services
{
    public interface IDestinationApi
    {
        [Post("/api/clientes")]
        Task Post(Model model);
    }
}