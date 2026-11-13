using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MapLibreOASIS.UnitTests;

[TestClass]
public class MapLibreStyleQueryTests
{
    private const ProviderType Provider = ProviderType.MapLibreOASIS;
    private static readonly Guid Owner = Guid.Parse("8dbbf7bc-8a31-4a35-8796-621f17cb5e39");
    private static readonly string[] Keys = { "world", "bright", "terrain" };
    private static readonly Guid[] Ids = Keys.Select(key => MapLibreStyleQuerySource.LogicalId(Owner, key)).ToArray();

    [TestMethod]
    public async Task ExactStyleDocumentsPreserveFullJsonAndNeverFollowReferencedResources()
    {
        using var replies = new Replies();
        using var source = new MapLibreStyleQuerySource(Owner, Keys, replies);
        var activated = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        Check(await Read(source, Ids), Ids);
        Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
        Assert.AreEqual(6, replies.Requests, "One exact GET per document; no tiles, glyphs, rendering or retries.");
        var before = replies.Requests;
        foreach (var selection in new[] { new[] { Guid.NewGuid() }, new[] { Ids[0], Guid.NewGuid() } })
        {
            var result = await Read(source, selection);
            Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
            Assert.AreEqual(before, replies.Requests, "Preflight the entire selection before transport.");
        }
    }

    [DataTestMethod]
    [DataRow("http-failure")]
    [DataRow("redirect")]
    [DataRow("partial-content")]
    [DataRow("wrong-content-type")]
    [DataRow("malformed-json")]
    [DataRow("duplicate-root")]
    [DataRow("duplicate-nested")]
    [DataRow("wrong-version")]
    [DataRow("fractional-version")]
    [DataRow("missing-name")]
    [DataRow("empty-name")]
    [DataRow("oversized-name")]
    [DataRow("empty-sources")]
    [DataRow("bad-source")]
    [DataRow("bad-source-kind")]
    [DataRow("empty-layers")]
    [DataRow("too-many-layers")]
    [DataRow("duplicate-layer")]
    [DataRow("bad-layer-kind")]
    [DataRow("unknown-layer-source")]
    [DataRow("oversized-body")]
    [DataRow("invalid-utf8")]
    [DataRow("missing-layers")]
    public async Task HttpSchemaIdentityAndPayloadFailuresRefuseActivation(string fault)
    {
        using var replies = new Replies { Fault = fault };
        using var source = new MapLibreStyleQuerySource(Owner, Keys, replies);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError, fault); Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", result.ErrorCode);
        Assert.AreEqual(1, replies.Requests, "A failed source read must not retry or request child resources.");
    }

    [TestMethod]
    public async Task OneFailedStylePublishesNoPartialResult()
    {
        using var replies = new Replies();
        using var source = new MapLibreStyleQuerySource(Owner, Keys, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        replies.FailStyle = "bright";
        var result = await Read(source, Ids);
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual(4, replies.Requests);
    }

    [TestMethod]
    public async Task InactiveAndCancelledReadsDoNotReachTransport()
    {
        using var replies = new Replies();
        using var source = new MapLibreStyleQuerySource(Owner, Keys, replies);
        Assert.IsTrue((await Read(source, Ids)).IsError);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.IsTrue((await source.ActivateQuerySourceAsync(cancellation.Token)).IsError);
        Assert.AreEqual(0, replies.Requests);
    }

    [TestMethod]
    public void InvalidCataloguesFailBeforeTransportConstruction()
    {
        var configurationOwner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
        CollectionAssert.AreEqual(new[] { "b0f5d38d-cc44-8e60-93b2-e201be79f2b9", "025230b9-0731-8dca-8443-af589d04f4cd", "2f4cf1cc-ed1b-85e2-88fc-75930d4b582a" },
            Keys.Select(x => MapLibreStyleQuerySource.LogicalId(configurationOwner, x).ToString()).ToArray());
        foreach (var keys in new[] { Array.Empty<string>(), new[] { "world", "world" }, new[] { "WORLD" },
                     new[] { "https://foreign.invalid/style.json" }, new[] { "world", "bright", "terrain", "world" } })
            Assert.ThrowsException<ArgumentException>(() => new MapLibreStyleQuerySource(Owner, keys));
        Assert.ThrowsException<ArgumentException>(() => new MapLibreStyleQuerySource(Guid.Empty, Keys));
        Assert.AreNotEqual(MapLibreStyleQuerySource.LogicalId(Owner, "world"), MapLibreStyleQuerySource.LogicalId(Guid.NewGuid(), "world"));
    }

    [TestMethod, TestCategory("MapLibreStyleNative")]
    public async Task PublicStyleCqlReturnsThreeAndTwoWholeNativeDocuments()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_MAPLIBRE_VERIFY_PUBLIC") != "1")
        {
            Assert.Inconclusive("Explicitly opt in to this public read-only native schedule."); return;
        }
        using var source = MapLibreStyleQuerySource.ForReviewedTeachingCatalogue(Owner);
        var activated = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        Check(await Read(source, Ids), Ids);
        Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
    }

    private static Task<OASISResult<IReadOnlyList<IHolon>>> Read(MapLibreStyleQuerySource source, Guid[] ids)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return source.ExecuteCosmicQueryAsync(plan.Result);
    }

    private static void Check(OASISResult<IReadOnlyList<IHolon>> result, Guid[] expected)
    {
        Assert.IsFalse(result.IsError, result.Exception?.ToString());
        CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
        foreach (var row in result.Result)
        {
            Assert.IsTrue(row.IsPublic && row.IsActive && row.CreatedByAvatarId == Owner && !row.Children.Any());
            var key = row.MetaData["MapLibreStyleKey"].ToString();
            Assert.AreEqual("https://demotiles.maplibre.org/" + MapLibreStyleQuerySource.ReviewedStylePath(key),
                row.MetaData["MapLibreStyleUrl"].ToString());
            Assert.AreEqual(key, row.ProviderUniqueStorageKey[Provider]);
            var text = row.MetaData["MapLibreStyleJson"].ToString();
            Assert.AreEqual(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text))),
                row.MetaData["MapLibreStyleJsonSha256"].ToString());
            Assert.AreEqual(64, row.MetaData["MapLibreContentSha256"].ToString().Length);
            Assert.IsTrue(int.Parse(row.MetaData["MapLibreContentBytes"].ToString()) > 0);
            Assert.IsFalse(text.Contains('\n'), "Store the complete JSON without formatting whitespace.");
            using var document = JsonDocument.Parse(text);
            Assert.AreEqual(8, document.RootElement.GetProperty("version").GetInt32());
            Assert.AreEqual(row.Name, document.RootElement.GetProperty("name").GetString());
            Assert.AreEqual(document.RootElement.GetProperty("sources").EnumerateObject().Count().ToString(),
                row.MetaData["MapLibreSourceCount"].ToString());
            Assert.AreEqual(document.RootElement.GetProperty("layers").GetArrayLength().ToString(),
                row.MetaData["MapLibreLayerCount"].ToString());
        }
    }

    private sealed class Replies : HttpMessageHandler
    {
        public string Fault { get; init; }
        public string FailStyle { get; set; }
        public int Requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Requests);
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.IsNull(request.Headers.Authorization);
            Assert.AreEqual("application/json", request.Headers.Accept.Single().MediaType);
            var key = request.RequestUri.AbsoluteUri switch
            {
                "https://demotiles.maplibre.org/style.json" => "world",
                "https://demotiles.maplibre.org/styles/osm-bright-gl-style/style.json" => "bright",
                "https://demotiles.maplibre.org/styles/osm-bright-gl-terrain/style.json" => "terrain",
                _ => throw new AssertFailedException("Unexpected child resource or endpoint requested.")
            };
            var source = new Dictionary<string, object> { ["type"] = "vector", ["url"] = "https://foreign.invalid/tiles.json" };
            var layer = new Dictionary<string, object> { ["id"] = "land", ["type"] = "fill", ["source"] = "demo" };
            var root = new Dictionary<string, object> { ["version"] = 8, ["name"] = "Style " + key,
                ["sources"] = new Dictionary<string, object> { ["demo"] = source }, ["layers"] = new[] { layer },
                ["glyphs"] = "https://foreign.invalid/font/{range}.pbf" };
            if (Fault == "wrong-version") root["version"] = 7;
            if (Fault == "fractional-version") root["version"] = 8.5;
            if (Fault == "missing-name") root.Remove("name");
            if (Fault == "empty-name") root["name"] = " ";
            if (Fault == "oversized-name") root["name"] = new string('a', 257);
            if (Fault == "empty-sources") root["sources"] = new Dictionary<string, object>();
            if (Fault == "bad-source") root["sources"] = "wrong";
            if (Fault == "bad-source-kind") source["type"] = "foreign-kind";
            if (Fault == "empty-layers") root["layers"] = Array.Empty<object>();
            if (Fault == "too-many-layers") root["layers"] = Enumerable.Range(0, 513).Select(x => new { id = "layer-" + x, type = "background" }).ToArray();
            if (Fault == "duplicate-layer") root["layers"] = new[] { layer, layer };
            if (Fault == "bad-layer-kind") layer["type"] = "foreign-kind";
            if (Fault == "unknown-layer-source") layer["source"] = "absent";
            if (Fault == "missing-layers") root.Remove("layers");
            var json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
            if (Fault == "malformed-json") json = "{";
            if (Fault == "duplicate-root") json = json[..^1] + ",\"version\":8}";
            if (Fault == "duplicate-nested") json = json[..^1] + ",\"metadata\":{\"x\":1,\"x\":2}}";
            if (Fault == "oversized-body") json += new string(' ', 262145);
            var response = new HttpResponseMessage(Fault == "http-failure" || FailStyle == key ? HttpStatusCode.ServiceUnavailable
                : Fault == "redirect" ? HttpStatusCode.Redirect : Fault == "partial-content" ? HttpStatusCode.PartialContent : HttpStatusCode.OK);
            response.Content = Fault == "invalid-utf8" ? new ByteArrayContent(new byte[] { 255 })
                : new StringContent(json, Encoding.UTF8, Fault == "wrong-content-type" ? "text/plain" : "application/json");
            if (Fault == "invalid-utf8") response.Content.Headers.ContentType = new("application/json");
            return Task.FromResult(response);
        }
    }
}
