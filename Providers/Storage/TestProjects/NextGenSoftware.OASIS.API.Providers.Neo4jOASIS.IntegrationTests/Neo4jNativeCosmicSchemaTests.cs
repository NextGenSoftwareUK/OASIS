using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow("Holon")] [DataRow("CosmicWriteReceipt")] [DataRow("OasisRecordVersion")] [DataRow("CosmicReplicaHead")]
    [DataRow("CosmicHolonOutboxSequence")] [DataRow("OutboxSequence")] [DataRow("OutboxOperationTarget")]
    public async Task NativeCosmicSchemaUniquenessSerializesCompetingCreates(string kind)
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        var label=kind.StartsWith("Outbox",StringComparison.Ordinal) ? "CosmicHolonOutbox":kind;
        var property = kind == "Holon" ? "Id" : kind=="CosmicReplicaHead" ? "HolonId" : "OperationId";
        var id = Guid.NewGuid().ToString("D"); var complete = false;
        var key=kind=="OasisRecordVersion" ? "Kind:'Holon',RecordId:$id,Version:1"
            : kind=="CosmicHolonOutboxSequence" ? "Name:$id"
            : kind=="OutboxSequence" ? "Sequence:$id"
            : kind=="OutboxOperationTarget" ? "OperationId:$id,TargetId:'owned schema target'":property+":$id";
        using var inspection = driver.AsyncSession();
        try
        {
            var activated = await provider.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Message + activated.Exception);
            var before = await (await inspection.RunAsync("MATCH (n:" + label + " {" + key + "}) RETURN count(n) AS count", new { id })).SingleAsync();
            Assert.AreEqual(0L, before["count"].As<long>());
            async Task<ClientException?> Create(string owner)
            {
                using var session = driver.AsyncSession();
                try
                {
                    await (await session.RunAsync("CREATE (n:" + label + " {" + key + ",OwnedFixture:$owner}) RETURN n", new { id, owner })).ConsumeAsync();
                    return null;
                }
                catch (ClientException rejected) { return rejected; }
            }
            var results = await Task.WhenAll(Create("owned claimant A"), Create("owned claimant B"));
            Assert.AreEqual(1, results.Count(result => result == null), "Owned uniqueness fixture: " + kind + "/" + id);
            var failure = results.Single(result => result != null)!;
            StringAssert.Contains(failure.Code, "ConstraintValidationFailed");
            var rows = await (await inspection.RunAsync("MATCH (n:" + label + " {" + key + "}) RETURN n.OwnedFixture AS owner", new { id })).ToListAsync();
            Assert.AreEqual(1, rows.Count); StringAssert.StartsWith(rows[0]["owner"].As<string>(), "owned claimant ");
            complete = true;
        }
        finally
        {
            if (complete) await (await inspection.RunAsync("MATCH (n:" + label + " {" + key + "}) DETACH DELETE n", new { id })).ConsumeAsync();
            await provider.DeActivateProviderAsync();
        }
    }
}
