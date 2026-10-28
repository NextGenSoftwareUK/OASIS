using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests
{
    /// <summary>The SOLID reader reading the caller's own Pod instead of the configured one.</summary>
    [Collection("SolidNativeConnectionHandler")]
    public class SolidNativeConnectionTests
    {
        private const string Pod = "https://alice.pod.example";

        private sealed class AlicesPod : HttpMessageHandler
        {
            public readonly List<(string Url, string Auth)> Requests = new List<(string, string)>();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var url = request.RequestUri.AbsoluteUri;
                Requests.Add((url, request.Headers.Authorization?.Parameter));
                var body = url switch
                {
                    Pod + "/" => "",
                    Pod + "/notes/" => $"@prefix ldp: <http://www.w3.org/ns/ldp#> . <{Pod}/notes/> ldp:contains <{Pod}/notes/one.ttl> .",
                    Pod + "/notes/one.ttl" => $"@prefix dc: <http://purl.org/dc/terms/> . <{Pod}/notes/one.ttl> dc:title \"First note\" .",
                    _ => null
                };
                if (body == null) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes(body)) };
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/turtle");
                return Task.FromResult(response);
            }
        }

        private static NativeConnection Connection(string podUrl = Pod + "/", string containers = "notes", string token = "pod-token")
        {
            var connection = new NativeConnection { ["podUrl"] = podUrl, ["containers"] = containers };
            if (token != null) connection["token"] = token;
            return connection;
        }

        private static SOLIDOASIS Configured() => new SOLIDOASIS("https://configured.example", "", null, new AlicesPod());

        [Fact]
        public async Task ReadsTheCallersPodWithTheCallersToken()
        {
            var original = SOLIDOASIS.NativeConnectionHandlerFactory;
            var pod = new AlicesPod();
            SOLIDOASIS.NativeConnectionHandlerFactory = () => pod;
            try
            {
                var provider = Configured();
                var sources = await provider.ListNativeDataSourcesAsync(Connection());
                Assert.False(sources.IsError, sources.Message);
                Assert.Equal("notes/", sources.Result.Single().Name);

                var page = await provider.ReadNativeRecordsAsync("notes/", null, 5, Connection());
                Assert.False(page.IsError, page.Message);
                var record = page.Result.Records.Single();
                Assert.Equal("notes/one.ttl", record.NativeKey);
                Assert.Equal("First note", record.Fields["title"]);
                Assert.All(pod.Requests, request => Assert.Equal("pod-token", request.Auth));
                Assert.All(pod.Requests, request => Assert.StartsWith(Pod, request.Url));
            }
            finally { SOLIDOASIS.NativeConnectionHandlerFactory = original; }
        }

        [Fact]
        public void IdentityIsTheHostAndPodPathNeverTheToken()
        {
            Assert.Equal("solid://alice.pod.example", Configured().DescribeNativeConnection(Connection()));
            Assert.Equal("solid://pod.example/alice", Configured().DescribeNativeConnection(Connection("https://Pod.Example/alice/")));
            Assert.DoesNotContain("pod-token", Configured().DescribeNativeConnection(Connection()));
            Assert.Null(Configured().DescribeNativeConnection(null));
        }

        [Fact]
        public async Task BadConnectionsAreRefusedBeforeAnyRequest()
        {
            var original = SOLIDOASIS.NativeConnectionHandlerFactory;
            var pod = new AlicesPod();
            SOLIDOASIS.NativeConnectionHandlerFactory = () => pod;
            try
            {
                var provider = Configured();
                Assert.Contains("https", (await provider.ListNativeDataSourcesAsync(Connection("http://alice.pod.example/"))).Message);
                Assert.Contains("https", (await provider.ListNativeDataSourcesAsync(Connection("https://user:pw@alice.pod.example/"))).Message);
                Assert.Contains("containers", (await provider.ListNativeDataSourcesAsync(new NativeConnection { ["podUrl"] = Pod + "/" })).Message);
                Assert.Contains("contaners", (await provider.ListNativeDataSourcesAsync(new NativeConnection { ["podUrl"] = Pod + "/", ["contaners"] = "x" })).Message);
                var own = await provider.ListNativeDataSourcesAsync(Connection(containers: "oasis/holons"));
                Assert.True(own.IsError);
                Assert.Empty(pod.Requests);
            }
            finally { SOLIDOASIS.NativeConnectionHandlerFactory = original; }
        }

        [Fact]
        public async Task TheDefaultHandlerRefusesInternalPodsWithTheRealReason()
        {
            var result = await Configured().ListNativeDataSourcesAsync(Connection("https://localhost/"));
            Assert.True(result.IsError);
            Assert.Contains("internal name", result.Message);
        }
    }

    [CollectionDefinition("SolidNativeConnectionHandler", DisableParallelization = true)]
    public class SolidNativeConnectionHandlerCollection { }
}
