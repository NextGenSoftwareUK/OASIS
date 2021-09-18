using System.Net.Http;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Services.HttpHandler
{
    public class HttpHandler : IHttpHandler
    {
        public HttpHandler(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private readonly HttpClient _httpClient;

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage)
        {
            return await _httpClient.SendAsync(requestMessage);
        }
    }
}