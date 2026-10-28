using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests
{
    /// <summary>The ActivityPub reader reading the caller's own server instead of the configured one.</summary>
    [Collection("ActivityPubNativeConnectionHandler")]
    public class ActivityPubNativeConnectionTests
    {
        private sealed class Instance : HttpMessageHandler
        {
            public readonly List<(string Path, string Auth)> Requests = new List<(string, string)>();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add((request.RequestUri.PathAndQuery, request.Headers.Authorization?.Parameter));
                var body = request.RequestUri.AbsolutePath switch
                {
                    "/api/v1/accounts/verify_credentials" => "{\"id\":\"7\",\"acct\":\"me\",\"statuses_count\":2,\"followers_count\":1,\"following_count\":0}",
                    "/api/v1/accounts/7/statuses" => "[{\"id\":\"20\",\"content\":\"<p>hi</p>\",\"visibility\":\"public\"}]",
                    _ => null
                };
                return Task.FromResult(body == null ? new HttpResponseMessage(HttpStatusCode.NotFound)
                    : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
            }
        }

        private static NativeConnection Connection(string url = "https://mine.example", string token = "secret-token") =>
            new NativeConnection { ["instanceUrl"] = url, ["accessToken"] = token };

        private static ActivityPubOASIS Configured() => new ActivityPubOASIS("https://configured.example", "configured-token", new Instance());

        [Fact]
        public async Task ReadsTheCallersServerWithTheCallersTokenNotTheConfiguredOne()
        {
            var original = ActivityPubOASIS.NativeConnectionHandlerFactory;
            var theirs = new Instance();
            ActivityPubOASIS.NativeConnectionHandlerFactory = () => theirs;
            try
            {
                var provider = Configured();
                var sources = await provider.ListNativeDataSourcesAsync(Connection());
                Assert.False(sources.IsError, sources.Message);
                Assert.Equal(new long?[] { 2, 1, 0 }, sources.Result.Select(source => source.EstimatedRecordCount));

                var page = await provider.ReadNativeRecordsAsync("statuses", null, 5, Connection());
                Assert.False(page.IsError, page.Message);
                Assert.Equal("20", page.Result.Records.Single().NativeKey);
                Assert.All(theirs.Requests, request => Assert.Equal("secret-token", request.Auth));
            }
            finally { ActivityPubOASIS.NativeConnectionHandlerFactory = original; }
        }

        [Fact]
        public void IdentityIsTheServerNeverTheToken()
        {
            var identity = Configured().DescribeNativeConnection(Connection("https://Mine.Example", "secret-token"));
            Assert.Equal("activitypub://mine.example", identity);
            Assert.DoesNotContain("secret", identity);
            Assert.Null(Configured().DescribeNativeConnection(null));
        }

        [Fact]
        public async Task BadConnectionsAreRefusedBeforeAnyRequest()
        {
            var original = ActivityPubOASIS.NativeConnectionHandlerFactory;
            var theirs = new Instance();
            ActivityPubOASIS.NativeConnectionHandlerFactory = () => theirs;
            try
            {
                var provider = Configured();
                Assert.True((await provider.ListNativeDataSourcesAsync(Connection("http://mine.example"))).IsError);
                Assert.Contains("https", (await provider.ListNativeDataSourcesAsync(Connection("http://mine.example"))).Message);
                Assert.Contains("accessToken", (await provider.ListNativeDataSourcesAsync(new NativeConnection { ["instanceUrl"] = "https://mine.example" })).Message);
                Assert.Contains("tokn", (await provider.ListNativeDataSourcesAsync(new NativeConnection { ["instanceUrl"] = "https://mine.example", ["tokn"] = "x" })).Message);
                Assert.Empty(theirs.Requests);
            }
            finally { ActivityPubOASIS.NativeConnectionHandlerFactory = original; }
        }

        [Fact]
        public async Task TheDefaultHandlerRefusesInternalAddressesWithTheRealReason()
        {
            var provider = Configured();
            var result = await provider.ListNativeDataSourcesAsync(Connection("https://localhost"));
            Assert.True(result.IsError);
            Assert.Contains("internal name", result.Message);

            var literal = await provider.ListNativeDataSourcesAsync(Connection("https://10.0.0.5"));
            Assert.Contains("not a public address", literal.Message);
        }
    }

    [CollectionDefinition("ActivityPubNativeConnectionHandler", DisableParallelization = true)]
    public class ActivityPubNativeConnectionHandlerCollection { }
}
