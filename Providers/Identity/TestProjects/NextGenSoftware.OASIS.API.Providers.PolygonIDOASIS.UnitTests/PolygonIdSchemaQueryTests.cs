using System;
using System.Collections.Generic;
using System.IO;
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

namespace NextGenSoftware.OASIS.API.Providers.PolygonIDOASIS.UnitTests;

[TestClass]
public class PolygonIdSchemaQueryTests
{
    private const ProviderType Provider = ProviderType.PolygonIDOASIS;
    private static readonly Guid Owner = Guid.Parse("c9098f3c-4d0c-4c12-9e99-21ae32ccf722");
    private static readonly string[] Keys = { "age-v3", "country-v2", "auth-v1" };
    private static readonly Guid[] Ids = Keys.Select(key => PolygonIdSchemaQuerySource.LogicalId(Owner, key)).ToArray();

    [TestMethod]
    public async Task ExactFullAndSubsetKeepCompleteSchemasAndNeverFetchReferences()
    {
        using var replies = new Replies();
        using var source = new PolygonIdSchemaQuerySource(Owner, Keys, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        Check(await Read(source, Ids), Ids);
        Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
        Assert.AreEqual(6, replies.Requests);
        var before = replies.Requests;
        foreach (var ids in new[] { new[] { Guid.NewGuid() }, new[] { Ids[0], Guid.NewGuid() } })
        {
            var result = await Read(source, ids);
            Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
            Assert.AreEqual(before, replies.Requests);
        }
    }

    [DataTestMethod]
    [DataRow("http")]
    [DataRow("redirect")]
    [DataRow("empty")]
    [DataRow("media")]
    [DataRow("html")]
    [DataRow("utf8")]
    [DataRow("malformed")]
    [DataRow("duplicate")]
    [DataRow("deep")]
    [DataRow("oversized")]
    [DataRow("dialect")]
    [DataRow("changed")]
    [DataRow("properties")]
    [DataRow("required")]
    [DataRow("metadata")]
    public async Task TransportEnvelopeSchemaOrContentPinFailureRejectsActivation(string fault)
    {
        using var replies = new Replies { Fault = fault };
        using var source = new PolygonIdSchemaQuerySource(Owner, Keys, replies);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError, fault); Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", result.ErrorCode);
        Assert.AreEqual(1, replies.Requests, "No retry or reference traversal after failure.");
    }

    [TestMethod]
    public async Task FailedSelectedSchemaReturnsNoPartialRows()
    {
        using var replies = new Replies();
        using var source = new PolygonIdSchemaQuerySource(Owner, Keys, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        replies.FailKey = "country-v2";
        var result = await Read(source, Ids);
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual(4, replies.Requests);
    }

    [TestMethod]
    public async Task InactiveAndCancelledReadsDoNotUseTransport()
    {
        using var replies = new Replies();
        using var source = new PolygonIdSchemaQuerySource(Owner, Keys, replies);
        Assert.IsTrue((await Read(source, Ids)).IsError);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.IsTrue((await source.ActivateQuerySourceAsync(cancellation.Token)).IsError);
        Assert.AreEqual(0, replies.Requests);
    }

    [TestMethod]
    public void InvalidCataloguesFailBeforeTransportConstruction()
    {
        var configurationOwner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
        CollectionAssert.AreEqual(new[] { "b33f9708-a3da-805c-8d7f-73fc6c7980ea", "395a8ace-db5c-81d0-a31b-2b4b83f27d40", "c972e7a0-677c-8b43-b2bb-fe90da46f9d1" },
            Keys.Select(x => PolygonIdSchemaQuerySource.LogicalId(configurationOwner, x).ToString()).ToArray());
        foreach (var keys in new[] { Array.Empty<string>(), new[] { "age-v3", "age-v3" }, new[] { "AGE-V3" },
                     new[] { "../secret" }, new[] { "age-v3", "country-v2", "auth-v1", "age-v3" } })
            Assert.ThrowsException<ArgumentException>(() => new PolygonIdSchemaQuerySource(Owner, keys));
        Assert.ThrowsException<ArgumentException>(() => new PolygonIdSchemaQuerySource(Guid.Empty, Keys));
        Assert.AreNotEqual(PolygonIdSchemaQuerySource.LogicalId(Owner, Keys[0]),
            PolygonIdSchemaQuerySource.LogicalId(Guid.NewGuid(), Keys[0]));
    }

    [TestMethod, TestCategory("PolygonIdSchemaNative")]
    public async Task OfficialPinnedSchemasReturnThreeAndTwoThroughCql()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_POLYGON_ID_SCHEMA_VERIFY_PUBLIC") != "1")
        {
            Assert.Inconclusive("Explicit opt-in to the reviewed public schema schedule is required."); return;
        }
        using var source = PolygonIdSchemaQuerySource.ForReviewedTeachingCatalogue(Owner);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        Check(await Read(source, Ids), Ids);
        Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
    }

    private static Task<OASISResult<IReadOnlyList<IHolon>>> Read(PolygonIdSchemaQuerySource source, Guid[] ids)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return source.ExecuteCosmicQueryAsync(plan.Result);
    }

    private static byte[] Fixture(string key)
    {
        using var input = typeof(PolygonIdSchemaQueryTests).Assembly.GetManifestResourceStream(
            "NextGenSoftware.OASIS.API.Providers.PolygonIDOASIS.UnitTests.Fixtures." + key + ".json");
        using var output = new MemoryStream(); input.CopyTo(output); return output.ToArray();
    }

    private static void Check(OASISResult<IReadOnlyList<IHolon>> result, Guid[] expected)
    {
        Assert.IsFalse(result.IsError, result.Exception?.ToString());
        CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
        foreach (var row in result.Result)
        {
            Assert.IsTrue(row.IsPublic && row.IsActive && row.CreatedByAvatarId == Owner && !row.Children.Any());
            var key = row.MetaData["PolygonIdSchemaKey"].ToString();
            Assert.AreEqual(PolygonIdSchemaQuerySource.ReviewedSha256(key), row.MetaData["PolygonIdContentSha256"].ToString());
            Assert.AreEqual(PolygonIdSchemaQuerySource.SourceCommit, row.MetaData["PolygonIdSourceCommit"].ToString());
            Assert.AreEqual(key, row.ProviderUniqueStorageKey[Provider]);
            var raw = Fixture(key);
            Assert.AreEqual(raw.Length.ToString(), row.MetaData["PolygonIdContentBytes"].ToString());
            var compact = row.MetaData["PolygonIdSchemaJson"].ToString();
            Assert.AreEqual(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(compact))), row.MetaData["PolygonIdSchemaJsonSha256"].ToString());
            using var original = JsonDocument.Parse(raw); using var projected = JsonDocument.Parse(compact);
            Assert.AreEqual(JsonSerializer.Serialize(original.RootElement), JsonSerializer.Serialize(projected.RootElement));
            Assert.IsFalse(projected.RootElement.TryGetProperty("title", out _));
            Assert.IsFalse(projected.RootElement.TryGetProperty("$id", out _));
        }
    }

    private sealed class Replies : HttpMessageHandler
    {
        public string Fault { get; init; }
        public string FailKey { get; set; }
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Requests); token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Get, request.Method); Assert.IsNull(request.Headers.Authorization);
            var key = Keys.Single(key => request.RequestUri.AbsoluteUri ==
                "https://raw.githubusercontent.com/iden3/claim-schema-vocab/" + PolygonIdSchemaQuerySource.SourceCommit + "/" + PolygonIdSchemaQuerySource.ReviewedSchemaPath(key));
            Assert.AreEqual("text/plain", request.Headers.Accept.Single().MediaType);
            var raw = Fixture(key); var text = Encoding.UTF8.GetString(raw);
            raw = Fault switch
            {
                "empty" => Array.Empty<byte>(), "utf8" => new byte[] { 255 }, "html" => Encoding.UTF8.GetBytes("<html>wrong</html>"),
                "malformed" => Encoding.UTF8.GetBytes("{"), "oversized" => new byte[65537],
                "deep" => Encoding.UTF8.GetBytes(new string('[', 40) + "0" + new string(']', 40)),
                "duplicate" => Encoding.UTF8.GetBytes(text.Replace("\"type\": \"object\"", "\"type\": \"object\",\"type\": \"object\"")),
                "dialect" => Encoding.UTF8.GetBytes(text.Replace("draft-07", "draft-04")),
                "changed" => Encoding.UTF8.GetBytes(text + " "),
                "properties" => Encoding.UTF8.GetBytes(text.Replace("\"properties\"", "\"other-properties\"")),
                "required" => Encoding.UTF8.GetBytes(text.Replace("\"required\"", "\"other-required\"")),
                "metadata" => Encoding.UTF8.GetBytes(text.Replace("\"$metadata\"", "\"other-metadata\"")),
                _ => raw
            };
            var status = Fault == "http" || key == FailKey ? HttpStatusCode.ServiceUnavailable
                : Fault == "redirect" ? HttpStatusCode.Found : HttpStatusCode.OK;
            var response = new HttpResponseMessage(status) { Content = new ByteArrayContent(raw) };
            response.Content.Headers.ContentType = new(Fault == "media" ? "application/json" : "text/plain");
            return Task.FromResult(response);
        }
    }
}
