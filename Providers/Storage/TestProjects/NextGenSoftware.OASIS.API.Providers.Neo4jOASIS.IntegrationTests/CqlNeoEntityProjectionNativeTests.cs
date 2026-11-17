using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    [TestMethod]
    [DoNotParallelize]
    public async Task NativeNeoEntityProjectionPreservesStagesAndProofBeforeDtoConstruction()
    {
        var rootPath=Environment.GetEnvironmentVariable("OASIS_CQL_NEO_ENTITY_PROJECTION_ROOT")
            ?? throw new InvalidOperationException("Use the explicit Neo entity-projection runner.");
        const string uri="bolt://127.0.0.1:17687";
        const string username="neo4j", password="oasis-local-test-only";
        var state=Path.Combine(rootPath,"LocalState/COSMIC/NeoEntityProjectionCurrent");Directory.CreateDirectory(state);
        var journal=Path.Combine(state,"fixture.pending.json");
        Assert.IsFalse(File.Exists(journal),"Preserve/reconcile prior projection fixture; never replay creates.");
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic(username,password));
        await using var inspection=driver.AsyncSession();
        async Task<long> Count()=> (await (await inspection.RunAsync("MATCH(h:Holon) RETURN count(h) AS n")).SingleAsync())["n"].As<long>();
        Assert.AreEqual(0L,await Count(),"Preserve unexpected native data.");
        var ids=Enumerable.Range(0,5).Select(_=>Guid.NewGuid()).ToArray();var actor=Guid.NewGuid();var other=Guid.NewGuid();
        async Task Checkpoint(string stage,bool first=false)
        {
            var next=first ? journal : journal+".next";
            await using(var file=new FileStream(next,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
            {await JsonSerializer.SerializeAsync(file,new{ids,actor,other,stage});file.Flush(true);}
            if(!first)File.Move(next,journal,true);
        }
        await Checkpoint("before-create",true);
        var provider=new Neo4jOASIS(uri,username,password){IsVersionControlEnabled=false};var active=false;
        try
        {
            var activation=await provider.ActivateProviderAsync();Assert.IsFalse(activation.IsError,activation.Message);active=true;
            await VerifyVirtualNativeLabel(provider, inspection, actor,
                new NativeConnection { ["uri"] = uri, ["username"] = username, ["password"] = password });
            var saved=await provider.SaveHolonAsync(CreateEntityProjectionFixture(ids,actor,other),saveChildren:true);
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.AreEqual(5L,await Count());
            await Checkpoint("created-awaiting-projection-verification");
            var checks=new List<object>();
            await VerifyEntityProjection((ICosmicQueryEntityProjectionStorageProvider)provider,ids[0],checks);
            await VerifyStandardEntityProjection((ICosmicQueryEntityProjectionStorageProvider)provider,ids[0],checks);
            await VerifyEntitySelectedComposition((ICosmicQueryEntityProjectionCompositionStorageProvider)provider,ids[0],ids,checks);
            await VerifyEntitySelectedReselection((ICosmicQueryEntityProjectionStorageProvider)provider,ids[0],checks);
            await VerifyEntitySelectedCardinality((ICosmicQueryEntityProjectionCardinalityStorageProvider)provider,ids[0],checks);
            await VerifyEntitySelectedAll((ICosmicQueryEntityProjectedAllStorageProvider)provider,ids[0],checks);
            await VerifyMetadataPresenceWire(provider, ids[0], checks);
            await VerifyDistinctWire(provider, ids, checks);
            await VerifyNativeFlattening(provider,ids[0],ids[1],checks);
            await VerifyChildSource(provider, actor, ids[0], ids[1], checks);
            await VerifyJoinOutputOrdering(provider, ids, checks);
            await VerifyNativeStringLength(provider, ids, checks);
            Assert.AreEqual(100,checks.Count);
            var deactivation=await provider.DeActivateProviderAsync();Assert.IsFalse(deactivation.IsError,deactivation.Message);active=false;
            await Checkpoint("before-exact-cleanup");
            var deleted=await (await inspection.RunAsync("MATCH(h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids=ids.Select(id=>id.ToString("D")).ToArray()})).ConsumeAsync();
            Assert.AreEqual(5,deleted.Counters.NodesDeleted);Assert.AreEqual(0L,await Count());
            await File.WriteAllTextAsync(Path.Combine(rootPath,"TestResults/Neo4jCurrent/neo-entity-projection-native.json"),
                JsonSerializer.Serialize(new{provider="Neo4jOASIS",checks=checks.Count,cases=checks,exactCleanupVerified=true}));
            File.Delete(journal);
        }
        finally{if(active)Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);}
    }

    private static async Task VerifyVirtualNativeLabel(Neo4jOASIS provider, IAsyncSession inspection, Guid actor, NativeConnection named)
    {
        var label = "VirtualFixture_" + Guid.NewGuid().ToString("N");
        var precedingLabels = provider.NativeImportLabels;
        provider.NativeImportLabels = label;
        try
        {
            await (await inspection.RunAsync($"CREATE (:`{label}` {{ tier:'Gold', name:'Ada', hidden:'private' }}), " +
                $"(:`{label}` {{ tier:'gold', name:'Grace' }}), (:`{label}` {{ tier:1, name:'Alan' }})")).ConsumeAsync();

            var native = await provider.ReadNativeRecordsAsync(label, null, 5);
            Assert.IsFalse(native.IsError, native.Message);
            var imported = native.Result.Records.Single(record => Equals(record.Fields["name"], "Ada"));
            var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, actor, provider.NativeDataProviderType,
                label, VirtualHolonMapping.SandboxConnectionName, provider.DescribeVirtualQueryConnection(null),
                new Dictionary<string, string> { ["tier"] = "tier", ["name"] = "name" });
            var query = await mapping.QueryAsync(actor, provider, null, "tier", "Gold", new[] { "name" }, 2);
            Assert.IsFalse(query.IsError, query.Message);
            var row = query.Result.Single();
            Assert.AreEqual(NativeRecordHolonMapper.StableHolonId(actor, provider.NativeDataProviderType,
                label, imported.NativeKey), row.Id);
            Assert.AreEqual("Ada", row.MetaData["name"]);
            Assert.AreEqual(1, row.MetaData.Count);

            var snapshot = row.PrepareSnapshot(actor);
            var changed = await (await inspection.RunAsync(
                $"MATCH (item:`{label}` {{tier:'Gold'}}) SET item.name = 'Ada updated'")).ConsumeAsync();
            Assert.AreEqual(1, changed.Counters.PropertiesSet);
            var after = await mapping.QueryAsync(actor, provider, null, "tier", "Gold", new[] { "name" }, 2);
            Assert.IsFalse(after.IsError, after.Message);
            var refreshed = after.Result.Single();
            Assert.AreEqual(row.Id, refreshed.Id);
            Assert.AreEqual("Ada updated", refreshed.MetaData["name"]);
            Assert.AreEqual("Ada", snapshot.MetaData["name"]);

            var integer = await provider.QueryNativeRecordsAsync(label, "tier", 1L, new[] { "name" }, 2);
            Assert.IsFalse(integer.IsError, integer.Message);
            Assert.AreEqual("Alan", integer.Result.Single().Fields["name"]);

            await (await inspection.RunAsync($"CREATE (:`{label}` {{ tier:'Gold', name:'Second Gold' }})")).ConsumeAsync();
            var gold = await provider.QueryNativeRecordsAsync(label, "tier", "Gold", new[] { "name" }, 2);
            Assert.IsFalse(gold.IsError, gold.Message);
            Assert.AreEqual(2, gold.Result.Count);
            var conjunction = await provider.QueryNativeRecordsAsync(label,
                new[] { new KeyValuePair<string, object>("tier", "Gold"),
                    new KeyValuePair<string, object>("name", gold.Result[1].Fields["name"]) }, new[] { "name" }, 1);
            Assert.IsFalse(conjunction.IsError, conjunction.Message);
            Assert.AreEqual(gold.Result[1].NativeKey, conjunction.Result.Single().NativeKey);

            // Only this serial test permits its disposable loopback Neo4j fixture.
            named["labels"] = label;
            var identity = provider.DescribeVirtualQueryConnection(named);
            var namedMapping = new VirtualHolonMapping(Guid.NewGuid(), 1, actor, provider.NativeDataProviderType,
                label, "owned-ci-graph", identity, mapping.Fields);
            var definition = namedMapping.ToDefinitionHolon();
            definition.Version = 1;
            Assert.IsFalse(definition.MetaData.Values.OfType<string>().Any(value => value.Contains(named["password"])));
            var reopened = VirtualHolonMapping.FromDefinitionHolon(actor, definition, identity);
            var previousPrivatePolicy = NativeConnectionGuard.AllowPrivateOverride;
            try
            {
                NativeConnectionGuard.AllowPrivateOverride = true;
                var matching = await reopened.QueryAsync(actor, provider, named, "tier", "Gold", new[] { "name" }, 2);
                Assert.IsFalse(matching.IsError, matching.Message);
                CollectionAssert.AreEquivalent(new[] { "Ada updated", "Second Gold" },
                    matching.Result.Select(result => (string)result.MetaData["name"]).ToArray());
                var retargeted = new NativeConnection(named) { ["database"] = "other" };
                var refused = await reopened.QueryAsync(actor, provider, retargeted, "tier", "Gold", new[] { "name" }, 2);
                Assert.AreEqual("VIRTUAL_HOLON_CONNECTION_CHANGED", refused.ErrorCode);
            }
            finally
            {
                NativeConnectionGuard.AllowPrivateOverride = previousPrivatePolicy;
            }
        }
        finally
        {
            provider.NativeImportLabels = precedingLabels;
            await (await inspection.RunAsync($"MATCH (n:`{label}`) DETACH DELETE n")).ConsumeAsync();
        }
    }
}
