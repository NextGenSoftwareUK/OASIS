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
    public Task NativeQueryTrackingModesKeepPerExecutionIdentityAndCallerBoundaries(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyTrackingModeJourney,nativeJourneySuffix:"-tracking-mode-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyTrackingModeJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        using var writer=CosmicSession.Create(manager,adapter,access).Result;
        var entity=new Holon{Id=Guid.NewGuid(),Name="tracking modes",
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var id=entity.Id;owned.Add(id);Assert.IsFalse(writer.Add(entity).IsError);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var created=await Timed("create-tracking-mode",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        using var reader=CosmicSession.Create(manager,adapter,access).Result;
        var source=reader.Linq<Holon>().Result.Where(row=>row.Id==id);
        var duplicates=source.Concat(source);
        var tracked=await Timed("tracking-shares-session-identity",()=>duplicates.ToCosmicListAsync());
        Assert.IsFalse(tracked.IsError,tracked.Message+tracked.Exception);Assert.AreEqual(2,tracked.Result.Count);
        Assert.AreSame(tracked.Result[0],tracked.Result[1]);
        Assert.AreEqual(CosmicEntityState.Unchanged,reader.State(tracked.Result[0]).Result);
        tracked.Result[0].Name="pending local edit";
        var again=await Timed("tracking-preserves-local-edit",()=>duplicates.ToCosmicListAsync());
        Assert.IsFalse(again.IsError,again.Message+again.Exception);Assert.AreSame(tracked.Result[0],again.Result[0]);
        Assert.AreEqual("pending local edit",again.Result[0].Name);
        var noTracking=duplicates.AsCosmicNoTracking();
        var plain=await Timed("no-tracking-separate-rows",()=>noTracking.ToCosmicListAsync());
        Assert.IsFalse(plain.IsError,plain.Message+plain.Exception);Assert.AreEqual(2,plain.Result.Count);
        Assert.AreNotSame(plain.Result[0],plain.Result[1]);Assert.AreNotSame(tracked.Result[0],plain.Result[0]);
        Assert.AreEqual("tracking modes",plain.Result[0].Name);
        Assert.AreEqual(CosmicEntityState.Detached,reader.State(plain.Result[0]).Result);
        var plainAgain=await Timed("no-tracking-fresh-execution",()=>noTracking.ToCosmicListAsync());
        Assert.IsFalse(plainAgain.IsError,plainAgain.Message+plainAgain.Exception);Assert.AreNotSame(plain.Result[0],plainAgain.Result[0]);
        var resolvedQuery=duplicates.AsCosmicNoTrackingWithIdentityResolution();
        var resolved=await Timed("per-read-identity-resolution",()=>resolvedQuery.ToCosmicListAsync());
        Assert.IsFalse(resolved.IsError,resolved.Message+resolved.Exception);Assert.AreEqual(2,resolved.Result.Count);
        Assert.AreSame(resolved.Result[0],resolved.Result[1]);Assert.AreNotSame(tracked.Result[0],resolved.Result[0]);
        Assert.AreEqual(CosmicEntityState.Detached,reader.State(resolved.Result[0]).Result);
        var resolvedAgain=await Timed("identity-map-not-retained",()=>resolvedQuery.ToCosmicListAsync());
        Assert.IsFalse(resolvedAgain.IsError,resolvedAgain.Message+resolvedAgain.Exception);
        Assert.AreSame(resolvedAgain.Result[0],resolvedAgain.Result[1]);Assert.AreNotSame(resolved.Result[0],resolvedAgain.Result[0]);
        var direct=await Timed("explicit-cql-mode",()=>reader.ReadAsync(reader.Query<Holon>().Result.Where(row=>row.Id==id).Result,
            CosmicQueryTrackingMode.NoTrackingWithIdentityResolution));
        Assert.IsFalse(direct.IsError,direct.Message+direct.Exception);Assert.AreEqual(1,direct.Result.Count);
        Assert.AreEqual(CosmicEntityState.Detached,reader.State(direct.Result[0]).Result);
        Assert.IsTrue(reader.Linq<Holon>((CosmicQueryTrackingMode)99).IsError);
        var invalid=await reader.ReadAsync(reader.Query<Holon>().Result,(CosmicQueryTrackingMode)99);
        Assert.IsTrue(invalid.IsError);Assert.AreEqual("COSMIC_QUERY_TRACKING_MODE_INVALID",invalid.ErrorCode);
        // Post-entity-set scalar selection is a separate, currently unregistered
        // native stage. Preserve its explicit refusal rather than using client evaluation.
        var unsupportedSelection=Assert.ThrowsException<CosmicLinqException>(()=>resolvedQuery.Select(row=>row.Name));
        Assert.AreEqual("CQL_EXPRESSION_UNSUPPORTED",unsupportedSelection.Code);
        var scalar=await Timed("mode-survives-scalar-composition",()=>source
            .AsCosmicNoTrackingWithIdentityResolution().Select(row=>row.Name).ToCosmicListAsync());
        Assert.IsFalse(scalar.IsError,scalar.Message+scalar.Exception);
        CollectionAssert.AreEqual(new[]{"tracking modes"},scalar.Result.ToArray());
        using var stateless=CosmicStatelessSession.Create(manager,adapter,access).Result;
        var statelessSource=stateless.Linq<Holon>().Result.Where(row=>row.Id==id);
        var statelessQuery=statelessSource.Concat(statelessSource).AsCosmicNoTrackingWithIdentityResolution();
        var statelessResolved=await Timed("stateless-per-read-identity",()=>statelessQuery.ToCosmicListAsync());
        Assert.IsFalse(statelessResolved.IsError,statelessResolved.Message+statelessResolved.Exception);
        Assert.AreEqual(2,statelessResolved.Result.Count);Assert.AreSame(statelessResolved.Result[0],statelessResolved.Result[1]);
        var statelessAgain=await Timed("stateless-map-not-retained",()=>statelessQuery.ToCosmicListAsync());
        Assert.IsFalse(statelessAgain.IsError,statelessAgain.Message+statelessAgain.Exception);
        Assert.AreNotSame(statelessResolved.Result[0],statelessAgain.Result[0]);
        var plainStateless=await Timed("stateless-explicit-no-tracking",()=>statelessQuery.AsCosmicNoTracking().ToCosmicListAsync());
        Assert.IsFalse(plainStateless.IsError,plainStateless.Message+plainStateless.Exception);
        Assert.AreNotSame(plainStateless.Result[0],plainStateless.Result[1]);
        var unsupported=Assert.ThrowsException<CosmicLinqException>(()=>statelessQuery.AsCosmicTracking());
        Assert.AreEqual("COSMIC_STATELESS_TRACKING_UNSUPPORTED",unsupported.Code);
        using var foreign=CosmicSession.Create(manager,adapter,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var foreignSource=foreign.Linq<Holon>().Result.Where(row=>row.Id==id);
        var hidden=await Timed("per-read-mode-preserves-access",()=>foreignSource.Concat(foreignSource)
            .AsCosmicNoTrackingWithIdentityResolution().ToCosmicListAsync());
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        reader.Dispose();
        var ended=await Timed("per-read-mode-preserves-lifetime",()=>resolvedQuery.ToCosmicListAsync());
        Assert.IsTrue(ended.IsError);
        return new NativeFixtureJourneyProof(8,1,new Dictionary<string,bool>{
            ["trackingRetainsSessionIdentityAndLocalEdits"]=true,["noTrackingUsesFreshRows"]=true,
            ["identityResolutionIsPerExecutionOnly"]=true,["resolvedRootsRemainDetached"]=true,
            ["explicitCqlAndLinqModesAgree"]=true,["statelessModesPreserveNoTrackingContract"]=true,
            ["scalarCompositionAndInvalidModeAdmission"]=true,["callerAndSessionLifetimePreserved"]=true});
    }
}
