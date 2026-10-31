using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false,0)] [DataRow(true,0)] [DataRow(false,1)] [DataRow(true,1)] [DataRow(false,2)] [DataRow(true,2)]
    public async Task V2HolonDeleteReplicatesOnlyAcknowledgedSuccess(bool synchronous,int mode)
    {
        var path=Path.GetFullPath("TestResults/AzureRoutingFiles/lifecycle.db");
        if(File.Exists(path)) Assert.Fail("Retained Azure routing evidence exists; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var healthy=(BlobServiceClient)ServiceField.GetValue(_provider)!;var verified=false;
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
            manager.RegisterProvider(_provider);manager.RegisterProvider(sqlite);
            var types=new[] {new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.AzureStorageOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(types).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(types).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            foreach(var primary in mode==0 ? types:types.Take(1))
            {
                var id=Guid.NewGuid();
                Assert.IsFalse((await _provider.SaveHolonAsync(new Holon {Id=id,Name="azure-original",IsActive=true},saveChildren:false)).IsError);
                Assert.IsFalse((await sqlite.SaveHolonAsync(new Holon {Id=id,Name="sqlite-original",IsActive=true},saveChildren:false)).IsError);
                var fault=new HolonRaceService {Healthy=healthy,Target=id.ToString(),LoseAcknowledgement=mode==2};
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                if(mode!=0) ServiceField.SetValue(_provider,fault);
                NextGenSoftware.OASIS.Common.OASISResult<IHolon> result;
                try
                {
                    var request=new Core.StorageOperationRequest {Operation="DeleteHolon",HolonId=id,SoftDelete=false,PreferredProvider=primary.Value};
                    result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                }
                finally {ServiceField.SetValue(_provider,healthy);}
                var blob=healthy.GetBlobContainerClient(_containers[0]).GetBlobClient(id.ToString());
                if(mode==0)
                {
                    Assert.IsFalse(result.IsError,result.Message);Assert.IsFalse(result.IsWarning,result.Message);
                    Assert.IsNotNull(result.Result);Assert.AreEqual(id,result.Result.Id);
                    Assert.AreEqual(primary.Value==Core.Enums.ProviderType.AzureStorageOASIS ? "azure-original":"sqlite-original",result.Result.Name);
                    Assert.IsFalse((await blob.ExistsAsync()).Value);Assert.IsNull((await sqlite.LoadHolonAsync(id,loadChildren:false)).Result);
                }
                else
                {
                    Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
                    Assert.AreEqual(1,fault.Reads);Assert.AreEqual(1,fault.Deletes);
                    Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
                    Assert.AreEqual("sqlite-original",(await sqlite.LoadHolonAsync(id,loadChildren:false)).Result.Name);
                    if(mode==1)
                    {
                        Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
                        var native=OasisJson.Deserialize<Holon>((await blob.DownloadContentAsync()).Value.Content.ToString());
                        Assert.AreEqual("owned-newer",native.Name);
                    }
                    else
                    {
                        Assert.IsInstanceOfType(result.Exception,typeof(AzureBlobMutationReconciliationException));
                        var pending=(AzureBlobMutationReconciliationException)result.Exception;
                        Assert.AreSame(fault.Cause,pending.InnerException);Assert.AreEqual(id.ToString(),pending.BlobName);
                        Assert.IsTrue(NativeMutationReconciliation.IsRequired(pending));Assert.IsFalse((await blob.ExistsAsync()).Value);
                    }
                }
                await blob.DeleteIfExistsAsync();
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
