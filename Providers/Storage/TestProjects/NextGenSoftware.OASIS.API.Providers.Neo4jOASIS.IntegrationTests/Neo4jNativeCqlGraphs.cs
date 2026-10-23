using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private sealed class GraphFixture
    {
        internal Provider Provider=null!;
        internal Holon Root=null!;
        internal IDriver Driver=null!;
        internal bool Known;
        internal string Manifest=null!;
        internal Func<Task>? BeforeChild;
        internal Func<Task>? AfterRootRow;
        internal List<Guid> Ids=new();
        internal List<Guid> Operations=new();
        internal Task Record(string stage)=>File.WriteAllTextAsync(Manifest,JsonSerializer.Serialize(new{ids=Ids,operations=Operations,stage}));
        internal async Task Edit(string text,object parameters)
        {
            Known=false;
            await Record("before-owned-edit");
            using var session=Driver.AsyncSession();
            await(await session.RunAsync(text,parameters)).ConsumeAsync();
            Known=true;
        }
    }

    public class GraphNativeHook:DispatchProxy
    {
        private object target=null!;
        private Func<Task> before=null!;
        private Func<Task>? afterRootRow;
        public static T Wrap<T>(T value,Func<Task> action,Func<Task>? afterRoot=null) where T:class
        {
            var proxy=Create<T,GraphNativeHook>();
            ((GraphNativeHook)(object)proxy).target=value;
            ((GraphNativeHook)(object)proxy).before=action;
            ((GraphNativeHook)(object)proxy).afterRootRow=afterRoot;
            return proxy;
        }
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name=="FetchAsync" && target is IResultCursor)return FetchAfterRoot(method,args);
            if(method.Name=="RunAsync" && args?.FirstOrDefault() is string text && text.Contains("MATCH (parent:Holon)-[edge:HAS_CHILD]"))
                return ReadAfterHook(method,args);
            var result=method.Invoke(target,args);
            if(result is IAsyncSession session)return Wrap(session,before,afterRootRow);
            if(result is Task<IAsyncTransaction> transaction)return WrapTransaction(transaction);
            if(result is Task<IResultCursor> cursor && args?.FirstOrDefault() is string query && query.Contains("AS traversalCycle"))
                return WrapRootCursor(cursor);
            return result;
        }
        private async Task<IAsyncTransaction> WrapTransaction(Task<IAsyncTransaction> transaction)
            =>Wrap(await transaction,before,afterRootRow);
        private async Task<IResultCursor> WrapRootCursor(Task<IResultCursor> cursor)
            =>Wrap(await cursor,before,afterRootRow);
        private async Task<bool> FetchAfterRoot(MethodInfo method,object?[]? args)
        {
            var found=await (Task<bool>)method.Invoke(target,args)!;
            if(found && afterRootRow!=null)await afterRootRow();
            return found;
        }
        private async Task<IResultCursor> ReadAfterHook(MethodInfo method,object?[] args)
        {
            await before();
            return await (Task<IResultCursor>)method.Invoke(target,args)!;
        }
    }

    private static HolonManager GraphManager(Provider provider)
    {
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        return new HolonManager(provider,dna,registry);
    }

    private static async Task WithNativeGraph(Func<GraphFixture,Task> test,bool manifested=true,int width=2,bool largeChild=false,Action<Holon>? configure=null,Func<Holon>? createRoot=null)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var manifest=Path.Combine(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!,"cql-graph-fixture.json");
        Assert.IsFalse(File.Exists(manifest),"Reconcile an unknown graph fixture before reuse.");
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        var fixture=new GraphFixture{Driver=driver,Manifest=manifest,Root=createRoot?.Invoke()??new Holon{Id=Guid.NewGuid(),Name="owned CQL graph root"}};
        for(var index=0;index<width;index++)
            fixture.Root.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=fixture.Root.Id,
                Name="owned CQL graph child "+index,HolonType=index==0?HolonType.Quest:HolonType.Cafe,
                Description=largeChild&&index==0?new string('x',10000):"before"});
        if(width==2)
        {
            var child=fixture.Root.Children.First();
            child.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id,Name="owned CQL grandchild",HolonType=HolonType.Quest});
        }
        configure?.Invoke(fixture.Root);
        var nodes=new List<IHolon>{fixture.Root};
        for(var index=0;index<nodes.Count;index++)nodes.AddRange(nodes[index].AllChildren);
        fixture.Ids.AddRange(nodes.Select(node=>node.Id));
        var hooked=GraphNativeHook.Wrap(driver,async()=>
        {
            var action=fixture.BeforeChild;fixture.BeforeChild=null;
            if(action!=null)await action();
        },async()=>
        {
            var action=fixture.AfterRootRow;fixture.AfterRootRow=null;
            if(action!=null)await action();
        });
        fixture.Provider=new Provider(uri,"neo4j","oasis-local-test-only",()=>hooked){IsVersionControlEnabled=false};
        await File.WriteAllTextAsync(manifest,JsonSerializer.Serialize(new{ids=fixture.Ids,operations=fixture.Operations,stage="before-create"}));
        try
        {
            Assert.IsFalse((await fixture.Provider.ActivateProviderAsync()).IsError);
            if(manifested)
            {
                var seeded=await fixture.Provider.SaveHolonAsync(fixture.Root,saveChildren:true);
                Assert.IsFalse(seeded.IsError,seeded.Message+seeded.Exception);
            }
            else
                foreach(var node in nodes)
                {
                    var seeded=await fixture.Provider.SaveHolonAsync(node,saveChildren:false);
                    Assert.IsFalse(seeded.IsError,seeded.Message+seeded.Exception);
                }
            fixture.Known=true;
            await test(fixture);
        }
        finally
        {
            try
            {
              if(fixture.Known)
              {
                using var cleanup=driver.AsyncSession();
                await(await cleanup.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids=fixture.Ids.Select(id=>id.ToString()).ToArray()})).ConsumeAsync();
                await(await cleanup.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v",new{ids=fixture.Ids.Select(id=>id.ToString()).ToArray()})).ConsumeAsync();
                await(await cleanup.RunAsync("MATCH (r:CosmicWriteReceipt) WHERE r.OperationId IN $operations DETACH DELETE r",new{operations=fixture.Operations.Select(id=>id.ToString()).ToArray()})).ConsumeAsync();
                File.Delete(manifest);
              }
            }
            finally { await fixture.Provider.DeActivateProviderAsync(); }
        }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeAnyChildReparentingChecksCurrentParentAndNewOwnerMembership(bool declaredEmpty)
        =>WithNativeGraph(async fixture=>
        {
            var other=new Holon{Id=Guid.NewGuid(),Name="owned CQL second parent"};
            if(declaredEmpty)other.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(
                new HolonChildRoleManifest(1,other.Id,new[]{new HolonChildRole("Children",Array.Empty<Guid>())}));
            fixture.Ids.Add(other.Id);await fixture.Record("before-owned-second-parent");fixture.Known=false;
            var created=await fixture.Provider.SaveHolonAsync(other,saveChildren:false);
            fixture.Known=!created.IsError;Assert.IsFalse(created.IsError,created.Message+created.Exception);
            var childId=fixture.Root.Children.First().Id;
            var loaded=await fixture.Provider.LoadHolonAsync(childId,loadChildren:false);
            Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);
            loaded.Result.ParentHolonId=other.Id;
            await fixture.Record("before-owned-reparent");fixture.Known=false;
            var moved=await fixture.Provider.SaveHolonAsync(loaded.Result,saveChildren:false);
            fixture.Known=!moved.IsError;Assert.IsFalse(moved.IsError,moved.Message+moved.Exception);
            foreach(var parent in new[]{fixture.Root.Id,other.Id})
            {
                var id=parent;
                var query=CosmicQuery<Holon>.Create().Where(root=>root.Id==id)
                    .WhereAnyChild(child=>child.Name=="owned CQL graph child 0");
                Assert.IsFalse(query.IsError,query.Message);
                var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.Neo4jOASIS,
                    new[]{new CosmicQueryTarget(ProviderType.Neo4jOASIS,Provider.CqlCapabilities)});
                Assert.IsFalse(plan.IsError,plan.Message);
                var read=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(read.IsError,read.Message+read.Exception);
                Assert.AreEqual(parent==other.Id&&!declaredEmpty?1:0,read.Result.Count);
            }
            await global::OASIS.ProviderTests.Fixtures.CqlDescendantAcceptance.VerifyMovedSubtreeAsync(
                fixture.Provider,fixture.Root.Id,other.Id,declaredEmpty,"owned CQL grandchild");
        },configure:root=>root.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(
            new HolonChildRoleManifest(1,root.Id,new[]{new HolonChildRole("Children",root.Children.Select(child=>child.Id))})));

    [DataTestMethod]
    [DataRow(true,true)] [DataRow(false,true)]
    [DataRow(true,false)] [DataRow(false,false)]
    public Task NativeGraphAccessFiltersBeforeChildBudget(bool visible,bool manifested)
        =>WithNativeGraph(async fixture=>
        {
            var policy=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
            var plan=GraphPlan(fixture,CosmicQueryGraphOptions.Create(maximumNodes:visible?2:1).Result,policy);
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(plan);
            Assert.IsFalse(read.IsError,read.Message+read.Exception);
            var root=read.Result.Single();
            Assert.AreEqual(visible?1:0,root.Children.Count());
            Assert.IsTrue(root.Children.All(child=>child.IsPublic && !child.Children.Any()));
            if(manifested)Assert.AreEqual(2,Core.Serialization.HolonChildRoleManifestJson.Deserialize(((Holon)root).ChildRoleManifestJson).Roles.Single().ChildIds.Count);
        },manifested:manifested,configure:root=>{root.IsPublic=true;root.Children.First().IsPublic=visible;});

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeGraphHydratesNamedTypedRoles(bool accessBound)
        =>WithNativeGraph(fixture=>global::OASIS.ProviderTests.Fixtures.CqlCustomRoleGraph.VerifyAsync(
            GraphManager(fixture.Provider),fixture.Provider,fixture.Root,accessBound),
            configure:global::OASIS.ProviderTests.Fixtures.CqlCustomRoleGraph.Configure,
            createRoot:global::OASIS.ProviderTests.Fixtures.CqlCustomRoleGraph.CreateRoot);

    private static CosmicQuery<Holon> BindNativeGraphPolicy(CosmicQuery<Holon> query,CosmicQueryReadAccess policy)
    {
        var bound=query.WithReadAccess(policy);
        if(bound.IsError)throw new InvalidOperationException(bound.Message);
        return bound.Result;
    }

    private static CosmicQueryPlan<Holon> GraphPlan(GraphFixture fixture,CosmicQueryGraphOptions options,CosmicQueryReadAccess? access=null)
    {
        var id=fixture.Root.Id;
        var query=CosmicQuery<Holon>.Create().Where(row=>row.Id==id).WithChildren(options);
        Assert.IsFalse(query.IsError,query.Message);
        var plan=CosmicQueryPlanner.Plan(access==null ? query.Result : BindNativeGraphPolicy(query.Result,access),ProviderType.Neo4jOASIS,
            new[]{new CosmicQueryTarget(ProviderType.Neo4jOASIS,Provider.CqlCapabilities)});
        Assert.IsFalse(plan.IsError,plan.Message);return plan.Result;
    }

    [DataTestMethod]
    [DataRow(true,0,true)] [DataRow(true,1,false)] [DataRow(true,2,true)]
    [DataRow(false,0,false)] [DataRow(false,2,false)]
    public Task NativeGraphDepthAndRecursion(bool recursive,int depth,bool grandchildren)
        =>WithNativeGraph(async fixture=>
        {
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(GraphPlan(fixture,CosmicQueryGraphOptions.Create(recursive:recursive,maxChildDepth:depth).Result));
            Assert.IsFalse(read.IsError,read.Message+read.Exception);
            var root=read.Result.Single();
            CollectionAssert.AreEqual(fixture.Root.Children.Select(child=>child.Id).ToArray(),root.Children.Select(child=>child.Id).ToArray());
            Assert.AreEqual(grandchildren?1:0,root.Children.Single(child=>child.HolonType==HolonType.Quest).Children.Count());
        });

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public Task NativeGraphPartialSavePreservesUnobservedMembers(bool manifested,bool accessBound)
        =>WithNativeGraph(async fixture=>
        {
            var manager=GraphManager(fixture.Provider);
            ICosmicQueryStorageProvider adapter=fixture.Provider;
            var policy=accessBound?CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result:null;
            var shape=CosmicQueryGraphOptions.Create(maxChildDepth:1,childType:accessBound?HolonType.All:HolonType.Quest).Result;
            var baseline=await fixture.Provider.ExecuteCosmicQueryAsync(GraphPlan(fixture,CosmicQueryGraphOptions.Create().Result));
            if(baseline.IsError)throw new InvalidOperationException(baseline.Message);
            var childState=baseline.Result.Single().Children.ToDictionary(child=>child.Id,
                child=>(child.Version,CosmicConcurrency.GetProviderRevisionId(child,ProviderType.Neo4jOASIS)));
            var read=await manager.ExecuteQueryAsync(GraphPlan(fixture,shape,policy),new[]{adapter});
            Assert.IsFalse(read.IsError,read.Message+read.Exception);
            var root=read.Result.Providers.Single().Rows.Single();
            Assert.AreEqual(HolonType.Quest,root.Children.Single().HolonType);
            root.AcceptChanges();
            root.Name+=" edited";var version=root.Version;
            var rejected=await manager.SaveHolonAsync(root,Guid.NewGuid(),saveChildren:true,providerType:ProviderType.Neo4jOASIS);
            Assert.IsTrue(rejected.IsError);Assert.IsFalse(rejected.IsSaved);Assert.AreEqual(version,root.Version);
            var operation=Guid.NewGuid();fixture.Operations.Add(operation);
            await fixture.Record("before-root-only-save");
            fixture.Known=false;
            var saved=await manager.SaveHolonAsync(root,Guid.NewGuid(),new CosmicWriteOptions(operation),saveChildren:false,providerType:ProviderType.Neo4jOASIS);
            fixture.Known=!saved.IsError;
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            var fresh=await fixture.Provider.ExecuteCosmicQueryAsync(GraphPlan(fixture,CosmicQueryGraphOptions.Create().Result));
            Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);
            Assert.AreEqual(2,fresh.Result.Single().Children.Count());
            foreach(var child in fresh.Result.Single().Children)
                Assert.AreEqual(childState[child.Id],(child.Version,CosmicConcurrency.GetProviderRevisionId(child,ProviderType.Neo4jOASIS)));
            Assert.AreEqual(version+1,fresh.Result.Single().Version);
        },manifested:manifested,configure:root=>{root.IsPublic=true;root.Children.First().IsPublic=true;});

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeGraphBudgetsRejectWithoutPartialResults(bool payload)
        =>WithNativeGraph(async fixture=>
        {
            var options=CosmicQueryGraphOptions.Create(maximumNodes:payload?100:2,maximumPayloadBytes:payload?8000:32000).Result;
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(GraphPlan(fixture,options));
            Assert.AreEqual("CQL_QUERY_LIMIT",read.ErrorCode);Assert.IsNull(read.Result);Assert.IsFalse(read.IsLoaded);
        },largeChild:payload);

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)]
    public Task NativeAnyChildFiltersVisibilityAndExactMetadata(int visibility)
    {
        var actor=Guid.NewGuid();
        return WithNativeGraph(async fixture=>
        {
            var id=fixture.Root.Id;
            var query=CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
                .Where(root=>root.Id==id).WhereAnyChild(child=>child.Name=="owned CQL graph child 0" &&
                    CosmicQueryMetadata.Value<long>(child,"precision")==9007199254740993L)
                .WhereAnyChild(child=>child.HolonType==HolonType.Cafe);
            Assert.IsFalse(query.IsError,query.Message);
            var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.Neo4jOASIS,
                new[]{new CosmicQueryTarget(ProviderType.Neo4jOASIS,Provider.CqlCapabilities)});
            Assert.IsFalse(plan.IsError,plan.Message);
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(read.IsError,read.Message+read.Exception);
            Assert.AreEqual(visibility==0?0:1,read.Result.Count);
        },configure:root=>
        {
            root.IsPublic=true;root.Children.Last().IsPublic=true;var child=root.Children.First();
            child.IsPublic=visibility==1;child.CreatedByAvatarId=visibility==2?actor:Guid.NewGuid();
            child.MetaData["precision"]=9007199254740993L;
        });
    }

    [TestMethod]
    public Task NativeAnyChildDoesNotCountAnUnlistedPhysicalEdge()
        =>WithNativeGraph(async fixture=>
        {
            var orphan=new Holon{Id=Guid.NewGuid(),ParentHolonId=fixture.Root.Id,Name="unlisted child",IsPublic=true};
            fixture.Ids.Add(orphan.Id);await fixture.Record("before-owned-orphan");fixture.Known=false;
            var saved=await fixture.Provider.SaveHolonAsync(orphan,saveChildren:false);fixture.Known=!saved.IsError;
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            await fixture.Edit("MATCH (parent:Holon {Id:$parent}),(child:Holon {Id:$child}) MERGE (parent)-[:HAS_CHILD]->(child)",
                new{parent=fixture.Root.Id.ToString(),child=orphan.Id.ToString()});
            var id=fixture.Root.Id;
            var query=CosmicQuery<Holon>.Create().Where(root=>root.Id==id).WhereAnyChild(child=>child.Name=="unlisted child");
            var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.Neo4jOASIS,
                new[]{new CosmicQueryTarget(ProviderType.Neo4jOASIS,Provider.CqlCapabilities)});
            Assert.IsFalse(plan.IsError,plan.Message);
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(0,read.Result.Count);
        },configure:root=>root.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(
            new HolonChildRoleManifest(1,root.Id,new[]{new HolonChildRole("Children",root.Children.Select(child=>child.Id))})));

    [TestMethod]
    public Task NativeMembershipProjectionUpgradePreservesCanonicalSnapshot()
        =>WithNativeGraph(async fixture=>
        {
            using var inspection=fixture.Driver.AsyncSession();var id=fixture.Root.Id.ToString();
            var before=(await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot",new{id})).SingleAsync())["snapshot"].As<string>();
            await fixture.Edit("MATCH (h:Holon)-[p:HAS_CQL_PROJECTION]->(h) WHERE h.Id IN $ids SET p.SchemaVersion=2 REMOVE p.f_HasChildManifest,p.f_ChildIds",
                new{ids=fixture.Ids.Select(value=>value.ToString()).ToArray()});
            var rootId=fixture.Root.Id;
            var query=CosmicQuery<Holon>.Create().Where(root=>root.Id==rootId).WhereAnyChild(child=>child.HolonType==HolonType.Quest);
            Assert.IsFalse(query.IsError,query.Message);
            var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.Neo4jOASIS,
                new[]{new CosmicQueryTarget(ProviderType.Neo4jOASIS,Provider.CqlCapabilities)});
            Assert.IsFalse(plan.IsError,plan.Message);
            var unready=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsTrue(unready.IsError);Assert.AreEqual("CQL_DATA_INVALID",unready.ErrorCode);
            var upgrader=new Provider("bolt://127.0.0.1:17687","neo4j","oasis-local-test-only");
            try{var activated=await upgrader.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message+activated.Exception);}
            finally{await upgrader.DeActivateProviderAsync();}
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);Assert.IsFalse(read.IsError,read.Message+read.Exception);
            Assert.AreEqual(1,read.Result.Count);
            var after=(await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot",new{id})).SingleAsync())["snapshot"].As<string>();
            Assert.AreEqual(before,after);
        });

    [TestMethod]
    public Task NativeGraphRejectsACorruptSelfCycle()
        =>WithNativeGraph(async fixture=>
        {
            using var inspection=fixture.Driver.AsyncSession();
            var id=fixture.Root.Id.ToString("D");
            var row=await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot",new{id})).SingleAsync();
            var source=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStoredHolon(row["snapshot"].As<string>());
            source.ParentHolonId=source.Id;
            ((Holon)source).ChildRoleManifestJson=null;
            var snapshot=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(source);
            await fixture.Edit("MATCH (h:Holon {Id:$id})-[p:HAS_CQL_PROJECTION]->(h) SET h.ParentHolonId=$id,h.SnapshotJson=$snapshot,p.f_ParentHolonId=$id CREATE (h)-[:HAS_CHILD]->(h)",new{id,snapshot});
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(GraphPlan(fixture,CosmicQueryGraphOptions.Create().Result));
            Assert.IsTrue(read.IsError);Assert.AreEqual("CQL_DATA_INVALID",read.ErrorCode);
            Assert.IsNull(read.Result);Assert.IsFalse(read.IsLoaded);
        },manifested:false);

    [TestMethod]
    public Task NativeGraphBatchesLargeOrderedMembership()
        =>WithNativeGraph(async fixture=>
        {
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(GraphPlan(fixture,CosmicQueryGraphOptions.Create(maximumNodes:151).Result));
            Assert.IsFalse(read.IsError,read.Message+read.Exception);
            CollectionAssert.AreEqual(fixture.Root.Children.Select(child=>child.Id).ToArray(),read.Result.Single().Children.Select(child=>child.Id).ToArray());
        },width:150);

    [TestMethod]
    public Task NativeGraphPagesRootsBeforeExpanding()
        =>WithNativeGraph(async fixture=>
        {
            var other=new Holon{Id=Guid.NewGuid(),Name="zz owned CQL graph root"};fixture.Ids.Add(other.Id);
            await fixture.Record("before-second-root-save");
            fixture.Known=false;var saved=await fixture.Provider.SaveHolonAsync(other,saveChildren:false);fixture.Known=!saved.IsError;
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            var ids=new[]{fixture.Root.Id,other.Id};
            var query=CosmicQuery<Holon>.Create().Where(row=>ids.Contains(row.Id)).OrderBy(row=>row.Name).Take(1)
                .WithChildren(CosmicQueryGraphOptions.Create(maximumNodes:4).Result);
            var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.Neo4jOASIS,new[]{new CosmicQueryTarget(ProviderType.Neo4jOASIS,Provider.CqlCapabilities)});
            Assert.IsFalse(plan.IsError,plan.Message);
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(fixture.Root.Id,read.Result.Single().Id);
        });

    [TestMethod]
    public Task NativeGraphRejectsMissingPersistedEdges()
        =>WithNativeGraph(async fixture=>
        {
            await fixture.Edit("MATCH (p:Holon {Id:$id})-[edge:HAS_CHILD]->() DELETE edge",new{id=fixture.Root.Id.ToString()});
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(GraphPlan(fixture,CosmicQueryGraphOptions.Create().Result));
            Assert.AreEqual("CQL_DATA_INVALID",read.ErrorCode);Assert.IsNull(read.Result);
        });

    [TestMethod]
    public Task NativeGraphRejectsDuplicateEdges()
        =>WithNativeGraph(async fixture=>
        {
            await fixture.Edit("MATCH (p:Holon {Id:$id})-[:HAS_CHILD]->(c:Holon) CREATE (p)-[:HAS_CHILD]->(c)",new{id=fixture.Root.Id.ToString()});
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(GraphPlan(fixture,CosmicQueryGraphOptions.Create().Result));
            Assert.AreEqual("CQL_DATA_INVALID",read.ErrorCode);Assert.IsNull(read.Result);
        });

    [TestMethod]
    public Task NativeGraphUpgradesOlderTypeProjectionsWithoutChangingLogicalRecords()
        =>WithNativeGraph(async fixture=>
        {
            using var inspection=fixture.Driver.AsyncSession();
            var before=(await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot",
                new{id=fixture.Root.Id.ToString()})).SingleAsync())["snapshot"].As<string>();
            await fixture.Edit("MATCH (h:Holon)-[p:HAS_CQL_PROJECTION]->(h) WHERE h.Id IN $ids SET p.SchemaVersion=1 REMOVE p.f_HolonType",
                new{ids=fixture.Ids.Select(id=>id.ToString()).ToArray()});
            var rejected=await fixture.Provider.ExecuteCosmicQueryAsync(GraphPlan(fixture,CosmicQueryGraphOptions.Create().Result));
            Assert.AreEqual("CQL_DATA_INVALID",rejected.ErrorCode);
            var upgrader=new Provider("bolt://127.0.0.1:17687","neo4j","oasis-local-test-only");
            try {var activated=await upgrader.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message+activated.Exception);}
            finally {await upgrader.DeActivateProviderAsync();}
            var selected=await fixture.Provider.ExecuteCosmicQueryAsync(GraphPlan(fixture,
                CosmicQueryGraphOptions.Create(childType:HolonType.Quest,maxChildDepth:1).Result));
            Assert.IsFalse(selected.IsError,selected.Message+selected.Exception);
            Assert.AreEqual(HolonType.Quest,selected.Result.Single().Children.Single().HolonType);
            var after=(await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot",
                new{id=fixture.Root.Id.ToString()})).SingleAsync())["snapshot"].As<string>();
            Assert.AreEqual(before,after,"Projection migration must not create a new logical revision or mutate its stored record.");
        });

    [TestMethod]
    public Task NativeGraphCannotReplaceAnEmbeddedRevisionWithADifferentNativeToken()
        =>WithNativeGraph(async fixture=>
        {
            await fixture.Edit("MATCH (h:Holon {Id:$id})-[p:HAS_CQL_PROJECTION]->(h) SET h.ProviderRevisionId=$revision,p.ProviderRevisionId=$revision",
                new{id=fixture.Root.Id.ToString(),revision=Guid.NewGuid().ToString()});
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(GraphPlan(fixture,CosmicQueryGraphOptions.Create().Result));
            Assert.AreEqual("CQL_DATA_INVALID",read.ErrorCode);Assert.IsNull(read.Result);
        });

    [TestMethod]
    public Task NativeGraphUsesDocumentedReadCommittedSemanticsAcrossCommands()
        =>WithNativeGraph(async fixture=>
        {
            fixture.BeforeChild=async()=>
            {
                var child=fixture.Root.Children.First();child.Description="after";
                fixture.Known=false;var saved=await fixture.Provider.SaveHolonAsync(child,saveChildren:false);fixture.Known=!saved.IsError;
                Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            };
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(GraphPlan(fixture,CosmicQueryGraphOptions.Create().Result));
            Assert.IsFalse(read.IsError,read.Message+read.Exception);
            Assert.AreEqual("after",read.Result.Single().Children.First().Description);
        });

    [TestMethod]
    public Task NativeGraphCancellationBeforeChildrenPreventsPublication()
        =>WithNativeGraph(async fixture=>
        {
            using var cancellation=new CancellationTokenSource();
            fixture.BeforeChild=()=>{cancellation.Cancel();return Task.CompletedTask;};
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(GraphPlan(fixture,CosmicQueryGraphOptions.Create().Result),cancellationToken:cancellation.Token);
            Assert.AreEqual("CQL_QUERY_CANCELLED",read.ErrorCode);Assert.IsNull(read.Result);Assert.IsFalse(read.IsLoaded);
        });
}
