using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    [TestMethod]
    public async Task NativeMongoEntityProjectionPreservesStagesAndProofBeforeDtoConstruction()
    {
        var rootPath=Environment.GetEnvironmentVariable("OASIS_CQL_MONGO_ENTITY_PROJECTION_ROOT")
            ?? throw new InvalidOperationException("Use the explicit Mongo entity-projection runner.");
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string database="cosmic_cql_entity_projection_v1";
        var state=Path.Combine(rootPath,"LocalState/COSMIC/MongoEntityProjectionCurrent");
        Directory.CreateDirectory(state);
        var journal=Path.Combine(state,"fixture.pending.json");
        Assert.IsFalse(File.Exists(journal),"Preserve/reconcile original projection fixture; never replay creates.");
        var client=new MongoClient(uri);
        using(var names=await client.ListDatabaseNamesAsync())
            Assert.IsFalse((await names.ToListAsync()).Contains(database),"Never reset an existing projection database.");
        var ids=Enumerable.Range(0,5).Select(_=>Guid.NewGuid()).ToArray();
        var actor=Guid.NewGuid();var other=Guid.NewGuid();
        await using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(file,new{database,ids,actor,other,stage="before-create"});file.Flush(true);
        }
        var provider=new MongoProvider(uri,database){IsVersionControlEnabled=false};
        var active=false;
        try
        {
            var activation=await provider.ActivateProviderAsync();Assert.IsFalse(activation.IsError,activation.Message);active=true;
            var root=new Holon{Id=ids[0],Name="protected source root",CreatedByAvatarId=actor,IsPublic=true,Version=1,
                MetaData=new(){["i64"]=9007199254740993L,["u64"]=ulong.MaxValue,["decimal"]=1.0000000000000000000000000001m,["text"]="Café 😀"}};
            root.Children.Add(new Holon{Id=ids[1],ParentHolonId=root.Id,Name="visible match",CreatedByAvatarId=actor,IsPublic=true,Version=1});
            var branch=new Holon{Id=ids[2],ParentHolonId=root.Id,Name="private branch",CreatedByAvatarId=other,Version=1};
            branch.Children.Add(new Holon{Id=ids[3],ParentHolonId=branch.Id,Name="grandchild one",CreatedByAvatarId=other,IsPublic=true,Version=1});
            branch.Children.Add(new Holon{Id=ids[4],ParentHolonId=branch.Id,Name="grandchild two",CreatedByAvatarId=other,IsPublic=true,Version=1});
            root.Children.Add(branch);
            var saved=await provider.SaveHolonAsync(root,saveChildren:true);Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            var acknowledged=journal+".next";
            await using(var file=new FileStream(acknowledged,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(file,new{database,ids,actor,other,stage="created-awaiting-projection-verification"});file.Flush(true);
            }
            File.Move(acknowledged,journal,true);
            var checks=new List<object>();
            await VerifyEntityProjection((ICosmicQueryEntityProjectionStorageProvider)provider,root.Id,checks);
            Assert.AreEqual(10,checks.Count);
            var deactivated=await provider.DeActivateProviderAsync();Assert.IsFalse(deactivated.IsError,deactivated.Message);active=false;
            await client.DropDatabaseAsync(database);
            using var remaining=await client.ListDatabaseNamesAsync();Assert.IsFalse((await remaining.ToListAsync()).Contains(database));
            var report=Path.Combine(rootPath,"TestResults/Neo4jCurrent/mongo-entity-projection-native.json");
            await File.WriteAllTextAsync(report,JsonSerializer.Serialize(new{provider="MongoDBOASIS",checks=checks.Count,cases=checks,exactCleanupVerified=true}));
            File.Delete(journal);
        }
        finally{if(active)Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);}
    }
}
