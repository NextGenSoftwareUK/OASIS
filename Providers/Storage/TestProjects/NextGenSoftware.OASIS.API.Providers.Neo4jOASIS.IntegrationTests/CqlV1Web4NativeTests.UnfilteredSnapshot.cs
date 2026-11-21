using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task UnfilteredSnapshotReReadsOriginalFieldsAndPersistsOnePrivateStableIdentity(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,snapshotPreparation:true);

    private static async Task<int> VerifyUnfilteredSnapshotNative(IOASISStorageProvider provider,
        VirtualHolonMapping mapping,List<Guid> ownedIds,Dictionary<string,long> timings)
    {
        var native=(INativeDataQueryProvider)provider;
        Assert.IsInstanceOfType<INativeDataSnapshotQueryProvider>(provider);
        var actor=mapping.OwnerAvatarId;
        async Task<T> Timed<T>(string name,Func<Task<T>> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await operation();}
            finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var displayed=await mapping.QueryProjectionAsync(actor,(INativeDataProjectionQueryProvider)provider,null,
            Array.Empty<NativeRecordFilter>(),
            new[]{new NativeProjectionColumn("textAlias","Text",typeof(string))},100);
        Assert.IsFalse(displayed.IsError,displayed.Message+displayed.Exception);
        Assert.AreEqual(2,displayed.Result.Count);
        var selectedId=displayed.Result[0].Id;
        var original=await Timed("snapshot-original-fields",()=>mapping.QuerySnapshotCandidatesAsync(
            actor,native,null,Array.Empty<NativeRecordFilter>(),new[]{"textAlias"},200));
        Assert.IsFalse(original.IsError,original.Message+original.Exception);
        Assert.AreEqual(2,original.Result.Count);
        var selected=original.Result.Single(row=>row.Id==selectedId);
        var snapshot=selected.PrepareSnapshot(actor);
        Assert.AreEqual(selectedId,snapshot.Id);
        Assert.IsFalse(snapshot.IsPublic);
        Assert.AreEqual(actor,snapshot.CreatedByAvatarId);
        var text=JsonSerializer.SerializeToElement(snapshot.MetaData["textAlias"]).GetString();
        CollectionAssert.Contains(new[]{"😀","abc\0def"},text);
        var absent=await provider.LoadHolonAsync(selectedId,loadChildren:false,recursive:false);
        Assert.IsNull(absent.Result,"Snapshot preparation must not persist the native-only row.");
        var foreign=await mapping.QuerySnapshotCandidatesAsync(Guid.NewGuid(),native,null,
            Array.Empty<NativeRecordFilter>(),new[]{"textAlias"},200);
        Assert.IsTrue(foreign.IsError);Assert.AreEqual("VIRTUAL_HOLON_FORBIDDEN",foreign.ErrorCode);
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings {LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna) {
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        ownedIds.Add(selectedId);
        var saved=await Timed("snapshot-owned-save",()=>manager.SaveOwnedHolonAsync(snapshot,actor,
            new CosmicWriteOptions(Guid.NewGuid()),saveChildren:false,recursive:false,
            providerType:mapping.Provider));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        var loaded=await Timed("snapshot-native-readback",()=>provider.LoadHolonAsync(selectedId,
            loadChildren:false,recursive:false));
        Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);Assert.IsNotNull(loaded.Result);
        Assert.AreEqual(selectedId,loaded.Result.Id);Assert.IsFalse(loaded.Result.IsPublic);
        Assert.AreEqual(actor,loaded.Result.CreatedByAvatarId);
        Assert.AreEqual(text,JsonSerializer.SerializeToElement(loaded.Result.MetaData["textAlias"]).GetString());
        return 3;
    }
}
