using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.TheGraphOASIS.UnitTests;

[TestClass]
public class TheGraphStudioStatusQueryTests
{
    private const ProviderType Provider = ProviderType.TheGraphOASIS;
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Keys = ["adexto-arbitrum", "adexto-base", "klerosboard-gnosis"];
    private static readonly Guid[] Ids = Keys.Select(key => TheGraphStudioStatusQuerySource.LogicalId(Owner, key)).ToArray();
    private static readonly string[] Cids = [
        "QmYpYjRUayUUBmiJpciHa4zgE91fonUVhdEg2gaTehvMo9",
        "QmT1W8wYCgRoTSBaPrw8wtZJhv1avLxiQkNRXmvtqGCj9m",
        "QmYaqJVD9qHhjiqrsywQqcQ2TimGRzfqQA8nBew1ZRvL2z"];

    [TestMethod]
    public void LogicalIdsMatchThePublishedWeb4Catalogue()
    {
        // These IDs are also asserted by the Python preset-generation tests.
        Guid[] published = [Guid.Parse("d34efac2-6513-855c-ac40-35bb83ea952e"),
            Guid.Parse("d76a1024-c71f-8527-96e5-4def4df3633b"),
            Guid.Parse("f3d72db4-5b1f-8daf-a8bb-e9658a8b6b8b")];
        CollectionAssert.AreEqual(published, Ids);
    }

    [TestMethod]
    public async Task ExactSelectionsUseOnlyTheReviewedDeploymentStatusQuery()
    {
        using var replies = new Replies();
        using var source = new TheGraphStudioStatusQuerySource(Owner, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);

        // The same LINQ expression selects either the complete catalogue or a subset.
        await CheckAsync(source, Ids, Ids);
        await CheckAsync(source, Ids[..2], Ids[..2]);
        Assert.AreEqual(6, replies.Requests);

        // A foreign identity must not become an arbitrary GraphQL request.
        await CheckAsync(source, [Guid.NewGuid()], null);
        Assert.AreEqual(6, replies.Requests);
    }

    [DataTestMethod]
    [DataRow("graphql-error")]
    [DataRow("wrong-deployment")]
    [DataRow("indexing-error")]
    [DataRow("invalid-hash")]
    [DataRow("zero-height")]
    [DataRow("zero-timestamp")]
    [DataRow("duplicate-field")]
    [DataRow("wrong-content-type")]
    [DataRow("oversized")]
    public async Task ActivationRequiresOneHealthyReviewedStatus(string fault)
    {
        using var replies = new Replies { Fault = fault };
        using var source = new TheGraphStudioStatusQuerySource(Owner, replies);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", result.ErrorCode);
        Assert.IsFalse(source.IsProviderActivated);
    }

    [TestMethod]
    public async Task OneFailedDeploymentNeverPublishesAPartialSelection()
    {
        using var replies = new Replies();
        using var source = new TheGraphStudioStatusQuerySource(Owner, replies);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        replies.FailBase = true;
        await CheckAsync(source, Ids, null);
    }

    [TestMethod]
    public void UnknownCatalogueKeysAndEmptyOwnersAreRejected()
    {
        Assert.ThrowsException<ArgumentException>(() => TheGraphStudioStatusQuerySource.LogicalId(Owner, "foreign"));
        Assert.ThrowsException<ArgumentException>(() => new TheGraphStudioStatusQuerySource(Guid.Empty));
    }

    [TestMethod, TestCategory("TheGraphStudioNative")]
    public async Task PublicStudioReturnsThreeAndTwoPinnedDeployments()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_THE_GRAPH_VERIFY_PUBLIC") != "1")
        {
            Assert.Inconclusive("Opt in to the reviewed public Studio query schedule.");
            return;
        }

        using var source = new TheGraphStudioStatusQuerySource(Owner);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        await CheckAsync(source, Ids, Ids);
        await CheckAsync(source, Ids[..2], Ids[..2]);
    }

    private static async Task CheckAsync(TheGraphStudioStatusQuerySource source, Guid[] selected, Guid[]? expected)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, selected).Result).Result
            .Where(row => selected.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, Provider, [new CosmicQueryTarget(Provider, source.QueryCapabilities)]);
        Assert.IsFalse(plan.IsError, plan.Message);
        var result = await source.ExecuteCosmicQueryAsync(plan.Result);
        if (expected is null)
        {
            Assert.IsTrue(result.IsError);
            Assert.IsNull(result.Result);
            return;
        }

        Assert.IsFalse(result.IsError, result.Exception?.ToString());
        CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
        foreach (var row in result.Result)
        {
            Assert.IsTrue(row.IsPublic && row.CreatedByAvatarId == Owner && !row.Children.Any());
            Assert.AreEqual(row.Name, row.ProviderUniqueStorageKey[Provider]);
            Assert.IsTrue(Cids.Contains(row.MetaData["GraphDeployment"].ToString()));
            Assert.IsTrue(long.TryParse(row.MetaData["GraphIndexedBlockNumber"].ToString(), out var height) && height > 0);
            Assert.IsTrue(row.MetaData["GraphIndexedBlockHash"].ToString()!.StartsWith("0x"));
        }
    }

    private sealed class Replies : HttpMessageHandler
    {
        public string? Fault { get; init; }
        public bool FailBase { get; set; }
        public int Requests;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Requests);
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("api.studio.thegraph.com", request.RequestUri!.Host);
            Assert.IsNull(request.Headers.Authorization);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.AreEqual("query Status { _meta { deployment hasIndexingErrors block { number hash timestamp } } }",
                body.RootElement.GetProperty("query").GetString());
            var index = request.RequestUri.AbsolutePath switch
            {
                "/query/1757874/adexto-arbitrum/v1.0.1" => 0,
                "/query/1757874/adexto-base/v1.0.1" => 1,
                "/query/66145/klerosboard-gnosis/version/latest" => 2,
                _ => throw new AssertFailedException("Unreviewed Studio route")
            };
            if (FailBase && index == 1) return new(HttpStatusCode.ServiceUnavailable);

            var json = JsonSerializer.Serialize(new
            {
                data = new { _meta = new { deployment = Fault == "wrong-deployment" ? Cids[(index + 1) % 3] : Cids[index],
                    hasIndexingErrors = Fault == "indexing-error", block = new {
                        number = Fault == "zero-height" ? 0 : 42,
                        timestamp = Fault == "zero-timestamp" ? 0 : 1700000000L,
                        hash = Fault == "invalid-hash" ? "0xnot-a-block" : "0x" + new string('a', 64) } } }
            });
            if (Fault == "graphql-error") json = "{\"errors\":[{\"message\":\"controlled\"}],\"data\":null}";
            if (Fault == "duplicate-field") json = json.Replace("\"number\":42", "\"number\":42,\"number\":43");
            if (Fault == "oversized") json += new string(' ', 32769);
            return new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8,
                Fault == "wrong-content-type" ? "text/html" : "application/json") };
        }
    }
}
