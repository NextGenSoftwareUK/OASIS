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
    public Task StatelessSessionUsesExplicitNativeBulkOperations(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,statelessJourney:true);

    private static async Task<int> VerifyStatelessJourney(IOASISStorageProvider provider,
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
        Holon New(string name,Guid owner)=>new() {Id=Guid.NewGuid(),Name=name,
            CreatedByAvatarId=owner,IsPublic=false,IsActive=true,IsNewHolon=true};
        CosmicWriteOptions Intent(params IHolon[] roots)=>new(Guid.NewGuid(),
            roots.ToDictionary(root=>root.Id,root=>CosmicConcurrency.GetProviderRevisionId(root,session.Authority)));
        void Saved(OASISResult<IEnumerable<IHolon>> result)
        {Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsSaved);}
        var first=New("stateless first",actor);var second=New("stateless second",actor);
        ownedIds.Add(first.Id);ownedIds.Add(second.Id);
        Saved(await Timed("create-bulk",()=>session.SaveAsync(new[]{first,second},Intent(first,second))));
        var missing=await session.SaveAsync(new[]{first},new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsTrue(missing.IsError);
        Assert.AreEqual("COSMIC_STATELESS_REVISIONS_REQUIRED",missing.ErrorCode);
        Assert.IsFalse(missing.IsSaved);

        var ids=new[]{first.Id,second.Id};
        var query=session.Query<Holon>().Result.Where(row=>ids.Contains(row.Id)).Result;
        var read1=await Timed("fresh-query-one",()=>session.ReadAsync(query));
        Assert.IsFalse(read1.IsError,read1.Message+read1.Exception);
        Assert.AreEqual(2,read1.Result.Count);
        var local=read1.Result.Single(row=>row.Id==first.Id);
        Assert.AreNotSame(first,local);
        var stale=HolonChangeTracking.Clone(local);
        local.Name="local only; never submitted";
        var read2=await Timed("fresh-query-two",()=>session.ReadAsync(query));
        Assert.IsFalse(read2.IsError,read2.Message+read2.Exception);
        var fresh=read2.Result.Single(row=>row.Id==first.Id);
        Assert.AreNotSame(local,fresh,"A stateless read must not reuse an identity-map entry.");
        Assert.AreEqual("stateless first",fresh.Name);
        Assert.AreEqual("local only; never submitted",local.Name);

        first.Name="stateless winner";
        var third=New("stateless third",actor);ownedIds.Add(third.Id);
        Saved(await Timed("mixed-create-update",()=>session.SaveAsync(new[]{first,third},Intent(first,third))));
        stale.Name="stale writer must not win";second.Name="second must not partially change";
        var conflict=await Timed("stale-batch-refusal",()=>session.SaveAsync(new[]{stale,second},Intent(stale,second)));
        Assert.IsTrue(conflict.IsError);
        Assert.IsFalse(conflict.IsSaved);
        ids=new[]{first.Id,second.Id,third.Id};
        query=session.Query<Holon>().Result.Where(row=>ids.Contains(row.Id)).Result;
        var after=await Timed("verify-no-partial-update",()=>session.ReadAsync(query));
        Assert.IsFalse(after.IsError,after.Message+after.Exception);
        Assert.AreEqual(3,after.Result.Count);
        Assert.AreEqual("stateless winner",after.Result.Single(row=>row.Id==first.Id).Name);
        Assert.AreEqual("stateless second",after.Result.Single(row=>row.Id==second.Id).Name);

        var foreignActor=Guid.NewGuid();
        var foreign=CosmicStatelessSession.Create(manager,native,
            CosmicQueryReadAccess.OwnerOrPublic(foreignActor).Result).Result;
        var other=New("stateless foreign survivor",foreignActor);ownedIds.Add(other.Id);
        Saved(await Timed("foreign-create",()=>foreign.SaveAsync(new[]{other},Intent(other))));
        var denied=await Timed("whole-delete-authorization",()=>session.DeleteAsync(new[]{first,other},Intent(first,other),softDelete:false));
        Assert.IsTrue(denied.IsError);
        Assert.IsFalse(denied.IsSaved);
        var foreignQuery=foreign.Query<Holon>().Result.Where(row=>row.Id==other.Id).Result;
        var mismatch=await session.ReadAsync(foreignQuery);
        Assert.IsTrue(mismatch.IsError);
        Assert.AreEqual("COSMIC_STATELESS_SCOPE_MISMATCH",mismatch.ErrorCode);
        var allIds=ownedIds.ToArray();
        var foreignAll=foreign.Query<Holon>().Result.Where(row=>allIds.Contains(row.Id)).Result;
        var privateRows=await Timed("foreign-private-query",()=>foreign.ReadAsync(foreignAll));
        Assert.IsFalse(privateRows.IsError,privateRows.Message);
        Assert.AreEqual(1,privateRows.Result.Count);
        Assert.AreEqual(other.Id,privateRows.Result[0].Id);
        var surviving=await Timed("verify-owner-survivors",()=>session.ReadAsync(query));
        Assert.IsFalse(surviving.IsError,surviving.Message);
        Assert.AreEqual(3,surviving.Result.Count);

        var deleted=await Timed("hard-delete-bulk",()=>session.DeleteAsync(
            surviving.Result,Intent(surviving.Result.Cast<IHolon>().ToArray()),softDelete:false));
        Saved(deleted);Assert.IsTrue(deleted.IsDeleted);
        var empty=await Timed("query-after-delete",()=>session.ReadAsync(query));
        Assert.IsFalse(empty.IsError,empty.Message);
        Assert.AreEqual(0,empty.Result.Count);
        await Timed("native-hard-absence",async ()=>{
            foreach(var id in ids)
            {
                var absent=await provider.LoadHolonAsync(id,loadChildren:false,recursive:false);
                Assert.IsNull(absent.Result,"Hard deletion must remove the native record.");
                Assert.IsTrue(!absent.IsError || absent.ErrorCode==OASISResultErrorCodes.RecordNotFound,
                    absent.Message+absent.Exception);
            }
            return true;
        });
        return 1;
    }
}
