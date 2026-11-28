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
    [DataTestMethod] [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
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
        CosmicQuery<Holon> EntitySource(CosmicQueryReadAccess policy,int start)
        {
            var selected=ids.Skip(start).Take(2).ToArray();
            return CosmicQuery<Holon>.Create(scope).WithReadAccess(policy).Result.Where(row=>selected.Contains(row.Id)).Result
                .OrderBy(row=>row.Description).Result.ThenBy(row=>row.Id).Result.Take(2).Result;
        }
        CosmicProjectionQuery<Holon,CosmicProjectionRecord> Source(CosmicQueryReadAccess policy,int start)
            =>EntitySource(policy,start).SelectColumns(columns).Result;
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
        var la=CosmicLinq.Create(manager,adapter,EntitySource(access,0),maximumResults:8).Result.Select(row=>new{Name=row.Name,Category=row.Description});
        var lb=CosmicLinq.Create(manager,adapter,EntitySource(access,1),maximumResults:8).Result.Select(row=>new{Name=row.Name,Category=row.Description});
        var lc=CosmicLinq.Create(manager,adapter,EntitySource(access,2),maximumResults:8).Result.Select(row=>new{Name=row.Name,Category=row.Description});
        var lchild=la.Concat(lb).Take(3);
        var lunion=await Timed("linq-selected-left-tree",()=>lchild.Union(lc).ToCosmicListAsync());
        Assert.IsFalse(lunion.IsError,lunion.Message+lunion.Exception);
        CollectionAssert.AreEqual(new[]{"a|x","a|y","b|z"},lunion.Result.Select(row=>row.Name+"|"+row.Category).ToArray());
        var lright=await Timed("linq-selected-right-tree",()=>la.Concat(lchild).ToCosmicListAsync());
        Assert.IsFalse(lright.IsError,lright.Message+lright.Exception);
        CollectionAssert.AreEqual(new[]{"a|x","a|y","a|x","a|y","a|x"},lright.Result.Select(row=>row.Name+"|"+row.Category).ToArray());
        var nativeCount=await Timed("selected-native-count-beyond-publication",()=>manager.ExecuteProjectedSetCountQueryAsync(
            Plan(query,new CosmicProjectedSetOptions(new CosmicSetQueryOptions(8,8,1048576,2))),new[]{adapter}));
        Assert.IsFalse(nativeCount.IsError,nativeCount.Message+nativeCount.Exception);Assert.AreEqual(5L,nativeCount.Result.Providers.Single().Value);
        var pa=CosmicLinq.Create(manager,adapter,EntitySource(access,0),maximumResults:4).Result.Select(row=>new SelectedCountProbe{Name=row.Name,Category=row.Description});
        var pb=CosmicLinq.Create(manager,adapter,EntitySource(access,1),maximumResults:4).Result.Select(row=>new SelectedCountProbe{Name=row.Name,Category=row.Description});
        var pc=CosmicLinq.Create(manager,adapter,EntitySource(access,2),maximumResults:4).Result.Select(row=>new SelectedCountProbe{Name=row.Name,Category=row.Description});
        var probe=pa.Concat(pb).Take(3).Concat(pc);SelectedCountProbe.ConstructorCalls=0;
        var counted=await Timed("linq-selected-native-long-count",()=>probe.ToCosmicLongCountAsync());
        Assert.IsFalse(counted.IsError,counted.Message+counted.Exception);Assert.AreEqual(5L,counted.Result);
        var any=await Timed("linq-selected-any-after-window",()=>probe.Take(1).ToCosmicAnyAsync(row=>row.Name=="b"));
        Assert.IsFalse(any.IsError,any.Message+any.Exception);Assert.IsFalse(any.Result);
        var all=await Timed("linq-selected-native-all",()=>probe.ToCosmicAllAsync(row=>row.Name=="a"||row.Name=="b"));
        Assert.IsFalse(all.IsError,all.Message+all.Exception);Assert.IsTrue(all.Result);
        Assert.AreEqual(0,SelectedCountProbe.ConstructorCalls,"Native selected counts must not construct selected objects.");
        var foreignCount=await Timed("selected-foreign-native-count",()=>manager.ExecuteProjectedSetCountQueryAsync(Plan(foreignTree),new[]{adapter}));
        Assert.IsFalse(foreignCount.IsError,foreignCount.Message+foreignCount.Exception);Assert.AreEqual(0L,foreignCount.Result.Providers.Single().Value);
        var sa=CosmicLinq.Create(manager,adapter,EntitySource(access,0),maximumResults:4).Result.Select(row=>row.Description);
        var sb=CosmicLinq.Create(manager,adapter,EntitySource(access,1),maximumResults:4).Result.Select(row=>row.Description);
        var sc=CosmicLinq.Create(manager,adapter,EntitySource(access,2),maximumResults:4).Result.Select(row=>row.Description);
        var scalars=sa.Concat(sb).Take(3).Concat(sc);
        var minimum=await Timed("selected-min-after-window",()=>scalars.Skip(1).Take(1).ToCosmicMinAsync());
        Assert.IsFalse(minimum.IsError,minimum.Message+minimum.Exception);Assert.AreEqual("y",minimum.Result);
        var maximum=await Timed("selected-native-max",()=>scalars.ToCosmicMaxAsync());
        Assert.IsFalse(maximum.IsError,maximum.Message+maximum.Exception);Assert.AreEqual("z",maximum.Result);
        var bindings=new[]{new CosmicProjectionOutputBinding("Only","Category")};
        var rebound=child.WithSelectedOutput(child.Output.SelectOutputColumns(bindings).Result);Assert.IsFalse(rebound.IsError,rebound.Message);
        Assert.AreEqual("CQL_SET_OUTPUT_INVALID",child.WithSelectedOutput(c).ErrorCode);
        var narrowed=CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.CreateTree(Node(rebound.Result),Leaf(c.SelectOutputColumns(bindings).Result),CosmicSetQueryKind.Union);
        Assert.IsFalse(narrowed.IsError,narrowed.Message);
        var shaped=await Timed("nested-reselected-visible-tuple",()=>manager.ExecuteProjectedSetQueryAsync(Plan(narrowed.Result),new[]{adapter},10));
        Assert.IsFalse(shaped.IsError,shaped.Message+shaped.Exception);
        CollectionAssert.AreEqual(new[]{"x","y","z"},shaped.Result.Providers.Single().Rows.Select(row=>(string)row["Only"]).ToArray());
        var na=EntitySource(access,0).Select(row=>row.Version).Result;
        var nb=EntitySource(access,1).Select(row=>row.Version).Result;
        var nc=EntitySource(access,2).Select(row=>row.Version).Result;
        var ordinaryNumericPlan=CosmicProjectedNumericPlanner.Plan<Holon,int,int>(na,0,CosmicScalarOperation.Sum,
            adapter.QueryProviderType,new[]{target});
        Assert.IsFalse(ordinaryNumericPlan.IsError,ordinaryNumericPlan.Message);
        var ordinaryNumeric=await Timed("ordinary-selected-numeric-control",()=>CosmicProjectedNumericExecutor.ExecuteAsync(ordinaryNumericPlan.Result,new[]{adapter}));
        Assert.IsFalse(ordinaryNumeric.IsError,ordinaryNumeric.Message+ordinaryNumeric.Exception);
        Assert.AreEqual(2,ordinaryNumeric.Result.Providers.Single().Value);
        var numberChild=CosmicProjectedSetQuery<Holon,int>.Create(na,nb,CosmicSetQueryKind.Concat).Result.Take(3).Result;
        var numberTree=CosmicProjectedSetQuery<Holon,int>.CreateTree(
            CosmicProjectedSetInput<Holon,int>.FromSet(numberChild).Result,CosmicProjectedSetInput<Holon,int>.FromSource(nc).Result,CosmicSetQueryKind.Concat).Result;
        async Task<TValue> Numeric<TValue>(string name,CosmicProjectedSetQuery<Holon,int> source,CosmicScalarOperation operation)
        {
            var plan=CosmicProjectedSetNumericPlanner.Plan<Holon,int,TValue>(source,0,operation,adapter.QueryProviderType,target,
                native.ProjectedSetCapabilities,new CosmicProjectedSetOptions(new CosmicSetQueryOptions(8,8,1048576,1)));
            Assert.IsFalse(plan.IsError,plan.Message+plan.Exception);
            var result=await Timed(name,()=>manager.ExecuteProjectedSetNumericQueryAsync(plan.Result,new[]{adapter}));
            Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);
            return result.Result.Providers.Single().Value;
        }
        Assert.AreEqual(5,await Numeric<int>("native-set-sum-beyond-publication",numberTree,CosmicScalarOperation.Sum));
        Assert.AreEqual(1d,await Numeric<double>("native-set-average-after-windows",numberTree,CosmicScalarOperation.Average));
        var numberUnion=CosmicProjectedSetQuery<Holon,int>.Create(na,nc,CosmicSetQueryKind.Union).Result;
        Assert.AreEqual(1,await Numeric<int>("native-set-sum-visible-distinct",numberUnion,CosmicScalarOperation.Sum));
        var nf=EntitySource(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,0).Select(row=>row.Version).Result;
        var numberForeign=CosmicProjectedSetQuery<Holon,int>.Create(nf,nf,CosmicSetQueryKind.Concat).Result;
        Assert.AreEqual(0,await Numeric<int>("native-set-foreign-sum",numberForeign,CosmicScalarOperation.Sum));
        var nla=CosmicLinq.Create(manager,adapter,EntitySource(access,0),maximumResults:4).Result.Select(row=>row.Version);
        var nlb=CosmicLinq.Create(manager,adapter,EntitySource(access,1),maximumResults:4).Result.Select(row=>row.Version);
        var nlc=CosmicLinq.Create(manager,adapter,EntitySource(access,2),maximumResults:4).Result.Select(row=>row.Version);
        var nativeNumbers=nla.Concat(nlb).Take(3).Concat(nlc);
        var lsum=await Timed("linq-selected-native-sum",()=>nativeNumbers.ToCosmicSumAsync());
        Assert.IsFalse(lsum.IsError,lsum.Message+lsum.Exception);Assert.AreEqual(5,lsum.Result);
        var lavg=await Timed("linq-selected-native-average",()=>nativeNumbers.ToCosmicAverageAsync());
        Assert.IsFalse(lavg.IsError,lavg.Message+lavg.Exception);Assert.AreEqual(1d,lavg.Result);
        SelectedFinalProbe.ConstructorCalls=0;SelectedCountProbe.ConstructorCalls=0;
        var typedView=probe.Select(row=>new SelectedFinalProbe{Label=row.Category}).Where(row=>row.Label=="x")
            .Distinct().OrderBy(row=>row.Label).Select(row=>new{Value=row.Label});
        var typedResult=await Timed("native-typed-final-shape",()=>typedView.ToCosmicListAsync());
        Assert.IsFalse(typedResult.IsError,typedResult.Message+typedResult.Exception);
        CollectionAssert.AreEqual(new[]{"x"},typedResult.Result.Select(row=>row.Value).ToArray());
        var typedCount=await Timed("typed-view-native-count",()=>typedView.ToCosmicCountAsync());
        Assert.IsFalse(typedCount.IsError,typedCount.Message+typedCount.Exception);Assert.AreEqual(1,typedCount.Result);
        var typedMin=await Timed("typed-view-native-min",()=>typedView.ToCosmicMinAsync(row=>row.Value));
        Assert.IsFalse(typedMin.IsError,typedMin.Message+typedMin.Exception);Assert.AreEqual("x",typedMin.Result);
        var typedNumberView=nativeNumbers.Select(number=>new{Number=number}).Where(row=>row.Number>0);
        var typedSum=await Timed("typed-view-native-sum",()=>typedNumberView.ToCosmicSumAsync(row=>row.Number));
        Assert.IsFalse(typedSum.IsError,typedSum.Message+typedSum.Exception);Assert.AreEqual(5,typedSum.Result);
        var typedRight=lc.Select(row=>new SelectedFinalProbe{Label=row.Category}).Select(row=>new{Value=row.Label});
        var recombined=typedView.Union(typedRight).OrderBy(row=>row.Value);
        var combinedTypedRows=await Timed("typed-view-recombined-native-union",()=>recombined.ToCosmicListAsync());
        Assert.IsFalse(combinedTypedRows.IsError,combinedTypedRows.Message+combinedTypedRows.Exception);
        CollectionAssert.AreEqual(new[]{"x","z"},combinedTypedRows.Result.Select(row=>row.Value).ToArray());
        var typedLast=await Timed("typed-view-native-last",()=>recombined.ToCosmicLastAsync());
        Assert.IsFalse(typedLast.IsError,typedLast.Message+typedLast.Exception);Assert.AreEqual("z",typedLast.Result.Value);
        Assert.AreEqual(0,SelectedCountProbe.ConstructorCalls,"Do not construct the original set DTO.");
        Assert.AreEqual(0,SelectedFinalProbe.ConstructorCalls,"Do not construct an intermediate final shape that was reselected.");
        var entitySelected=CosmicLinq.Create(manager,adapter,EntitySource(access,0),maximumResults:8).Result
            .Where(row=>row.Description=="y").Select(row=>new{Name=row.Name,Category=row.Description}).OrderBy(row=>row.Category);
        var entityOther=CosmicLinq.Create(manager,adapter,EntitySource(access,2),maximumResults:8).Result
            .Where(row=>row.Name=="b").Select(row=>new{Name=row.Name,Category=row.Description}).OrderBy(row=>row.Category);
        var entityControl=await Timed("ordinary-entity-selected-control",()=>entitySelected.ToCosmicListAsync());
        Assert.IsFalse(entityControl.IsError,entityControl.Message+entityControl.Exception);
        CollectionAssert.AreEqual(new[]{"a|y"},entityControl.Result.Select(row=>row.Name+"|"+row.Category).ToArray());
        var entityValues=await Timed("entity-selections-feed-native-sets",()=>entitySelected.Concat(entityOther).Concat(lc).ToCosmicListAsync());
        Assert.IsFalse(entityValues.IsError,entityValues.Message+entityValues.Exception);
        CollectionAssert.AreEqual(new[]{"a|y","b|z","a|x","b|z"},entityValues.Result.Select(row=>row.Name+"|"+row.Category).ToArray());
        var capturedEntity=EntitySource(access,0).Compose().Result.Where(row=>row.Description=="y").Result.SelectColumns(columns).Result;
        var entityInput=CosmicProjectedSetInput<Holon,CosmicProjectionRecord>.FromEntityProjection(capturedEntity).Result;
        var capturedEntitySet=CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.CreateTree(entityInput,Leaf(c),CosmicSetQueryKind.Union).Result;
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",CosmicProjectedSetPlanner.Plan(capturedEntitySet,adapter.QueryProviderType,target,flat).ErrorCode);
        Assert.AreEqual("CQL_QUERY_LIMIT",CosmicProjectedSetPlanner.Plan(capturedEntitySet,adapter.QueryProviderType,target,native.ProjectedSetCapabilities,
            new CosmicProjectedSetOptions(new CosmicSetQueryOptions(8,8,1048576,10),maximumStageRows:1)).ErrorCode);
        var foreignEntity=EntitySource(foreign.Source.ReadAccess,0).Compose().Result.Where(row=>row.Description=="y").Result.SelectColumns(columns).Result;
        var hiddenEntitySet=CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.CreateTree(
            CosmicProjectedSetInput<Holon,CosmicProjectionRecord>.FromEntityProjection(foreignEntity).Result,Leaf(foreign),CosmicSetQueryKind.Union).Result;
        Assert.AreEqual(0,(await Read("entity-selected-foreign",hiddenEntitySet)).Length);
        var unbounded=CosmicQuery<Holon>.Create(scope).WithReadAccess(access).Result.Where(row=>ids.Contains(row.Id)).Result;
        var guardedEntity=unbounded.Compose().Result.Where(row=>row.Name=="b").Result.SelectColumns(columns).Result;
        var guardedSet=CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.CreateTree(
            CosmicProjectedSetInput<Holon,CosmicProjectionRecord>.FromEntityProjection(guardedEntity).Result,Leaf(c),CosmicSetQueryKind.Concat).Result.Take(0).Result;
        var guardedPlan=Plan(guardedSet,new CosmicProjectedSetOptions(new CosmicSetQueryOptions(1,8,1048576,10)));
        var guardedRead=await Timed("entity-source-bound-before-empty-set",()=>manager.ExecuteProjectedSetQueryAsync(guardedPlan,new[]{adapter},10));
        Assert.IsTrue(guardedRead.IsError,"A late empty selected set must not hide an oversized original entity input.");
        var identityEnrolled=adapter.QueryProviderType is ProviderType.MongoDBOASIS or ProviderType.SQLLiteDBOASIS;
        if(identityEnrolled)
        {
            var identityA=CosmicLinq.Create(manager,adapter,EntitySource(access,0),maximumResults:8).Result;
            var identityC=CosmicLinq.Create(manager,adapter,EntitySource(access,2),maximumResults:8).Result;
            var lateA=identityA.Concat(identityA).Where(row=>row.Description=="x")
                .Select(row=>new{Name=row.Name,Category=row.Description}).Skip(1);
            var lateC=identityC.Union(identityC).OrderBy(row=>row.Description)
                .Select(row=>new{Name=row.Name,Category=row.Description}).Where(row=>row.Name=="b");
            var lateControl=await Timed("identity-selected-ordinary-control",()=>lateA.ToCosmicListAsync());
            Assert.IsFalse(lateControl.IsError,lateControl.Message+lateControl.Exception);
            CollectionAssert.AreEqual(new[]{"a|x"},lateControl.Result.Select(row=>row.Name+"|"+row.Category).ToArray());
            var lateRows=await Timed("identity-selected-feed-native-sets",()=>lateA.Concat(lateC).Concat(lc).ToCosmicListAsync());
            Assert.IsFalse(lateRows.IsError,lateRows.Message+lateRows.Exception);
            CollectionAssert.AreEqual(new[]{"a|x","b|z","a|x","b|z"},lateRows.Result.Select(row=>row.Name+"|"+row.Category).ToArray());
            var hiddenBase=CosmicLinq.Create(manager,adapter,EntitySource(foreign.Source.ReadAccess,0),maximumResults:8).Result;
            var hiddenLate=hiddenBase.Concat(hiddenBase).Select(row=>new{Name=row.Name,Category=row.Description});
            var hiddenPlain=hiddenBase.Select(row=>new{Name=row.Name,Category=row.Description});
            var hiddenRows=await Timed("identity-selected-foreign",()=>hiddenLate.Concat(hiddenPlain).ToCosmicListAsync());
            Assert.IsFalse(hiddenRows.IsError,hiddenRows.Message+hiddenRows.Exception);Assert.AreEqual(0,hiddenRows.Result.Count);
            var oversized=CosmicLinq.Create(manager,adapter,unbounded,maximumResults:1).Result;
            var lateEmpty=oversized.Concat(oversized).Select(row=>new{Name=row.Name,Category=row.Description}).Concat(lc).Take(0);
            var refused=await Timed("identity-source-bound-before-empty",()=>lateEmpty.ToCosmicListAsync());
            Assert.IsTrue(refused.IsError,"An empty outer result cannot hide oversized original identity-set inputs.");
        }
        using var cancel=new CancellationTokenSource();cancel.Cancel();
        var stopped=await Timed("selected-cancel",()=>manager.ExecuteProjectedSetQueryAsync(Plan(query),new[]{adapter},10,cancellationToken:cancel.Token));
        Assert.IsTrue(stopped.IsError);Assert.AreEqual("CQL_QUERY_CANCELLED",stopped.ErrorCode);
        var journeyEvidence=new Dictionary<string,bool>{["nativeEntityStageSelectedOperands"]=true,["typedViewsFeedFurtherNativeSets"]=true,["typedViewNativeElements"]=true,["typedViewCardinalityAndExtrema"]=true,["typedViewNumericTerminal"]=true,["typedFinalSelectionWithoutIntermediateConstructors"]=true,["registeredLinqNativeSumAverage"]=true,["sharedOrdinaryNumericDecoderControl"]=true,["nativeSumAverageAfterSetWindows"]=true,["numericVisibleEqualityAndForeignScope"]=true,["nestedVisibleOutputBindings"]=true,["nativeScalarExtremaAfterWindows"]=true,["nativeCountIndependentOfPublication"]=true,["nativeCardinalityWithoutConstructors"]=true,["foreignNativeCount"]=true,["registeredLinqLeftTree"]=true,["registeredLinqRightTree"]=true,["explicitTreeAdmission"]=true,["concatOccurrences"]=true,
            ["visibleTupleUnion"]=true,["visibleTupleIntersectExcept"]=true,["nodeWindows"]=true,["rightNested"]=true,
            ["foreignIsolation"]=true,["childBoundBeforeEmpty"]=true,["cancellation"]=true};
        if(identityEnrolled)journeyEvidence.Add("nativeIdentitySelectedOperands",true);
        return new NativeFixtureJourneyProof(identityEnrolled?27:26,1,journeyEvidence);
    }
    public sealed class SelectedFinalProbe
    {
        public static int ConstructorCalls;
        public string Label {get;set;}="";
        public SelectedFinalProbe(){ConstructorCalls++;}
    }
    public sealed class SelectedCountProbe
    {
        public static int ConstructorCalls;
        public string Name {get;set;}="";
        public string Category {get;set;}="";
        public SelectedCountProbe(){ConstructorCalls++;}
    }

}
