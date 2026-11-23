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
    public Task NativeLateSetCompositionPreservesOccurrencesDomainsAndWindows(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyLateSetCompositionJourney,nativeJourneySuffix:"-late-set-composition-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyLateSetCompositionJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var native=(ICosmicQuerySetProjectionCompositionStorageProvider)adapter;
        Assert.IsTrue(native.SupportsSetProjectionComposition);
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        using var writer=CosmicSession.Create(manager,adapter,access).Result;
        var rows=new[]{"beta","alpha","alpha","invalid"}.Select((name,index)=>new Holon{
            Id=Guid.NewGuid(),Name=name,Description=(index+1).ToString("D2"),
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true,
            MetaData=new Dictionary<string,object>{{"quantity",index==3?(object)"not-an-integer":(index+1)*10}}
        }).ToArray();
        owned.AddRange(rows.Select(row=>row.Id));
        foreach(var row in rows)Assert.IsFalse(writer.Add(row).IsError);
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,watch.ElapsedMilliseconds);}
        }
        var saved=await Timed("create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        var versions=rows.Select(row=>row.Version).ToArray();
        var scope=CosmicQueryScope.Named(adapter.QueryProviderType).Result;
        var root=CosmicQuery<Holon>.Create(scope).WithReadAccess(access).Result;
        CosmicQuery<Holon> Input(Guid[] ids)=>root.Where(row=>ids.Contains(row.Id)).Result.OrderBy(row=>row.Description).Result;
        IQueryable<Holon> Native(Guid[] ids)=>CosmicLinq.Create(manager,adapter,Input(ids)).Result;
        var left=Native(new[]{rows[0].Id,rows[1].Id});
        var right=Native(new[]{rows[1].Id,rows[2].Id}).OrderByDescending(row=>row.Description);
        foreach(var kind in new[]{CosmicSetQueryKind.Union,CosmicSetQueryKind.Concat,CosmicSetQueryKind.Intersect,CosmicSetQueryKind.Except})
        {
            var set=kind switch{
                CosmicSetQueryKind.Union=>left.Union(right),CosmicSetQueryKind.Concat=>left.Concat(right),
                CosmicSetQueryKind.Intersect=>left.Intersect(right),_=>left.Except(right)};
            var result=await Timed("kind-"+kind,()=>set.Select(row=>row.Name).Where(value=>value!="invalid").ToCosmicListAsync());
            Assert.IsFalse(result.IsError,result.Message+result.Exception);
            Assert.AreEqual(kind==CosmicSetQueryKind.Union?3:kind==CosmicSetQueryKind.Concat?4:1,result.Result.Count);
        }
        var page=await Timed("selected-order-page",()=>left.Concat(right)
            .Select(row=>new{row.Id,row.Name,Quantity=CosmicQueryMetadata.Value<int>(row,"quantity")})
            .Where(row=>row.Name=="alpha").OrderByDescending(row=>row.Quantity).ThenBy(row=>row.Name)
            .Skip(1).Take(1).ToCosmicListAsync());
        Assert.IsFalse(page.IsError,page.Message+page.Exception);Assert.AreEqual(1,page.Result.Count);
        Assert.AreEqual(rows[1].Id,page.Result[0].Id);Assert.AreEqual(20,page.Result[0].Quantity);
        var distinct=await Timed("selected-distinct",()=>left.Union(right).Select(row=>row.Name).Distinct().OrderBy(value=>value).ToCosmicListAsync());
        Assert.IsFalse(distinct.IsError,distinct.Message+distinct.Exception);
        CollectionAssert.AreEqual(new[]{"alpha","beta"},distinct.Result.ToArray());
        var reversed=await Timed("selected-reverse",()=>left.Concat(right).Select(row=>row.Id).Reverse().ToCosmicListAsync());
        Assert.IsFalse(reversed.IsError,reversed.Message+reversed.Exception);
        CollectionAssert.AreEqual(new[]{rows[1].Id,rows[2].Id,rows[1].Id,rows[0].Id},reversed.Result.ToArray());
        var after=await Timed("post-window-filter",()=>left.Concat(right).Select(row=>row.Name)
            .Take(1).Where(value=>value=="alpha").ToCosmicListAsync());
        Assert.IsFalse(after.IsError,after.Message+after.Exception);Assert.AreEqual(0,after.Result.Count);
        var reshaped=await Timed("entity-window-reselection",()=>left.Concat(right).Take(3)
            .Select(row=>new{Quantity=CosmicQueryMetadata.Value<int>(row,"quantity")})
            .Where(row=>row.Quantity>=20).OrderByDescending(row=>row.Quantity).Take(1)
            .Select(row=>new{Amount=row.Quantity}).ToCosmicListAsync());
        Assert.IsFalse(reshaped.IsError,reshaped.Message+reshaped.Exception);Assert.AreEqual(1,reshaped.Result.Count);
        Assert.AreEqual(30,reshaped.Result[0].Amount);
        var invalid=Native(new[]{rows[3].Id});
        var hiddenBad=await Timed("invalid-cell-before-empty-output",()=>invalid.Union(invalid)
            .Select(row=>CosmicQueryMetadata.Value<int>(row,"quantity")).Where(value=>value<0).Take(0).ToCosmicListAsync());
        Assert.IsTrue(hiddenBad.IsError);Assert.IsFalse(hiddenBad.IsLoaded);
        var allIds=rows.Take(3).Select(row=>row.Id).ToArray();
        var bounded=CosmicSetQuery<Holon>.Create(Input(allIds),Input(allIds),CosmicSetQueryKind.Concat).Result
            .Select(row=>row.Name).Result.Take(0).Result;
        var plan=CosmicSetProjectionPlanner.Plan(bounded,adapter.QueryProviderType,
            new CosmicProjectionTarget(adapter.QueryProviderType,adapter.QueryCapabilities,native.ProjectionCapabilities),
            native.SetQueryCapabilities,new CosmicSetQueryOptions(maximumLeftInputRows:1,maximumRightInputRows:3,maximumResultRows:10),
            native.CompositionCapabilities);
        Assert.IsFalse(plan.IsError,plan.Message);
        var tooMany=await Timed("input-bound-before-empty-output",()=>manager.ExecuteSetProjectionQueryAsync(plan.Result,new[]{adapter}));
        Assert.IsTrue(tooMany.IsError);Assert.IsFalse(tooMany.IsLoaded);
        var foreignRoot=CosmicQuery<Holon>.Create(scope)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var foreign=CosmicLinq.Create(manager,adapter,foreignRoot.Where(row=>allIds.Contains(row.Id)).Result).Result;
        var foreignRows=await Timed("foreign-scope",()=>foreign.Concat(foreign).Select(row=>row.Name)
            .Where(value=>value=="alpha").ToCosmicListAsync());
        Assert.IsFalse(foreignRows.IsError,foreignRows.Message+foreignRows.Exception);Assert.AreEqual(0,foreignRows.Result.Count);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var cancelled=await Timed("cancelled",()=>left.Concat(right).Select(row=>row.Name).Take(1).ToCosmicListAsync(cancellation.Token));
        Assert.IsTrue(cancelled.IsError);Assert.AreEqual("CQL_QUERY_CANCELLED",cancelled.ErrorCode);
        var fresh=await Timed("source-versions",()=>Native(rows.Select(row=>row.Id).ToArray()).Select(row=>new{row.Id,row.Version}).ToCosmicListAsync());
        Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);Assert.AreEqual(4,fresh.Result.Count);
        foreach(var row in fresh.Result)Assert.AreEqual(versions[Array.FindIndex(rows,item=>item.Id==row.Id)],row.Version);
        var calls=0;Func<string,bool> forbidden=value=>{calls++;return true;};
        Assert.ThrowsException<CosmicLinqException>(()=>left.Concat(right).Select(row=>row.Name).Where(value=>forbidden(value)));
        Assert.AreEqual(0,calls);
        return new NativeFixtureJourneyProof(8,1,new Dictionary<string,bool>{
            ["allFourEntitySetKinds"]=true,["selectedFilterOrderingPaging"]=true,
            ["selectedDistinctAndReverse"]=true,["entityAndSelectedWindowOrderAndReselection"]=true,
            ["invalidCellCannotHideBehindSelectedFilter"]=true,["inputBudgetBeforeEmptyOutput"]=true,
            ["callerAndVersionsPreserved"]=true,["noManagedSelectorAndCancellation"]=true});
    }
}
