using System;
using System.Buffers.Binary;
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
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.WormholeOASIS;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests;

[TestClass]
public class WormholePublicOperationMetadataTests
{
    private static readonly string[] Ids = Enumerable.Range(0, 3).Select(i => "2/" + new string('a', 64) + "/" + ((ulong.MaxValue - (ulong)i).ToString())).ToArray();
    private static byte[] Body(string id)
    {
        var body = new byte[58];
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(0, 4), uint.MaxValue);
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(4, 4), 123);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(8, 2), 2);
        Convert.FromHexString(new string('a', 64)).CopyTo(body, 10);
        BinaryPrimitives.WriteUInt64BigEndian(body.AsSpan(42, 8), ulong.Parse(id.Split('/')[2]));
        body[50] = 15; Encoding.ASCII.GetBytes("payload").CopyTo(body, 51);
        return body;
    }
    private static Dictionary<string, string> Catalogue() => Ids.ToDictionary(id => id,
        id => Convert.ToHexString(SHA256.HashData(Body(id))).ToLowerInvariant());

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "";
        public List<string> Calls = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Get, request.Method); Assert.IsNull(request.Content);
            Assert.IsNull(request.Headers.Authorization); Assert.IsFalse(request.Headers.Contains("Cookie"));
            Assert.AreEqual("", request.RequestUri.Query); Assert.AreEqual("", request.RequestUri.Fragment);
            const string prefix = "/api/v1/operations/";
            Assert.IsTrue(request.RequestUri.AbsolutePath.StartsWith(prefix));
            string id = request.RequestUri.AbsolutePath.Substring(prefix.Length);
            Assert.IsTrue(Ids.Contains(id)); Calls.Add(id);
            var body = Body(id);
            if (Fault == "changed-body") body[51] ^= 1;
            var bytes = new byte[6 + 66 + body.Length];
            bytes[0] = 1; BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(1, 4), 7); bytes[5] = 1;
            body.CopyTo(bytes, 72);
            if (Fault == "version") bytes[0] = 2;
            if (Fault == "signature-frame") bytes[5] = 2;
            if (Fault == "truncated") bytes = bytes.Take(80).ToArray();
            var parts = id.Split('/');
            var envelope = new Dictionary<string, object> { ["raw"] = Convert.ToBase64String(bytes),
                ["guardianSetIndex"] = Fault == "guardian" ? 8 : 7, ["isDuplicated"] = Fault == "ambiguous" };
            if (Fault == "base64") envelope["raw"] = "AQ== ";
            var record = new Dictionary<string, object> { ["id"] = Fault == "id" ? "different" : id,
                ["emitterChain"] = Fault == "chain" ? 1 : 2,
                ["emitterAddress"] = new { hex = new string(Fault == "address" ? 'b' : 'a', 64) },
                ["sequence"] = Fault == "sequence" ? "0" : parts[2], ["vaa"] = envelope };
            if (Fault == "error") record["error"] = false;
            if (Fault == "missing") record.Remove("vaa");
            string text = JsonSerializer.Serialize(record);
            if (Fault == "duplicate-json") text = "{\"id\":\"x\",\"id\":\"x\"}";
            if (Fault == "oversize") text = new string(' ', 262145);
            bool denied = Fault == "http" || Fault == "last" && id == Ids.Last();
            return Task.FromResult(new HttpResponseMessage(denied ? HttpStatusCode.NotFound : HttpStatusCode.OK) {
                Content = new StringContent(text, Encoding.UTF8, "application/json") });
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("subset")] [DataRow("window")]
    [DataRow("changed-body")] [DataRow("version")] [DataRow("signature-frame")] [DataRow("truncated")]
    [DataRow("guardian")] [DataRow("ambiguous")] [DataRow("base64")] [DataRow("id")] [DataRow("chain")]
    [DataRow("address")] [DataRow("sequence")] [DataRow("error")] [DataRow("missing")]
    [DataRow("duplicate-json")] [DataRow("oversize")] [DataRow("http")] [DataRow("last")]
    [DataRow("empty-window-error")] [DataRow("budget")] [DataRow("unknown")] [DataRow("cancel")] [DataRow("inactive")]
    public async Task CompletePinnedSelectionOrExplicitError(string scenario)
    {
        var native = new Native(); using var client = new HttpClient(native); var owner = Guid.NewGuid();
        using var source = new WormholePublicOperationMetadataSource(client, new Uri("https://example.invalid/"), Catalogue(), owner);
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Calls.Clear(); native.Fault = scenario == "empty-window-error" ? "last" : scenario;
        var keys = scenario == "subset" ? Ids.Take(2).ToArray() : scenario == "unknown" ? new[] { "2/" + new string('b', 64) + "/0" } : Ids;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.WormholeOASIS).Result)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, WormholePublicOperationMetadataSource.AddressMetadata))).Result;
        if (scenario == "point") {
            string key = Ids[0];
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.WormholeOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, WormholePublicOperationMetadataSource.AddressMetadata) == key).Result;
        }
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.WormholeOASIS,
            new[] { new CosmicQueryTarget(ProviderType.WormholeOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancel = new CancellationTokenSource(); if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "normal" or "point" or "subset" or "window") {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario is "point" or "window" ? 1 : scenario == "subset" ? 2 : 3, result.Result.Count);
            CollectionAssert.AreEqual(scenario == "point" ? Ids.Take(1).ToArray() : keys, native.Calls.ToArray());
            foreach (var row in result.Result) {
                Assert.IsTrue(row.IsPublic); Assert.AreEqual(owner, row.CreatedByAvatarId);
                Assert.AreEqual("false", row.MetaData["WormholeSignaturesVerified"]);
                Assert.AreEqual("4294967295", row.MetaData["WormholeTimestamp"]);
                Assert.AreEqual(Catalogue()[row.Name], row.MetaData["WormholeBodySha256"]);
                Assert.AreEqual(row.Name.Split('/')[2], row.MetaData["WormholeSequence"]);
            }
        } else { Assert.IsTrue(result.IsError); Assert.IsTrue(result.Result == null || result.Result.Count == 0); }
        if (scenario is "inactive" or "cancel" or "unknown") Assert.AreEqual(0, native.Calls.Count);
        if (scenario is "last" or "empty-window-error") Assert.AreEqual(3, native.Calls.Count);
    }

    [TestMethod]
    public async Task UnsupportedQueriesAndNativeWritesNeverDispatch()
    {
        var native = new Native(); using var client = new HttpClient(native);
        using var source = new WormholePublicOperationMetadataSource(client, new Uri("https://example.invalid/"), Catalogue(), Guid.NewGuid());
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.WormholeOASIS).Result).Where(row => row.Name == "any").Result;
        Assert.IsTrue(CosmicQueryPlanner.Plan(query, ProviderType.WormholeOASIS,
            new[] { new CosmicQueryTarget(ProviderType.WormholeOASIS, source.QueryCapabilities) }).IsError);
        Assert.IsTrue((await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), IsNewHolon = true }, saveChildren: false)).IsError);
        Assert.IsTrue((await source.DeleteHolonAsync(Guid.NewGuid())).IsError);
        Assert.AreEqual(0, native.Calls.Count);
    }

    [DataTestMethod]
    [DataRow("scheme")] [DataRow("owner")] [DataRow("duplicate")] [DataRow("fingerprint")]
    [DataRow("sequence")] [DataRow("address")] [DataRow("chain")] [DataRow("count")]
    public void InvalidCatalogueFailsBeforeAnyRequest(string fault)
    {
        var entries = Catalogue().ToList();
        if (fault == "duplicate") entries.Add(entries[0]);
        if (fault == "fingerprint") entries[0] = new(entries[0].Key, "bad");
        if (fault == "sequence") entries[0] = new("2/" + new string('a', 64) + "/01", entries[0].Value);
        if (fault == "address") entries[0] = new("2/" + new string('A', 64) + "/0", entries[0].Value);
        if (fault == "chain") entries[0] = new("0/" + new string('a', 64) + "/0", entries[0].Value);
        if (fault == "count") entries.Clear();
        using var client = new HttpClient(new Native());
        Assert.ThrowsException<ArgumentException>(() => new WormholePublicOperationMetadataSource(client,
            new Uri(fault == "scheme" ? "http://example.invalid/" : "https://example.invalid/"), entries,
            fault == "owner" ? Guid.Empty : Guid.NewGuid()));
    }
}
