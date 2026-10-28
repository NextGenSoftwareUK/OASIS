using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests
{
    /// <summary>The IPFS reader listing directories through the caller's own node.</summary>
    public class IPFSNativeConnectionTests
    {
        private sealed class Node : HttpMessageHandler
        {
            public readonly List<(HttpMethod Method, string Url)> Requests = new List<(HttpMethod, string)>();
            public HttpStatusCode Status = HttpStatusCode.OK;
            public string Body = "{\"Objects\":[{\"Hash\":\"QmDir\",\"Links\":[{\"Name\":\"b.txt\",\"Hash\":\"QmB\",\"Size\":2,\"Type\":2},{\"Name\":\"a.txt\",\"Hash\":\"QmA\",\"Size\":1,\"Type\":2}]}]}";

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add((request.Method, request.RequestUri.AbsoluteUri));
                return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(Body, Encoding.UTF8, "application/json") });
            }
        }

        private static NativeConnection Connection(string api = "https://ipfs.example.com:5001/", string paths = "/ipfs/QmDir") =>
            new NativeConnection { ["apiUrl"] = api, ["paths"] = paths };

        private static IPFSOASIS Provider(Node node) =>
            new IPFSOASIS(new NextGenSoftware.OASIS.API.DNA.OASISDNA()) { NativeConnectionHandler = () => node };

        [Fact]
        public async Task ListsTheCallersDirectoryThroughTheCallersNodeWithPostAndPagesByName()
        {
            var node = new Node();
            var provider = Provider(node);

            var sources = await provider.ListNativeDataSourcesAsync(Connection());
            Assert.False(sources.IsError, sources.Message);
            Assert.Equal(2, sources.Result.Single().EstimatedRecordCount);

            var first = await provider.ReadNativeRecordsAsync("/ipfs/QmDir", null, 1, Connection());
            Assert.False(first.IsError, first.Message);
            Assert.Equal("a.txt", first.Result.Records.Single().NativeKey);
            Assert.Equal("QmA", first.Result.Records.Single().Fields["cid"]);
            Assert.Equal("a.txt", first.Result.ContinuationToken);

            Assert.All(node.Requests, request => Assert.Equal(HttpMethod.Post, request.Method));
            Assert.All(node.Requests, request => Assert.StartsWith("https://ipfs.example.com:5001/api/v0/ls?arg=%2Fipfs%2FQmDir", request.Url));
        }

        [Fact]
        public void IdentityIsTheNodeAddress()
        {
            Assert.Equal("ipfs://ipfs.example.com:5001", Provider(new Node()).DescribeNativeConnection(Connection()));
            Assert.Null(Provider(new Node()).DescribeNativeConnection(null));
        }

        [Fact]
        public async Task BadConnectionsAreRefusedBeforeAnyRequest()
        {
            var node = new Node();
            var provider = Provider(node);

            Assert.Contains("apiUrl", (await provider.ListNativeDataSourcesAsync(Connection("ftp://x"))).Message);
            Assert.Contains("apiUrl", (await provider.ListNativeDataSourcesAsync(Connection("https://user:pw@ipfs.example.com"))).Message);
            Assert.Contains("paths", (await provider.ListNativeDataSourcesAsync(new NativeConnection { ["apiUrl"] = "https://ipfs.example.com" })).Message);
            Assert.Contains("not a valid", (await provider.ListNativeDataSourcesAsync(Connection(paths: "QmNoPrefix"))).Message);
            Assert.Contains("not a valid", (await provider.ListNativeDataSourcesAsync(Connection(paths: "/ipfs/Qm/../x"))).Message);
            Assert.Equal("NATIVE_IMPORT_READ_FAILED", (await provider.ReadNativeRecordsAsync("/ipfs/QmOther", null, 5, Connection())).ErrorCode);
            Assert.Empty(node.Requests);
        }

        [Fact]
        public async Task ABadResponseFromTheNodeIsAnErrorNotAnEmptyDirectory()
        {
            var failing = new Node { Status = HttpStatusCode.InternalServerError };
            Assert.True((await Provider(failing).ListNativeDataSourcesAsync(Connection())).IsError);

            var redirected = new Node { Status = HttpStatusCode.Found };
            Assert.True((await Provider(redirected).ListNativeDataSourcesAsync(Connection())).IsError); // a redirect is never followed or accepted

            var wrongShape = new Node { Body = "{\"Objects\":[]}" };
            var shape = await Provider(wrongShape).ListNativeDataSourcesAsync(Connection());
            Assert.True(shape.IsError);

            var duplicate = new Node { Body = "{\"Objects\":[{\"Links\":[{\"Name\":\"a\",\"Hash\":\"Q1\"},{\"Name\":\"a\",\"Hash\":\"Q2\"}]}]}" };
            Assert.True((await Provider(duplicate).ReadNativeRecordsAsync("/ipfs/QmDir", null, 5, Connection())).IsError);
        }

        [Fact]
        public async Task TheDefaultHandlerRefusesInternalNodesWithTheRealReason()
        {
            var provider = new IPFSOASIS(new NextGenSoftware.OASIS.API.DNA.OASISDNA());
            var result = await provider.ListNativeDataSourcesAsync(Connection("https://localhost:5001/"));
            Assert.True(result.IsError);
            Assert.Contains("internal name", result.Message);
        }
    }
}
