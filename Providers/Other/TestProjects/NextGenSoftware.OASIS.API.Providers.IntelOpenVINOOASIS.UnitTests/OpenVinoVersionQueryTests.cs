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

namespace NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS.UnitTests;
[TestClass]
public class OpenVinoVersionQueryTests
{
    private const ProviderType Provider = ProviderType.IntelOpenVINOOASIS;
    private static readonly Guid Owner = Guid.Parse("955d502d-1c46-4e3b-a105-ea8f4a6865b0");
    private static readonly string[] Versions = { "1", "2", "3" };
    private static readonly Guid[] Ids = Versions.Select(key => OpenVinoVersionQuerySource.LogicalId(Owner, key)).ToArray();

    [TestMethod]
    public async Task FullAndSubsetQueriesPreserveNativeMetadataAndPreflightEveryId()
    {
        using var replies = new Replies(); using var source = new OpenVinoVersionQuerySource(Owner, Versions, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        Check(await Read(source, Ids), Ids); Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
        Assert.AreEqual(18, replies.Calls, "Three SDK metadata/readiness GETs per observation; no inference or scan.");
        var before = replies.Calls;
        foreach (var ids in new[] { new[] { Guid.NewGuid() }, new[] { Ids[0], Guid.NewGuid() } })
        {
            var result = await Read(source, ids); Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
            Assert.AreEqual(before, replies.Calls);
        }
    }

    [DataTestMethod]
    [DataRow("server-name")]
    [DataRow("server-version")]
    [DataRow("not-ready")]
    [DataRow("model-name")]
    [DataRow("platform")]
    [DataRow("missing-version")]
    [DataRow("duplicate-version")]
    [DataRow("leading-zero")]
    [DataRow("zero-version")]
    [DataRow("many-versions")]
    [DataRow("input-name")]
    [DataRow("input-type")]
    [DataRow("input-shape")]
    [DataRow("output-name")]
    [DataRow("output-type")]
    [DataRow("output-shape")]
    public async Task InvalidNativeIdentityVersionOrTensorShapeRejectsActivation(string fault)
    {
        using var replies = new Replies { Fault = fault }; using var source = new OpenVinoVersionQuerySource(Owner, Versions, replies);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError, fault); Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", result.ErrorCode); Assert.IsTrue(replies.Calls <= 3);
    }

    [TestMethod]
    public async Task AdditionalAdvertisedVersionsDoNotBecomeAdmittedRecords()
    {
        using var replies = new Replies { ExtraVersion = true }; using var source = new OpenVinoVersionQuerySource(Owner, Versions.Take(2), replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        var result = await Read(source, Ids.Take(2).ToArray()); Check(result, Ids.Take(2).ToArray());
        Assert.IsTrue(result.Result.All(row => row.MetaData["OpenVinoAdvertisedVersionsJson"].ToString().Contains("4")));
        var before = replies.Calls; Assert.IsTrue((await Read(source, new[] { Ids[2] })).IsError); Assert.AreEqual(before, replies.Calls);
    }

    [TestMethod]
    public async Task FailedSelectedVersionReturnsNoPartialRows()
    {
        using var replies = new Replies(); using var source = new OpenVinoVersionQuerySource(Owner, Versions, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError); replies.FailVersion = "2";
        var result = await Read(source, Ids); Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual(12, replies.Calls);
    }

    [TestMethod]
    public async Task InactiveCancelledAndDisposedSourcesDoNotDispatch()
    {
        using var replies = new Replies(); using var source = new OpenVinoVersionQuerySource(Owner, Versions, replies);
        Assert.IsTrue((await Read(source, Ids)).IsError);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.IsTrue((await source.ActivateQuerySourceAsync(cancelled.Token)).IsError); Assert.AreEqual(0, replies.Calls);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError); var before = replies.Calls; source.Dispose();
        Assert.IsTrue((await Read(source, Ids)).IsError); Assert.AreEqual(before, replies.Calls);
    }

    [TestMethod]
    public void InvalidCataloguesFailBeforeNativeTransportConstruction()
    {
        var configurationOwner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
        CollectionAssert.AreEqual(new[] { "76dc34ff-dbbc-8541-9fd4-6708c14086a9", "9a25a55f-5a66-85d0-9700-891eacd87dd6", "acd7cd30-fffe-844a-a048-a20e67e1afed" },
            Versions.Select(x => OpenVinoVersionQuerySource.LogicalId(configurationOwner, x).ToString()).ToArray());
        foreach (var versions in new[] { Array.Empty<string>(), new[] { "1", "1" }, new[] { "01" }, new[] { "4" }, new[] { "../1" }, new[] { "1", "2", "3", "1" } })
            Assert.ThrowsException<ArgumentException>(() => new OpenVinoVersionQuerySource(Owner, versions));
        Assert.ThrowsException<ArgumentException>(() => new OpenVinoVersionQuerySource(Guid.Empty, Versions));
        Assert.AreNotEqual(Ids[0], OpenVinoVersionQuerySource.LogicalId(Guid.NewGuid(), "1"));
    }

    [TestMethod, TestCategory("OpenVinoModelNative")]
    public async Task OpenVinoModelNativeReturnsThreeAndTwoThroughCql()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_OPENVINO_MODEL_NATIVE") != "owned-loopback-28000")
        { Assert.Inconclusive("Explicit owned OVMS fixture required."); return; }
        using var source = OpenVinoVersionQuerySource.ForReviewedTeachingCatalogue(Owner);
        var activation = await source.ActivateQuerySourceAsync(); Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        Check(await Read(source, Ids), Ids); Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
    }

    private static Task<OASISResult<IReadOnlyList<IHolon>>> Read(OpenVinoVersionQuerySource source, Guid[] ids)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message); return source.ExecuteCosmicQueryAsync(plan.Result);
    }

    private static void Check(OASISResult<IReadOnlyList<IHolon>> result, Guid[] expected)
    {
        Assert.IsFalse(result.IsError, result.Exception?.ToString());
        CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
        foreach (var row in result.Result)
        {
            Assert.IsTrue(row.IsPublic && row.IsActive && row.CreatedByAvatarId == Owner && !row.Children.Any());
            var version = row.MetaData["OpenVinoRequestedVersion"].ToString(); Assert.IsTrue(Versions.Contains(version));
            Assert.AreEqual(version, row.ProviderUniqueStorageKey[Provider]);
            Assert.AreEqual("OpenVINO cosmic_dummy version " + version, row.Name);
            Assert.AreEqual(OpenVinoVersionQuerySource.ServerVersion, row.MetaData["OpenVinoServerVersion"].ToString());
            var compact = row.MetaData["OpenVinoMetadataJson"].ToString();
            Assert.AreEqual(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(compact))), row.MetaData["OpenVinoMetadataJsonSha256"].ToString());
            using var document = JsonDocument.Parse(compact); var model = document.RootElement;
            Assert.AreEqual("cosmic_dummy", model.GetProperty("name").GetString());
            Assert.AreEqual("FP32", model.GetProperty("inputs")[0].GetProperty("datatype").GetString());
            CollectionAssert.AreEqual(new long[] { 1, 10 }, model.GetProperty("inputs")[0].GetProperty("shape").EnumerateArray().Select(x => x.GetInt64()).ToArray());
            Assert.AreEqual("FP16", model.GetProperty("rt_info").GetProperty("model_info").GetProperty("precision").GetString());
            Assert.IsTrue(model.GetProperty("versions").EnumerateArray().Any(x => x.GetString() == version));
            Assert.AreEqual(JsonSerializer.Serialize(model.GetProperty("versions")), row.MetaData["OpenVinoAdvertisedVersionsJson"].ToString());
        }
    }

    private sealed class Replies : HttpMessageHandler
    {
        public string Fault { get; init; }
        public string FailVersion { get; set; }
        public bool ExtraVersion { get; init; }
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls); ct.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Get, request.Method); Assert.IsNull(request.Content); Assert.IsNull(request.Headers.Authorization);
            Assert.AreEqual("http://127.0.0.1:28000", request.RequestUri.GetLeftPart(UriPartial.Authority));
            var path = request.RequestUri.AbsolutePath; object result;
            if (path == "/v2") result = new { name = Fault == "server-name" ? "other" : "OpenVINO Model Server", version = Fault == "server-version" ? "wrong" : OpenVinoVersionQuerySource.ServerVersion };
            else
            {
                var pieces = path.Split('/'); Assert.AreEqual("models", pieces[2]); Assert.AreEqual("cosmic_dummy", pieces[3]); Assert.AreEqual("versions", pieces[4]);
                var version = pieces[5]; Assert.IsTrue(Versions.Contains(version));
                if (path.EndsWith("/ready", StringComparison.Ordinal))
                    return Task.FromResult(new HttpResponseMessage(Fault == "not-ready" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK) { Content = new ByteArrayContent(Array.Empty<byte>()) });
                Assert.AreEqual(6, pieces.Length, "Metadata only; no inference, listing or management request.");
                if (version == FailVersion) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("{\"error\":\"controlled\"}", Encoding.UTF8, "application/json") });
                var versions = Fault == "missing-version" ? new[] { "2", "3" } : Fault == "duplicate-version" ? new[] { "1", "1", "2" }
                    : Fault == "leading-zero" ? new[] { "01", "2", "3" } : Fault == "zero-version" ? new[] { "0", "1", "2" }
                    : Fault == "many-versions" ? Enumerable.Range(1, 33).Select(x => x.ToString()).ToArray()
                    : ExtraVersion ? new[] { "1", "2", "3", "4" } : Versions;
                result = new { name = Fault == "model-name" ? "other" : "cosmic_dummy", versions, platform = Fault == "platform" ? "other" : "OpenVINO",
                    inputs = new[] { new { name = Fault == "input-name" ? "x" : "b", datatype = Fault == "input-type" ? "FP16" : "FP32", shape = Fault == "input-shape" ? new[] { 1, 9 } : new[] { 1, 10 } } },
                    outputs = new[] { new { name = Fault == "output-name" ? "x" : "a", datatype = Fault == "output-type" ? "FP16" : "FP32", shape = Fault == "output-shape" ? new[] { 2, 10 } : new[] { 1, 10 } } },
                    rt_info = new { model_info = new { precision = "FP16", resolution = new { height = "200", width = "300" } } } };
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(result), Encoding.UTF8, "application/json") });
        }
    }
}
