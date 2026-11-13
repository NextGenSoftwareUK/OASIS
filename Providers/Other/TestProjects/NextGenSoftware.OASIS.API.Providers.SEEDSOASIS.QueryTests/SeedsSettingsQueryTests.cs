using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
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

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.QueryTests;
[TestClass]
public class SeedsSettingsQueryTests
{
    private const ProviderType Provider = ProviderType.SEEDSOASIS;
    private static readonly Guid Owner = Guid.Parse("825d3154-a7dc-4185-ae75-7de2be68a32b");
    private static readonly string[] Keys = { "unity.high", "unity.medium", "unity.low" };
    private static readonly Guid[] Ids = Keys.Select(key => SeedsSettingsQuerySource.LogicalId(Owner, key)).ToArray();
    private static readonly Dictionary<string, ulong> Bounds = new()
    {
        ["unity.high"] = 15338590735463301120UL,
        ["unity.medium"] = 15338590738079003936UL,
        ["unity.low"] = 15338590737719623680UL
    };

    [TestMethod]
    public async Task CurrentObservationsPreserveMetadataAndPreflightAllKeys()
    {
        using var replies = new Replies(); using var source = new SeedsSettingsQuerySource(Owner, Keys, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        Check(await Read(source, Ids), Ids); Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
        Assert.AreEqual(12, replies.Requests, "One info and one exact table read per observation, no retries or scans.");
        var before = replies.Requests;
        foreach (var ids in new[] { new[] { Guid.NewGuid() }, new[] { Ids[0], Guid.NewGuid() } })
        {
            var result = await Read(source, ids); Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
            Assert.AreEqual(before, replies.Requests);
        }
    }

    [TestMethod]
    public async Task FullUint64SurvivesWithoutFloatingPointConversion()
    {
        using var replies = new Replies { MaximumValue = true }; using var source = new SeedsSettingsQuerySource(Owner, Keys, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        var result = await Read(source, Ids); Check(result, Ids);
        Assert.IsTrue(result.Result.All(row => row.MetaData["SeedsSettingValue"].ToString() == "18446744073709551615"));
        foreach (var row in result.Result)
        {
            using var json = JsonDocument.Parse(row.MetaData["SeedsRowJson"].ToString());
            Assert.AreEqual(ulong.MaxValue, json.RootElement.GetProperty("value").GetUInt64());
        }
    }

    [DataTestMethod]
    [DataRow("wrong-chain")]
    [DataRow("zero-head")]
    [DataRow("lib-ahead")]
    [DataRow("wrong-head-id")]
    [DataRow("uppercase-head")]
    [DataRow("bad-head")]
    [DataRow("missing-head")]
    [DataRow("empty")]
    [DataRow("wrong-key")]
    [DataRow("more")]
    [DataRow("next-key")]
    [DataRow("two-rows")]
    [DataRow("negative")]
    [DataRow("fraction")]
    [DataRow("overflow")]
    [DataRow("string-value")]
    [DataRow("missing-description")]
    [DataRow("huge-description")]
    [DataRow("bad-impact")]
    [DataRow("duplicate")]
    [DataRow("http")]
    [DataRow("media")]
    [DataRow("utf8")]
    [DataRow("oversized")]
    [DataRow("deep")]
    [DataRow("error")]
    public async Task FailedIdentityEnvelopeOrTableContractRejectsActivation(string fault)
    {
        using var replies = new Replies { Fault = fault }; using var source = new SeedsSettingsQuerySource(Owner, Keys, replies);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError, fault); Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", result.ErrorCode);
        Assert.IsTrue(replies.Requests <= 2, "No retry or continuing beyond failed required stage.");
    }

    [TestMethod]
    public async Task FailedSelectedRecordReturnsNoPartialRows()
    {
        using var replies = new Replies(); using var source = new SeedsSettingsQuerySource(Owner, Keys, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError); replies.FailKey = "unity.medium";
        var result = await Read(source, Ids); Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual(8, replies.Requests);
    }

    [TestMethod]
    public async Task InactiveAndCancelledReadsDoNotDispatch()
    {
        using var replies = new Replies(); using var source = new SeedsSettingsQuerySource(Owner, Keys, replies);
        Assert.IsTrue((await Read(source, Ids)).IsError);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.IsTrue((await source.ActivateQuerySourceAsync(cancelled.Token)).IsError); Assert.AreEqual(0, replies.Requests);
    }

    [TestMethod]
    public void InvalidCataloguesFailBeforeConstructingTransport()
    {
        var configurationOwner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
        CollectionAssert.AreEqual(new[] { "999101fa-bdd3-8b42-969e-eafeaaad3e64", "e8b73bc1-15f5-8dad-b92d-47b8750638eb", "743062bf-03df-81d3-a29b-6eb015a80858" },
            Keys.Select(x => SeedsSettingsQuerySource.LogicalId(configurationOwner, x).ToString()).ToArray());
        foreach (var keys in new[] { Array.Empty<string>(), new[] { "unity.high", "unity.high" }, new[] { "UNITY.HIGH" },
                     new[] { "other" }, new[] { "unity.high", "unity.medium", "unity.low", "unity.high" } })
            Assert.ThrowsException<ArgumentException>(() => new SeedsSettingsQuerySource(Owner, keys));
        Assert.ThrowsException<ArgumentException>(() => new SeedsSettingsQuerySource(Guid.Empty, Keys));
        Assert.AreNotEqual(Ids[0], SeedsSettingsQuerySource.LogicalId(Guid.NewGuid(), Keys[0]));
    }

    [TestMethod, TestCategory("SeedsSettingsNative")]
    public async Task OfficialConfigurationReturnsThreeAndTwoThroughCql()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_SEEDS_SETTINGS_VERIFY_PUBLIC") != "1")
        { Assert.Inconclusive("Explicit opt-in to the reviewed public configuration schedule is required."); return; }
        using var source = SeedsSettingsQuerySource.ForReviewedTeachingCatalogue(Owner);
        var activation = await source.ActivateQuerySourceAsync(); Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        Check(await Read(source, Ids), Ids); Check(await Read(source, Ids.Take(2).ToArray()), Ids.Take(2).ToArray());
    }

    private static Task<OASISResult<IReadOnlyList<IHolon>>> Read(SeedsSettingsQuerySource source, Guid[] ids)
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
            var key = row.MetaData["SeedsSettingKey"].ToString(); Assert.IsTrue(Keys.Contains(key));
            Assert.AreEqual(key, row.ProviderUniqueStorageKey[Provider]); Assert.AreEqual("SEEDS " + key, row.Name);
            Assert.AreEqual(SeedsSettingsQuerySource.ChainId, row.MetaData["SeedsChainId"].ToString());
            Assert.AreEqual("settgs.seeds", row.MetaData["SeedsContract"].ToString());
            Assert.AreEqual("config", row.MetaData["SeedsTable"].ToString());
            Assert.AreEqual(SeedsSettingsQuerySource.Consistency, row.MetaData["SeedsConsistency"].ToString());
            Assert.IsTrue(ulong.TryParse(row.MetaData["SeedsSettingValue"].ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out _));
            using var json = JsonDocument.Parse(row.MetaData["SeedsRowJson"].ToString());
            Assert.AreEqual(key, json.RootElement.GetProperty("param").GetString());
            Assert.AreEqual(row.MetaData["SeedsSettingValue"].ToString(), json.RootElement.GetProperty("value").GetUInt64().ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(row.Description, json.RootElement.GetProperty("description").GetString());
        }
    }

    private sealed class Replies : HttpMessageHandler
    {
        public string Fault { get; init; }
        public string FailKey { get; set; }
        public bool MaximumValue { get; init; }
        public int Requests;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Requests); Assert.AreEqual(HttpMethod.Post, request.Method); Assert.IsNull(request.Headers.Authorization);
            Assert.AreEqual("mainnet.telos.net", request.RequestUri.Host);
            using var body = JsonDocument.Parse(await request.Content.ReadAsStringAsync(token));
            string text;
            if (request.RequestUri.AbsolutePath == "/v1/chain/get_info")
            {
                Assert.AreEqual(0, body.RootElement.EnumerateObject().Count());
                var hash = "00000064" + new string('a', 56);
                text = JsonSerializer.Serialize(new { chain_id = Fault == "wrong-chain" ? new string('0', 64) : SeedsSettingsQuerySource.ChainId,
                    head_block_num = Fault == "zero-head" ? 0 : 100, last_irreversible_block_num = Fault == "lib-ahead" ? 101 : 99,
                    head_block_id = Fault == "wrong-head-id" ? "00000065" + new string('a', 56) : Fault == "uppercase-head" ? hash.ToUpperInvariant() : Fault == "bad-head" ? "bad" : hash });
                if (Fault == "missing-head") text = text.Replace("head_block_num", "other_head_num");
            }
            else
            {
                Assert.AreEqual("/v1/chain/get_table_rows", request.RequestUri.AbsolutePath);
                var args = body.RootElement;
                var lower = ulong.Parse(args.GetProperty("lower_bound").GetString(), CultureInfo.InvariantCulture);
                var key = Bounds.Single(row => row.Value == lower).Key;
                Assert.AreEqual((lower + 1).ToString(CultureInfo.InvariantCulture), args.GetProperty("upper_bound").GetString());
                Assert.AreEqual("settgs.seeds", args.GetProperty("code").GetString()); Assert.AreEqual("settgs.seeds", args.GetProperty("scope").GetString());
                Assert.AreEqual("config", args.GetProperty("table").GetString()); Assert.AreEqual(1, args.GetProperty("limit").GetInt32());
                Assert.AreEqual("i64", args.GetProperty("key_type").GetString()); Assert.AreEqual("1", args.GetProperty("index_position").GetString());
                Assert.IsTrue(args.GetProperty("json").GetBoolean()); Assert.IsFalse(args.GetProperty("reverse").GetBoolean()); Assert.IsFalse(args.GetProperty("show_payer").GetBoolean());
                if (key == FailKey) return new(HttpStatusCode.ServiceUnavailable);
                var row = new { param = Fault == "wrong-key" ? "other" : key, value = MaximumValue ? ulong.MaxValue : 90UL,
                    description = Fault == "huge-description" ? new string('x', 2049) : "Test native configuration", impact = Fault == "bad-impact" ? "other" : "high" };
                var rows = Fault == "empty" ? Array.Empty<object>() : Fault == "two-rows" ? new object[] { row, row } : new object[] { row };
                text = JsonSerializer.Serialize(new { rows, more = Fault == "more", next_key = Fault == "next-key" ? "more" : "" });
                if (Fault == "missing-description") text = text.Replace("description", "other_description");
                if (Fault == "negative") text = text.Replace("\"value\":90", "\"value\":-1");
                if (Fault == "fraction") text = text.Replace("\"value\":90", "\"value\":1.5");
                if (Fault == "overflow") text = text.Replace("\"value\":90", "\"value\":18446744073709551616");
                if (Fault == "string-value") text = text.Replace("\"value\":90", "\"value\":\"90\"");
            }
            if (Fault == "duplicate") text = text[..^1] + ",\"x\":1,\"x\":2}";
            if (Fault == "error") text = "{\"error\":{\"code\":500}}";
            if (Fault == "deep") text = new string('[', 40) + "0" + new string(']', 40);
            byte[] bytes = Fault == "oversized" ? new byte[131073] : Fault == "utf8" ? new byte[] { 255 } : Encoding.UTF8.GetBytes(text);
            var response = new HttpResponseMessage(Fault == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
            response.Content.Headers.ContentType = new(Fault == "media" ? "text/html" : "application/json");
            return response;
        }
    }
}
