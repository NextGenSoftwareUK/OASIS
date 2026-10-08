using EosSharp;
using EosSharp.Core;
using EosSharp.Core.Interfaces;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient
{
    internal sealed class CompatibleEosClient : EosBase
    {
        public CompatibleEosClient(EosConfigurator configurator)
            : base(configurator, new CompatibleHttpHandler())
        {
        }
    }

    internal sealed class CompatibleHttpHandler : IHttpHandler
    {
        private readonly HttpHandler _inner = new HttpHandler();

        public void ClearResponseCache() => _inner.ClearResponseCache();
        public Task<TResponseData> PostJsonAsync<TResponseData>(string url, object data) => _inner.PostJsonAsync<TResponseData>(url, data);
        public Task<TResponseData> PostJsonAsync<TResponseData>(string url, object data, CancellationToken cancellationToken) => _inner.PostJsonAsync<TResponseData>(url, data, cancellationToken);
        public Task<TResponseData> PostJsonWithCacheAsync<TResponseData>(string url, object data, bool reload) => PostJsonWithCacheAsync<TResponseData>(url, data, CancellationToken.None, reload);

        public async Task<TResponseData> PostJsonWithCacheAsync<TResponseData>(string url, object data, CancellationToken cancellationToken, bool reload)
        {
            string cacheKey = GetRequestHashKey(url, data);
            TResponseData response = await _inner.PostJsonAsync<TResponseData>(url, data, cancellationToken);
            _inner.UpdateResponseDataCache(cacheKey, response);
            return response;
        }

        public Task<TResponseData> GetJsonAsync<TResponseData>(string url) => _inner.GetJsonAsync<TResponseData>(url);
        public Task<TResponseData> GetJsonAsync<TResponseData>(string url, CancellationToken cancellationToken) => _inner.GetJsonAsync<TResponseData>(url, cancellationToken);
        public Task<Stream> SendAsync(HttpRequestMessage request) => _inner.SendAsync(request);
        public Task<Stream> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => _inner.SendAsync(request, cancellationToken);
        public void UpdateResponseDataCache<TResponseData>(string key, TResponseData data) => _inner.UpdateResponseDataCache(key, data);

        public string GetRequestHashKey(string url, object data)
        {
            string canonicalRequest = $"{url}\n{JsonConvert.SerializeObject(data)}";
            return System.Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));
        }

        public Task<Stream> BuildSendResponse(HttpResponseMessage response) => _inner.BuildSendResponse(response);
        public Task<string> StreamToStringAsync(Stream stream) => _inner.StreamToStringAsync(stream);
    }
}
