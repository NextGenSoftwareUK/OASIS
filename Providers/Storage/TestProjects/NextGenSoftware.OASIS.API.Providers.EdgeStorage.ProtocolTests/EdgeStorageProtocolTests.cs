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
using NextGenSoftware.OASIS.API.Providers.FastlyOASIS;
using NextGenSoftware.OASIS.API.Providers.NetlifyBlobsOASIS;

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
