using System.Net;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public sealed class DynamoOrdinarySaveLifecycleTests
{
    private sealed class Probe(string table,Dictionary<string,AttributeValue> original):AmazonDynamoDBClient(
        new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",MaxErrorRetry=0})
    {
        public Dictionary<string,AttributeValue> Stored=original;public int Reads,Writes;
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken token=default)
        {Reads++;Assert.AreEqual(table,request.TableName);Assert.IsTrue(request.ConsistentRead);return Task.FromResult(new GetItemResponse{HttpStatusCode=HttpStatusCode.OK,Item=Stored});}
        public override Task<PutItemResponse> PutItemAsync(PutItemRequest request,CancellationToken token=default)
        {
            Writes++;Assert.AreEqual(table,request.TableName);Assert.AreEqual("#data = :observed",request.ConditionExpression);
            Assert.AreEqual(Stored["data_json"].S,request.ExpressionAttributeValues[":observed"].S);
            var previous=Stored;Stored=request.Item;return Task.FromResult(new PutItemResponse{HttpStatusCode=HttpStatusCode.OK,Attributes=previous});
        }
    }
    public static IEnumerable<object[]> Cases()
    {foreach(var kind in Enumerable.Range(0,3))foreach(var change in Enumerable.Range(0,4))foreach(var synchronous in new[]{false,true})foreach(var history in new[]{false,true})yield return new object[]{kind,change,synchronous,history};}

    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task OrdinarySaveMustRespectObservedTombstoneBeforeMutation(int kind,int change,bool synchronous,bool history)
    {
        IHolonBase source=kind==0?new Avatar{Username="owned",Email="owned@example.invalid"}:kind==1?new AvatarDetail{Username="owned",Email="owned@example.invalid"}:new Holon();
        source.Id=Guid.NewGuid();source.Version=3;source.VersionId=Guid.NewGuid();source.IsActive=false;
        source.DeletedDate=new DateTime(2026,10,20,0,0,0,DateTimeKind.Utc);source.DeletedByAvatarId=Guid.NewGuid();
        var originalJson=OasisJson.Serialize(source);var item=new Dictionary<string,AttributeValue>{{"id",new(){S=source.Id.ToString()}},{"is_deleted",new(){BOOL=true}},{"data_json",new(){S=originalJson}}};
        var table=kind==0?"OasisAvatars":kind==1?"OasisAvatarDetails":"OasisHolons";
        if(kind<2){item["username"]=new(){S="owned"};item["email"]=new(){S="owned@example.invalid"};}
        else{item["parentHolonId"]=new(){S=Guid.Empty.ToString()};item["holon_type"]=new(){N=((int)((IHolon)source).HolonType).ToString()};}
        if(change==0){source.IsActive=true;source.DeletedDate=DateTime.MinValue;}else if(change==1)source.DeletedDate=source.DeletedDate.AddTicks(1);else if(change==2)source.DeletedByAvatarId=Guid.NewGuid();else source.Name="compatible tombstone edit";
        var proposedJson=OasisJson.Serialize(source);var provider=new DynamoDBOASIS("owned","owned","us-east-1"){IsVersionControlEnabled=history};
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var probe=new Probe(table,item);field.SetValue(provider,probe);
        try{
            if(kind==0)Check(synchronous?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source));
            else if(kind==1)Check(synchronous?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source));
            else Check(synchronous?provider.SaveHolon((IHolon)source,saveChildren:false):await provider.SaveHolonAsync((IHolon)source,saveChildren:false));
            Assert.AreEqual(1,probe.Reads);Assert.AreEqual(change==3?1:0,probe.Writes);
            if(change!=3){Assert.AreEqual(originalJson,probe.Stored["data_json"].S);Assert.AreEqual(proposedJson,OasisJson.Serialize(source));}
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
        void Check<T>(OASISResult<T> result){
            Assert.AreEqual(change!=3,result.IsError,result.Exception?.ToString());Assert.AreEqual(change==3,result.IsSaved);
            if(change!=3){Assert.IsInstanceOfType<StorageLifecycleConflictException>(result.Exception);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);Assert.AreEqual(0,result.SavedCount);Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception));}
        }
    }
}
