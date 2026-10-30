using System.Text.Json;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS.UnitTests;

[TestClass]
public class CouchbaseNativeQueryBoundaryTests
{
    private sealed class Reader : CouchbaseOASIS
    {
        public string Mode = "reverse";
        public int Calls;
        public int Produced;
        public Action? BeforeRead;
        public Reader() : base("couchbase://127.0.0.1:1", "fixture", "fixture") { IsProviderActivated = true; }
        protected override async IAsyncEnumerable<NativeCqlDocument> ReadNativeCqlAsync(IReadOnlyList<Guid> ids, [EnumeratorCancellation] CancellationToken token)
        {
            Calls++;
            BeforeRead?.Invoke();
            token.ThrowIfCancellationRequested();
            if (Mode == "missing") yield break;
            var rows = ids.Reverse().Select((id, index) => {
                var key = Mode == "foreign" && index == 1 ? Guid.NewGuid() :
                    Mode == "duplicate" ? ids[0] : id;
                return new NativeCqlDocument {
                    Key = key.ToString("D"), Id = Mode == "identity" && index == 1 ? Guid.NewGuid().ToString("D") : key.ToString("D"),
                    IsDeleted = Mode == "deleted" && index == 0,
                    DataJson = Mode == "budget" ? new string(' ', 4 * 1024 * 1024 + 1) :
                        JsonSerializer.Serialize(new Holon { Id = Mode == "payload" && index == 1 ? Guid.NewGuid() : key },
                            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                };
            }).ToArray();
            foreach (var row in rows) { Produced++; yield return row; }
            if (Mode == "terminal") throw new IOException("Injected native terminal query failure");
        }
    }

    private static CosmicQueryPlan<Holon> Plan(Reader provider, Guid[] ids, bool page = false)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CouchbaseOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.CouchbaseOASIS, ids).Result).Result;
        if (page) query = query.Skip(1).Result.Take(1).Result;
        return CosmicQueryPlanner.Plan(query, ProviderType.CouchbaseOASIS,
            new[] { new CosmicQueryTarget(ProviderType.CouchbaseOASIS, provider.QueryCapabilities) }).Result;
    }

    [TestMethod]
    public async Task NativeOrderingCannotChangeTheRequestedPageOrBudget()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }
            .OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var provider = new Reader();
        var page = await provider.ExecuteCosmicQueryAsync(Plan(provider, ids, true));
        Assert.IsFalse(page.IsError, page.Message);
        Assert.AreEqual(ids[1], page.Result.Single().Id);
        var limited = await provider.ExecuteCosmicQueryAsync(Plan(provider, ids), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", limited.ErrorCode); Assert.IsNull(limited.Result);
    }

    [DataTestMethod]
    [DataRow("foreign")]
    [DataRow("duplicate")]
    [DataRow("identity")]
    [DataRow("payload")]
    [DataRow("terminal")]
    public async Task NativeIdentityCorruptionCannotPublishPartialRows(string mode)
    {
        var provider = new Reader { Mode = mode };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, new[] { Guid.NewGuid(), Guid.NewGuid() }));
        Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode); Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task EmptyCatalogueMakesNoCallAndMissingRecordsRemainEmpty()
    {
        var provider = new Reader { Mode = "missing" };
        var empty = await provider.ExecuteCosmicQueryAsync(Plan(provider, Array.Empty<Guid>()));
        Assert.IsFalse(empty.IsError); Assert.AreEqual(0, provider.Calls);
        var missing = await provider.ExecuteCosmicQueryAsync(Plan(provider, new[] { Guid.NewGuid() }));
        Assert.IsFalse(missing.IsError); Assert.AreEqual(0, missing.LoadedCount); Assert.AreEqual(1, provider.Calls);
    }

    [TestMethod]
    public async Task DecodedPayloadBudgetAndActiveCancellationPublishNoRows()
    {
        var provider = new Reader { Mode = "budget" };
        var plan = Plan(provider, new[] { Guid.NewGuid(), Guid.NewGuid() });
        var oversized = await provider.ExecuteCosmicQueryAsync(plan);
        Assert.AreEqual("CQL_QUERY_LIMIT", oversized.ErrorCode); Assert.IsNull(oversized.Result);
        Assert.AreEqual(1, provider.Produced, "Budget failure must stop native enumeration before another row.");
        using var cancellation = new CancellationTokenSource();
        provider.BeforeRead = cancellation.Cancel;
        var stopped = await provider.ExecuteCosmicQueryAsync(plan, cancellationToken: cancellation.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", stopped.ErrorCode); Assert.IsNull(stopped.Result);
    }
}
