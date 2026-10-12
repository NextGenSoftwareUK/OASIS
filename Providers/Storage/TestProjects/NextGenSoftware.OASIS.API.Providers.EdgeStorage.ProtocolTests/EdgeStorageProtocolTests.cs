using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.DenoDeployOASIS;
using NextGenSoftware.OASIS.API.Providers.FastlyOASIS;
using NextGenSoftware.OASIS.API.Providers.NetlifyBlobsOASIS;
using NextGenSoftware.OASIS.API.Providers.TigrisOASIS;
using NextGenSoftware.OASIS.API.Providers.VercelKVOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests
{
    /// <summary>In-process stand-in for an HTTP service: routes requests to a handler and records them.</summary>
    internal sealed class FakeService : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string, HttpResponseMessage> _route;
        public readonly List<(HttpRequestMessage Request, string Body)> Requests = new();

        public FakeService(Func<HttpRequestMessage, string, HttpResponseMessage> route) => _route = route;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            lock (Requests) Requests.Add((request, body));
            return _route(request, body);
        }

        public static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };
        public static HttpResponseMessage Text(string value) => new(HttpStatusCode.OK) { Content = new StringContent(value) };
        public static HttpResponseMessage Status(HttpStatusCode code) => new(code) { Content = new StringContent(string.Empty) };
    }

    [TestClass]
    public class FastlyProtocolTests
    {
        private const string Store = "store123";
        private readonly ConcurrentDictionary<string, string> _items = new();
        private FakeService _service;
        private FastlyOASIS.FastlyOASIS _provider;

        [TestInitialize]
        public void Init()
        {
            var keysPath = $"/resources/stores/kv/{Store}/keys";
            _service = new FakeService((req, body) =>
            {
                Assert.AreEqual("api.fastly.com", req.RequestUri.Host);
                Assert.AreEqual("token-abc", req.Headers.GetValues("Fastly-Key").Single());
                var path = req.RequestUri.AbsolutePath;
                if (path == $"/resources/stores/kv/{Store}") return FakeService.Json(new { id = Store, name = "oasis" });
                if (path == keysPath && req.Method == HttpMethod.Get)
                {
                    // Two-key pages so the provider must follow next_cursor.
                    var query = System.Web.HttpUtility.ParseQueryString(req.RequestUri.Query);
                    var all = _items.Keys.Where(k => k.StartsWith(query["prefix"] ?? string.Empty, StringComparison.Ordinal)).OrderBy(k => k).ToList();
                    var start = int.TryParse(query["cursor"], out var c) ? c : 0;
                    var page = all.Skip(start).Take(2).ToList();
                    var next = start + 2 < all.Count ? (start + 2).ToString() : null;
                    return FakeService.Json(new { data = page, meta = new { next_cursor = next, limit = 2 } });
                }
                var key = Uri.UnescapeDataString(path[(keysPath.Length + 1)..]);
                if (req.Method == HttpMethod.Put) { _items[key] = body; return FakeService.Status(HttpStatusCode.OK); }
                if (req.Method == HttpMethod.Get) return _items.TryGetValue(key, out var v) ? FakeService.Text(v) : FakeService.Status(HttpStatusCode.NotFound);
                if (req.Method == HttpMethod.Delete) return _items.TryRemove(key, out _) ? FakeService.Status(HttpStatusCode.NoContent) : FakeService.Status(HttpStatusCode.NotFound);
                return FakeService.Status(HttpStatusCode.MethodNotAllowed);
            });
            _provider = new FastlyOASIS.FastlyOASIS(new FastlyKvBackend("token-abc", Store, _service));
        }

        [TestMethod]
        public async Task Activates_against_the_store_endpoint()
            => Assert.IsFalse((await _provider.ActivateProviderAsync()).IsError);

        [TestMethod]
        public async Task Holons_round_trip_through_kv_items_and_paginated_listing()
        {
            for (var i = 0; i < 5; i++) Assert.IsFalse((await _provider.SaveHolonAsync(new Holon { Name = $"h{i}" })).IsError);

            var all = await _provider.LoadAllHolonsAsync();
            Assert.IsFalse(all.IsError, all.Message);
            Assert.AreEqual(5, all.Result.Count());
            Assert.IsTrue(_service.Requests.Count(r => r.Request.RequestUri.Query.Contains("cursor=")) >= 2, "listing must follow next_cursor");
            Assert.IsTrue(_service.Requests.Where(r => r.Request.Method == HttpMethod.Put).All(r => r.Request.Content.Headers.ContentType.MediaType == "application/octet-stream"));
        }

        [TestMethod]
        public async Task Service_errors_surface_as_OASIS_errors()
        {
            var failing = new FastlyOASIS.FastlyOASIS(new FastlyKvBackend("token-abc", Store, new FakeService((_, _) => FakeService.Status(HttpStatusCode.Unauthorized))));
            var result = await failing.SaveAvatarAsync(new Avatar { Username = "neo", Email = "neo@m.io" });
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "401");
        }
    }

    internal sealed class HandlerHttpClientFactory : Amazon.Runtime.HttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public HandlerHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
        public override HttpClient CreateHttpClient(Amazon.Runtime.IClientConfig clientConfig) => new(_handler, disposeHandler: false);
    }

    [TestClass]
    public class TigrisS3ProtocolTests
    {
        private const string Bucket = "oasis-test";
        private readonly ConcurrentDictionary<string, string> _objects = new();
        private TigrisOASIS.TigrisOASIS _provider;
        private FakeService _service;

        [TestInitialize]
        public void Init()
        {
            _service = new FakeService((req, body) =>
            {
                Assert.IsTrue(req.Headers.Contains("Authorization") && req.Headers.GetValues("Authorization").Single().StartsWith("AWS4-HMAC-SHA256"), "requests must be SigV4 signed");
                var path = Uri.UnescapeDataString(req.RequestUri.AbsolutePath).TrimStart('/');
                if (path == Bucket || path == Bucket + "/")
                {
                    var query = System.Web.HttpUtility.ParseQueryString(req.RequestUri.Query);
                    if (req.Method == HttpMethod.Head) return FakeService.Status(HttpStatusCode.OK);
                    if (query["list-type"] != "2") return FakeService.Status(HttpStatusCode.OK);
                    var prefix = query["prefix"] ?? string.Empty;
                    var all = _objects.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(k => k).ToList();
                    var start = int.TryParse(query["continuation-token"], out var s) ? s : 0;
                    var page = all.Skip(start).Take(2).ToList();
                    var truncated = start + 2 < all.Count;
                    var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ListBucketResult xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\">"
                              + $"<Name>{Bucket}</Name><Prefix>{prefix}</Prefix><KeyCount>{page.Count}</KeyCount><MaxKeys>1000</MaxKeys><IsTruncated>{truncated.ToString().ToLowerInvariant()}</IsTruncated>"
                              + string.Concat(page.Select(k => $"<Contents><Key>{k}</Key><Size>1</Size></Contents>"))
                              + (truncated ? $"<NextContinuationToken>{start + 2}</NextContinuationToken>" : string.Empty) + "</ListBucketResult>";
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(xml, Encoding.UTF8, "application/xml") };
                }

                var key = path[(Bucket.Length + 1)..];
                if (req.Method == HttpMethod.Put) { _objects[key] = DecodeAwsChunked(req, body); return FakeService.Status(HttpStatusCode.OK); }
                if (req.Method == HttpMethod.Delete) { _objects.TryRemove(key, out _); return FakeService.Status(HttpStatusCode.NoContent); }
                if (_objects.TryGetValue(key, out var v)) return FakeService.Text(v);
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Error><Code>NoSuchKey</Code><Message>The specified key does not exist.</Message></Error>", Encoding.UTF8, "application/xml")
                };
            });

            var s3 = new Amazon.S3.AmazonS3Client(new Amazon.Runtime.BasicAWSCredentials("ak", "sk"), new Amazon.S3.AmazonS3Config
            {
                ServiceURL = "https://fly.storage.tigris.dev",
                ForcePathStyle = true,
                AuthenticationRegion = "auto",
                HttpClientFactory = new HandlerHttpClientFactory(_service),
                MaxErrorRetry = 0
            });
            _provider = new TigrisOASIS.TigrisOASIS(new S3ObjectBackend(s3, Bucket));
        }

        // The SDK streams uploads as aws-chunked ("<hex-size>[;ext]\r\n<data>\r\n ... 0\r\n[trailers]"); S3 servers decode it.
        private static string DecodeAwsChunked(HttpRequestMessage req, string body)
        {
            var encoding = req.Content?.Headers.ContentEncoding;
            var sha = req.Headers.TryGetValues("x-amz-content-sha256", out var v) ? v.FirstOrDefault() : null;
            if ((encoding == null || !encoding.Contains("aws-chunked")) && (sha == null || !sha.StartsWith("STREAMING-"))) return body;

            var sb = new StringBuilder();
            var pos = 0;
            while (pos < body.Length)
            {
                var lineEnd = body.IndexOf("\r\n", pos, StringComparison.Ordinal);
                var header = body[pos..lineEnd];
                var size = Convert.ToInt32(header.Split(';')[0], 16);
                if (size == 0) break;
                sb.Append(body, lineEnd + 2, size);
                pos = lineEnd + 2 + size + 2;
            }
            return sb.ToString();
        }

        [TestMethod]
        public async Task Holons_round_trip_as_s3_objects_with_continuation()
        {
            for (var i = 0; i < 5; i++) Assert.IsFalse((await _provider.SaveHolonAsync(new Holon { Name = $"h{i}" })).IsError);
            var all = await _provider.LoadAllHolonsAsync();
            Assert.IsFalse(all.IsError, all.Message);
            Assert.AreEqual(5, all.Result.Count());
            Assert.IsTrue(_service.Requests.Count(r => r.Request.RequestUri.Query.Contains("continuation-token")) >= 2);
        }

        [TestMethod]
        public async Task Missing_object_is_a_not_found_error()
            => Assert.IsTrue((await _provider.LoadAvatarAsync(Guid.NewGuid())).IsError);
    }

    [TestClass]
    public class DenoKvConnectProtocolTests
    {
        private sealed class ByteKeyComparer : IComparer<byte[]>
        {
            public int Compare(byte[] x, byte[] y) => x.AsSpan().SequenceCompareTo(y);
        }

        private readonly SortedDictionary<byte[], (byte[] Value, DenoDeployOASIS.Datapath.ValueEncoding Encoding)> _kv = new(new ByteKeyComparer());
        private FakeService _service;
        private DenoDeployOASIS.DenoDeployOASIS _provider;
        private int _metadataCalls;

        [TestInitialize]
        public void Init()
        {
            _service = new FakeService((req, body) =>
            {
                if (req.RequestUri.AbsolutePath == "/databases/db-1/connect")
                {
                    _metadataCalls++;
                    Assert.AreEqual("deploy-token", req.Headers.Authorization.Parameter);
                    StringAssert.Contains(body, "supportedVersions");
                    return FakeService.Json(new
                    {
                        version = 2, uuid = "uuid-1", token = "data-token", expiresAt = DateTimeOffset.UtcNow.AddHours(1),
                        endpoints = new[] { new { url = "https://kv.example/eventual", consistency = "eventual" }, new { url = "https://kv.example/strong", consistency = "strong" } }
                    });
                }

                Assert.AreEqual("kv.example", req.RequestUri.Host);
                Assert.IsTrue(req.RequestUri.AbsolutePath.StartsWith("/strong/"), "data path must use the strong endpoint");
                Assert.AreEqual("data-token", req.Headers.Authorization.Parameter);
                Assert.AreEqual("2", req.Headers.GetValues("x-denokv-version").Single());
                Assert.AreEqual("uuid-1", req.Headers.GetValues("x-denokv-database-id").Single());
                Assert.AreEqual("application/x-protobuf", req.Content.Headers.ContentType.MediaType);
                var bytes = req.Content.ReadAsByteArrayAsync().Result;

                if (req.RequestUri.AbsolutePath == "/strong/atomic_write")
                {
                    var write = DenoDeployOASIS.Datapath.AtomicWrite.Parser.ParseFrom(bytes);
                    foreach (var m in write.Mutations)
                    {
                        if (m.MutationType == DenoDeployOASIS.Datapath.MutationType.MSet) _kv[m.Key.ToByteArray()] = (m.Value.Data.ToByteArray(), m.Value.Encoding);
                        else if (m.MutationType == DenoDeployOASIS.Datapath.MutationType.MDelete) _kv.Remove(m.Key.ToByteArray());
                    }
                    return Proto(new DenoDeployOASIS.Datapath.AtomicWriteOutput { Status = DenoDeployOASIS.Datapath.AtomicWriteStatus.AwSuccess });
                }

                var read = DenoDeployOASIS.Datapath.SnapshotRead.Parser.ParseFrom(bytes);
                var output = new DenoDeployOASIS.Datapath.SnapshotReadOutput { Status = DenoDeployOASIS.Datapath.SnapshotReadStatus.SrSuccess, ReadIsStronglyConsistent = true };
                foreach (var range in read.Ranges)
                {
                    var comparer = new ByteKeyComparer();
                    var start = range.Start.ToByteArray();
                    var end = range.End.ToByteArray();
                    var rangeOut = new DenoDeployOASIS.Datapath.ReadRangeOutput();
                    foreach (var kv in _kv.Where(kv => comparer.Compare(kv.Key, start) >= 0 && comparer.Compare(kv.Key, end) < 0).Take(range.Limit))
                        rangeOut.Values.Add(new DenoDeployOASIS.Datapath.KvEntry { Key = Google.Protobuf.ByteString.CopyFrom(kv.Key), Value = Google.Protobuf.ByteString.CopyFrom(kv.Value.Value), Encoding = kv.Value.Encoding });
                    output.Ranges.Add(rangeOut);
                }
                return Proto(output);
            });
            _provider = new DenoDeployOASIS.DenoDeployOASIS(new DenoKvConnectBackend("https://api.deno.com/databases/db-1/connect", "deploy-token", _service));
        }

        private static HttpResponseMessage Proto(Google.Protobuf.IMessage message)
            => new(HttpStatusCode.OK) { Content = new ByteArrayContent(Google.Protobuf.MessageExtensions.ToByteArray(message)) };

        [TestMethod]
        public void Keys_use_the_denokv_tuple_string_encoding()
        {
            CollectionAssert.AreEqual(new byte[] { 0x02, (byte)'a', 0x00 }, DenoKvConnectBackend.EncodeKey("a"));
            CollectionAssert.AreEqual(new byte[] { 0x02, (byte)'a', 0x00, 0xFF, (byte)'b', 0x00 }, DenoKvConnectBackend.EncodeKey("a\0b"));
            Assert.AreEqual("a\0b", DenoKvConnectBackend.DecodeKey(Google.Protobuf.ByteString.CopyFrom(DenoKvConnectBackend.EncodeKey("a\0b"))));
        }

        [TestMethod]
        public async Task Avatars_and_holons_round_trip_over_kv_connect()
        {
            Assert.IsFalse((await _provider.ActivateProviderAsync()).IsError);
            var avatar = await _provider.SaveAvatarAsync(new Avatar { Username = "morpheus", Email = "m@m.io" });
            Assert.IsFalse(avatar.IsError, avatar.Message);
            Assert.AreEqual(avatar.Result.Id, (await _provider.LoadAvatarByUsernameAsync("morpheus")).Result.Id);

            for (var i = 0; i < 3; i++) await _provider.SaveHolonAsync(new Holon { Name = $"h{i}" });
            Assert.AreEqual(3, (await _provider.LoadAllHolonsAsync()).Result.Count());
            Assert.IsTrue(_kv.Values.All(v => v.Encoding == DenoDeployOASIS.Datapath.ValueEncoding.VeBytes));
            Assert.AreEqual(1, _metadataCalls, "one metadata exchange; later calls reuse the unexpired token");
        }
    }

    internal sealed class FakeDaprStateApi : DaprOASIS.IDaprStateApi
    {
        private readonly Dictionary<string, (string Value, int Version)> _state = new();
        public int ConflictsToInject;
        public int Conflicts;

        public Task<(string Value, string ETag)> GetAsync(string store, string key, CancellationToken ct)
        {
            lock (_state) return Task.FromResult(_state.TryGetValue(key, out var s) ? (s.Value, s.Version.ToString()) : ((string)null, (string)null));
        }

        public Task SaveAsync(string store, string key, string value, CancellationToken ct)
        {
            lock (_state) _state[key] = (value, _state.TryGetValue(key, out var s) ? s.Version + 1 : 1);
            return Task.CompletedTask;
        }

        public Task<bool> TrySaveAsync(string store, string key, string value, string etag, CancellationToken ct)
        {
            lock (_state)
            {
                if (ConflictsToInject > 0) { ConflictsToInject--; Conflicts++; return Task.FromResult(false); }
                var exists = _state.TryGetValue(key, out var s);
                var matches = string.IsNullOrEmpty(etag) ? !exists : exists && s.Version.ToString() == etag;
                if (!matches) { Conflicts++; return Task.FromResult(false); }
                _state[key] = (value, exists ? s.Version + 1 : 1);
                return Task.FromResult(true);
            }
        }

        public Task DeleteAsync(string store, string key, CancellationToken ct) { lock (_state) _state.Remove(key); return Task.CompletedTask; }
        public Task<bool> HealthyAsync(CancellationToken ct) => Task.FromResult(true);
        public int Count { get { lock (_state) return _state.Count; } }
    }

    [TestClass]
    public class DaprStateProtocolTests
    {
        [TestMethod]
        public async Task Index_updates_retry_on_etag_conflicts_and_listing_stays_complete()
        {
            var api = new FakeDaprStateApi { ConflictsToInject = 3 };
            var provider = new DaprOASIS.DaprOASIS(new DaprOASIS.DaprStateBackend(api, "statestore"));
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);

            await Task.WhenAll(Enumerable.Range(0, 10).Select(i => provider.SaveHolonAsync(new Holon { Name = $"h{i}" })));
            var all = await provider.LoadAllHolonsAsync();
            Assert.IsFalse(all.IsError, all.Message);
            Assert.AreEqual(10, all.Result.Count(), "concurrent saves must not lose index entries");
            Assert.IsTrue(api.Conflicts >= 3);
        }

        [TestMethod]
        public async Task Deleting_removes_the_key_from_its_index()
        {
            var api = new FakeDaprStateApi();
            var provider = new DaprOASIS.DaprOASIS(new DaprOASIS.DaprStateBackend(api, "statestore"));
            var avatar = (await provider.SaveAvatarAsync(new Avatar { Username = "seraph", Email = "s@m.io" })).Result;
            Assert.IsFalse((await provider.DeleteAvatarAsync(avatar.Id, softDelete: false)).IsError);
            Assert.AreEqual(0, (await provider.LoadAllAvatarsAsync()).Result.Count());
        }
    }

    [TestClass]
    public class VercelKVProtocolTests
    {
        private readonly ConcurrentDictionary<string, string> _redis = new();
        private FakeService _service;
        private VercelKVOASIS.VercelKVOASIS _provider;

        [TestInitialize]
        public void Init()
        {
            _service = new FakeService((req, body) =>
            {
                Assert.AreEqual(HttpMethod.Post, req.Method);
                Assert.AreEqual("kv-token", req.Headers.Authorization.Parameter);
                var cmd = JsonSerializer.Deserialize<string[]>(body);
                switch (cmd[0])
                {
                    case "PING": return FakeService.Json(new { result = "PONG" });
                    case "GET": return FakeService.Json(new { result = _redis.TryGetValue(cmd[1], out var v) ? v : null });
                    case "SET": _redis[cmd[1]] = cmd[2]; return FakeService.Json(new { result = "OK" });
                    case "DEL": return FakeService.Json(new { result = _redis.TryRemove(cmd[1], out _) ? 1 : 0 });
                    case "SCAN":
                        // Return one key per call so the provider must iterate the cursor to the end.
                        var prefix = cmd[3].TrimEnd('*').Replace("\\", string.Empty);
                        var all = _redis.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(k => k).ToList();
                        var at = int.Parse(cmd[1]);
                        var next = at + 1 < all.Count ? (at + 1).ToString() : "0";
                        return FakeService.Json(new { result = new object[] { next, all.Skip(at).Take(1).ToArray() } });
                    default: return FakeService.Json(new { error = $"ERR unknown command '{cmd[0]}'" });
                }
            });
            _provider = new VercelKVOASIS.VercelKVOASIS(new UpstashRestBackend("https://example.kv.vercel-storage.com", "kv-token", _service));
        }

        [TestMethod]
        public async Task Holons_round_trip_through_redis_commands_and_scan()
        {
            Assert.IsFalse((await _provider.ActivateProviderAsync()).IsError);
            for (var i = 0; i < 3; i++) await _provider.SaveHolonAsync(new Holon { Name = $"h{i}" });

            var all = await _provider.LoadAllHolonsAsync();
            Assert.IsFalse(all.IsError, all.Message);
            Assert.AreEqual(3, all.Result.Count());
        }

        [TestMethod]
        public async Task Redis_errors_surface_as_OASIS_errors()
        {
            var failing = new VercelKVOASIS.VercelKVOASIS(new UpstashRestBackend("https://example.kv.vercel-storage.com", "kv-token",
                new FakeService((_, _) => FakeService.Json(new { error = "WRONGPASS invalid token" }))));
            var result = await failing.ActivateProviderAsync();
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "WRONGPASS");
        }
    }

    [TestClass]
    public class NetlifyBlobsProtocolTests
    {
        private const string Site = "site-1";
        private readonly ConcurrentDictionary<string, string> _blobs = new();
        private FakeService _service;
        private NetlifyBlobsOASIS.NetlifyBlobsOASIS _provider;

        [TestInitialize]
        public void Init()
        {
            var storePath = $"/api/v1/blobs/{Site}/site:oasis";
            _service = new FakeService((req, body) =>
            {
                var path = Uri.UnescapeDataString(req.RequestUri.AbsolutePath);
                if (req.RequestUri.Host == "signed.example")
                {
                    Assert.IsNull(req.Headers.Authorization, "the API token must not be sent to signed URLs");
                    var key = path.TrimStart('/');
                    if (req.Method == HttpMethod.Put) { _blobs[key] = body; return FakeService.Status(HttpStatusCode.OK); }
                    return _blobs.TryGetValue(key, out var v) ? FakeService.Text(v) : FakeService.Status(HttpStatusCode.NotFound);
                }

                Assert.AreEqual("api.netlify.com", req.RequestUri.Host);
                Assert.AreEqual("Bearer", req.Headers.Authorization.Scheme);
                Assert.AreEqual("pat-xyz", req.Headers.Authorization.Parameter);

                if (path == storePath)
                {
                    var prefix = System.Web.HttpUtility.ParseQueryString(req.RequestUri.Query)["prefix"] ?? string.Empty;
                    return FakeService.Json(new { blobs = _blobs.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).Select(k => new { key = k, etag = "e" }), directories = Array.Empty<string>() });
                }

                var blobKey = path[(storePath.Length + 1)..];
                if (req.Method == HttpMethod.Delete) { _blobs.TryRemove(blobKey, out _); return FakeService.Status(HttpStatusCode.NoContent); }
                Assert.AreEqual("application/json;type=signed-url", req.Headers.Accept.Single().ToString().Replace(" ", string.Empty));
                if (req.Method == HttpMethod.Get && !_blobs.ContainsKey(blobKey)) return FakeService.Status(HttpStatusCode.NotFound);
                return FakeService.Json(new { url = $"https://signed.example/{blobKey}" });
            });
            _provider = new NetlifyBlobsOASIS.NetlifyBlobsOASIS(new NetlifyBlobsBackend(Site, "pat-xyz", "oasis", "https://api.netlify.com", _service));
        }

        [TestMethod]
        public async Task Avatars_use_signed_urls_for_reads_and_writes()
        {
            Assert.IsFalse((await _provider.ActivateProviderAsync()).IsError);
            var saved = await _provider.SaveAvatarAsync(new Avatar { Username = "trinity", Email = "t@m.io" });
            Assert.IsFalse(saved.IsError, saved.Message);

            var loaded = await _provider.LoadAvatarByEmailAsync("t@m.io");
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(saved.Result.Id, loaded.Result.Id);
            Assert.IsTrue(_service.Requests.Any(r => r.Request.RequestUri.Host == "signed.example" && r.Request.Method == HttpMethod.Put));
        }

        [TestMethod]
        public async Task Missing_blobs_are_not_found_errors_and_deletes_are_direct()
        {
            Assert.IsTrue((await _provider.LoadHolonAsync(Guid.NewGuid(), loadChildren: false)).IsError);

            var holon = new Holon { Name = "x" };
            await _provider.SaveHolonAsync(holon);
            Assert.IsFalse((await _provider.DeleteHolonAsync(holon.Id)).IsError);
            Assert.IsFalse(_service.Requests.Any(r => r.Request.Method == HttpMethod.Delete && r.Request.RequestUri.Host == "signed.example"));
            Assert.AreEqual(0, _blobs.Count);
        }
    }
}
