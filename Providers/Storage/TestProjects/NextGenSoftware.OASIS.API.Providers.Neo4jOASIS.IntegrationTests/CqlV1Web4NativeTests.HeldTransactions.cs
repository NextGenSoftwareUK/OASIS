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
    [DataTestMethod] [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")]
    public Task NativeHeldOwnerPreservesExistingMixedUnitOfWork(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyMixedUnitOfWorkJourney,
            nativeJourneySuffix:"-held-owner-mixed-uow-native.json");

    [DataTestMethod] [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")]
    public Task NativeHeldTransactionsKeepProvisionalWritesAndReadsInsideOneOwner(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyHeldTransactionJourney,
            nativeJourneySuffix:"-held-transaction-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyHeldTransactionJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;var authority=adapter.QueryProviderType;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        var scope=CosmicQueryScope.Named(authority).Result;
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {var watch=System.Diagnostics.Stopwatch.StartNew();try{return await action();}finally{timings.Add(name,watch.ElapsedMilliseconds);}}
        Holon New(string name)
        {
            var row=new Holon{Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=actor,CreatedDate=DateTime.UtcNow,
                IsActive=true,IsPublic=false,IsNewHolon=true,Version=1,VersionId=Guid.NewGuid()};owned.Add(row.Id);return row;
        }
        ConditionalHolonUnitOfWorkRequest Write(Holon row,Guid? revision=null)=>new(
            new ConditionalHolonWrite(Guid.NewGuid(),actor,authority,new[]{new HolonWriteCondition(row.Id,revision)}),
            new[]{new ConditionalHolonUnitOfWorkEntry(row,ConditionalHolonOperation.Save)});
        CosmicQuery<Holon> Query(Guid id,CosmicQueryReadAccess policy)=>CosmicQuery<Holon>.Create(scope)
            .WithReadAccess(policy).Result.Where(row=>row.Id==id).Result.Take(2).Result;
        async Task<IReadOnlyList<Holon>> Outside(Guid id)
        {
            var source=CosmicLinq.Create(manager,adapter,Query(id,access),maximumResults:4);Assert.IsFalse(source.IsError,source.Message);
            var read=await source.Result.ToCosmicListAsync();Assert.IsFalse(read.IsError,read.Message+read.Exception);return read.Result;
        }
        Assert.IsTrue(provider is ICosmicNativeTransactionStorageProvider);
        var first=New("held-create");var second=New("held-rollback");
        var opened=await CosmicNativeTransaction.BeginAsync(manager,adapter,access,new CosmicNativeTransactionOptions(maximumRows:32));
        Assert.IsFalse(opened.IsError,opened.Message+opened.Exception);
        await using(var transaction=opened.Result)
        {
            var staged=await Timed("held-create-stage",()=>transaction.StageAsync(Write(first)));
            Assert.IsFalse(staged.IsError,staged.Message+staged.Exception);Assert.IsFalse(staged.IsSaved);
            Assert.AreEqual(CosmicNativeTransactionState.Active,transaction.State);
            var privateRows=await Timed("held-read-own-create",()=>transaction.ReadAsync(Query(first.Id,access),4));
            Assert.IsFalse(privateRows.IsError,privateRows.Message+privateRows.Exception);Assert.AreEqual("held-create",privateRows.Result.Single().Name);
            Assert.AreEqual(0,(await Timed("held-outside-before-commit",()=>Outside(first.Id))).Count);
            var changed=(Holon)staged.Result.ReadProvisionalRows().Single();
            var revision=CosmicConcurrency.GetProviderRevisionId(changed,authority);Assert.IsTrue(revision.HasValue);
            changed.Name="held-updated";changed.IsNewHolon=false;changed.Version++;changed.VersionId=Guid.NewGuid();
            Assert.AreEqual("held-create",staged.Result.ReadProvisionalRows().Single().Name,"Provisional packets are frozen, not mutable accepted entities.");
            var updated=await Timed("held-update-stage",()=>transaction.StageAsync(Write(changed,revision)));
            Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsFalse(updated.IsSaved);
            var ownUpdate=await Timed("held-read-own-update",()=>transaction.ReadAsync(Query(first.Id,access),4));
            Assert.IsFalse(ownUpdate.IsError,ownUpdate.Message+ownUpdate.Exception);Assert.AreEqual("held-updated",ownUpdate.Result.Single().Name);
            var committed=await Timed("held-commit",()=>transaction.CommitAsync());
            Assert.IsFalse(committed.IsError,committed.Message+committed.Exception);Assert.IsTrue(committed.IsSaved);
            Assert.AreEqual(2,committed.Result.Operations.Count);Assert.AreEqual(CosmicNativeTransactionState.Committed,transaction.State);
            Assert.IsTrue((await transaction.CommitAsync()).IsError,"Never replay a committed native transaction.");
        }
        Assert.AreEqual("held-updated",(await Timed("held-outside-after-commit",()=>Outside(first.Id))).Single().Name);
        var rollback=await CosmicNativeTransaction.BeginAsync(manager,adapter,access);Assert.IsFalse(rollback.IsError,rollback.Message+rollback.Exception);
        await using(var transaction=rollback.Result)
        {
            var staged=await Timed("held-rollback-stage",()=>transaction.StageAsync(Write(second)));
            Assert.IsFalse(staged.IsError,staged.Message+staged.Exception);Assert.IsFalse(staged.IsSaved);
            var inside=await Timed("held-read-before-rollback",()=>transaction.ReadAsync(Query(second.Id,access),4));
            Assert.IsFalse(inside.IsError,inside.Message+inside.Exception);Assert.AreEqual(1,inside.Result.Count);
            var aborted=await Timed("held-rollback",()=>transaction.RollbackAsync());Assert.IsFalse(aborted.IsError,aborted.Message+aborted.Exception);
            Assert.AreEqual(CosmicNativeTransactionState.RolledBack,transaction.State);
            Assert.IsTrue((await transaction.ReadAsync(Query(second.Id,access),4)).IsError);
        }
        Assert.AreEqual(0,(await Timed("held-outside-after-rollback",()=>Outside(second.Id))).Count);
        var foreignAccess=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var foreign=await CosmicNativeTransaction.BeginAsync(manager,adapter,foreignAccess);Assert.IsFalse(foreign.IsError,foreign.Message+foreign.Exception);
        await using(var transaction=foreign.Result)
        {
            var hidden=await Timed("held-foreign",()=>transaction.ReadAsync(Query(first.Id,foreignAccess),4));
            Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
            var mismatch=await transaction.ReadAsync(Query(first.Id,access),4);Assert.IsTrue(mismatch.IsError);
            using var cancelled=new CancellationTokenSource();cancelled.Cancel();
            var stopped=await transaction.CommitAsync(cancelled.Token);Assert.IsTrue(stopped.IsError);
            Assert.AreEqual(CosmicNativeTransactionState.Active,transaction.State);
            Assert.IsFalse((await Timed("held-foreign-rollback",()=>transaction.RollbackAsync())).IsError);
        }
        return new NativeFixtureJourneyProof(9,2,new Dictionary<string,bool>{["heldNativeOwner"]=true,["provisionalWriteWithoutSavedAcknowledgement"]=true,
            ["readOwnUncommittedWrites"]=true,["outsideReadCannotSeeProvisionalWrites"]=true,["sequentialNativeRevisionUpdate"]=true,
            ["immutableProvisionalSnapshots"]=true,["acknowledgedCommitAndRollback"]=true,["capturedCallerPolicy"]=true,["cancelBeforeCommitDoesNotDispatch"]=true});
    }
}
