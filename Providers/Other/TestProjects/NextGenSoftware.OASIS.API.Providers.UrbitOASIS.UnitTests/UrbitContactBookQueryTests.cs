using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.UrbitOASIS.UnitTests;

[TestClass]
public sealed class UrbitContactBookQueryTests
{
    private static readonly Guid Owner = Guid.Parse("d5cc4384-ff72-4e7d-a780-1595d56c3265");
    private static readonly Guid Connection = Guid.Parse("eaa5679e-9250-4abb-bb5e-d415dcf1f117");
    private static readonly string[] Cids = { "0v1", "0v2", "0v3" };
    private static readonly Guid[] Ids = Cids.Select(cid => UrbitContactBookQuerySource.LogicalId(Owner, Connection, cid)).ToArray();
    private const ProviderType Provider = ProviderType.UrbitOASIS;

    private sealed class Replies : HttpMessageHandler
    {
        public int Calls;
        public int FailAt;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Calls++;
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.IsNull(request.Content);
            var path = request.RequestUri!.AbsolutePath;
            Assert.IsTrue(path.StartsWith("/~/scry/contacts/v1/book/id/", StringComparison.Ordinal));
            var cid = path.Split('/').Last().Replace(".json", "", StringComparison.Ordinal);
            Assert.IsTrue(Cids.Contains(cid));
            var body = "[{}, {\"nickname\":{\"type\":\"text\",\"value\":\"Private " + cid + "\"},\"bio\":{\"type\":\"text\",\"value\":\"Owner overlay\"},\"unreviewed\":{\"type\":\"text\",\"value\":\"Never mapped\"}}]";
            return Task.FromResult(new HttpResponseMessage(Calls == FailAt ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK) {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private static UrbitContactsV1Client Reader(HttpClient http) => new(new Uri("https://owned.invalid/"), http);
    private static CosmicQueryPlan<Holon> Plan(UrbitContactBookQuerySource source, Guid[] ids,
        Guid? actor = null, int? skip = null, int? take = null, ProviderType authority = Provider)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor ?? Owner, authority, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        if (skip is not null) query = query.Skip(skip.Value).Result;
        if (take is not null) query = query.Take(take.Value).Result;
        var plan = CosmicQueryPlanner.Plan(query, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }

    [TestMethod]
    public async Task FullSubsetAndPrivateNativeMappingUseOnlySelectedPointReads()
    {
        using var replies = new Replies(); using var http = new HttpClient(replies); using var client = Reader(http);
        using var source = new UrbitContactBookQuerySource(client, Owner, Connection, Cids);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        foreach (var ids in new[] { Ids, Ids.Take(2).ToArray() })
        {
            var result = await source.ExecuteCosmicQueryAsync(Plan(source, ids));
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            CollectionAssert.AreEquivalent(ids, result.Result.Select(row => row.Id).ToArray());
            foreach (var row in result.Result)
            {
                Assert.IsFalse(row.IsPublic); Assert.AreEqual(Owner, row.CreatedByAvatarId); Assert.IsFalse(row.Children.Any());
                var cid = row.MetaData["UrbitBookPageId"].ToString()!;
                Assert.AreEqual(UrbitContactBookQuerySource.LogicalId(Owner, Connection, cid), row.Id);
                Assert.AreEqual(cid, row.ProviderUniqueStorageKey[Provider]);
                Assert.AreEqual("Private " + cid, row.MetaData["OverlayNickname"]);
                Assert.IsNull(row.MetaData["ContactNickname"]);
                Assert.IsFalse(row.MetaData.ContainsKey("unreviewed"));
            }
        }
        Assert.AreEqual(6, replies.Calls); // one probe plus three/two point reads
    }

    [TestMethod]
    public async Task ForeignActorCannotUseAnAuthoritativeCatalogueToReadOwnersBook()
    {
        using var replies = new Replies(); using var http = new HttpClient(replies); using var client = Reader(http);
        using var source = new UrbitContactBookQuerySource(client, Owner, Connection, Cids);
        await source.ActivateQuerySourceAsync();
        var result = await source.ExecuteCosmicQueryAsync(Plan(source, Ids, Guid.NewGuid()));
        Assert.IsFalse(result.IsError); Assert.AreEqual(0, result.Result.Count); Assert.AreEqual(1, replies.Calls);
    }

    [TestMethod]
    public async Task AnotherConnectionsLogicalKeyRejectsBeforeNativeRead()
    {
        using var replies = new Replies(); using var http = new HttpClient(replies); using var client = Reader(http);
        using var source = new UrbitContactBookQuerySource(client, Owner, Connection, Cids);
        await source.ActivateQuerySourceAsync();
        var foreign = new[] { UrbitContactBookQuerySource.LogicalId(Owner, Guid.NewGuid(), "0v1") };
        Assert.AreEqual("CQL_QUERY_FAILED", (await source.ExecuteCosmicQueryAsync(Plan(source, foreign))).ErrorCode);
        Assert.AreEqual(1, replies.Calls);
    }

    [TestMethod]
    public async Task InactiveSourceDoesNotTouchNativeBook()
    {
        using var replies = new Replies(); using var http = new HttpClient(replies); using var client = Reader(http);
        using var source = new UrbitContactBookQuerySource(client, Owner, Connection, Cids);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", (await source.ExecuteCosmicQueryAsync(Plan(source, Ids))).ErrorCode);
        Assert.AreEqual(0, replies.Calls);
    }

    [TestMethod]
    public async Task CancelledQuerySendsNoAdditionalRead()
    {
        using var replies = new Replies(); using var http = new HttpClient(replies); using var client = Reader(http);
        using var source = new UrbitContactBookQuerySource(client, Owner, Connection, Cids);
        await source.ActivateQuerySourceAsync(); using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED", (await source.ExecuteCosmicQueryAsync(Plan(source, Ids), cancellationToken: cancelled.Token)).ErrorCode);
        Assert.AreEqual(1, replies.Calls);
    }

    [TestMethod]
    public async Task SecondReadFailureNeverPublishesTheFirstPrivateRow()
    {
        using var replies = new Replies(); using var http = new HttpClient(replies); using var client = Reader(http);
        using var source = new UrbitContactBookQuerySource(client, Owner, Connection, Cids);
        await source.ActivateQuerySourceAsync(); replies.FailAt = 3;
        var result = await source.ExecuteCosmicQueryAsync(Plan(source, Ids));
        Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode); Assert.IsNull(result.Result);
        Assert.IsInstanceOfType(result.Exception, typeof(HttpRequestException)); Assert.AreEqual(3, replies.Calls);
    }

    [TestMethod]
    public async Task FailedActivationPreservesInactiveStateAndDoesNotRetry()
    {
        using var replies = new Replies { FailAt = 1 }; using var http = new HttpClient(replies); using var client = Reader(http);
        using var source = new UrbitContactBookQuerySource(client, Owner, Connection, Cids);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", (await source.ActivateQuerySourceAsync()).ErrorCode);
        Assert.IsFalse(source.IsProviderActivated); Assert.AreEqual(1, replies.Calls);
    }

    [TestMethod]
    public async Task PagingKeepsDeterministicIdentityOrderAndBoundedSourceReads()
    {
        using var replies = new Replies(); using var http = new HttpClient(replies); using var client = Reader(http);
        using var source = new UrbitContactBookQuerySource(client, Owner, Connection, Cids);
        await source.ActivateQuerySourceAsync();
        var result = await source.ExecuteCosmicQueryAsync(Plan(source, Ids, skip: 1, take: 1));
        Assert.IsFalse(result.IsError); Assert.AreEqual(1, result.Result.Count);
        Assert.AreEqual(Ids.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).Skip(1).First(), result.Result[0].Id);
        Assert.AreEqual(4, replies.Calls);
    }

    [TestMethod]
    public async Task HistoricalReadRejectsWithoutCurrentStateFallback()
    {
        using var replies = new Replies(); using var http = new HttpClient(replies); using var client = Reader(http);
        using var source = new UrbitContactBookQuerySource(client, Owner, Connection, Cids) { IsVersionControlEnabled = true };
        await source.ActivateQuerySourceAsync();
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", (await source.ExecuteCosmicQueryAsync(Plan(source, Ids))).ErrorCode);
        Assert.AreEqual(1, replies.Calls);
    }

    [TestMethod]
    public async Task ResultBudgetDoesNotPublishATruncatedPrivatePage()
    {
        using var replies = new Replies(); using var http = new HttpClient(replies); using var client = Reader(http);
        using var source = new UrbitContactBookQuerySource(client, Owner, Connection, Cids);
        await source.ActivateQuerySourceAsync();
        var result = await source.ExecuteCosmicQueryAsync(Plan(source, Ids), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode); Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task DisposingSourcePreservesCallerOwnedNativeReader()
    {
        using var replies = new Replies(); using var http = new HttpClient(replies); using var client = Reader(http);
        var source = new UrbitContactBookQuerySource(client, Owner, Connection, Cids); source.Dispose();
        Assert.IsFalse((await client.ReadBookPageAsync("0v1")).IsError);
        Assert.IsTrue((await source.ActivateQuerySourceAsync()).IsError); Assert.AreEqual(1, replies.Calls);
    }

    [TestMethod]
    public void LogicalIdentitySeparatesOwnerConnectionAndNativeKey()
    {
        Assert.AreNotEqual(Ids[0], UrbitContactBookQuerySource.LogicalId(Guid.NewGuid(), Connection, "0v1"));
        Assert.AreNotEqual(Ids[0], UrbitContactBookQuerySource.LogicalId(Owner, Guid.NewGuid(), "0v1"));
        Assert.AreNotEqual(Ids[0], Ids[1]);
        Assert.AreEqual(Ids[0], UrbitContactBookQuerySource.LogicalId(Owner, Connection, "0v1"));
    }

    [DataTestMethod]
    [DataRow("empty")][DataRow("duplicate")][DataRow("unsafe")][DataRow("unbounded")]
    public void InvalidCatalogueRejectsBeforeAnyNativeRequest(string fault)
    {
        using var replies = new Replies(); using var http = new HttpClient(replies); using var client = Reader(http);
        var keys = fault switch { "empty" => Array.Empty<string>(), "duplicate" => new[] { "0v1", "0v1" },
            "unsafe" => new[] { "0v../x" }, _ => Enumerable.Range(1, 33).Select(i => "0v" + i).ToArray() };
        Assert.ThrowsException<ArgumentException>(() => new UrbitContactBookQuerySource(client, Owner, Connection, keys));
        Assert.AreEqual(0, replies.Calls);
    }

    [DataTestMethod]
    [DataRow(true)][DataRow(false)]
    public void MissingOwnerOrStableConnectionRejectsBeforeTransport(bool missingOwner)
    {
        using var replies = new Replies(); using var http = new HttpClient(replies); using var client = Reader(http);
        Assert.ThrowsException<ArgumentException>(() => new UrbitContactBookQuerySource(client,
            missingOwner ? Guid.Empty : Owner, missingOwner ? Connection : Guid.Empty, Cids));
        Assert.AreEqual(0, replies.Calls);
    }

    [TestMethod]
    public async Task AnotherReadAuthorityDoesNotReadThePrivateNativeBook()
    {
        using var replies = new Replies(); using var http = new HttpClient(replies); using var client = Reader(http);
        using var source = new UrbitContactBookQuerySource(client, Owner, Connection, Cids);
        await source.ActivateQuerySourceAsync();
        Assert.AreEqual("CQL_ACCESS_INVALID", (await source.ExecuteCosmicQueryAsync(Plan(source, Ids, authority: ProviderType.MongoDBOASIS))).ErrorCode);
        Assert.AreEqual(1, replies.Calls);
    }
}
