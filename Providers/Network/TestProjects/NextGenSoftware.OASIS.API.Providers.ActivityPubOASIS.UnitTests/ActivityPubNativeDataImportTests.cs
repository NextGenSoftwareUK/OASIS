using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests
{
    /// <summary>The ActivityPub native reader against a scripted Mastodon-compatible instance.</summary>
    public class ActivityPubNativeDataImportTests
    {
        private const string Instance = "https://social.example";

        private sealed class ScriptedInstance : HttpMessageHandler
        {
            public readonly List<string> Requests = new List<string>();
            public string NextLink;
            public string StatusesJson = "[]";

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri.PathAndQuery;
                Requests.Add(path);
                string body = path switch
                {
                    "/api/v1/accounts/verify_credentials" => "{\"id\":\"42\",\"acct\":\"ada\",\"statuses_count\":120,\"followers_count\":7,\"following_count\":3}",
                    var p when p.StartsWith("/api/v1/accounts/42/statuses") => StatusesJson,
                    var p when p.StartsWith("/api/v1/accounts/42/followers") =>
                        "[{\"id\":\"900\",\"username\":\"grace\",\"acct\":\"grace@other.example\",\"display_name\":\"Grace\",\"note\":\"<p>hi</p>\",\"followers_count\":5,\"bot\":false}]",
                    _ => null
                };
                if (body == null) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
                if (NextLink != null && path.StartsWith("/api/v1/accounts/42/statuses")) response.Headers.TryAddWithoutValidation("Link", $"<{NextLink}>; rel=\"next\"");
                return Task.FromResult(response);
            }
        }

        private static ActivityPubOASIS Provider(ScriptedInstance instance, string token = "token") => new ActivityPubOASIS(Instance, token, instance);

        [Fact]
        public async Task ListsTheAccountsThreeSourcesWithCounts()
        {
            var sources = await Provider(new ScriptedInstance()).ListNativeDataSourcesAsync();
            Assert.False(sources.IsError, sources.Message);
            Assert.Equal(new[] { "statuses", "followers", "following" }, sources.Result.Select(source => source.Name));
            Assert.Equal(new long?[] { 120, 7, 3 }, sources.Result.Select(source => source.EstimatedRecordCount));
        }

        [Fact]
        public async Task StatusesPageWithTheInstancesOwnCursorAndTypedFields()
        {
            var instance = new ScriptedInstance
            {
                StatusesJson = "[{\"id\":\"1002\",\"content\":\"<p>second</p>\",\"created_at\":\"2026-10-27T10:00:00Z\",\"visibility\":\"public\",\"sensitive\":false,\"replies_count\":2,\"media_attachments\":[{},{}],\"in_reply_to_id\":null}," +
                                "{\"id\":\"1001\",\"content\":\"<p>first</p>\",\"visibility\":\"unlisted\"}]",
                NextLink = Instance + "/api/v1/accounts/42/statuses?limit=2&max_id=1001"
            };

            var page = await Provider(instance).ReadNativeRecordsAsync("statuses", null, 2);

            Assert.False(page.IsError, page.Message);
            Assert.Equal(new[] { "1002", "1001" }, page.Result.Records.Select(record => record.NativeKey));
            Assert.Equal("1001", page.Result.ContinuationToken);
            var second = page.Result.Records[0].Fields;
            Assert.Equal("<p>second</p>", second["content"]);
            Assert.Equal(false, second["sensitive"]);
            Assert.Equal(2L, second["replies_count"]);
            Assert.Equal(2L, second["media_count"]);
            Assert.Null(second["in_reply_to_id"]);

            await Provider(instance).ReadNativeRecordsAsync("statuses", "1001", 2);
            Assert.Contains(instance.Requests, request => request.EndsWith("limit=2&max_id=1001"));
        }

        [Fact]
        public async Task LastPageHasNoCursorAndAccountsMapToRecords()
        {
            var instance = new ScriptedInstance();
            var followers = await Provider(instance).ReadNativeRecordsAsync("followers", null, 500);
            Assert.False(followers.IsError, followers.Message);
            Assert.Null(followers.Result.ContinuationToken);
            var record = Assert.Single(followers.Result.Records);
            Assert.Equal("900", record.NativeKey);
            Assert.Equal("Grace", record.Fields["display_name"]);
            Assert.Equal(5L, record.Fields["followers_count"]);
            Assert.Contains(instance.Requests, request => request.Contains("followers?limit=80")); // the instance's page cap, not the caller's 500
        }

        [Fact]
        public async Task TokenlessUnknownSourceBadCursorAndForeignNextLinkAreRefused()
        {
            Assert.Equal("NATIVE_IMPORT_UNAVAILABLE", (await Provider(new ScriptedInstance(), "").ListNativeDataSourcesAsync()).ErrorCode);
            Assert.Equal("NATIVE_IMPORT_READ_FAILED", (await Provider(new ScriptedInstance()).ReadNativeRecordsAsync("blocks", null, 5)).ErrorCode);
            Assert.Equal("NATIVE_IMPORT_READ_FAILED", (await Provider(new ScriptedInstance()).ReadNativeRecordsAsync("statuses", "abc", 5)).ErrorCode);

            var foreign = new ScriptedInstance
            {
                StatusesJson = "[{\"id\":\"5\",\"content\":\"x\"}]",
                NextLink = "https://evil.example/api/v1/accounts/42/statuses?max_id=5"
            };
            var result = await Provider(foreign).ReadNativeRecordsAsync("statuses", null, 5);
            Assert.True(result.IsError);
            Assert.Equal("NATIVE_IMPORT_READ_FAILED", result.ErrorCode);
        }
    }
}
