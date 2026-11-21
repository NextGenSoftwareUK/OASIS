using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task SessionRangesSaveAndAttachThroughNativeAuthority(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,rangeRegistration:true);

    private static async Task<int> VerifyRangeRegistration(IOASISStorageProvider provider,
        List<Guid> ownedIds,Dictionary<string,long> timings)
    {
        var native=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings {LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna) {
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();
        using var session=CosmicSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        Holon New(string name)=>new(){Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=actor,
            IsPublic=false,IsActive=true,IsNewHolon=true};
        var first=New("range first");var second=New("range second");
        ownedIds.Add(first.Id);ownedIds.Add(second.Id);
        var added=session.AddRange(new[]{first,second});
        Assert.IsFalse(added.IsError,added.Message);Assert.AreEqual(2,added.Result.Count);
        foreach(var root in added.Result)
        {
            Assert.AreEqual(CosmicEntityState.Added,session.State(root).Result);
            var absent=await provider.LoadHolonAsync(root.Id,loadChildren:false,recursive:false);
            Assert.IsNull(absent.Result,"Registration must not write.");
        }
        var clock=System.Diagnostics.Stopwatch.StartNew();
        var created=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        timings["range-create"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        var loadedFirst=await provider.LoadHolonAsync(first.Id,loadChildren:false,recursive:false);
        var loadedSecond=await provider.LoadHolonAsync(second.Id,loadChildren:false,recursive:false);
        Assert.IsFalse(loadedFirst.IsError,loadedFirst.Message);Assert.IsNotNull(loadedFirst.Result);
        Assert.IsFalse(loadedSecond.IsError,loadedSecond.Message);Assert.IsNotNull(loadedSecond.Result);
        Assert.AreEqual("range first",loadedFirst.Result.Name);
        Assert.AreEqual("range second",loadedSecond.Result.Name);
        Assert.IsFalse(session.Clear().IsError);
        var attached=session.AttachRange(new[]{loadedFirst.Result,loadedSecond.Result});
        Assert.IsFalse(attached.IsError,attached.Message);
        Assert.AreEqual(CosmicEntityState.Unchanged,session.State(loadedFirst.Result).Result);
        var noop=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsFalse(noop.IsError,noop.Message);Assert.IsFalse(noop.IsSaved);
        loadedFirst.Result.Name="range updated";
        clock.Restart();
        var updated=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        timings["range-attached-update"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        var readback=await provider.LoadHolonAsync(first.Id,loadChildren:false,recursive:false);
        Assert.IsFalse(readback.IsError,readback.Message);Assert.AreEqual("range updated",readback.Result.Name);
        Assert.AreEqual(CosmicEntityState.Unchanged,session.State(loadedFirst.Result).Result);
        using var foreign=CosmicSession.Create(manager,native,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var ids=new[]{first.Id,second.Id};
        var query=foreign.Query<Holon>().Result.Where(row=>ids.Contains(row.Id)).Result;
        clock.Restart();
        var hidden=await foreign.ReadAsync(query);
        timings["range-foreign-read"]=clock.ElapsedMilliseconds;
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        return 3;
    }
}
