using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jReadActivationNativeTests
{
    [TestMethod]
    public async Task ColdReadEnrollmentQueriesExistingHolonAndRefusesLegacyWithoutRepair()
    {
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var endpoint = Environment.GetEnvironmentVariable("OASIS_NEO4J_READ_ACTIVATION_URI");
        Assert.IsTrue(Uri.TryCreate(endpoint, UriKind.Absolute, out var uri));
        Assert.AreEqual("bolt", uri!.Scheme);
        Assert.AreEqual("127.0.0.1", uri.Host);
        Assert.IsTrue(uri.Port > 0);
        const string password = "oasis-fixture-only";
        var id = Guid.NewGuid();
        var legacyId = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var writer = new Provider(endpoint!, "neo4j", password);
        var reader = new Provider(endpoint!, "neo4j", password);
        using var observer = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", password));
        using var observation = observer.AsyncSession();
        async Task<string> Snapshot()
        {
            var row = await (await observation.RunAsync(
                "MATCH (h:Holon {Id:$id}) MATCH (h)-[p:HAS_CQL_PROJECTION]->(h) "
                + "RETURN h.SnapshotJson AS snapshot,coalesce(h.ProviderRevisionId,'') AS revision,"
                + "coalesce(p.ProviderRevisionId,'') AS projectionRevision,p.SchemaVersion AS schema",
                new { id = id.ToString() })).SingleAsync();
            return System.Text.Json.JsonSerializer.Serialize(new[] {
                row["snapshot"].As<string>(), row["revision"].As<string>(),
                row["projectionRevision"].As<string>(), row["schema"].As<long>().ToString() });
        }
        try
        {
            var initial = await (await observation.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync();
            Assert.AreEqual(0L, initial["count"].As<long>(), "Only a fresh isolated CI service is admitted.");
            var activated = await writer.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Message);
            var saved = await writer.SaveHolonAsync(new Holon {
                Id = id, Name = "owned cold CQL read", CreatedByAvatarId = actor, IsActive = true
            }, saveChildren: false);
            Assert.IsFalse(saved.IsError, saved.Message);
            var before = await Snapshot();
            Assert.IsFalse((await writer.DeActivateProviderAsync()).IsError);
            var enrollment = await reader.ActivateQuerySourceAsync();
            Assert.IsFalse(enrollment.IsError, enrollment.Message);
            Assert.IsTrue(reader.IsQuerySourceActivated);
            Assert.IsFalse(reader.IsProviderActivated);
            var access = CosmicQueryReadAccess.OwnerPublicOrShared(actor, Array.Empty<Guid>(), false);
            Assert.IsFalse(access.IsError, access.Message);
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).WithReadAccess(access.Result);
            Assert.IsFalse(query.IsError, query.Message);
            var plan = CosmicQueryPlanner.Plan(query.Result, ProviderType.Neo4jOASIS,
                new[] { new CosmicQueryTarget(ProviderType.Neo4jOASIS, Provider.CqlCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var read = await ((ICosmicQueryStorageProvider)reader).ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(read.IsError, read.Message);
            Assert.AreEqual(id, read.Result.Single().Id);
            Assert.AreEqual(before, await Snapshot(), "Read enrollment/query must preserve native values and revisions.");

            // This deliberately incomplete record exists only in the fresh owned CI database.
            await (await observation.RunAsync("CREATE (:Holon {Id:$id})", new { id = legacyId.ToString() })).ConsumeAsync();
            var refused = await reader.ActivateQuerySourceAsync();
            Assert.IsTrue(refused.IsError);
            Assert.AreEqual("CQL_SCHEMA_UPGRADE_REQUIRED", refused.ErrorCode);
            Assert.IsFalse(reader.IsQuerySourceActivated);
            var legacy = await (await observation.RunAsync(
                "MATCH (h:Holon {Id:$id}) OPTIONAL MATCH (h)-[p:HAS_CQL_PROJECTION]->(h) RETURN count(p) AS count",
                new { id = legacyId.ToString() })).SingleAsync();
            Assert.AreEqual(0L, legacy["count"].As<long>(), "Read verification cannot repair legacy data.");
            Assert.AreEqual(before, await Snapshot());
        }
        finally
        {
            Assert.IsFalse((await reader.DeActivateProviderAsync()).IsError);
            Assert.IsFalse((await writer.DeActivateProviderAsync()).IsError);
            await (await observation.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",
                new { ids = new[] { id.ToString(), legacyId.ToString() } })).ConsumeAsync();
            var remaining = await (await observation.RunAsync(
                "MATCH (h:Holon) WHERE h.Id IN $ids RETURN count(h) AS count",
                new { ids = new[] { id.ToString(), legacyId.ToString() } })).SingleAsync();
            Assert.AreEqual(0L, remaining["count"].As<long>());
        }
    }
}
