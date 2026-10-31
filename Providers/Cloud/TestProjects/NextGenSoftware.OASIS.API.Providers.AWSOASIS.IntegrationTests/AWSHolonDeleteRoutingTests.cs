using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false,0)] [DataRow(true,0)] [DataRow(false,1)] [DataRow(true,1)] [DataRow(false,2)] [DataRow(true,2)]
    public async Task V2HardHolonDeletionReplicatesOnlyAcknowledgedSuccess(bool synchronous,int mode)
    {
        var path=Path.GetFullPath("TestResults/AWSRoutingFiles/delete-routing.db");
        if(File.Exists(path)) Assert.Fail("Retained SQLite routing evidence exists; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var healthy=Client;var verified=false;
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
            manager.RegisterProvider(_provider);manager.RegisterProvider(sqlite);
            var types=new[] {new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.AWSOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(types).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(types).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            foreach(var primary in mode==0 ? types:types.Take(1))
            {
                var id=Guid.NewGuid();
                Assert.IsFalse((await _provider.SaveHolonAsync(new Holon {Id=id,Name="aws-original",IsActive=true},saveChildren:false)).IsError);
                Assert.IsFalse((await sqlite.SaveHolonAsync(new Holon {Id=id,Name="sqlite-original",IsActive=true},saveChildren:false)).IsError);
                using var fault=new HolonDeleteRaceClient {Healthy=healthy,Target=id,LoseAcknowledgement=mode==2};
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                if(mode!=0) ClientField.SetValue(_provider,fault);
                NextGenSoftware.OASIS.Common.OASISResult<IHolon> result;
                try
                {
                    var request=new Core.StorageOperationRequest {Operation="DeleteHolon",HolonId=id,SoftDelete=false,PreferredProvider=primary.Value};
                    result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                }
                finally {ClientField.SetValue(_provider,healthy);}
                var native=await healthy.GetItemAsync(new GetItemRequest {TableName="OasisHolons",Key=Key(id),ConsistentRead=true});
                if(mode==0)
                {
                    Assert.IsFalse(result.IsError,result.Message);Assert.IsFalse(result.IsWarning,result.Message);Assert.IsNotNull(result.Result);
                    Assert.AreEqual(id,result.Result.Id);Assert.AreEqual(primary.Value==Core.Enums.ProviderType.AWSOASIS ? "aws-original":"sqlite-original",result.Result.Name);
                    Assert.AreEqual(0,native.Item.Count);Assert.IsNull((await sqlite.LoadHolonAsync(id,loadChildren:false)).Result);
                }
                else
                {
                    Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
                    Assert.AreEqual(1,fault.Reads);Assert.AreEqual(1,fault.Deletes);
                    Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
                    Assert.AreEqual("sqlite-original",(await sqlite.LoadHolonAsync(id,loadChildren:false)).Result.Name);
                    if(mode==1)
                    {
                        Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);Assert.IsInstanceOfType(result.Exception,typeof(ConditionalCheckFailedException));
                        Assert.AreEqual("owned-newer",OasisJson.Deserialize<Holon>(native.Item["data_json"].S).Name);
                    }
                    else
                    {
                        Assert.IsInstanceOfType(result.Exception,typeof(DynamoDBOASIS.DynamoDBMutationReconciliationException));
                        var pending=(DynamoDBOASIS.DynamoDBMutationReconciliationException)result.Exception;
                        Assert.AreSame(fault.Cause,pending.InnerException);Assert.AreEqual(id.ToString(),pending.RecordKey);
                        Assert.AreEqual("OasisHolons",pending.TableName);Assert.IsTrue(NativeMutationReconciliation.IsRequired(pending));Assert.AreEqual(0,native.Item.Count);
                    }
                }
                await healthy.DeleteItemAsync(new DeleteItemRequest {TableName="OasisHolons",Key=Key(id)});
            }
            verified=true;
        }
        finally
        {
            ClientField.SetValue(_provider,healthy);await sqlite.DeActivateProviderAsync();
            if(verified) foreach(var file in new[] {path,path+"-wal",path+"-shm"}) if(File.Exists(file)) File.Delete(file);
        }
    }
}
