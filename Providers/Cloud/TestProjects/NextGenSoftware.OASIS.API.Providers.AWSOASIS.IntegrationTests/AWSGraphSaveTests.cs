using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task V2PartialGraphCommitFencesReplayReplicationAndDescendants(bool synchronous)
    {
        var path=Path.GetFullPath("TestResults/AWSRoutingFiles/delete-routing.db");
        if(File.Exists(path)) Assert.Fail("Retained SQLite routing evidence exists; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var healthy=Client;var verified=false;
        using var fault=new LostPutAcknowledgementClient {Healthy=healthy,FailOnCommit=2};
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoReplicationEnabled=true,IsAutoFailOverEnabled=true};
            manager.RegisterProvider(_provider);manager.RegisterProvider(sqlite);
            var types=new[] {new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.AWSOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(types).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(types).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            var root=new Holon {Name="owned-partial-root",IsActive=true};
            var child=new Holon {Name="owned-partial-child",IsActive=true};
            var leaf=new Holon {Name="owned-unsubmitted-leaf",IsActive=true};
            root.Children=new List<IHolon> {child};child.Children=new List<IHolon> {leaf};
            var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            ClientField.SetValue(_provider,fault);
            NextGenSoftware.OASIS.Common.OASISResult<IHolon> result;
            try
            {
                var request=new Core.StorageOperationRequest {Operation="SaveHolon",Payload=root,SaveChildren=true,Recursive=true,PreferredProvider=Core.Enums.ProviderType.AWSOASIS};
                result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
            }
            finally {ClientField.SetValue(_provider,healthy);}
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(2,fault.Commits);
            Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            Assert.IsInstanceOfType(result.Exception,typeof(DynamoDBOASIS.DynamoDBPartialMutationException));
            var partial=(DynamoDBOASIS.DynamoDBPartialMutationException)result.Exception;
            Assert.AreEqual(root.Id,partial.AcknowledgedRecordIds.Single());Assert.AreNotEqual(Guid.Empty,root.Id);
            var pending=(DynamoDBOASIS.DynamoDBMutationReconciliationException)partial.InnerException;
            Assert.AreSame(fault.Cause,pending.InnerException);Assert.IsTrue(Guid.TryParse(pending.RecordKey,out var childId));
            Assert.AreEqual(Guid.Empty,child.Id);Assert.AreEqual(Guid.Empty,leaf.Id);
            foreach(var id in new[] {root.Id,childId})
            {
                var native=await healthy.GetItemAsync(new GetItemRequest {TableName="OasisHolons",Key=Key(id),ConsistentRead=true});
                Assert.IsTrue(native.Item.Count>0);
                Assert.IsNull((await sqlite.LoadHolonAsync(id,loadChildren:false)).Result);
            }
            var childRead=await healthy.GetItemAsync(new GetItemRequest {TableName="OasisHolons",Key=Key(childId),ConsistentRead=true});
            Assert.AreEqual(root.Id.ToString(),childRead.Item["parentHolonId"].S);
            var descendants=await _provider.LoadHolonsForParentAsync(childId,loadChildren:false);
            Assert.IsFalse(descendants.IsError,descendants.Message);Assert.AreEqual(0,descendants.Result.Count());
            foreach(var id in new[] {root.Id,childId}) await healthy.DeleteItemAsync(new DeleteItemRequest {TableName="OasisHolons",Key=Key(id)});
            verified=true;
        }
        finally
        {
            ClientField.SetValue(_provider,healthy);await sqlite.DeActivateProviderAsync();
            if(verified) foreach(var file in new[] {path,path+"-wal",path+"-shm"}) if(File.Exists(file)) File.Delete(file);
        }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task V2ReplicatesSelectedGraphWithGeneratedIdentities(bool synchronous)
    {
        var path=Path.GetFullPath("TestResults/AWSRoutingFiles/delete-routing.db");
        if(File.Exists(path)) Assert.Fail("Retained SQLite routing evidence exists; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var verified=false;
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoReplicationEnabled=true};
            manager.RegisterProvider(_provider);manager.RegisterProvider(sqlite);
            var types=new[] {new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.AWSOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(types).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            foreach(var primary in types)
            {
                var root=new Holon {Name="owned-v2-graph-root",IsActive=true};
                var child=new Holon {Name="owned-v2-graph-child",IsActive=true};
                var leaf=new Holon {Name="owned-v2-graph-leaf",IsActive=true};
                root.Children=new List<IHolon> {child};child.Children=new List<IHolon> {leaf};
                var request=new Core.StorageOperationRequest {Operation="SaveHolon",Payload=root,SaveChildren=true,Recursive=true,PreferredProvider=primary.Value};
                var saved=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                Assert.IsFalse(saved.IsError,saved.Message);Assert.IsFalse(saved.IsWarning,saved.Message);Assert.IsTrue(saved.IsSaved);
                Assert.AreNotEqual(Guid.Empty,root.Id);Assert.AreNotEqual(Guid.Empty,child.Id);Assert.AreNotEqual(Guid.Empty,leaf.Id);
                foreach(var store in new IOASISStorageProvider[] {_provider,sqlite})
                {
                    var loaded=await store.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);
                    Assert.IsFalse(loaded.IsError,loaded.Message);
                    var c=loaded.Result.Children.Single();var l=c.Children.Single();
                    Assert.AreEqual(child.Id,c.Id);Assert.AreEqual(leaf.Id,l.Id);
                    Assert.AreEqual(root.Id,c.ParentHolonId);Assert.AreEqual(child.Id,l.ParentHolonId);
                    Assert.AreEqual(child.Name,c.Name);Assert.AreEqual(leaf.Name,l.Name);
                }
                foreach(var node in new[] {root,child,leaf})
                {
                    await Client.DeleteItemAsync(new DeleteItemRequest {TableName="OasisHolons",Key=Key(node.Id)});
                    Assert.IsFalse((await sqlite.DeleteHolonAsync(node.Id)).IsError);
                }
            }
            verified=true;
        }
        finally
        {
            await sqlite.DeActivateProviderAsync();
            if(verified) foreach(var file in new[] {path,path+"-wal",path+"-shm"}) if(File.Exists(file)) File.Delete(file);
        }
    }

    [DataTestMethod]
    [DataRow(false,false,true,0)] [DataRow(true,false,true,0)]
    [DataRow(false,true,false,0)] [DataRow(true,true,false,0)]
    [DataRow(false,true,true,1)] [DataRow(true,true,true,1)]
    [DataRow(false,true,true,0)] [DataRow(true,true,true,0)]
    public async Task NativeGraphSaveHonorsSelectionAndParentLinks(bool synchronous,bool children,bool recursive,int depth)
    {
        var root=new Holon {Name="owned-graph-root",IsActive=true};
        var child=new Holon {Name="owned-graph-child",IsActive=true};
        var leaf=new Holon {Name="owned-graph-leaf",IsActive=true};
        root.Children=new List<IHolon> {child};child.Children=new List<IHolon> {leaf};
        try
        {
            var result=synchronous ? _provider.SaveHolon(root,children,recursive,depth):await _provider.SaveHolonAsync(root,children,recursive,depth);
            Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsSaved);Assert.AreSame(root,result.Result);
            var expected=children ? recursive && depth==0 ? 3:2:1;
            if(children)
            {
                foreach(var providerChildren in new[] {false,true})
                {
                    var loaded=await _provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:providerChildren);
                    Assert.IsFalse(loaded.IsError,loaded.Message);
                    // OasisJson intentionally excludes holon navigation; provider loading follows persisted parent IDs.
                    if(!providerChildren) {Assert.AreEqual(0,loaded.Result.Children.Count());continue;}
                    Assert.AreEqual(child.Id,loaded.Result.Children.Single().Id);
                    Assert.AreEqual(root.Id,loaded.Result.Children.Single().ParentHolonId);
                    if(expected==3) Assert.AreEqual(leaf.Id,loaded.Result.Children.Single().Children.Single().Id);
                    else Assert.AreEqual(0,loaded.Result.Children.Single().Children.Count());
                }
            }
            var nodes=new[] {root,child,leaf};
            for(var index=0;index<nodes.Length;index++)
            {
                var node=nodes[index];
                if(index>=expected) {Assert.AreEqual(Guid.Empty,node.Id,"Unselected caller identity must not be published.");continue;}
                Assert.AreNotEqual(Guid.Empty,node.Id);
                var raw=await Client.GetItemAsync(new GetItemRequest {TableName="OasisHolons",Key=Key(node.Id),ConsistentRead=true});
                Assert.IsTrue(raw.Item.Count>0,"Each selected node must have its own native item.");
                var canonical=OasisJson.Deserialize<Holon>(raw.Item["data_json"].S);
                Assert.AreEqual(node.Id,canonical.Id);Assert.AreEqual(node.Name,canonical.Name);
                if(index>0)
                {
                    Assert.AreEqual(nodes[index-1].Id,node.ParentHolonId);
                    Assert.AreEqual(node.ParentHolonId,canonical.ParentHolonId);
                    Assert.AreEqual(node.ParentHolonId.ToString(),raw.Item["parentHolonId"].S);
                }
            }
        }
        finally
        {
            foreach(var node in new[] {root,child,leaf}) if(node.Id!=Guid.Empty)
                await Client.DeleteItemAsync(new DeleteItemRequest {TableName="OasisHolons",Key=Key(node.Id)});
        }
    }
}
