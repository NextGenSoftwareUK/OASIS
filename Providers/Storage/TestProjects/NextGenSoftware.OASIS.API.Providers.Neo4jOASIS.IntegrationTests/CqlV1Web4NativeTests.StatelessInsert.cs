using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task StatelessInsertPublishesGeneratedIdsAndNeverUpserts(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,statelessInsertJourney:true);

    private static async Task<int> VerifyStatelessInsertJourney(IOASISStorageProvider provider,
        List<Guid> ownedIds,Dictionary<string,long> timings)
    {
        var native=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings {LogSwitchingProviders=false}
        }};
        var registry=new ProviderManager(null,dna) {
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false
        };
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();
        var opened=CosmicStatelessSession.Create(manager,native,
            CosmicQueryReadAccess.OwnerOrPublic(actor).Result);
        Assert.IsFalse(opened.IsError,opened.Message);
        var session=opened.Result;
        async Task<T> Timed<T>(string name,Func<Task<T>> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try {return await operation();}
            finally {timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Holon New(string name,Guid owner)=>new() {Name=name,CreatedByAvatarId=owner,
            IsPublic=false,IsActive=true,IsNewHolon=true,MetaData=new()};
        void Saved(OASISResult<IEnumerable<IHolon>> result)
        {Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsSaved);}

        var first=New("generated first",actor);var second=New("generated second",actor);
        Assert.AreEqual(Guid.Empty,first.Id);Assert.AreEqual(Guid.Empty,second.Id);
        Saved(await Timed("generated-create-bulk",()=>session.InsertAsync(
            new[]{first,second},new CosmicWriteOptions(Guid.NewGuid()))));
        Assert.AreNotEqual(Guid.Empty,first.Id);Assert.AreNotEqual(Guid.Empty,second.Id);
        Assert.AreNotEqual(first.Id,second.Id);
        ownedIds.Add(first.Id);ownedIds.Add(second.Id);
        Assert.IsFalse(first.IsNewHolon);Assert.IsFalse(second.IsNewHolon);
        Assert.IsTrue(CosmicConcurrency.GetProviderRevisionId(first,session.Authority).HasValue);
        Assert.IsTrue(CosmicConcurrency.GetProviderRevisionId(second,session.Authority).HasValue);
        var ids=new[]{first.Id,second.Id};
        var query=session.Query<Holon>().Result.Where(row=>ids.Contains(row.Id)).Result;
        var loaded=await Timed("read-generated",()=>session.ReadAsync(query));
        Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);
        Assert.AreEqual(2,loaded.Result.Count);

        var foreignActor=Guid.NewGuid();
        var foreign=CosmicStatelessSession.Create(manager,native,
            CosmicQueryReadAccess.OwnerOrPublic(foreignActor).Result).Result;
        var parent=New("restricted foreign parent",foreignActor);
        var policy=HolonPermissionSet.Empty.Encode();
        Assert.IsFalse(policy.IsError,policy.Message);
        parent.MetaData[HolonPermissions.MetadataKey]=policy.Result;
        Saved(await Timed("foreign-parent-create",()=>foreign.InsertAsync(
            new[]{parent},new CosmicWriteOptions(Guid.NewGuid()))));
        ownedIds.Add(parent.Id);
        var blockedOne=New("blocked unparented",actor);
        var blockedTwo=New("blocked parented",actor);blockedTwo.ParentHolonId=parent.Id;
        var refused=await Timed("whole-batch-parent-permission",()=>session.InsertAsync(
            new[]{blockedOne,blockedTwo},new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(refused.IsError);Assert.IsFalse(refused.IsSaved);
        Assert.AreEqual("COSMIC_ADD_CHILDREN_FORBIDDEN",refused.ErrorCode);
        Assert.AreEqual(Guid.Empty,blockedOne.Id,"A refused batch must not publish a generated identity.");
        Assert.AreEqual(Guid.Empty,blockedTwo.Id,"A refused batch must not publish a generated identity.");
        var blockedNames=new[]{"blocked unparented","blocked parented"};
        var absentQuery=session.Query<Holon>().Result.Where(row=>blockedNames.Contains(row.Name)).Result;
        var absent=await Timed("verify-no-failed-inserts",()=>session.ReadAsync(absentQuery));
        Assert.IsFalse(absent.IsError,absent.Message);
        Assert.AreEqual(0,absent.Result.Count,"Parent authorization must refuse the whole batch.");
        var parentId=parent.Id;
        var parentQuery=foreign.Query<Holon>().Result.Where(row=>row.Id==parentId).Result;
        var survivor=await Timed("verify-parent-survives",()=>foreign.ReadAsync(parentQuery));
        Assert.IsFalse(survivor.IsError,survivor.Message);
        Assert.AreEqual(1,survivor.Result.Count);

        var collision=New("must never upsert",actor);collision.Id=first.Id;
        var notUpdated=await Timed("assigned-create-only-conflict",()=>session.InsertAsync(
            new[]{collision},new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(notUpdated.IsError);Assert.IsFalse(notUpdated.IsSaved);
        Assert.AreEqual(CosmicConcurrencyException.Code,notUpdated.ErrorCode);
        var unchanged=await Timed("verify-created-values",()=>session.ReadAsync(query));
        Assert.IsFalse(unchanged.IsError,unchanged.Message);
        Assert.AreEqual(2,unchanged.Result.Count);
        Assert.AreEqual("generated first",unchanged.Result.Single(row=>row.Id==first.Id).Name);
        Assert.AreEqual("generated second",unchanged.Result.Single(row=>row.Id==second.Id).Name);
        return 1;
    }
}
