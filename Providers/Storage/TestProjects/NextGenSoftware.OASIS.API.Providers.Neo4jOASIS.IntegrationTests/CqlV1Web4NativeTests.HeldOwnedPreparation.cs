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
    [DataTestMethod] [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeHeldOwnedPreparationEnforcesPermissionsAndDefersPublication(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyHeldOwnedPreparation,
            nativeJourneySuffix:"-held-owned-preparation-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyHeldOwnedPreparation(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;var authority=adapter.QueryProviderType;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {var watch=System.Diagnostics.Stopwatch.StartNew();try{return await action();}finally{timings.Add(name,watch.ElapsedMilliseconds);}}
        async Task<IReadOnlyList<Holon>> Outside(Guid id)
        {
            var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result).WithReadAccess(access).Result.Where(row=>row.Id==id).Result.Take(2).Result;
            var read=await CosmicLinq.Create(manager,adapter,query,4).Result.ToCosmicListAsync();
            Assert.IsFalse(read.IsError,read.Message+read.Exception);return read.Result;
        }
        var entity=new Holon{Id=Guid.Empty,Name="owned-held",IsActive=true,IsPublic=false,IsNewHolon=true};
        var opened=await CosmicNativeTransaction.BeginAsync(manager,adapter,access,new CosmicNativeTransactionOptions(maximumRows:32));
        Assert.IsFalse(opened.IsError,opened.Message+opened.Exception);
        Guid planned;
        await using(var transaction=opened.Result)
        {
            var prepared=await Timed("owned-prepare-create",()=>manager.PrepareOwnedUnitOfWorkAsync(new[]{entity},Array.Empty<IHolon>(),actor,
                new CosmicWriteOptions(Guid.NewGuid()),true,authority,Array.Empty<Guid>(),null,
                authorityRead:id=>transaction.ReadAuthorityRecordAsync(id)));
            Assert.IsFalse(prepared.IsError,prepared.Message+prepared.Exception);using var owner=prepared.Result;
            planned=owner.Request.Entries.Single().Holon.Id;owned.Add(planned);Assert.AreNotEqual(Guid.Empty,planned);Assert.AreEqual(Guid.Empty,entity.Id);
            var staged=await Timed("owned-stage-create",()=>transaction.StageAsync(owner.Request));
            Assert.IsFalse(staged.IsError,staged.Message+staged.Exception);Assert.IsFalse(staged.IsSaved);Assert.AreEqual(Guid.Empty,entity.Id);
            var refused=owner.PublishCommitted(new OASISResult<IEnumerable<IHolon>>(staged.Result.ReadProvisionalRows()){IsSaved=false});
            Assert.IsTrue(refused.IsError);Assert.AreEqual(Guid.Empty,entity.Id);
            var committed=await Timed("owned-commit",()=>transaction.CommitAsync());Assert.IsFalse(committed.IsError,committed.Message+committed.Exception);Assert.IsTrue(committed.IsSaved);
            var acknowledged=committed.Result.Operations.Single(item=>item.OperationId==owner.Request.Write.OperationId);
            var published=await Timed("owned-publish",()=>Task.FromResult(owner.PublishCommitted(
                new OASISResult<IEnumerable<IHolon>>(acknowledged.ReadProvisionalRows()){IsSaved=true,SavedCount=acknowledged.HolonIds.Count})));
            Assert.IsFalse(published.IsError,published.Message+published.Exception);Assert.IsTrue(published.IsSaved);Assert.AreEqual(planned,entity.Id);
            Assert.IsTrue(HolonChangeTracking.TryGetAcceptedRevision(entity,authority,out _));
        }
        Assert.AreEqual("owned-held",(await Timed("owned-outside-read",()=>Outside(planned))).Single().Name);
        var outsider=Guid.NewGuid();var foreign=await CosmicNativeTransaction.BeginAsync(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(outsider).Result);
        Assert.IsFalse(foreign.IsError,foreign.Message+foreign.Exception);
        await using(var transaction=foreign.Result)
        {
            var attempt=HolonChangeTracking.Clone(entity);attempt.Name="forbidden";
            var revision=CosmicConcurrency.GetProviderRevisionId(entity,authority);Assert.IsTrue(revision.HasValue);
            var denied=await Timed("owned-prepare-forbidden",()=>manager.PrepareOwnedUnitOfWorkAsync(new[]{attempt},Array.Empty<IHolon>(),outsider,
                new CosmicWriteOptions(Guid.NewGuid(),new Dictionary<Guid,Guid?>{{planned,revision}}),true,authority,Array.Empty<Guid>(),null,
                authorityRead:id=>transaction.ReadAuthorityRecordAsync(id)));
            Assert.IsTrue(denied.IsError);Assert.AreEqual("COSMIC_WRITE_FORBIDDEN",denied.ErrorCode,denied.Message+denied.Exception);
            Assert.IsFalse((await transaction.RollbackAsync()).IsError);
        }
        var rollbackEntity=new Holon{Id=Guid.Empty,Name="owned-rollback",IsActive=true,IsPublic=false,IsNewHolon=true};
        var rollback=await CosmicNativeTransaction.BeginAsync(manager,adapter,access);Assert.IsFalse(rollback.IsError,rollback.Message+rollback.Exception);
        Guid rolledId;
        await using(var transaction=rollback.Result)
        {
            var prepared=await Timed("owned-prepare-rollback",()=>manager.PrepareOwnedUnitOfWorkAsync(new[]{rollbackEntity},Array.Empty<IHolon>(),actor,
                new CosmicWriteOptions(Guid.NewGuid()),true,authority,Array.Empty<Guid>(),null,
                authorityRead:id=>transaction.ReadAuthorityRecordAsync(id)));
            Assert.IsFalse(prepared.IsError,prepared.Message+prepared.Exception);using var owner=prepared.Result;
            rolledId=owner.Request.Entries.Single().Holon.Id;owned.Add(rolledId);
            var staged=await Timed("owned-stage-rollback",()=>transaction.StageAsync(owner.Request));Assert.IsFalse(staged.IsError,staged.Message+staged.Exception);
            var aborted=await Timed("owned-native-rollback",()=>transaction.RollbackAsync());Assert.IsFalse(aborted.IsError,aborted.Message+aborted.Exception);
            Assert.AreEqual(Guid.Empty,rollbackEntity.Id);Assert.IsFalse(HolonChangeTracking.TryGetAcceptedRevision(rollbackEntity,authority,out _));
        }
        Assert.AreEqual(0,(await Timed("owned-absent-after-rollback",()=>Outside(rolledId))).Count);
        return new NativeFixtureJourneyProof(6,1,new Dictionary<string,bool>{["sharedOwnedPreparation"]=true,["generatedIdentityDeferred"]=true,
            ["provisionalPublicationRefused"]=true,["acknowledgedTrackedPublication"]=true,["writePermissionEnforced"]=true,["rollbackLeavesCallerUnpublished"]=true});
    }
}
