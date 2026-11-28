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
    public Task NativeMongoRecursiveSetsPreserveNodeWindowsAndInputProofs(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyNativeSetTree,
            nativeJourneySuffix:"-recursive-entity-set-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyNativeSetTree(IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;var sets=(ICosmicSetQueryStorageProvider)provider;
        Assert.IsTrue(sets.SetQueryCapabilities.SupportsNestedSets);
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {var clock=System.Diagnostics.Stopwatch.StartNew();try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}}
        var records=Enumerable.Range(0,4).Select(i=>new Holon{Id=Guid.NewGuid(),Name="tree-"+i,CreatedByAvatarId=actor,
            IsActive=true,IsPublic=false,IsNewHolon=true}).ToArray();var ids=records.Select(row=>row.Id).ToArray();owned.AddRange(ids);
        using(var writer=CosmicSession.Create(manager,adapter,access).Result)
        {
            foreach(var row in records)Assert.IsFalse(writer.Add(row).IsError);
            var save=await Timed("create-tree-fixtures",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsFalse(save.IsError,save.Message+save.Exception);Assert.IsTrue(save.IsSaved);
        }
        var scope=CosmicQueryScope.Named(adapter.QueryProviderType).Result;
        CosmicQuery<Holon> Source(CosmicQueryReadAccess policy)=>CosmicQuery<Holon>.Create(scope).WithReadAccess(policy).Result
            .Where(row=>ids.Contains(row.Id)).Result;
        var root=Source(access);
        var a=root.Where(row=>row.Name=="tree-0"||row.Name=="tree-1").Result.OrderBy(row=>row.Name).Result.Take(2).Result;
        var b=root.Where(row=>row.Name=="tree-1"||row.Name=="tree-2").Result.OrderBy(row=>row.Name).Result.Take(2).Result;
        var c=root.Where(row=>row.Name=="tree-2"||row.Name=="tree-3").Result.OrderBy(row=>row.Name).Result.Take(2).Result;
        CosmicSetInput<Holon> Leaf(CosmicQuery<Holon> source)=>CosmicSetInput<Holon>.FromSource(source).Result;
        CosmicSetInput<Holon> Node(CosmicSetQuery<Holon> set)=>CosmicSetInput<Holon>.FromSet(set).Result;
        var child=CosmicSetQuery<Holon>.Create(a,b,CosmicSetQueryKind.Concat).Result.Take(3).Result;
        CosmicSetQuery<Holon> Parent(CosmicSetQueryKind kind)=>CosmicSetQuery<Holon>.CreateTree(Node(child),Leaf(c),kind).Result;
        var intent=Parent(CosmicSetQueryKind.Concat);Assert.AreEqual(2,intent.TreeDepth);Assert.AreEqual(3,intent.LeafCount);
        var target=new CosmicQueryTarget(adapter.QueryProviderType,adapter.QueryCapabilities);
        var options=new CosmicSetQueryOptions(8,8,1048576,10);
        CosmicSetQueryPlan<Holon> Plan(CosmicSetQuery<Holon> query,CosmicSetQueryOptions? bounds=null)
        {var plan=CosmicSetQueryPlanner.Plan(query,adapter.QueryProviderType,target,sets.SetQueryCapabilities,bounds??options);Assert.IsFalse(plan.IsError,plan.Message+plan.Exception);return plan.Result;}
        var oldProfile=new CosmicSetQueryCapabilities(sets.SetQueryCapabilities.Features,sets.SetQueryCapabilities.ResultStages);
        Assert.IsFalse(oldProfile.SupportsNestedSets);
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",CosmicSetQueryPlanner.Plan(intent,adapter.QueryProviderType,target,oldProfile).ErrorCode);
        Assert.IsNotNull(typeof(CosmicSetQueryOptions).GetConstructor(new[]{typeof(int),typeof(int),typeof(int),typeof(int)}));
        Assert.AreEqual("CQL_SET_ACCESS_MISMATCH",CosmicSetQuery<Holon>.CreateTree(Node(child),Leaf(Source(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)),CosmicSetQueryKind.Concat).ErrorCode);
        var other=root.WithScope(CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result);
        Assert.AreEqual("CQL_SET_SCOPE_MISMATCH",CosmicSetQuery<Holon>.CreateTree(Node(child),Leaf(other),CosmicSetQueryKind.Union).ErrorCode);
        var modelBuilder=new CosmicModelBuilder();modelBuilder.Entity<Holon>();var model=modelBuilder.Build();Assert.IsFalse(model.IsError,model.Message);
        var mapped=CosmicQuery<Holon>.Create(scope).WithReadAccess(access).Result.WithModel(model.Result).Result;
        Assert.AreEqual("CQL_SET_MODEL_MISMATCH",CosmicSetQuery<Holon>.CreateTree(Node(child),Leaf(mapped),CosmicSetQueryKind.Union).ErrorCode);
        Assert.AreEqual("CQL_QUERY_LIMIT",CosmicSetQueryPlanner.Plan(intent.Take(0).Result,adapter.QueryProviderType,target,sets.SetQueryCapabilities,new CosmicSetQueryOptions(8,8,1048576,10,1)).ErrorCode);
        var depth=child;for(var i=depth.TreeDepth;i<CosmicSetQuery<Holon>.MaximumTreeDepth;i++)depth=CosmicSetQuery<Holon>.CreateTree(Node(depth),Leaf(a),CosmicSetQueryKind.Concat).Result;
        Assert.AreEqual("CQL_QUERY_LIMIT",CosmicSetQuery<Holon>.CreateTree(Node(depth),Leaf(a),CosmicSetQueryKind.Concat).ErrorCode);
        var width=child;while(width.LeafCount<CosmicSetQuery<Holon>.MaximumTreeLeaves)width=CosmicSetQuery<Holon>.CreateTree(Node(width),Node(width),CosmicSetQueryKind.Concat).Result;
        Assert.AreEqual("CQL_QUERY_LIMIT",CosmicSetQuery<Holon>.CreateTree(Node(width),Node(width),CosmicSetQueryKind.Concat).ErrorCode);
        var rows=await Timed("nested-concat-order",()=>manager.ExecuteTypedSetQueryAsync(Plan(intent),new[]{adapter},10));
        Assert.IsFalse(rows.IsError,rows.Message+rows.Exception);CollectionAssert.AreEqual(new[]{ids[0],ids[1],ids[1],ids[2],ids[3]},rows.Result.Providers[0].Rows.Select(row=>row.Id).ToArray());
        var count=await Timed("nested-count-no-publication-cap",()=>manager.ExecuteSetCountQueryAsync(Plan(intent,new CosmicSetQueryOptions(8,8,1048576,2)),new[]{adapter}));
        Assert.IsFalse(count.IsError,count.Message+count.Exception);Assert.AreEqual(5L,count.Result.Providers[0].Value);
        var window=await Timed("parent-window-after-child-window",()=>manager.ExecuteTypedSetQueryAsync(Plan(intent.Skip(1).Result.Take(3).Result),new[]{adapter},10));
        Assert.IsFalse(window.IsError,window.Message+window.Exception);CollectionAssert.AreEqual(new[]{ids[1],ids[1],ids[2]},window.Result.Providers[0].Rows.Select(row=>row.Id).ToArray());
        var union=await Timed("nested-union",()=>manager.ExecuteTypedSetQueryAsync(Plan(Parent(CosmicSetQueryKind.Union)),new[]{adapter},10));
        Assert.IsFalse(union.IsError,union.Message+union.Exception);CollectionAssert.AreEqual(ids,union.Result.Providers[0].Rows.Select(row=>row.Id).ToArray());
        var intersection=CosmicSetQuery<Holon>.CreateTree(Node(child),Leaf(b),CosmicSetQueryKind.Intersect).Result;
        var matched=await Timed("nested-intersect",()=>manager.ExecuteTypedSetQueryAsync(Plan(intersection),new[]{adapter},10));
        Assert.IsFalse(matched.IsError,matched.Message+matched.Exception);Assert.AreEqual(ids[1],matched.Result.Providers[0].Rows.Single().Id);
        var difference=CosmicSetQuery<Holon>.CreateTree(Node(child),Leaf(b),CosmicSetQueryKind.Except).Result;
        var excluded=await Timed("nested-except",()=>manager.ExecuteTypedSetQueryAsync(Plan(difference),new[]{adapter},10));
        Assert.IsFalse(excluded.IsError,excluded.Message+excluded.Exception);Assert.AreEqual(ids[0],excluded.Result.Providers[0].Rows.Single().Id);
        var right=CosmicSetQuery<Holon>.CreateTree(Leaf(a),Node(child),CosmicSetQueryKind.Concat).Result;
        var rightRows=await Timed("right-nested-operand",()=>manager.ExecuteTypedSetQueryAsync(Plan(right),new[]{adapter},10));
        Assert.IsFalse(rightRows.IsError,rightRows.Message+rightRows.Exception);CollectionAssert.AreEqual(new[]{ids[0],ids[1],ids[0],ids[1],ids[1]},rightRows.Result.Providers[0].Rows.Select(row=>row.Id).ToArray());
        var fullChild=CosmicSetQuery<Holon>.Create(a,b,CosmicSetQueryKind.Concat).Result;
        var hiddenOutput=CosmicSetQuery<Holon>.CreateTree(Node(fullChild),Leaf(c),CosmicSetQueryKind.Concat).Result.Take(0).Result;
        var excessOutput=await Timed("child-output-bound-before-empty",()=>manager.ExecuteTypedSetQueryAsync(Plan(hiddenOutput,new CosmicSetQueryOptions(2,2,1048576,10)),new[]{adapter},10));
        Assert.IsTrue(excessOutput.IsError);Assert.AreEqual("CQL_QUERY_LIMIT",excessOutput.ErrorCode);
        var badChild=CosmicSetQuery<Holon>.Create(root.OrderBy(row=>row.Name).Result,b,CosmicSetQueryKind.Concat).Result.Take(0).Result;
        var hiddenLeaf=CosmicSetQuery<Holon>.CreateTree(Node(badChild),Leaf(c),CosmicSetQueryKind.Concat).Result.Take(0).Result;
        var excessLeaf=await Timed("deep-leaf-bound-before-empty",()=>manager.ExecuteTypedSetQueryAsync(Plan(hiddenLeaf,new CosmicSetQueryOptions(2,2,1048576,10)),new[]{adapter},10));
        Assert.IsTrue(excessLeaf.IsError);Assert.AreEqual("CQL_QUERY_LIMIT",excessLeaf.ErrorCode);
        var foreign=Source(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).OrderBy(row=>row.Name).Result.Take(2).Result;
        var foreignChild=CosmicSetQuery<Holon>.Create(foreign,foreign,CosmicSetQueryKind.Concat).Result;
        var foreignTree=CosmicSetQuery<Holon>.CreateTree(Node(foreignChild),Leaf(foreign),CosmicSetQueryKind.Concat).Result;
        var denied=await Timed("foreign-tree-count",()=>manager.ExecuteSetCountQueryAsync(Plan(foreignTree),new[]{adapter}));
        Assert.IsFalse(denied.IsError,denied.Message+denied.Exception);Assert.AreEqual(0L,denied.Result.Providers[0].Value);
        using var cancel=new CancellationTokenSource();cancel.Cancel();
        var cancelled=await Timed("cancel-tree",()=>manager.ExecuteTypedSetQueryAsync(Plan(intent),new[]{adapter},10,cancel.Token));
        Assert.IsTrue(cancelled.IsError);Assert.AreEqual("CQL_QUERY_CANCELLED",cancelled.ErrorCode);
        return new NativeFixtureJourneyProof(10,1,new Dictionary<string,bool>{
            ["sharedActorAuthorityModelAdmission"]=true,["boundedTreeAndExplicitEnrollment"]=true,["nestedConcatOccurrenceOrder"]=true,
            ["perNodeWindowsPreserved"]=true,["allFourNativeIdentityOperators"]=true,["countIndependentOfPublication"]=true,
            ["rightNestedInput"]=true,["deepSourceAndChildOutputBoundsBeforeEmpty"]=true,["foreignPolicyPreserved"]=true,["cancellationPreserved"]=true});
    }
}
