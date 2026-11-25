using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS.UnitTests;

[TestClass]
public class ArweaveCqlExecutionTests
{
    private static async Task<(ArweaveOASIS Provider, FakeArweaveService Service, Guid[] Ids, CosmicQuery<Holon> Query)> Setup(int count = 1)
    {
        var ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var service = new FakeArweaveService();
        var provider = new ArweaveOASIS(service, FakeArweaveService.Wallet) { IsProviderActivated = true };
        foreach (var id in ids)
            Assert.IsFalse((await provider.SaveHolonAsync(new Holon { Id = id, Name = "coffee", IsNewHolon = true }, saveChildren: false)).IsError);
        service.Reads = 0;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ArweaveOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.ArweaveOASIS, ids).Result).Result;
        return (provider, service, ids, query);
    }

    private static CosmicQueryPlan<Holon> Plan(ArweaveOASIS provider, CosmicQuery<Holon> query)
        => CosmicQueryPlanner.Plan(query, ProviderType.ArweaveOASIS, new[] { new CosmicQueryTarget(ProviderType.ArweaveOASIS, provider.QueryCapabilities) }).Result;

    [TestMethod]
    public async Task NativeSelectionAndPagingReturnAuthorizedDetachedRows()
    {
        var (provider, service, _, query) = await Setup(3);
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, query.Skip(1).Take(1).Result));
        Assert.IsFalse(result.IsError, result.Message); Assert.IsTrue(result.IsLoaded);
        Assert.AreEqual(1, result.Result.Count); Assert.AreEqual(3, service.Reads);
        Assert.AreEqual(0, result.Result[0].Children.Count());
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task MissingOrDeletedRecordReturnsNoRow(bool softDelete)
    {
        var (p, s, ids, q) = await Setup();
        if (softDelete) Assert.IsFalse((await p.DeleteHolonAsync(ids[0])).IsError);
        else s.Transactions.Clear();
        var result = await p.ExecuteCosmicQueryAsync(Plan(p, q));
        Assert.IsFalse(result.IsError, result.Message); Assert.AreEqual(0, result.Result.Count);
    }

    [TestMethod]
    public async Task ForeignTransactionsCannotImpersonateRecords()
    {
        var (p, s, _, q) = await Setup();
        // Replaying the owner's tags with forged ciphertext is rejected by authenticated decryption, never served.
        var genuine = s.Transactions.Single();
        s.Transactions.Add((new string('z', 43), new byte[] { 1, 2, 3 },
            new Dictionary<string, string>(genuine.Tags) { ["OASIS-Seq"] = long.MaxValue.ToString() }));
        var result = await p.ExecuteCosmicQueryAsync(Plan(p, q));
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode);
    }

    [DataTestMethod][DataRow(1)][DataRow(2)]
    public async Task FailedSourcePublishesNoPartialRows(int failingRead)
    {
        var (p, s, _, q) = await Setup(2);
        s.ReadFailure = _ => s.Reads == failingRead ? new IOException("gateway unavailable") : null;
        var result = await p.ExecuteCosmicQueryAsync(Plan(p, q));
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode);
    }

    [TestMethod]
    public async Task DuplicateSequenceIsAmbiguousAndFailsClosed()
    {
        var (p, s, _, q) = await Setup();
        var genuine = s.Transactions.Single();
        s.Transactions.Add((new string('y', 43), genuine.Data, new Dictionary<string, string>(genuine.Tags)));
        var result = await p.ExecuteCosmicQueryAsync(Plan(p, q));
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task CompletePageOverBudgetFailsWithoutRows()
    {
        var (p, _, _, q) = await Setup(2);
        var result = await p.ExecuteCosmicQueryAsync(Plan(p, q), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode); Assert.IsNull(result.Result);
    }
}
