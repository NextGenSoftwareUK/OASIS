using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    private sealed class LifecycleDispatchProbe : AmazonDynamoDBClient
    {
        public required AmazonDynamoDBClient Healthy;
        public int Mutations;
        public LifecycleDispatchProbe():base(new BasicAWSCredentials("fakeMyKeyId","fakeSecretAccessKey"),
            new AmazonDynamoDBConfig {ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",UseHttp=true}) { }
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)=>Healthy.GetItemAsync(request,cancellationToken);
        public override Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken cancellationToken=default)
        {Mutations++;return Healthy.TransactWriteItemsAsync(request,cancellationToken);}
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task V2CorruptLifecycleNeverDispatchesOrReplays(bool synchronous)
    {
        var path=Path.GetFullPath("TestResults/AWSRoutingFiles/delete-routing.db");
        if(File.Exists(path)) Assert.Fail("Retained SQLite routing evidence exists; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var healthy=Client;var verified=false;
        using var probe=new LifecycleDispatchProbe {Healthy=healthy};
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
            foreach(var detail in new[] {false,true})
            foreach(var malformed in new[] {false,true})
            foreach(var soft in new[] {false,true})
            {
                var id=Guid.NewGuid();await SeedPair(id);
                Assert.IsFalse((await sqlite.SaveAvatarAsync(new Avatar {Id=id,Username="owned-integrity-"+id,IsActive=true,
                    AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)})).IsError);
                Assert.IsFalse((await sqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username="owned-integrity-"+id,IsActive=true,XP=777})).IsError);
                var json=malformed ? "{ invalid canonical JSON":detail ? OasisJson.Serialize(new AvatarDetail {Id=Guid.NewGuid(),XP=999})
                    :OasisJson.Serialize(new Avatar {Id=Guid.NewGuid(),Username="owned-wrong-id"});
                var table=detail ? "OasisAvatarDetails":"OasisAvatars";
                await healthy.UpdateItemAsync(new UpdateItemRequest {TableName=table,Key=Key(id),UpdateExpression="SET data_json = :json",
                    ExpressionAttributeValues=new() {{":json",new AttributeValue {S=json}}}});
                var originals=new Dictionary<string,string>();
                foreach(var name in new[] {"OasisAvatars","OasisAvatarDetails"})
                    originals[name]=(await healthy.GetItemAsync(new GetItemRequest {TableName=name,Key=Key(id),ConsistentRead=true})).Item["data_json"].S;
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                ClientField.SetValue(_provider,probe);
                NextGenSoftware.OASIS.Common.OASISResult<bool> result;
                try
                {
                    var request=new Core.StorageOperationRequest {Operation="DeleteAvatar",AvatarId=id,SoftDelete=soft,PreferredProvider=Core.Enums.ProviderType.AWSOASIS};
                    result=synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request);
                }
                finally {ClientField.SetValue(_provider,healthy);}
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);
                Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
                if(malformed) Assert.IsInstanceOfType(result.Exception.InnerException,typeof(Newtonsoft.Json.JsonException));
                Assert.IsFalse(AvatarTombstoneEvidence.TryGet(result,out _));Assert.AreEqual(0,probe.Mutations);
                Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
                foreach(var record in originals)
                    Assert.AreEqual(record.Value,(await healthy.GetItemAsync(new GetItemRequest {TableName=record.Key,Key=Key(id),ConsistentRead=true})).Item["data_json"].S);
                Assert.IsTrue((await sqlite.LoadAvatarAsync(id)).Result.IsActive);
                var local=(await sqlite.LoadAvatarDetailAsync(id)).Result;Assert.IsTrue(local.IsActive);Assert.IsFalse(local.IsDeleted);Assert.AreEqual(777L,local.XP);
                foreach(var name in originals.Keys) await healthy.DeleteItemAsync(new DeleteItemRequest {TableName=name,Key=Key(id)});
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
