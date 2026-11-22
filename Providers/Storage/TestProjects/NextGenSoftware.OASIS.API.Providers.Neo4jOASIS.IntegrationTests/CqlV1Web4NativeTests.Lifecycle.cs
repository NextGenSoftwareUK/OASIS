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
    public Task SessionValidationAndAcknowledgedCallbacksPreserveNativeOutcome(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,lifecycleJourney:true);

    private static async Task<int> VerifyLifecycleJourney(IOASISStorageProvider provider,
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
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();
        CosmicSession Open(Guid who)=>CosmicSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(who).Result).Result;
        using var session=Open(actor);var beforeCalls=0;var afterCalls=0;
        var kind=ConditionalHolonOperation.Save;
        var root=new Holon {Id=Guid.NewGuid(),Name="unapproved root",CreatedByAvatarId=actor,
            IsPublic=false,IsActive=true,IsNewHolon=true,MetaData=new Dictionary<string,object>{{"cost",12.34m}}};
        ownedIds.Add(root.Id);Assert.IsFalse(session.Add(root).IsError);
        var builder=new CosmicValidationModelBuilder();var entity=builder.Entity<Holon>();
        var nameRule=entity.Property(row=>row.Name).IsRequired().HasMaxLength(4);
        entity.Property("MetaData.cost",row=>(decimal)row.MetaData["cost"]).HasPrecision(5,2);
        var frozenSmall=builder.Build();nameRule.HasMaxLength(80);var model=builder.Build();
        async Task<T> Timed<T>(string name,Func<Task<T>> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try {return await operation();}
            finally {timings.Add(name,clock.ElapsedMilliseconds);}
        }
        void Context(CosmicSessionSaveContext context,bool committed)
        {
            Assert.AreEqual(actor,context.ActorId);Assert.AreEqual(native.QueryProviderType,context.Authority);
            Assert.AreNotEqual(Guid.Empty,context.OperationId);Assert.AreEqual(kind,context.Operation);
            Assert.AreEqual(committed,context.IsCommitted);Assert.AreEqual(1,context.Roots.Count);
            Assert.AreSame(root,context.Roots[0]);
            var busy=session.Query<Holon>();Assert.IsTrue(busy.IsError);Assert.AreEqual("COSMIC_SESSION_BUSY",busy.ErrorCode);
            var configure=session.SetLifecycle(null);Assert.IsTrue(configure.IsError);
            Assert.AreEqual("COSMIC_SESSION_BUSY",configure.ErrorCode);
        }
        async Task<OASISResult<bool>> Approve(CosmicSessionSaveContext context,CancellationToken token)
        {beforeCalls++;Context(context,false);await Task.Yield();token.ThrowIfCancellationRequested();
            var validation=model.Validate(context);Assert.IsFalse(validation.IsError,validation.Message);return validation;}
        Task<OASISResult<bool>> Acknowledge(CosmicSessionSaveContext context)
        {afterCalls++;Context(context,true);return Task.FromResult(new OASISResult<bool>(true));}
        Assert.IsFalse(session.SetLifecycle(new CosmicSessionLifecycle((context,token)=>{
            beforeCalls++;Context(context,false);
            var savedName=root.Name;var savedCost=root.MetaData["cost"];
            try {
                var length=frozenSmall.Validate(context);Assert.IsTrue(length.IsError);
                Assert.AreEqual("COSMIC_MODEL_VALIDATION_FAILED",length.ErrorCode);
                root.Name=null;Assert.IsTrue(model.Validate(context).IsError);
                root.Name="valid";root.MetaData["cost"]=1234.56m;Assert.IsTrue(model.Validate(context).IsError);
                root.MetaData["cost"]=12.345m;Assert.IsTrue(model.Validate(context).IsError);
                root.MetaData["cost"]=12.3400m;Assert.IsFalse(model.Validate(context).IsError);
            } finally {root.Name=savedName;root.MetaData["cost"]=savedCost;}
            var immutable=frozenSmall.Validate(context);Assert.IsTrue(immutable.IsError);
            return Task.FromResult(immutable);
        },Acknowledge)).IsError);
        await Timed("validation-veto-and-native-absence",async ()=>{
            var veto=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
            Assert.IsTrue(veto.IsError);Assert.IsFalse(veto.IsSaved);
            Assert.AreEqual("COSMIC_SESSION_VALIDATION_REFUSED",veto.ErrorCode);
            var id=root.Id;var rows=await session.Linq<Holon>().Result.Where(row=>row.Id==id).ToCosmicListAsync();
            Assert.IsFalse(rows.IsError,rows.Message);Assert.AreEqual(0,rows.Result.Count);Assert.AreEqual(0,afterCalls);
            return true;
        });
        Assert.IsFalse(session.SetLifecycle(new CosmicSessionLifecycle(Approve,Acknowledge)).IsError);
        root.Name="approved create";
        var created=await Timed("approved-create",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message);Assert.IsTrue(created.IsSaved);
        Assert.AreEqual(2,beforeCalls);Assert.AreEqual(1,afterCalls);
        Assert.AreEqual(CosmicEntityState.Unchanged,session.State(root).Result);

        Assert.IsFalse(session.SetLifecycle(new CosmicSessionLifecycle(Approve,context=>{
            afterCalls++;Context(context,true);
            return Task.FromException<OASISResult<bool>>(new InvalidOperationException("owned callback failure"));
        })).IsError);
        root.Name="committed despite callback failure";
        await Timed("postcommit-callback-failure",async ()=>{
            var committed=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
            Assert.IsTrue(committed.IsError);Assert.IsTrue(committed.IsSaved);
            Assert.AreEqual("COSMIC_SESSION_POST_COMMIT_CALLBACK_FAILED",committed.ErrorCode);
            Assert.IsTrue(committed.Exception is InvalidOperationException);
            Assert.AreEqual("owned callback failure",committed.Exception.Message);Assert.IsNotNull(committed.Result);
            Assert.AreEqual(root.Id,committed.Result.Single().Id);
            Assert.AreEqual(CosmicEntityState.Unchanged,session.State(root).Result);
            var values=await session.GetDatabaseValuesAsync(root);Assert.IsFalse(values.IsError,values.Message);
            Assert.AreEqual("committed despite callback failure",values.Result.Name);return true;
        });
        await Timed("acknowledged-noop-skips-callbacks",async ()=>{
            var noop=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
            Assert.IsFalse(noop.IsError,noop.Message);Assert.IsFalse(noop.IsSaved);
            Assert.AreEqual(3,beforeCalls);Assert.AreEqual(2,afterCalls);return true;
        });
        Assert.IsFalse(session.SetLifecycle(new CosmicSessionLifecycle((context,token)=>{
            beforeCalls++;Context(context,false);
            return Task.FromCanceled<OASISResult<bool>>(new CancellationToken(true));
        },Acknowledge)).IsError);
        root.Name="cancelled change";
        await Timed("cancelled-validation",async ()=>{
            var cancelled=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
            Assert.IsTrue(cancelled.IsError);Assert.IsFalse(cancelled.IsSaved);
            Assert.AreEqual("COSMIC_SESSION_CANCELLED",cancelled.ErrorCode);return true;
        });
        var originalId=root.Id;
        Assert.IsFalse(session.SetLifecycle(new CosmicSessionLifecycle((context,token)=>{
            beforeCalls++;Context(context,false);root.Id=Guid.NewGuid();
            return Task.FromResult(new OASISResult<bool>(true));
        },Acknowledge)).IsError);
        await Timed("identity-changing-validation-refused",async ()=>{
            try {
                var changed=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
                Assert.IsTrue(changed.IsError);Assert.IsFalse(changed.IsSaved);
                Assert.AreEqual("COSMIC_SESSION_IDENTITY_CHANGED",changed.ErrorCode);
            } finally {root.Id=originalId;}
            var values=await session.GetDatabaseValuesAsync(root);Assert.IsFalse(values.IsError,values.Message);
            Assert.AreEqual("committed despite callback failure",values.Result.Name);return true;
        });
        var foreignActor=Guid.NewGuid();using var foreign=Open(foreignActor);var foreignAfter=0;var foreignApproved=false;
        var foreignRoot=HolonChangeTracking.Clone(root);foreignRoot.CreatedByAvatarId=foreignActor;
        Assert.IsFalse(foreign.Update(foreignRoot).IsError);
        Assert.IsFalse(foreign.SetLifecycle(new CosmicSessionLifecycle((context,token)=>{
            Assert.AreEqual(foreignActor,context.ActorId);Assert.IsFalse(context.IsCommitted);foreignApproved=true;
            return Task.FromResult(new OASISResult<bool>(true));
        },context=>{foreignAfter++;return Task.FromResult(new OASISResult<bool>(true));})).IsError);
        await Timed("owned-write-refusal-skips-postcommit",async ()=>{
            var denied=await foreign.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
            Assert.IsTrue(foreignApproved);Assert.IsTrue(denied.IsError);Assert.IsFalse(denied.IsSaved);
            Assert.AreNotEqual("COSMIC_SESSION_VALIDATION_FAILED",denied.ErrorCode);Assert.AreEqual(0,foreignAfter);return true;
        });
        kind=ConditionalHolonOperation.HardDelete;
        Assert.IsFalse(session.SetLifecycle(model.CreateLifecycle(Acknowledge)).IsError);
        Assert.IsFalse(session.Remove(root,softDelete:false).IsError);
        var deleted=await Timed("approved-hard-delete",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsTrue(deleted.IsSaved);Assert.IsTrue(deleted.IsDeleted);
        Assert.AreEqual(5,beforeCalls);Assert.AreEqual(3,afterCalls);
        await Timed("verify-physical-absence",async ()=>{
            var absent=await provider.LoadHolonAsync(originalId,loadChildren:false,recursive:false);
            Assert.IsNull(absent.Result);Assert.AreEqual(CosmicEntityState.Detached,session.State(root).Result);return true;
        });
        return 1;
    }
}
