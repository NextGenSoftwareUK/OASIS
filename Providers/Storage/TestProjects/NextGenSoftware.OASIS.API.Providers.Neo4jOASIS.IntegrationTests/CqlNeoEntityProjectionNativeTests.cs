using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    [TestMethod]
    public async Task NativeNeoEntityProjectionPreservesStagesAndProofBeforeDtoConstruction()
    {
        var rootPath=Environment.GetEnvironmentVariable("OASIS_CQL_NEO_ENTITY_PROJECTION_ROOT")
            ?? throw new InvalidOperationException("Use the explicit Neo entity-projection runner.");
        const string uri="bolt://127.0.0.1:17687";
        var state=Path.Combine(rootPath,"LocalState/COSMIC/NeoEntityProjectionCurrent");Directory.CreateDirectory(state);
        var journal=Path.Combine(state,"fixture.pending.json");
        Assert.IsFalse(File.Exists(journal),"Preserve/reconcile prior projection fixture; never replay creates.");
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
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
        var provider=new Neo4jOASIS(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=false};var active=false;
        try
        {
            var activation=await provider.ActivateProviderAsync();Assert.IsFalse(activation.IsError,activation.Message);active=true;
            var saved=await provider.SaveHolonAsync(CreateEntityProjectionFixture(ids,actor,other),saveChildren:true);
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.AreEqual(5L,await Count());
            await Checkpoint("created-awaiting-projection-verification");
            var checks=new List<object>();
            await VerifyEntityProjection((ICosmicQueryEntityProjectionStorageProvider)provider,ids[0],checks);
            await VerifyStandardEntityProjection((ICosmicQueryEntityProjectionStorageProvider)provider,ids[0],checks);
            Assert.AreEqual(14,checks.Count);
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
}
