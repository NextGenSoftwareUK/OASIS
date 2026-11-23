using System.Collections.ObjectModel;
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
    public sealed class FluentGraphNode:Holon
    {
        public int Score {get;set;}
        public List<FluentGraphNode> Components {get;set;}=new();
        public override ReadOnlyCollection<IHolon> AllChildren
            =>Children.Concat<IHolon>(Components).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()
            =>base.CaptureChildCollections().Concat(new[]{new HolonChildCollection(
                nameof(Components),Components,members=>Components=members.Cast<FluentGraphNode>().ToList())}).ToArray();
        // No local materialization factory: the configured model must own the codec.
        public override IReadOnlyList<HolonChildRoleBinding> GetChildRoleBindings()
            =>base.GetChildRoleBindings().Concat(new[]{HolonChildRoleBinding.Create<FluentGraphNode>(
                nameof(Components),members=>Components=members.ToList())}).ToArray();
    }

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeFluentModelsHydrateSelectedTypedGraphs(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyFluentGraphJourney,nativeJourneySuffix:"-fluent-graph-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyFluentGraphJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();
        var builder=new CosmicModelBuilder();
        builder.Entity<FluentGraphNode>().Property(row=>row.Score).HasMetadataKey("graph_score");
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        FluentGraphNode New(string name,int score)=>new(){Id=Guid.NewGuid(),Name=name,Score=score,
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var root=New("root",10);root.IsPublic=true;
        var child=New("child",20);var leaf=New("leaf",30);
        child.Components.Add(leaf);root.Components.Add(child);
        owned.AddRange(new[]{root.Id,child.Id,leaf.Id});
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var saved=await Timed("create-graph",()=>manager.SaveOwnedModelHolonsAsync(
            new[]{root},actor,new CosmicWriteOptions(Guid.NewGuid()),providerType:adapter.QueryProviderType,
            metadataModel:built.Result));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        var versions=new[]{root,child,leaf}.ToDictionary(row=>row.Id,row=>row.VersionId);
        var manifests=new[]{root,child,leaf}.ToDictionary(row=>row.Id,row=>row.ChildRoleManifestJson);
        using var owner=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(actor).Result,built.Result).Result;
        var graph=CosmicQueryGraphOptions.Create(maxChildDepth:2,maximumNodes:10).Result;
        var query=owner.Query<FluentGraphNode>().Result.Where(row=>row.Id==root.Id && row.Score==10).Result
            .WithChildren(graph);
        Assert.IsFalse(query.IsError,query.Message);
        var read=await Timed("read-graph",()=>owner.ReadAsync(query.Result,trackResults:false));
        Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(1,read.Result.Count);
        var loaded=read.Result.Single();Assert.AreEqual(10,loaded.Score);
        Assert.AreEqual(1,loaded.Components.Count);Assert.AreEqual(20,loaded.Components[0].Score);
        Assert.AreEqual(1,loaded.Components[0].Components.Count);
        Assert.AreEqual(30,loaded.Components[0].Components[0].Score);
        foreach(var row in new[]{loaded,loaded.Components[0],loaded.Components[0].Components[0]})
        {
            Assert.AreEqual(versions[row.Id],row.VersionId);
            Assert.AreEqual(manifests[row.Id],row.ChildRoleManifestJson);
            Assert.IsTrue(row.MetaData.ContainsKey("graph_score"));
        }
        using var foreign=CosmicSession.Create(manager,adapter,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,built.Result).Result;
        var publicQuery=foreign.Query<FluentGraphNode>().Result.Where(row=>row.Id==root.Id).Result.WithChildren(graph);
        Assert.IsFalse(publicQuery.IsError,publicQuery.Message);
        var visible=await Timed("foreign-selected-graph",()=>foreign.ReadAsync(publicQuery.Result,trackResults:false));
        Assert.IsFalse(visible.IsError,visible.Message+visible.Exception);Assert.AreEqual(1,visible.Result.Count);
        Assert.AreEqual(0,visible.Result.Single().Components.Count);
        Assert.AreEqual(manifests[root.Id],visible.Result.Single().ChildRoleManifestJson);
        return new NativeFixtureJourneyProof(5,1,new Dictionary<string,bool>{
            ["typedRootChildGrandchild"]=true,["modelCodecWithoutLocalFactory"]=true,
            ["identityRevisionAndManifestPreserved"]=true,["privateChildrenExcluded"]=true,
            ["selectedManifestRetained"]=true});
    }
}
