using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task V2GraphReplicationAndPartialCommitFence(bool synchronous,bool partial)
    {
        var path=Path.GetFullPath("TestResults/AzureRoutingFiles/lifecycle.db");
        if(File.Exists(path)) Assert.Fail("Retained Azure routing evidence exists; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var healthy=(BlobServiceClient)ServiceField.GetValue(_provider)!;var verified=false;
        var fault=new LostUploadService {Healthy=healthy,FailOnCommit=2};
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoReplicationEnabled=true,IsAutoFailOverEnabled=true};
            manager.RegisterProvider(_provider);manager.RegisterProvider(sqlite);
            var types=new[] {new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.AzureStorageOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(types).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(types).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            foreach(var primary in partial ? types.Take(1):types)
            {
                var root=new Holon {Name="owned-v2-root",IsActive=true};var child=new Holon {Name="owned-v2-child",IsActive=true};
                var leaf=new Holon {Name="owned-v2-leaf",IsActive=true};
                root.Children=new List<IHolon> {child};child.Children=new List<IHolon> {leaf};
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                if(partial) ServiceField.SetValue(_provider,fault);
                NextGenSoftware.OASIS.Common.OASISResult<IHolon> result;
                try
                {
                    var request=new Core.StorageOperationRequest {Operation="SaveHolon",Payload=root,SaveChildren=true,Recursive=true,PreferredProvider=primary.Value};
                    result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                }
                finally {ServiceField.SetValue(_provider,healthy);}
                var cleanup=new List<Guid>();
                if(partial)
                {
                    Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(2,fault.Commits);
                    Assert.IsInstanceOfType(result.Exception,typeof(AzureBlobPartialMutationException));
                    var acknowledged=(AzureBlobPartialMutationException)result.Exception;
                    Assert.AreEqual(root.Id,acknowledged.AcknowledgedRecordIds.Single());Assert.AreNotEqual(Guid.Empty,root.Id);
                    var pending=(AzureBlobMutationReconciliationException)acknowledged.InnerException;
                    Assert.AreSame(fault.Cause,pending.InnerException);Assert.IsTrue(Guid.TryParse(pending.BlobName,out var childId));
                    Assert.AreEqual(Guid.Empty,child.Id);Assert.AreEqual(Guid.Empty,leaf.Id);
                    Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
                    foreach(var id in new[] {root.Id,childId})
                    {
                        var blob=healthy.GetBlobContainerClient(_containers[0]).GetBlobClient(id.ToString());
                        var native=OasisJson.Deserialize<Holon>((await blob.DownloadContentAsync()).Value.Content.ToString());
                        Assert.AreEqual(id,native.Id);if(id==childId) Assert.AreEqual(root.Id,native.ParentHolonId);
                        Assert.IsNull((await sqlite.LoadHolonAsync(id,loadChildren:false)).Result);cleanup.Add(id);
                    }
                    var descendants=await _provider.LoadHolonsForParentAsync(childId,loadChildren:false);
                    Assert.IsFalse(descendants.IsError,descendants.Message);Assert.AreEqual(0,descendants.Result.Count());
                }
                else
                {
                    Assert.IsFalse(result.IsError,result.Message);Assert.IsFalse(result.IsWarning,result.Message);Assert.IsTrue(result.IsSaved);
                    Assert.AreNotEqual(Guid.Empty,root.Id);Assert.AreNotEqual(Guid.Empty,child.Id);Assert.AreNotEqual(Guid.Empty,leaf.Id);
                    foreach(var store in new IOASISStorageProvider[] {_provider,sqlite})
                    {
                        var loaded=await store.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);
                        Assert.IsFalse(loaded.IsError,loaded.Message);var c=loaded.Result.Children.Single();var l=c.Children.Single();
                        Assert.AreEqual(child.Id,c.Id);Assert.AreEqual(leaf.Id,l.Id);Assert.AreEqual(root.Id,c.ParentHolonId);Assert.AreEqual(child.Id,l.ParentHolonId);
                    }
                    cleanup.AddRange(new[] {root.Id,child.Id,leaf.Id});
                }
                foreach(var id in cleanup) await healthy.GetBlobContainerClient(_containers[0]).GetBlobClient(id.ToString()).DeleteAsync();
            }
            verified=true;
        }
        finally
        {
            ServiceField.SetValue(_provider,healthy);await sqlite.DeActivateProviderAsync();
            if(verified) foreach(var file in new[] {path,path+"-wal",path+"-shm"}) if(File.Exists(file)) File.Delete(file);
        }
    }
}
