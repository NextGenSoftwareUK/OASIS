using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.BlueSkyOASIS.UnitTests
{
    /// <summary>
    /// Provider identity, record-key mapping and the XRPC session/rate-limit protocol, without a PDS.
    /// The end-to-end storage contract runs against a real PDS in BlueSkyNativeStorageConformanceTests.
    /// </summary>
    [TestClass]
    public class BlueSkyOASISProviderTests
    {
        private static readonly Uri Pds = new("http://pds.invalid");
        private const string Did = "did:plc:testaccount";

        /// <summary>Scripted PDS: each request is answered by the next handler; every request is recorded.</summary>
        private sealed class ScriptedPds : HttpMessageHandler
        {
            private readonly Queue<Func<HttpRequestMessage, string?, HttpResponseMessage>> _replies = new();
            public List<(string Path, string? Bearer, string? Body)> Calls { get; } = new();
            public ScriptedPds Then(Func<HttpRequestMessage, string?, HttpResponseMessage> reply) { _replies.Enqueue(reply); return this; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(token);
                Calls.Add((request.RequestUri!.AbsolutePath, request.Headers.Authorization?.Parameter, body));
                Assert.IsTrue(_replies.Count > 0, $"Unexpected request {request.RequestUri}");
                return _replies.Dequeue()(request, body);
            }
        }

        private static HttpResponseMessage Json(HttpStatusCode status, object body)
            => new(status) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };

        private static HttpResponseMessage Session(string access, string refresh)
            => Json(HttpStatusCode.OK, new { did = Did, handle = "oasis.test", accessJwt = access, refreshJwt = refresh });

        private static HttpResponseMessage Error(HttpStatusCode status, string error)
            => Json(status, new { error, message = error + " message" });

        [TestMethod]
        public void PublishingInstance_IdentifiesItselfAndStartsDeactivated()
        {
            using var provider = new BlueSkyOASIS("oasis.test", "app-password", "http://pds.invalid");
            Assert.AreEqual(ProviderType.BlueSkyOASIS, provider.ProviderType.Value);
            Assert.AreEqual("BlueSkyOASIS", provider.ProviderName);
            Assert.AreEqual(ProviderCategory.StorageAndNetwork, provider.ProviderCategory.Value);
            Assert.IsFalse(string.IsNullOrWhiteSpace(provider.ProviderDescription));
            Assert.IsFalse(provider.IsProviderActivated);
        }

        [TestMethod]
        public async Task PublishingInstance_RefusesStorageExplicitly()
        {
            using var provider = new BlueSkyOASIS("oasis.test", "app-password", "http://pds.invalid");
            var activated = await provider.ActivateProviderAsync();
            Assert.IsTrue(activated.IsError);
            StringAssert.Contains(activated.Message, "record encryption key");
            Assert.IsFalse(provider.IsProviderActivated);
        }

        [TestMethod]
        public async Task PublishingWithoutCredentials_FailsWithoutNetwork()
        {
            using var provider = new BlueSkyOASIS(null, null, "http://pds.invalid");
            var post = await provider.PublishPostAsync("hello");
            Assert.IsTrue(post.IsError);
            StringAssert.Contains(post.Message, "credentials");
        }

        [TestMethod]
        public void StorageInstance_RequiresA32ByteKey()
        {
            Assert.ThrowsException<ArgumentException>(() => new BlueSkyOASIS(Pds, "oasis.test", "pw", new byte[16]));
            using var provider = new BlueSkyOASIS(Pds, "oasis.test", "pw", new byte[32]);
            Assert.IsFalse(provider.IsProviderActivated);
        }

        [TestMethod]
        public void RecordKeys_AreValidAndInjective()
        {
            var grammar = new Regex("^[A-Za-z0-9._:~-]{1,512}$");
            Assert.AreEqual("plain-key_1.2:3", AtProtoRepoKeyValueBackend.RecordKeyFor("plain-key_1.2:3"));
            var keys = new[] { "oasis/private/v1/abc", "~verbatim", ".", "..", "", new string('a', 513), "ключ", "a b" };
            var mapped = keys.Select(AtProtoRepoKeyValueBackend.RecordKeyFor).ToArray();
            foreach (var rkey in mapped)
            {
                Assert.IsTrue(grammar.IsMatch(rkey), rkey);
                StringAssert.StartsWith(rkey, "~"); // hashed keys and only hashed keys start with '~'
                Assert.AreEqual(65, rkey.Length);
            }
            Assert.AreEqual(mapped.Length, mapped.Distinct().Count());
            Assert.AreEqual(new string('a', 512), AtProtoRepoKeyValueBackend.RecordKeyFor(new string('a', 512)));
        }

        [TestMethod]
        public async Task ExpiredAccessToken_IsRefreshedOnceAndTheCallRetried()
        {
            var pds = new ScriptedPds()
                .Then((_, _) => Session("access-1", "refresh-1"))
                .Then((_, _) => Error(HttpStatusCode.BadRequest, "ExpiredToken"))
                .Then((_, _) => Session("access-2", "refresh-2"))
                .Then((_, _) => Json(HttpStatusCode.OK, new { did = Did }));
            using var client = new AtProtoSessionClient(Pds, "oasis.test", "pw", pds);

            var session = await client.QueryAsync("com.atproto.server.getSession", string.Empty, CancellationToken.None);

            Assert.AreEqual(Did, session.GetProperty("did").GetString());
            CollectionAssert.AreEqual(new[]
            {
                "/xrpc/com.atproto.server.createSession", "/xrpc/com.atproto.server.getSession",
                "/xrpc/com.atproto.server.refreshSession", "/xrpc/com.atproto.server.getSession"
            }, pds.Calls.Select(c => c.Path).ToArray());
            CollectionAssert.AreEqual(new[] { null, "access-1", "refresh-1", "access-2" }, pds.Calls.Select(c => c.Bearer).ToArray());
        }

        [TestMethod]
        public async Task ExpiredRefreshToken_CreatesANewSession()
        {
            var pds = new ScriptedPds()
                .Then((_, _) => Session("access-1", "refresh-1"))
                .Then((_, _) => Error(HttpStatusCode.BadRequest, "ExpiredToken"))
                .Then((_, _) => Error(HttpStatusCode.BadRequest, "ExpiredToken"))
                .Then((_, _) => Session("access-2", "refresh-2"))
                .Then((_, _) => Json(HttpStatusCode.OK, new { did = Did }));
            using var client = new AtProtoSessionClient(Pds, "oasis.test", "pw", pds);

            await client.QueryAsync("com.atproto.server.getSession", string.Empty, CancellationToken.None);

            Assert.AreEqual("/xrpc/com.atproto.server.createSession", pds.Calls[3].Path);
            Assert.AreEqual("access-2", pds.Calls[4].Bearer);
        }

        [TestMethod]
        public async Task RateLimited_WaitsForTheResetThenRetries()
        {
            var pds = new ScriptedPds()
                .Then((_, _) =>
                {
                    var limited = Error(HttpStatusCode.TooManyRequests, "RateLimitExceeded");
                    limited.Headers.Add("RateLimit-Reset", DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeSeconds().ToString());
                    return limited;
                })
                .Then((_, _) => Session("access-1", "refresh-1"));
            using var client = new AtProtoSessionClient(Pds, "oasis.test", "pw", pds);

            Assert.AreEqual(Did, await client.GetDidAsync(CancellationToken.None));
            Assert.AreEqual(2, pds.Calls.Count);
        }

        [TestMethod]
        public async Task RateLimitBeyondTheWaitBound_FailsImmediately()
        {
            var pds = new ScriptedPds().Then((_, _) =>
            {
                var limited = Error(HttpStatusCode.TooManyRequests, "RateLimitExceeded");
                limited.Headers.Add("RateLimit-Reset", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString());
                return limited;
            });
            using var client = new AtProtoSessionClient(Pds, "oasis.test", "pw", pds);

            var error = await Assert.ThrowsExceptionAsync<AtProtoXrpcException>(() => client.GetDidAsync(CancellationToken.None));
            Assert.AreEqual(HttpStatusCode.TooManyRequests, error.Status);
            StringAssert.Contains(error.Message, "wait bound");
            Assert.AreEqual(1, pds.Calls.Count);
        }

        [TestMethod]
        public async Task Get_MissingRecordIsNull_AndAMismatchedKeyIsRejected()
        {
            var pds = new ScriptedPds()
                .Then((_, _) => Session("access-1", "refresh-1"))
                .Then((_, _) => Error(HttpStatusCode.BadRequest, "RecordNotFound"))
                .Then((_, _) => Json(HttpStatusCode.OK, new
                {
                    uri = $"at://{Did}/one.oasis.record/k1", cid = "bafy",
                    value = new Dictionary<string, string> { ["$type"] = "one.oasis.record", ["key"] = "other", ["value"] = "v" }
                }));
            using var client = new AtProtoSessionClient(Pds, "oasis.test", "pw", pds);
            var backend = new AtProtoRepoKeyValueBackend(client);

            Assert.IsNull(await backend.GetAsync("k1"));
            await Assert.ThrowsExceptionAsync<InvalidDataException>(() => backend.GetAsync("k1"));
        }

        [TestMethod]
        public async Task Put_WritesTheTypedRecordUnderItsRecordKey_AndRejectsOversizedBodies()
        {
            var pds = new ScriptedPds()
                .Then((_, _) => Session("access-1", "refresh-1"))
                .Then((_, body) =>
                {
                    var request = JsonNode.Parse(body!)!;
                    Assert.AreEqual(Did, request["repo"]!.GetValue<string>());
                    Assert.AreEqual("one.oasis.record", request["collection"]!.GetValue<string>());
                    Assert.AreEqual(AtProtoRepoKeyValueBackend.RecordKeyFor("a/b"), request["rkey"]!.GetValue<string>());
                    Assert.AreEqual("a/b", request["record"]!["key"]!.GetValue<string>());
                    Assert.AreEqual("one.oasis.record", request["record"]!["$type"]!.GetValue<string>());
                    return Json(HttpStatusCode.OK, new { uri = $"at://{Did}/one.oasis.record/{request["rkey"]}", cid = "bafy" });
                });
            using var client = new AtProtoSessionClient(Pds, "oasis.test", "pw", pds);
            var backend = new AtProtoRepoKeyValueBackend(client);

            await backend.PutAsync("a/b", "value");
            await Assert.ThrowsExceptionAsync<InvalidDataException>(() => backend.PutAsync("big", new string('x', AtProtoRepoKeyValueBackend.MaxPutRecordBodyBytes)));
            Assert.AreEqual(2, pds.Calls.Count); // the oversized record never reached the PDS
        }
    }
}
