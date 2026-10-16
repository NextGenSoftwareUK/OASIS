using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeHardHolonDeleteReturnsOriginalAndRemovesItem(bool synchronous,bool byKey)
    {
        var value=new Holon {Id=Guid.NewGuid(),Name="owned-delete-original",IsActive=true,ParentHolonId=Guid.NewGuid()};
        value.MetaData["retained-field"]="retained-value";
        Assert.IsFalse((await _provider.SaveHolonAsync(value,saveChildren:false)).IsError);
        try
        {
            var result=byKey ? synchronous ? _provider.DeleteHolon(value.Id.ToString()):await _provider.DeleteHolonAsync(value.Id.ToString())
                :synchronous ? _provider.DeleteHolon(value.Id):await _provider.DeleteHolonAsync(value.Id);
            Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsSaved);Assert.IsTrue(result.IsDeleted);
            Assert.AreEqual(value.Id,result.Result.Id);Assert.AreEqual(value.Name,result.Result.Name);Assert.AreEqual(value.ParentHolonId,result.Result.ParentHolonId);
            Assert.AreEqual("retained-value",result.Result.MetaData["retained-field"]);
            Assert.AreEqual(0,(await Client.GetItemAsync(new GetItemRequest {TableName="OasisHolons",Key=Key(value.Id),ConsistentRead=true})).Item.Count);
            var missing=await _provider.DeleteHolonAsync(value.Id);
            Assert.IsTrue(missing.IsError);Assert.IsNull(missing.Result);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);
        }
        finally {await Client.DeleteItemAsync(new DeleteItemRequest {TableName="OasisHolons",Key=Key(value.Id)});}
    }

    private sealed class HolonDeleteRaceClient : AmazonDynamoDBClient
    {
        public required AmazonDynamoDBClient Healthy;public required Guid Target;public int Reads,Deletes;
        public HolonDeleteRaceClient():base(new BasicAWSCredentials("fakeMyKeyId","fakeSecretAccessKey"),
            new AmazonDynamoDBConfig {ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",UseHttp=true}) { }
        public override async Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)
        {
            var observed=await Healthy.GetItemAsync(request,cancellationToken);
            if(request.TableName=="OasisHolons" && request.Key["id"].S==Target.ToString() && Interlocked.Increment(ref Reads)==1)
            {
                var newer=OasisJson.Deserialize<Holon>(observed.Item["data_json"].S);newer.Name="owned-newer";
                await Healthy.UpdateItemAsync(new UpdateItemRequest {TableName=request.TableName,Key=request.Key,UpdateExpression="SET data_json = :json",
                    ExpressionAttributeValues=new() {{":json",new AttributeValue {S=OasisJson.Serialize(newer)}}}},cancellationToken);
            }
            return observed;
        }
        public override Task<DeleteItemResponse> DeleteItemAsync(DeleteItemRequest request,CancellationToken cancellationToken=default)
        {Deletes++;return Healthy.DeleteItemAsync(request,cancellationToken);}
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeConcurrentHolonUpdatePreventsStaleDelete(bool synchronous)
    {
        var value=new Holon {Id=Guid.NewGuid(),Name="owned-original",IsActive=true};
        Assert.IsFalse((await _provider.SaveHolonAsync(value,saveChildren:false)).IsError);
        var healthy=Client;using var race=new HolonDeleteRaceClient {Healthy=healthy,Target=value.Id};
        try
        {
            ClientField.SetValue(_provider,race);
            var result=synchronous ? _provider.DeleteHolon(value.Id):await _provider.DeleteHolonAsync(value.Id);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
            Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);Assert.IsInstanceOfType(result.Exception,typeof(ConditionalCheckFailedException));
            Assert.AreEqual(1,race.Reads);Assert.AreEqual(1,race.Deletes);
            var stored=await healthy.GetItemAsync(new GetItemRequest {TableName="OasisHolons",Key=Key(value.Id),ConsistentRead=true});
            var native=OasisJson.Deserialize<Holon>(stored.Item["data_json"].S);
            Assert.AreEqual(value.Id,native.Id);Assert.AreEqual("owned-newer",native.Name);Assert.IsFalse(native.IsDeleted);
        }
        finally {ClientField.SetValue(_provider,healthy);await healthy.DeleteItemAsync(new DeleteItemRequest {TableName="OasisHolons",Key=Key(value.Id)});}
    }
}
