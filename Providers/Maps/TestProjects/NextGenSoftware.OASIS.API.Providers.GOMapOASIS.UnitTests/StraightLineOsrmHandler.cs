using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.UnitTests
{
    /// <summary>
    /// Answers OSRM route requests offline with the two requested endpoints as the
    /// route geometry, so provider tests that draw routes never reach the network.
    /// </summary>
    public sealed class StraightLineOsrmHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            string path = request.RequestUri!.AbsolutePath;
            string[] points = Uri.UnescapeDataString(path.Substring(path.LastIndexOf('/') + 1)).Split(';');
            string coordinates = string.Join(",", points.Select(p => "[" + p + "]"));
            string body = "{\"code\":\"Ok\",\"routes\":[{\"distance\":1000,\"duration\":120,"
                        + "\"geometry\":{\"type\":\"LineString\",\"coordinates\":[" + coordinates + "]}}]}";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
