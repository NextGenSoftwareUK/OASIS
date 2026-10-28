using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [TestMethod]
    public async Task NativeNeoDestinationCatalogueOwnsMeaningAcrossRestartAndRollback()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
        Assert.IsNotNull(root);
        var state = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent");
        Directory.CreateDirectory(state);
        using var lease = File.Open(Path.Combine(state, "neo-environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var journal = Path.Combine(state, "neo-destination.pending.json");
        Assert.IsFalse(File.Exists(journal), "Reconcile original outcomes before any new write.");
        var actor = Guid.NewGuid();
        var ids = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var operations = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var provider = new Provider(uri, "neo4j", "oasis-local-test-only");
        using var driver = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspect = driver.AsyncSession();
        var complete = false;
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var captured = await provider.CaptureStorageIdentityAsync();
            Assert.IsFalse(captured.IsError, captured.Message);
            var source = captured.Result;
            var oracle = new CosmicStorageIdentity(ProviderType.OracleDBOASIS, new string('B', 64), Guid.NewGuid());
            var aws = new CosmicStorageIdentity(ProviderType.AWSOASIS, new string('C', 64), "declaration-only");
            HolonReplicationRouteBinding Route(CosmicStorageIdentity target, string policy = "policy-one") => new(source, target, policy);
            HolonReplicationEnrollment Enrol(params HolonReplicationRouteBinding[] routes)
                => new(routes[0].SourceId, routes.Select(route => route.TargetId), source, routes);
            var catalogue = (IHolonReplicationDestinationCatalogue)provider.GetBoundHolonReplicationOutbox(source).Result;
            foreach (var target in new[] { oracle.Authority, aws.Authority })
            {
                var existing = await catalogue.ReadDestinationBindingAsync(target);
                Assert.IsFalse(existing.IsError, existing.Message);
                Assert.IsNull(existing.Result, "Preserve another owner's existing destination; no implicit rebind.");
            }
            await File.WriteAllTextAsync(journal, JsonSerializer.Serialize(new { actor, ids, operations, source = source.BindingFingerprint }));
            ConditionalHolonWriteRequest Request(int i) => new(new ConditionalHolonWrite(operations[i], actor,
                ProviderType.Neo4jOASIS, new[] { new HolonWriteCondition(ids[i], null) }), ConditionalHolonOperation.Save,
                new[] { new Holon { Id = ids[i], Name = "owned-destination-" + i } }, saveChildren: false);
            async Task<long> Count(string label) => (await (await inspect.RunAsync(
                "MATCH (n:" + label + ") WHERE n.OperationId IN $ops RETURN count(n) AS count",
                new { ops = operations.Select(op => op.ToString("D")).ToArray() })).SingleAsync())["count"].As<long>();
            var initial = Enrol(Route(oracle));
            var saved = await provider.ExecuteReplicatingHolonWriteAsync(Request(0), initial);
            Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
            Assert.AreEqual(operations[0], (await catalogue.ReadDestinationBindingAsync(oracle.Authority)).Result.EstablishedOperationId);

            var replacement = new CosmicStorageIdentity(oracle.Authority, oracle.EndpointFingerprint, Guid.NewGuid());
            var reopened = new Provider(uri, "neo4j", "oasis-local-test-only");
            try
            {
                Assert.IsFalse((await reopened.ActivateProviderAsync()).IsError);
                Assert.IsFalse((await reopened.ExecuteReplicatingHolonWriteAsync(Request(0), initial)).IsError);
                var refused = await reopened.ExecuteReplicatingHolonWriteAsync(Request(1), Enrol(Route(replacement)));
                Assert.AreEqual(HolonDestinationRebindRequiredException.Code, refused.ErrorCode);
            }
            finally { Assert.IsFalse((await reopened.DeActivateProviderAsync()).IsError); }
            // AWS sorts first: its approval must roll back when Oracle conflicts.
            var rollback = await provider.ExecuteReplicatingHolonWriteAsync(Request(1), Enrol(Route(aws), Route(replacement)));
            Assert.AreEqual(HolonDestinationRebindRequiredException.Code, rollback.ErrorCode);
            Assert.IsNull((await catalogue.ReadDestinationBindingAsync(aws.Authority)).Result);
            Assert.AreEqual(1L, await Count("CosmicWriteReceipt"));
            Assert.AreEqual(1L, await Count("CosmicHolonOutbox"));

            var key = source.BindingFingerprint + ":" + oracle.Authority;
            var corrupt = await (await inspect.RunAsync(
                "MATCH (d:CosmicDestinationBinding {Key:$key,EstablishedOperationId:$op}) WITH d,d.RecordDigest AS original SET d.RecordDigest=$bad RETURN original",
                new { key, op = operations[0].ToString("D"), bad = new string('0', 64) })).SingleAsync();
            Assert.AreEqual("COSMIC_OUTBOX_CORRUPT", (await catalogue.ReadDestinationBindingAsync(oracle.Authority)).ErrorCode);
            Assert.IsTrue((await provider.ExecuteReplicatingHolonWriteAsync(Request(1), initial)).IsError);
            await (await inspect.RunAsync("MATCH (d:CosmicDestinationBinding {Key:$key}) SET d.RecordDigest=$digest",
                new { key, digest = corrupt["original"].As<string>() })).ConsumeAsync();
            var updated = await provider.ExecuteReplicatingHolonWriteAsync(Request(1), Enrol(Route(oracle, "policy-two")));
            Assert.IsFalse(updated.IsError, updated.Message + updated.Exception);
            Assert.AreEqual("policy-one", (await catalogue.ReadDestinationBindingAsync(oracle.Authority)).Result.Route.PolicyBinding);
            await (await inspect.RunAsync("MATCH (d:CosmicDestinationBinding {Key:$key,EstablishedOperationId:$op}) DELETE d",
                new { key, op = operations[0].ToString("D") })).ConsumeAsync();
            var historical = await catalogue.ReadDestinationBindingAsync(oracle.Authority);
            Assert.IsFalse(historical.IsError, historical.Message + historical.Exception);
            Assert.AreEqual(operations[1], historical.Result.EstablishedOperationId);
            var recorded = (await (await inspect.RunAsync("MATCH (d:CosmicDestinationBinding {Key:$key}) RETURN count(d) AS count", new { key })).SingleAsync())["count"].As<long>();
            Assert.AreEqual(0L, recorded, "Historical reads cannot create approval records.");
            var bootstrapped = await provider.ExecuteReplicatingHolonWriteAsync(Request(2), initial);
            Assert.IsFalse(bootstrapped.IsError, bootstrapped.Message + bootstrapped.Exception);
            Assert.AreEqual(operations[1], (await catalogue.ReadDestinationBindingAsync(oracle.Authority)).Result.EstablishedOperationId);
            Assert.AreEqual(3L, await Count("CosmicWriteReceipt"));
            Assert.AreEqual(3L, await Count("CosmicHolonOutbox"));
            complete = true;
        }
        finally
        {
            Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
            if (complete)
            {
                var opStrings = operations.Select(op => op.ToString("D")).ToArray();
                await (await inspect.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = ids.Select(id => id.ToString("D")).ToArray() })).ConsumeAsync();
                await (await inspect.RunAsync("MATCH (n) WHERE (n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId IN $ops DELETE n", new { ops = opStrings })).ConsumeAsync();
                await (await inspect.RunAsync("MATCH (d:CosmicDestinationBinding) WHERE d.EstablishedOperationId IN $ops DELETE d", new { ops = opStrings })).ConsumeAsync();
                File.Delete(journal);
            }
        }
    }
}
