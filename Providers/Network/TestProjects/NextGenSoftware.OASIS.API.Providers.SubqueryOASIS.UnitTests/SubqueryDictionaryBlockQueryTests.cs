using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SubqueryOASIS.UnitTests;

[TestClass]
public class SubqueryDictionaryBlockQueryTests
{
    private const ProviderType Provider = ProviderType.SubqueryOASIS;
    private static readonly Guid Owner = Guid.Parse("0b05ec72-ce47-49f4-8439-d29d978877bc");
    private static readonly long[] Heights = [400000000, 400000002, 400000003];
    private static readonly Guid[] Ids = Heights.Select(height => SubqueryDictionaryBlockQuerySource.LogicalId(Owner, height)).ToArray();

    [TestMethod]
    public async Task ExactKeySetsUseTheBoundedNativeDictionaryContract()
    {
        using var replies = new Replies();
        using var source = new SubqueryDictionaryBlockQuerySource(Owner, Heights, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        await Check(source, Ids, Ids);
        await Check(source, Ids[..2], Ids[..2]);
        Assert.AreEqual(6, replies.Requests);
        await Check(source, [Guid.NewGuid()], null);
        Assert.AreEqual(6, replies.Requests, "An unknown ID must not become an HTTP request.");
    }

    [DataTestMethod]
    [DataRow("graphql-error")]
    [DataRow("wrong-genesis")]
    [DataRow("lagging-index")]
    [DataRow("uncovered-start")]
    [DataRow("wrong-height")]
    [DataRow("noncanonical-height")]
    [DataRow("duplicate-height-field")]
    [DataRow("too-many-rows")]
    [DataRow("empty")]
    [DataRow("wrong-content-type")]
    [DataRow("oversized")]
    public async Task ActivationRejectsUnverifiedOrMalformedMatches(string fault)
    {
        using var replies = new Replies { Fault = fault };
        using var source = new SubqueryDictionaryBlockQuerySource(Owner, Heights, replies);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", result.ErrorCode);
        Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual(1, replies.Requests, "A malformed response is not retried.");
    }

    [TestMethod]
    public async Task OneFailedHeightDoesNotPublishPartialRows()
    {
        using var replies = new Replies();
        using var source = new SubqueryDictionaryBlockQuerySource(Owner, Heights, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        replies.FailHeight = Heights[1];
        await Check(source, Ids, null);
        Assert.AreEqual(4, replies.Requests);
    }

    [TestMethod]
    public async Task MovingIndexCursorDoesNotChangeHistoricalMatchAttributes()
    {
        using var replies = new Replies();
        using var source = new SubqueryDictionaryBlockQuerySource(Owner, Heights, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        var before = await Read(source, Ids[..1]);
        replies.IndexedHeight++;
        var after = await Read(source, Ids[..1]);
        Assert.IsFalse(before.IsError || after.IsError);
        CollectionAssert.AreEquivalent(before.Result[0].MetaData.Keys.ToArray(), after.Result[0].MetaData.Keys.ToArray());
        foreach (var key in before.Result[0].MetaData.Keys)
            Assert.AreEqual(before.Result[0].MetaData[key].ToString(), after.Result[0].MetaData[key].ToString());
        Assert.IsFalse(after.Result[0].MetaData.ContainsKey("SubqueryIndexedThrough"));
    }

    [TestMethod]
    public async Task InactiveAndCancelledQueriesMakeNoNativeRequest()
    {
        using var replies = new Replies();
        using var source = new SubqueryDictionaryBlockQuerySource(Owner, Heights, replies);
        Assert.IsTrue((await Read(source, Ids)).IsError);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.IsTrue((await source.ActivateQuerySourceAsync(cancellation.Token)).IsError);
        Assert.AreEqual(0, replies.Requests);
    }

    [TestMethod]
    public void InvalidCataloguesFailBeforeOpeningATransport()
    {
        foreach (var heights in new long[][] { [], [1, 1], [0], [-1], [long.MaxValue], Enumerable.Range(1, 9).Select(x => (long)x).ToArray() })
            Assert.ThrowsException<ArgumentException>(() => new SubqueryDictionaryBlockQuerySource(Owner, heights));
        Assert.ThrowsException<ArgumentException>(() => new SubqueryDictionaryBlockQuerySource(Guid.Empty, Heights));
    }

    [TestMethod, TestCategory("SubqueryDictionaryNative")]
    public async Task PublicDictionaryReturnsThreeAndTwoConfiguredBlockMatches()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_SUBQUERY_VERIFY_PUBLIC") != "1")
        {
            Assert.Inconclusive("Explicitly opt in to this public read-only dictionary schedule.");
            return;
        }
        using var source = new SubqueryDictionaryBlockQuerySource(Owner, Heights);
        var activated = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        await Check(source, Ids, Ids);
        await Check(source, Ids[..2], Ids[..2]);
    }

    private static Task<NextGenSoftware.OASIS.Common.OASISResult<IReadOnlyList<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>>> Read(
        SubqueryDictionaryBlockQuerySource source, Guid[] ids)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, Provider, [new CosmicQueryTarget(Provider, source.QueryCapabilities)]);
        Assert.IsFalse(plan.IsError, plan.Message);
        return source.ExecuteCosmicQueryAsync(plan.Result);
    }

    private static async Task Check(SubqueryDictionaryBlockQuerySource source, Guid[] selected, Guid[]? expected)
    {
        var result = await Read(source, selected);
        if (expected is null) { Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); return; }
        Assert.IsFalse(result.IsError, result.Exception?.ToString());
        CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
        foreach (var row in result.Result)
        {
            Assert.IsTrue(row.IsPublic && row.CreatedByAvatarId == Owner && !row.Children.Any());
            Assert.IsTrue(Heights.Select(x => x.ToString()).Contains(row.MetaData["SubqueryBlockHeight"].ToString()));
            Assert.AreEqual(SubqueryDictionaryBlockQuerySource.ReportedGenesis, row.MetaData["SubqueryReportedGenesisHash"].ToString());
            Assert.IsFalse(row.MetaData.ContainsKey("BlockHash"), "A dictionary match is not a block-header proof.");
        }
    }

    private sealed class Replies : HttpMessageHandler
    {
        public string? Fault { get; init; }
        public long? FailHeight { get; set; }
        public long IndexedHeight { get; set; } = 522211686;
        public int Requests;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Requests);
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("https://dict-tyk.subquery.network/query/arbitrum", request.RequestUri!.AbsoluteUri);
            Assert.IsNull(request.Headers.Authorization);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var text = body.RootElement.GetProperty("query").GetString()!;
            StringAssert.Contains(text, "orderBy:BLOCK_HEIGHT_ASC,first:1,distinct:[BLOCK_HEIGHT]");
            Assert.AreEqual(SubqueryDictionaryBlockQuerySource.TransferTopic, body.RootElement.GetProperty("variables").GetProperty("topic").GetString());
            var bounds = Regex.Match(text, "greaterThanOrEqualTo:\"([0-9]+)\",lessThan:\"([0-9]+)\"");
            Assert.IsTrue(bounds.Success);
            var height = long.Parse(bounds.Groups[1].Value);
            Assert.AreEqual(height + 1, long.Parse(bounds.Groups[2].Value));
            if (FailHeight == height) return new(HttpStatusCode.ServiceUnavailable);
            var nodes = Fault == "empty" ? Array.Empty<object>() : Fault == "too-many-rows"
                ? new object[] { new { blockHeight = height.ToString() }, new { blockHeight = height.ToString() } }
                : new object[] { new { blockHeight = Fault == "wrong-height" ? (height + 1).ToString() : Fault == "noncanonical-height" ? "0" + height : height.ToString() } };
            var json = JsonSerializer.Serialize(new { data = new {
                _metadata = new { lastProcessedHeight = Fault == "lagging-index" ? height - 1 : IndexedHeight,
                    genesisHash = Fault == "wrong-genesis" ? "0x" + new string('a', 64) : SubqueryDictionaryBlockQuerySource.ReportedGenesis,
                    startHeight = Fault == "uncovered-start" ? height + 1 : 1 }, evmLogs = new { nodes } } });
            if (Fault == "graphql-error") json = "{\"errors\":[{\"message\":\"controlled\"}],\"data\":null}";
            if (Fault == "duplicate-height-field") json = json.Replace("\"blockHeight\":\"" + height + "\"", "\"blockHeight\":\"" + height + "\",\"blockHeight\":\"" + height + "\"");
            if (Fault == "oversized") json += new string(' ', 32769);
            return new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, Fault == "wrong-content-type" ? "text/html" : "application/json") };
        }
    }
}
