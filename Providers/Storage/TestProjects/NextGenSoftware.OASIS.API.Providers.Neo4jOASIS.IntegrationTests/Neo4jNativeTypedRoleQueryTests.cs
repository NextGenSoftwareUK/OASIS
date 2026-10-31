using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public partial class Neo4jNativeAvatarTests
{
    public sealed class NativeRoleMember:Holon { }
    public sealed class NativeRoleOwner:Holon
    {
        public List<NativeRoleMember> Components {get;set;}=new();
        public override ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Components).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection(nameof(Components),Components,members=>Components=members.Cast<NativeRoleMember>().ToList())}).ToArray();
        public override IReadOnlyList<HolonChildRoleBinding> GetChildRoleBindings()=>base.GetChildRoleBindings()
            .Concat(new[]{HolonChildRoleBinding.Create<NativeRoleMember>(nameof(Components),members=>Components=members.ToList(),
                source=>Core.Helpers.Mapper<IHolon,NativeRoleMember>.MapBaseHolonProperties(source))}).ToArray();
    }

    [DataTestMethod]
    [DataRow(false,"all")] [DataRow(true,"all")]
    [DataRow(false,"search")] [DataRow(true,"search")]
    [DataRow(false,"metadata")] [DataRow(true,"metadata")]
    [DataRow(false,"pairs")] [DataRow(true,"pairs")]
    [DataRow(false,"parent")] [DataRow(true,"parent")]
    [DataRow(false,"parent-key")] [DataRow(true,"parent-key")]
    public async Task NativeCosmicTypedQueryRolesAndRootOnlyReceipts(bool synchronous,string route)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var provider=new Provider(uri,"neo4j","oasis-local-test-only");
        var other=new Provider(uri,"neo4j","oasis-local-test-only");
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspection=driver.AsyncSession();
        var ids=new List<string>();var operations=new List<string>();var complete=false;
        Guid Id(){var value=Guid.NewGuid();ids.Add(value.ToString());return value;}
        Guid Op(){var value=Guid.NewGuid();operations.Add(value.ToString());return value;}
        async Task<string> Snapshot(Guid id)=>(await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS json",new{id=id.ToString()})).SingleAsync())["json"].As<string>();
        try
        {
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        Assert.IsFalse((await other.ActivateProviderAsync()).IsError);
        var writer=TypedRoleManager(provider);var reader=TypedRoleManager(other);var actor=Guid.NewGuid();
        var parent=new Holon{Id=Id(),Name="query parent"};
        var parentKey=parent.Id.ToString();
        parent.ProviderUniqueStorageKey[ProviderType.Neo4jOASIS]=parentKey;
        var root=new NativeRoleOwner{Id=Id(),Name="typed query owner",HolonType=HolonType.Quest};
        root.MetaData["typed-kind"]="query-owner";
        root.Components.Add(new NativeRoleMember{Id=Id(),Name="query member",HolonType=HolonType.Holon});
        parent.Children.Add(root);
        var saved=await writer.SaveHolonAsync(parent,actor,new CosmicWriteOptions(Op(),ids.ToDictionary(id=>Guid.Parse(id),id=>(Guid?)null)),providerType:ProviderType.Neo4jOASIS);
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
        var originalVersion=root.VersionId;
        var pairs=new Dictionary<string,string>{{"typed-kind","query-owner"}};
        var loaded=route switch
        {
            "search"=>synchronous ? reader.SearchHolons<NativeRoleOwner>("typed query owner",actor,searchOnlyForCurrentAvatar:false,holonType:HolonType.Quest,providerType:ProviderType.Neo4jOASIS)
                :await reader.SearchHolonsAsync<NativeRoleOwner>("typed query owner",actor,searchOnlyForCurrentAvatar:false,holonType:HolonType.Quest,providerType:ProviderType.Neo4jOASIS),
            "all"=>synchronous ? reader.LoadAllHolons<NativeRoleOwner>(holonType:HolonType.Quest,cache:false,providerType:ProviderType.Neo4jOASIS)
                :await reader.LoadAllHolonsAsync<NativeRoleOwner>(holonType:HolonType.Quest,providerType:ProviderType.Neo4jOASIS),
            "metadata"=>synchronous ? reader.LoadHolonsByMetaData<NativeRoleOwner>("typed-kind","query-owner",providerType:ProviderType.Neo4jOASIS)
                :await reader.LoadHolonsByMetaDataAsync<NativeRoleOwner>("typed-kind","query-owner",providerType:ProviderType.Neo4jOASIS),
            "pairs"=>synchronous ? reader.LoadHolonsByMetaData<NativeRoleOwner>(pairs,MetaKeyValuePairMatchMode.All,providerType:ProviderType.Neo4jOASIS)
                :await reader.LoadHolonsByMetaDataAsync<NativeRoleOwner>(pairs,MetaKeyValuePairMatchMode.All,providerType:ProviderType.Neo4jOASIS),
            "parent"=>synchronous ? reader.LoadHolonsForParent<NativeRoleOwner>(parent.Id,providerType:ProviderType.Neo4jOASIS)
                :await reader.LoadHolonsForParentAsync<NativeRoleOwner>(parent.Id,providerType:ProviderType.Neo4jOASIS),
            _=>synchronous ? reader.LoadHolonsForParent<NativeRoleOwner>(parentKey,providerType:ProviderType.Neo4jOASIS)
                :await reader.LoadHolonsForParentAsync<NativeRoleOwner>(parentKey,providerType:ProviderType.Neo4jOASIS)
        };
        Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);
        var result=Only(loaded.Result);
        Assert.AreEqual(root.Id,result.Id);Empty(result.Children);Assert.AreEqual(originalVersion,result.VersionId);
        Assert.AreEqual("query member",Only(result.Components).Name);
        Assert.IsFalse(result.HasHolonChanged());
        result.Components[0].Name="query member edited";
        var resaved=await reader.SaveHolonAsync(result,actor,new CosmicWriteOptions(Op()),providerType:ProviderType.Neo4jOASIS);
        Assert.IsFalse(resaved.IsError,resaved.Message+resaved.Exception);
        Assert.AreEqual(originalVersion,result.PreviousVersionId);
        var independentlyLoaded=await writer.LoadHolonAsync<NativeRoleOwner>(root.Id,providerType:ProviderType.Neo4jOASIS);
        Assert.IsFalse(independentlyLoaded.IsError,independentlyLoaded.Message+independentlyLoaded.Exception);
        Assert.AreEqual("query member edited",Only(independentlyLoaded.Result.Components).Name);
        var component=Only(independentlyLoaded.Result.Components);
        var childBefore=await Snapshot(component.Id);
        var observedManifest=independentlyLoaded.Result.ChildRoleManifestJson;
        Assert.IsNotNull(observedManifest);
        // Direct root-only input deliberately has no role observation or loaded members.
        var scalarOnly=new Holon {Id=root.Id,ParentHolonId=parent.Id,Name="scalar-only owner"};
        var condition=new ConditionalHolonWrite(Op(),actor,ProviderType.Neo4jOASIS,
            new[]{new HolonWriteCondition(root.Id,CosmicConcurrency.GetProviderRevisionId(independentlyLoaded.Result,ProviderType.Neo4jOASIS))});
        var request=new ConditionalHolonWriteRequest(condition,ConditionalHolonOperation.Save,new[]{scalarOnly},saveChildren:false);
        var partial=synchronous ? other.ExecuteConditionalHolonWrite(request):await other.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsFalse(partial.IsError,partial.Message+partial.Exception);
        var partialRoot=AsHolon(Only(partial.Result));
        Empty(partialRoot.Children);Assert.AreEqual(observedManifest,partialRoot.ChildRoleManifestJson);
        var replay=synchronous ? provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);
        var replayRoot=AsHolon(Only(replay.Result));
        Assert.AreEqual(partialRoot.VersionId,replayRoot.VersionId);Assert.AreEqual(observedManifest,replayRoot.ChildRoleManifestJson);
        Assert.AreEqual(childBefore,await Snapshot(component.Id));
        var afterPartial=await writer.LoadHolonAsync<NativeRoleOwner>(root.Id,providerType:ProviderType.Neo4jOASIS);
        Assert.IsFalse(afterPartial.IsError,afterPartial.Message+afterPartial.Exception);
        Assert.AreEqual(observedManifest,afterPartial.Result.ChildRoleManifestJson);
        Assert.AreEqual("query member edited",Only(afterPartial.Result.Components).Name);
        if(route=="search")
        {
            var rootBeforeSearch=await Snapshot(root.Id);
            foreach(var nativeChildren in new[]{false,true})
            foreach(var childType in new[]{HolonType.Holon,HolonType.Quest})
            {
                var selected=synchronous ? reader.SearchHolons("scalar-only owner",actor,searchOnlyForCurrentAvatar:false,
                        maxChildDepth:1,loadChildrenFromProvider:nativeChildren,childHolonType:childType,providerType:ProviderType.Neo4jOASIS)
                    :await reader.SearchHolonsAsync("scalar-only owner",actor,searchOnlyForCurrentAvatar:false,
                        maxChildDepth:1,loadChildrenFromProvider:nativeChildren,childHolonType:childType,providerType:ProviderType.Neo4jOASIS);
                Assert.IsFalse(selected.IsError,selected.Message+selected.Exception);var selectedRoot=Only(selected.Result);
                Assert.AreEqual(root.Id,selectedRoot.Id);
                if(childType==HolonType.Quest)Empty(selectedRoot.Children);
                else Assert.AreEqual(component.Id,Only(selectedRoot.Children).Id);
                Assert.IsFalse(selectedRoot.HasHolonChanged());
            }
            Assert.AreEqual(rootBeforeSearch,await Snapshot(root.Id));Assert.AreEqual(childBefore,await Snapshot(component.Id));
        }
        complete=true;
        }
        finally
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{ids,operations,complete}));
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v",new{ids})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt) WHERE r.OperationId IN $operations DETACH DELETE r",new{operations})).ConsumeAsync();
            }
            await provider.DeActivateProviderAsync();await other.DeActivateProviderAsync();
        }
    }

    private static T Only<T>(IEnumerable<T> values){var rows=values.ToList();Assert.AreEqual(1,rows.Count);return rows[0];}
    private static void Empty<T>(IEnumerable<T> values)=>Assert.AreEqual(0,values.Count());
    private static Holon AsHolon(IHolon value){Assert.IsInstanceOfType<Holon>(value);return (Holon)value;}
    private static HolonManager TypedRoleManager(Provider provider)
    {
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings {LogSwitchingProviders=false}}};
        dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var providers=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        providers.RegisterProvider(provider);return new HolonManager(null,dna,providers);
    }
}
