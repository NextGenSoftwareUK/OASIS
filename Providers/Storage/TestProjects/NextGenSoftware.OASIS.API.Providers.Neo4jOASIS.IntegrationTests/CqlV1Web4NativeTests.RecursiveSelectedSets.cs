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
    [DataTestMethod] [DataRow("MongoDBOASIS")]
    public Task NativeRecursiveSelectedSetsUseVisibleTupleEquality(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyNativeSelectedSetTree,
            nativeJourneySuffix:"-recursive-selected-set-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyNativeSelectedSetTree(IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;var native=(ICosmicQueryProjectedSetStorageProvider)provider;
        Assert.IsTrue(native.ProjectedSetCapabilities.SupportsNestedSets);
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> read)
        {var watch=System.Diagnostics.Stopwatch.StartNew();try{return await read();}finally{timings.Add(name,watch.ElapsedMilliseconds);}}
        var records=new[]{new Holon{Name="a",Description="x"},new Holon{Name="a",Description="y"},
            new Holon{Name="a",Description="x"},new Holon{Name="b",Description="z"}};
        using(var writer=CosmicSession.Create(manager,adapter,access).Result)
        {
            foreach(var row in records){row.Id=Guid.NewGuid();owned.Add(row.Id);row.IsNewHolon=true;row.IsActive=true;row.CreatedByAvatarId=actor;Assert.IsFalse(writer.Add(row).IsError);}
            var save=await Timed("create-selected-tree",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsFalse(save.IsError,save.Message+save.Exception);Assert.IsTrue(save.IsSaved);
        }
        var ids=records.Select(row=>row.Id).ToArray();var scope=CosmicQueryScope.Named(adapter.QueryProviderType).Result;
        var columns=new[]{CosmicProjectionSelection<Holon>.Create("Name",row=>row.Name).Result,
            CosmicProjectionSelection<Holon>.Create("Category",row=>row.Description).Result};
        CosmicProjectionQuery<Holon,CosmicProjectionRecord> Source(CosmicQueryReadAccess policy,int start)
        {
            var selected=ids.Skip(start).Take(2).ToArray();
            return CosmicQuery<Holon>.Create(scope).WithReadAccess(policy).Result.Where(row=>selected.Contains(row.Id)).Result
                .OrderBy(row=>row.Description).Result.ThenBy(row=>row.Id).Result.Take(2).Result.SelectColumns(columns).Result;
        }
        var a=Source(access,0);var b=Source(access,1);var c=Source(access,2);
        // b sorts x before y. Child occurrences: ax,ay,ax; c: ax,bz.
        CosmicProjectedSetInput<Holon,CosmicProjectionRecord> Leaf(CosmicProjectionQuery<Holon,CosmicProjectionRecord> query)
            =>CosmicProjectedSetInput<Holon,CosmicProjectionRecord>.FromSource(query).Result;
        CosmicProjectedSetInput<Holon,CosmicProjectionRecord> Node(CosmicProjectedSetQuery<Holon,CosmicProjectionRecord> query)
            =>CosmicProjectedSetInput<Holon,CosmicProjectionRecord>.FromSet(query).Result;
        var child=CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.Create(a,b,CosmicSetQueryKind.Concat).Result.Take(3).Result;
        CosmicProjectedSetQuery<Holon,CosmicProjectionRecord> Parent(CosmicSetQueryKind kind)
            =>CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.CreateTree(Node(child),Leaf(c),kind).Result;
        var query=Parent(CosmicSetQueryKind.Concat);
        var target=new CosmicProjectionCompositionTarget(adapter.QueryProviderType,adapter.QueryCapabilities,native.ProjectionCapabilities,native.CompositionCapabilities);
        var bounds=new CosmicProjectedSetOptions(new CosmicSetQueryOptions(8,8,1048576,10));
        CosmicProjectedSetPlan<Holon,CosmicProjectionRecord> Plan(CosmicProjectedSetQuery<Holon,CosmicProjectionRecord> value,CosmicProjectedSetOptions? limits=null)
        {var plan=CosmicProjectedSetPlanner.Plan(value,adapter.QueryProviderType,target,native.ProjectedSetCapabilities,limits??bounds);Assert.IsFalse(plan.IsError,plan.Message+plan.Exception);return plan.Result;}
        var old=native.ProjectedSetCapabilities;
        var flat=new CosmicProjectedSetCapabilities(old.Features,old.ValueDomains.ToArray(),old.EqualityDomains.ToArray(),old.ResultStages);
        Assert.IsFalse(flat.SupportsNestedSets);Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",CosmicProjectedSetPlanner.Plan(query,adapter.QueryProviderType,target,flat).ErrorCode);
        Assert.AreEqual("CQL_SET_ACCESS_MISMATCH",CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.CreateTree(Node(child),Leaf(Source(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,2)),CosmicSetQueryKind.Union).ErrorCode);
        string[] Values(CosmicProjectionExecution<CosmicProjectionRecord> value)=>value.Providers.Single().Rows.Select(row=>(string)row["Name"]+"|"+(string)row["Category"]).ToArray();
        async Task<string[]> Read(string name,CosmicProjectedSetQuery<Holon,CosmicProjectionRecord> value)
        {var read=await Timed(name,()=>manager.ExecuteProjectedSetQueryAsync(Plan(value),new[]{adapter},10));Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.IsTrue(read.IsLoaded);return Values(read.Result);}
        CollectionAssert.AreEqual(new[]{"a|x","a|y","a|x","a|x","b|z"},await Read("selected-concat",query));
        CollectionAssert.AreEqual(new[]{"a|x","a|y","b|z"},await Read("selected-union",Parent(CosmicSetQueryKind.Union)));
        CollectionAssert.AreEqual(new[]{"a|x"},await Read("selected-intersect",Parent(CosmicSetQueryKind.Intersect)));
        CollectionAssert.AreEqual(new[]{"a|y"},await Read("selected-except",Parent(CosmicSetQueryKind.Except)));
        CollectionAssert.AreEqual(new[]{"a|y","a|x","a|x"},await Read("selected-parent-window",query.Skip(1).Result.Take(3).Result));
        var right=CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.CreateTree(Leaf(a),Node(child),CosmicSetQueryKind.Concat).Result;
        CollectionAssert.AreEqual(new[]{"a|x","a|y","a|x","a|y","a|x"},await Read("selected-right-tree",right));
        var foreign=Source(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,0);
        var foreignChild=CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.Create(foreign,foreign,CosmicSetQueryKind.Concat).Result;
        var foreignTree=CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.CreateTree(Node(foreignChild),Leaf(foreign),CosmicSetQueryKind.Concat).Result;
        Assert.AreEqual(0,(await Read("selected-foreign",foreignTree)).Length);
        var full=CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.Create(a,b,CosmicSetQueryKind.Concat).Result;
        var hidden=CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.CreateTree(Node(full),Leaf(c),CosmicSetQueryKind.Concat).Result.Take(0).Result;
        var excess=await Timed("selected-child-bound-before-empty",()=>manager.ExecuteProjectedSetQueryAsync(Plan(hidden,new CosmicProjectedSetOptions(new CosmicSetQueryOptions(2,2,1048576,10))),new[]{adapter},10));
        Assert.IsTrue(excess.IsError);Assert.AreEqual("CQL_QUERY_LIMIT",excess.ErrorCode);
        using var cancel=new CancellationTokenSource();cancel.Cancel();
        var stopped=await Timed("selected-cancel",()=>manager.ExecuteProjectedSetQueryAsync(Plan(query),new[]{adapter},10,cancellationToken:cancel.Token));
        Assert.IsTrue(stopped.IsError);Assert.AreEqual("CQL_QUERY_CANCELLED",stopped.ErrorCode);
        return new NativeFixtureJourneyProof(9,1,new Dictionary<string,bool>{["explicitTreeAdmission"]=true,["concatOccurrences"]=true,
            ["visibleTupleUnion"]=true,["visibleTupleIntersectExcept"]=true,["nodeWindows"]=true,["rightNested"]=true,
            ["foreignIsolation"]=true,["childBoundBeforeEmpty"]=true,["cancellation"]=true});
    }
}
