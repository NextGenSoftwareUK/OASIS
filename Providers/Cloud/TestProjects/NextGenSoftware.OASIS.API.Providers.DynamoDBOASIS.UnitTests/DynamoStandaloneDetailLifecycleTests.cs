using System.Net;
using System.Reflection;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public sealed class DynamoStandaloneDetailLifecycleTests
{
    private sealed class Probe(int mode, bool soft):AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{RegionEndpoint=RegionEndpoint.USEast1,MaxErrorRetry=0})
    {
        public int Reads, Transactions; public string? Token; public string Original = "";public bool ExistingTombstone;
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)
        {
            Reads++; Assert.AreEqual("OasisAvatarDetails",request.TableName); Assert.IsTrue(request.ConsistentRead);
            var id=Guid.Parse(request.Key["id"].S);
            Original=OasisJson.Serialize(new AvatarDetail{Id=id,IsActive=!ExistingTombstone,DeletedDate=ExistingTombstone?new DateTime(2026,10,20,0,0,0,DateTimeKind.Utc):DateTime.MinValue});
            return Task.FromResult(new GetItemResponse{HttpStatusCode=mode==1?HttpStatusCode.BadGateway:HttpStatusCode.OK,
                Item=mode==2?new():new(){{"id",new AttributeValue{S=id.ToString()}},{"data_json",new AttributeValue{S=Original}}}});
        }
        public override Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken cancellationToken=default)
        {
            Transactions++; Token=request.ClientRequestToken; Assert.IsFalse(string.IsNullOrWhiteSpace(Token));
            Assert.AreEqual(1,request.TransactItems.Count);
            var write=request.TransactItems.Single();
            if(ExistingTombstone){Assert.IsNotNull(write.ConditionCheck,"Matching tombstones must not create another history revision.");Assert.AreEqual(Original,write.ConditionCheck.ExpressionAttributeValues[":expected"].S);if(mode==4)throw new IOException("lost condition-only acknowledgement");return Task.FromResult(new TransactWriteItemsResponse{HttpStatusCode=HttpStatusCode.OK});}
            if(soft){ Assert.IsNotNull(write.Update); Assert.AreEqual("OasisAvatarDetails",write.Update.TableName);
                Assert.AreEqual(Original,write.Update.ExpressionAttributeValues[":expected"].S);
                var updated=OasisJson.DeserializeStored<AvatarDetail>(write.Update.ExpressionAttributeValues[":json"].S);
                Assert.IsTrue(updated.IsDeleted); Assert.IsFalse(updated.IsActive);
            } else { Assert.IsNotNull(write.Delete); Assert.AreEqual("OasisAvatarDetails",write.Delete.TableName);
                Assert.AreEqual(Original,write.Delete.ExpressionAttributeValues[":expected"].S); }
            if(mode==3)throw new TransactionCanceledException("conflict"){CancellationReasons=new(){new CancellationReason{Code="ConditionalCheckFailed"}}};
            if(mode==4)throw new IOException("lost native return");
            if(mode==6)throw new AmazonDynamoDBException("native validation rejection"){StatusCode=HttpStatusCode.BadRequest,ErrorCode="ValidationException"};
            return Task.FromResult(new TransactWriteItemsResponse{HttpStatusCode=mode==5?HttpStatusCode.BadGateway:HttpStatusCode.OK});
        }
    }
    public static IEnumerable<object[]> Cases(){foreach(var soft in new[]{false,true})foreach(var mode in Enumerable.Range(0,7))yield return new object[]{soft,mode};}
    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task MatchingDetailTombstoneUsesConditionOnly(bool synchronous,bool lost)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var probe=new Probe(lost?4:0,true){ExistingTombstone=true};field.SetValue(provider,probe);provider.IsProviderActivated=true;
        try{var result=synchronous?provider.DeleteAvatarDetail(Guid.NewGuid()):await provider.DeleteAvatarDetailAsync(Guid.NewGuid());Assert.AreEqual(lost,result.IsError,result.Exception?.ToString());Assert.AreEqual(!lost,result.Result);Assert.AreEqual(0,result.DeletedCount);Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception));if(lost)Assert.IsInstanceOfType<IOException>(result.Exception);Assert.AreEqual(1,probe.Reads);Assert.AreEqual(1,probe.Transactions);}
        finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task StandaloneDetailUsesOriginalConditionAndTruthfulOutcome(bool soft,int mode)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var probe=new Probe(mode,soft);
        field.SetValue(provider,probe);provider.IsProviderActivated=true;
        try {
            var result=await provider.DeleteAvatarDetailAsync(Guid.NewGuid(),soft);
            Assert.AreEqual(mode!=0,result.IsError,result.Exception?.ToString());
            Assert.AreEqual(mode==0,result.Result);Assert.AreEqual(mode==0,result.IsDeleted);Assert.AreEqual(mode==0,result.IsSaved);
            Assert.AreEqual(1,probe.Reads);Assert.AreEqual(mode is 1 or 2?0:1,probe.Transactions);
            if(mode==2)Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
            if(mode==3)Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
            if(mode==6){Assert.IsInstanceOfType<AmazonDynamoDBException>(result.Exception);Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception));}
            if(mode is 4 or 5){Assert.IsInstanceOfType<DynamoDBMutationReconciliationException>(result.Exception);
                Assert.AreEqual(probe.Token,((DynamoDBMutationReconciliationException)result.Exception).ClientRequestToken);}
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
