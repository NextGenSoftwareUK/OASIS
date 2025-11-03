using System.Net.Http;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler
{
    public interface IHttpHandler
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage);
    }
}