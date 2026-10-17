using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests
{
    /// <summary>In-process stand-in for a bridge API: routes each request to a handler and records it.</summary>
    internal sealed class FakeApi : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string, HttpResponseMessage> _route;
        public readonly List<(HttpRequestMessage Request, string Body)> Requests = new();

        public FakeApi(Func<HttpRequestMessage, string, HttpResponseMessage> route) => _route = route;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request, body));
            return _route(request, body);
        }

        public static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        public static HttpResponseMessage Status(HttpStatusCode code, string body = "") => new(code) { Content = new StringContent(body) };
        public static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
    }
}
