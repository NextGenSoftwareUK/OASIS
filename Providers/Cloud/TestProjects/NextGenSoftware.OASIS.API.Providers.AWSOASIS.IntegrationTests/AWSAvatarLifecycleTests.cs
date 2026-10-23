using System.Reflection;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task V2DeleteReplicatesCommittedTombstoneInBothDirections(bool synchronous)
    {
        var path=Path.GetFullPath("TestResults/AWSRoutingFiles/delete-routing.db");
        if(File.Exists(path)) Assert.Fail("Retained SQLite lifecycle evidence exists; refusing overwrite.");
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
            foreach(var preferred in types)
            {
                var id=Guid.NewGuid();var actor=Guid.NewGuid();
                foreach(var store in new IOASISStorageProvider[] {_provider,sqlite})
                {
                    var native=ReferenceEquals(store,_provider);
                    Assert.IsFalse((await store.SaveAvatarAsync(new Avatar {Id=id,Username="owned-v2-"+id.ToString("N"),
                        FirstName=native ? "aws-profile":"sqlite-profile",IsActive=true,DeletedByAvatarId=actor,
                        AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)})).IsError);
                    var detail=new AvatarDetail {Id=id,Username="owned-v2-"+id.ToString("N"),IsActive=true,XP=native ? 501:502};
                    detail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name=native ? "aws-inventory":"sqlite-inventory"});
                    Assert.IsFalse((await store.SaveAvatarDetailAsync(detail)).IsError);
                }
                var request=new Core.StorageOperationRequest {Operation="DeleteAvatar",AvatarId=id,SoftDelete=true,PreferredProvider=preferred.Value};
                var deleted=synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request);
                Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsFalse(deleted.IsWarning,deleted.Message);
                Assert.IsTrue(AvatarTombstoneEvidence.TryGet(deleted,out var committed));
                var aws=await ReadPair(id);var localProfile=(await sqlite.LoadAvatarAsync(id)).Result;var localDetail=(await sqlite.LoadAvatarDetailAsync(id)).Result;
                Assert.IsFalse(aws.Profile.IsActive);Assert.IsFalse(aws.Detail.IsActive);Assert.IsFalse(localProfile.IsActive);Assert.IsFalse(localDetail.IsActive);
                foreach(var date in new[] {aws.Profile.DeletedDate,aws.Detail.DeletedDate,localProfile.DeletedDate,localDetail.DeletedDate}) Assert.AreEqual(committed.DeletedAtUtc,date);
                foreach(var owner in new[] {aws.Profile.DeletedByAvatarId,aws.Detail.DeletedByAvatarId,localProfile.DeletedByAvatarId,localDetail.DeletedByAvatarId}) Assert.AreEqual(actor,owner);
                Assert.AreEqual("aws-profile",aws.Profile.FirstName);Assert.AreEqual("sqlite-profile",localProfile.FirstName);
                Assert.AreEqual(501L,aws.Detail.XP);Assert.AreEqual(502L,localDetail.XP);
                Assert.AreEqual("aws-inventory",aws.Detail.Inventory.Single().Name);Assert.AreEqual("sqlite-inventory",localDetail.Inventory.Single().Name);
                var hardRequest=new Core.StorageOperationRequest {Operation="DeleteAvatar",AvatarId=id,SoftDelete=false,PreferredProvider=preferred.Value};
                var hard=synchronous ? router.RouteRequest<bool>(hardRequest):await router.RouteRequestAsync<bool>(hardRequest);
                Assert.IsFalse(hard.IsError,hard.Message);Assert.IsFalse(hard.IsWarning,hard.Message);Assert.IsTrue(hard.Result);
                foreach(var table in new[] {"OasisAvatars","OasisAvatarDetails"})
                    Assert.AreEqual(0,(await Client.GetItemAsync(new GetItemRequest {TableName=table,Key=Key(id),ConsistentRead=true})).Item.Count);
                Assert.IsNull((await sqlite.LoadAvatarAsync(id)).Result);Assert.IsNull((await sqlite.LoadAvatarDetailAsync(id)).Result);
            }
            verified=true;
        }
        finally
        {
            await sqlite.DeActivateProviderAsync();
            if(verified) foreach(var file in new[] {path,path+"-wal",path+"-shm"}) if(File.Exists(file)) File.Delete(file);
        }
    }

    private static readonly FieldInfo ClientField=typeof(DynamoDBOASIS.DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
    private AmazonDynamoDBClient Client=>(AmazonDynamoDBClient)ClientField.GetValue(_provider)!;
    private static Dictionary<string,AttributeValue> Key(Guid id)=>new() {{"id",new AttributeValue {S=id.ToString()}}};
    private async Task<(Avatar Profile,AvatarDetail Detail)> ReadPair(Guid id)
    {
        var profile=await Client.GetItemAsync(new GetItemRequest {TableName="OasisAvatars",Key=Key(id),ConsistentRead=true});
        var detail=await Client.GetItemAsync(new GetItemRequest {TableName="OasisAvatarDetails",Key=Key(id),ConsistentRead=true});
        return (OasisJson.Deserialize<Avatar>(profile.Item["data_json"].S),OasisJson.Deserialize<AvatarDetail>(detail.Item["data_json"].S));
    }

    private async Task SeedPair(Guid id)
    {
        Assert.IsFalse((await _provider.SaveAvatarAsync(new Avatar {Id=id,Username="owned-pair-"+id.ToString("N"),FirstName="preserved",IsActive=true})).IsError);
        var detail=new AvatarDetail {Id=id,Username="owned-pair-"+id.ToString("N"),XP=321,Karma=29,IsActive=true};
        detail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="preserved inventory"});
        Assert.IsFalse((await _provider.SaveAvatarDetailAsync(detail)).IsError);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task PairLifecycleAndIncomingTombstoneAreAtomicAndPreserveData(bool synchronous)
    {
        var id=Guid.NewGuid();await SeedPair(id);
        var invalid=synchronous ? _provider.DeleteAvatar("owned-pair-"+id.ToString("N"),false):await _provider.DeleteAvatarAsync("owned-pair-"+id.ToString("N"),false);
        Assert.IsTrue(invalid.IsError);Assert.IsInstanceOfType<ArgumentException>(invalid.Exception);Assert.IsFalse(invalid.Result);Assert.AreEqual(0,invalid.DeletedCount);
        var untouched=await ReadPair(id);Assert.IsTrue(untouched.Profile.IsActive);Assert.IsTrue(untouched.Detail.IsActive);Assert.AreEqual(321L,untouched.Detail.XP);Assert.AreEqual(29L,untouched.Detail.Karma);Assert.AreEqual(1,untouched.Detail.Inventory.Count);
        var deleted=synchronous ? _provider.DeleteAvatar(id.ToString()):await _provider.DeleteAvatarAsync(id.ToString());
        Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsTrue(deleted.IsSaved);Assert.IsTrue(deleted.IsDeleted);
        Assert.IsTrue(AvatarTombstoneEvidence.TryGet(deleted,out var committed));
        var pair=await ReadPair(id);
        Assert.IsFalse(pair.Profile.IsActive);Assert.IsFalse(pair.Detail.IsActive);
        Assert.AreEqual(committed.DeletedAtUtc,pair.Profile.DeletedDate);Assert.AreEqual(pair.Profile.DeletedDate,pair.Detail.DeletedDate);
        Assert.AreEqual("preserved",pair.Profile.FirstName);Assert.AreEqual(321L,pair.Detail.XP);
        Assert.AreEqual("preserved inventory",pair.Detail.Inventory.Single().Name);
        var repeated=synchronous ? _provider.DeleteAvatar(id):await _provider.DeleteAvatarAsync(id);
        Assert.IsFalse(repeated.IsError,repeated.Message);Assert.AreEqual(committed.DeletedAtUtc,(await ReadPair(id)).Detail.DeletedDate);
        var hard=synchronous ? _provider.DeleteAvatar(id,false):await _provider.DeleteAvatarAsync(id,false);
        Assert.IsFalse(hard.IsError,hard.Message);
        foreach(var table in new[] {"OasisAvatars","OasisAvatarDetails"})
            Assert.AreEqual(0,(await Client.GetItemAsync(new GetItemRequest {TableName=table,Key=Key(id),ConsistentRead=true})).Item.Count);
        var target=Guid.NewGuid();await SeedPair(target);
        var tombstone=new AvatarTombstone(target,new DateTime(2026,10,16,0,0,0,DateTimeKind.Utc),Guid.NewGuid());
        var capability=(IAvatarTombstoneStorageProvider)_provider;
        foreach(var repeat in new[] {false,true})
        {
            var applied=synchronous ? capability.ApplyAvatarTombstone(tombstone):await capability.ApplyAvatarTombstoneAsync(tombstone);
            Assert.IsFalse(applied.IsError,applied.Message);pair=await ReadPair(target);
            Assert.AreEqual(tombstone.DeletedAtUtc,pair.Profile.DeletedDate);Assert.AreEqual(tombstone.DeletedAtUtc,pair.Detail.DeletedDate);
            Assert.AreEqual(tombstone.DeletedByAvatarId,pair.Profile.DeletedByAvatarId);Assert.AreEqual(tombstone.DeletedByAvatarId,pair.Detail.DeletedByAvatarId);
            Assert.AreEqual(321L,pair.Detail.XP);Assert.AreEqual("preserved inventory",pair.Detail.Inventory.Single().Name);
        }
        var conflict=await capability.ApplyAvatarTombstoneAsync(new AvatarTombstone(target,tombstone.DeletedAtUtc.AddSeconds(1),tombstone.DeletedByAvatarId));
        Assert.IsTrue(conflict.IsError);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,conflict.ErrorCode);
        Assert.AreEqual(tombstone.DeletedAtUtc,(await ReadPair(target)).Detail.DeletedDate);
        Assert.IsFalse((await _provider.DeleteAvatarAsync(target,false)).IsError);
    }

    private sealed class ConcurrentDetailClient : AmazonDynamoDBClient
    {
        public required AmazonDynamoDBClient Healthy;
        public required Guid Target;
        public bool CreateMissing;
        public int Changes;
        public ConcurrentDetailClient():base(new BasicAWSCredentials("fakeMyKeyId","fakeSecretAccessKey"),
            new AmazonDynamoDBConfig {ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",UseHttp=true}) { }
        public override async Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)
        {
            var observed=await Healthy.GetItemAsync(request,cancellationToken);
            if(request.TableName=="OasisAvatarDetails" && request.Key["id"].S==Target.ToString() && Changes++==0)
            {
                Assert.AreEqual(CreateMissing,observed.Item.Count==0);
                var newer=CreateMissing ? new AvatarDetail {Id=Target,IsActive=true}:OasisJson.Deserialize<AvatarDetail>(observed.Item["data_json"].S);
                newer.XP=999;
                if(CreateMissing)
                    await Healthy.PutItemAsync(new PutItemRequest {TableName=request.TableName,Item=new() {
                        {"id",new AttributeValue {S=Target.ToString()}},{"data_json",new AttributeValue {S=OasisJson.Serialize(newer)}}}},cancellationToken);
                else
                    await Healthy.UpdateItemAsync(new UpdateItemRequest {TableName=request.TableName,Key=request.Key,
                        UpdateExpression="SET data_json = :json",ExpressionAttributeValues=new() {{":json",new AttributeValue {S=OasisJson.Serialize(newer)}}}},cancellationToken);
            }
            return observed; // Actual SDK snapshot observed before the real concurrent write.
        }
        public override Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken cancellationToken=default)
            =>Healthy.TransactWriteItemsAsync(request,cancellationToken);
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task ConcurrentDetailChangeCancelsWholeLifecycleTransaction(bool softDelete,bool createMissing)
    {
        var id=Guid.NewGuid();await SeedPair(id);var healthy=Client;
        if(createMissing)
        {
            await healthy.DeleteItemAsync(new DeleteItemRequest {TableName="OasisAvatarDetails",Key=Key(id)});
            Assert.AreEqual(0,(await healthy.GetItemAsync(new GetItemRequest {TableName="OasisAvatarDetails",Key=Key(id),ConsistentRead=true})).Item.Count);
        }
        using var concurrent=new ConcurrentDetailClient {Healthy=healthy,Target=id,CreateMissing=createMissing};
        ClientField.SetValue(_provider,concurrent);
        NextGenSoftware.OASIS.Common.OASISResult<bool> result;
        try {result=await _provider.DeleteAvatarAsync(id,softDelete);}
        finally {ClientField.SetValue(_provider,healthy);}
        Assert.IsTrue(result.IsError);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
        Assert.IsInstanceOfType(result.Exception,typeof(TransactionCanceledException));
        Assert.IsFalse(AvatarTombstoneEvidence.TryGet(result,out _));
        var pair=await ReadPair(id);Assert.IsTrue(pair.Profile.IsActive);Assert.IsTrue(pair.Detail.IsActive);
        Assert.IsFalse(pair.Profile.IsDeleted);Assert.IsFalse(pair.Detail.IsDeleted);Assert.AreEqual(999L,pair.Detail.XP);
        Assert.IsFalse((await _provider.DeleteAvatarAsync(id,false)).IsError);
    }
}
