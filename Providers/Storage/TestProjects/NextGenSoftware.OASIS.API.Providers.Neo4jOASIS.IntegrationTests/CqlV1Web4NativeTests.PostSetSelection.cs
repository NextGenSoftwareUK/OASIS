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
    public sealed class PostSetSelectionDto {public Guid Id {get;set;} public string Name {get;set;}}

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeLateSetSelectionsPreserveIdentityWindowsAndDomainTiming(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyLateSetSelectionJourney,nativeJourneySuffix:"-post-set-selection-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyLateSetSelectionJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var native=(ICosmicQuerySetProjectionStorageProvider)adapter;
        Assert.IsTrue(native.SupportsSetProjection);
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        using var writer=CosmicSession.Create(manager,adapter,access).Result;
        var rows=new[]{"other","same","same","discarded"}.Select((name,index)=>new Holon{
            Id=Guid.NewGuid(),Name=name,Description=(index+1).ToString("D2"),
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true,
            MetaData=new Dictionary<string,object>{{"quantity",index==3?(object)"not-an-integer":(index+1)*10}}
        }).ToArray();
        owned.AddRange(rows.Select(row=>row.Id));
        foreach(var row in rows)Assert.IsFalse(writer.Add(row).IsError);
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var created=await Timed("create-post-set-selection",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        var versions=rows.Select(row=>row.Version).ToArray();
        var root=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result).WithReadAccess(access).Result;
        CosmicQuery<Holon> Input(Guid[] ids)=>root.Where(row=>ids.Contains(row.Id)).Result.OrderBy(row=>row.Description).Result;
        IQueryable<Holon> Native(Guid[] ids)=>CosmicLinq.Create(manager,adapter,Input(ids)).Result;
        var left=Native(new[]{rows[0].Id,rows[1].Id});
        var right=Native(new[]{rows[1].Id,rows[2].Id}).OrderByDescending(row=>row.Description);
        var union=await Timed("entity-union-then-scalar",()=>left.Union(right).Select(row=>row.Name).ToCosmicListAsync());
        Assert.IsFalse(union.IsError,union.Message+union.Exception);
        CollectionAssert.AreEqual(new[]{"other","same","same"},union.Result.ToArray());
        var concat=await Timed("entity-concat-then-dto",()=>left.Concat(right)
            .Select(row=>new PostSetSelectionDto{Id=row.Id,Name=row.Name}).ToCosmicListAsync());
        Assert.IsFalse(concat.IsError,concat.Message+concat.Exception);
        CollectionAssert.AreEqual(new[]{rows[0].Id,rows[1].Id,rows[2].Id,rows[1].Id},concat.Result.Select(row=>row.Id).ToArray());
        var intersect=await Timed("entity-intersect-then-record",()=>left.Intersect(right)
            .Select(row=>new{row.Id,row.Name}).ToCosmicListAsync());
        Assert.IsFalse(intersect.IsError,intersect.Message+intersect.Exception);Assert.AreEqual(1,intersect.Result.Count);
        Assert.AreEqual(rows[1].Id,intersect.Result[0].Id);
        var except=await Timed("entity-except-then-scalar",()=>left.Except(right).Select(row=>row.Name).ToCosmicListAsync());
        Assert.IsFalse(except.IsError,except.Message+except.Exception);
        CollectionAssert.AreEqual(new[]{"other"},except.Result.ToArray());
        var window=await Timed("entity-window-before-projection",()=>left.Union(right)
            .OrderBy(row=>row.Description).Skip(1).Take(1).Where(row=>row.Name=="same")
            .Select(row=>new{row.Id,row.Name}).ToCosmicListAsync());
        Assert.IsFalse(window.IsError,window.Message+window.Exception);Assert.AreEqual(1,window.Result.Count);
        Assert.AreEqual(rows[1].Id,window.Result[0].Id);
        var reselected=await Timed("late-selection-reselection",()=>left.Concat(right)
            .Select(row=>new{row.Id,row.Name}).Select(row=>row.Name).ToCosmicListAsync());
        Assert.IsFalse(reselected.IsError,reselected.Message+reselected.Exception);
        CollectionAssert.AreEqual(new[]{"other","same","same","same"},reselected.Result.ToArray());
        var malformedLeft=Native(new[]{rows[0].Id,rows[3].Id});
        var malformedRight=Native(new[]{rows[3].Id});
        var late=await Timed("discarded-invalid-cell-not-selected",()=>malformedLeft.Except(malformedRight)
            .Select(row=>CosmicQueryMetadata.Value<int>(row,"quantity")).ToCosmicListAsync());
        Assert.IsFalse(late.IsError,late.Message+late.Exception);CollectionAssert.AreEqual(new[]{10},late.Result.ToArray());
        var invalid=await Timed("retained-invalid-cell-refused",()=>malformedLeft.Union(malformedRight)
            .Select(row=>CosmicQueryMetadata.Value<int>(row,"quantity")).ToCosmicListAsync());
        Assert.IsTrue(invalid.IsError);Assert.IsFalse(invalid.IsLoaded);
        var boundedSet=CosmicSetQuery<Holon>.Create(Input(new[]{rows[0].Id,rows[1].Id}),
            Input(new[]{rows[1].Id,rows[2].Id}),CosmicSetQueryKind.Union).Result.Take(0).Result;
        var selected=boundedSet.Select(row=>row.Name);Assert.IsFalse(selected.IsError,selected.Message);
        var plan=CosmicSetProjectionPlanner.Plan(selected.Result,adapter.QueryProviderType,
            new CosmicProjectionTarget(adapter.QueryProviderType,adapter.QueryCapabilities,native.ProjectionCapabilities),
            native.SetQueryCapabilities,new CosmicSetQueryOptions(maximumLeftInputRows:1,maximumRightInputRows:2,maximumResultRows:100));
        Assert.IsFalse(plan.IsError,plan.Message);
        var bound=await Timed("input-bound-before-empty-selection",()=>manager.ExecuteSetProjectionQueryAsync(plan.Result,new[]{adapter}));
        Assert.IsTrue(bound.IsError);Assert.IsFalse(bound.IsLoaded);
        var foreign=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var ids=rows.Select(row=>row.Id).ToArray();
        var foreignRows=CosmicLinq.Create(manager,adapter,foreign.Where(row=>ids.Contains(row.Id)).Result).Result;
        var hidden=await Timed("late-set-preserves-caller",()=>foreignRows.Union(foreignRows).Select(row=>row.Name).ToCosmicListAsync());
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        var fresh=await Timed("late-selection-preserves-source",()=>Native(ids).Select(row=>new{row.Id,row.Version}).ToCosmicListAsync());
        Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);Assert.AreEqual(4,fresh.Result.Count);
        foreach(var item in fresh.Result)Assert.AreEqual(versions[Array.FindIndex(rows,row=>row.Id==item.Id)],item.Version);
        var calls=0;Func<string,string> forbidden=value=>{calls++;return value;};
        Assert.ThrowsException<CosmicLinqException>(()=>left.Union(right).Select(row=>forbidden(row.Name)));
        Assert.AreEqual(0,calls);
        CosmicQuery<ShadowStockHolon> Mapped(string key)
        {
            var model=new CosmicModelBuilder();
            model.Entity<ShadowStockHolon>().ShadowProperty<int>("Quantity").HasMetadataKey(key);
            var built=model.Build();Assert.IsFalse(built.IsError,built.Message);
            return CosmicQuery<ShadowStockHolon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
                .WithModel(built.Result).Result.WithReadAccess(access).Result;
        }
        var ambiguous=CosmicSetQuery<ShadowStockHolon>.Create(Mapped("left_quantity"),Mapped("right_quantity"),
            CosmicSetQueryKind.Union).Result.Select(row=>CosmicShadow.Value<int>(row,"Quantity"));
        Assert.IsTrue(ambiguous.IsError);Assert.AreEqual("CQL_SET_PROJECTION_MAPPING_MISMATCH",ambiguous.ErrorCode);
        return new NativeFixtureJourneyProof(9,1,new Dictionary<string,bool>{
            ["allFourSetsRetainEntityIdentity"]=true,["concatOccurrenceOrderAndDetachedShapes"]=true,
            ["entityWindowPrecedesSelection"]=true,["reselectionRetainsNativeColumns"]=true,
            ["discardedInvalidDomainNotDecoded"]=true,["retainedInvalidDomainRefusedAtomically"]=true,
            ["inputBudgetPrecedesEmptyOutput"]=true,["callerAndSourceVersionsPreserved"]=true,
            ["noSelectorExecutionAndMappingMismatchRefused"]=true});
    }
}
