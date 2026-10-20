using System.Net;
using System.Reflection;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public sealed class DynamoLifecycleReadAcknowledgementTests
{
    private sealed class ReadProbe(bool detail,int mode):AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{RegionEndpoint=RegionEndpoint.USEast1,MaxErrorRetry=0})
    {
        public int Reads;public int Transactions;
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken token=default)
        {
            Reads++;Assert.IsTrue(request.ConsistentRead);
            var id=Guid.Parse(request.Key["id"].S);var isDetail=request.TableName=="OasisAvatarDetails";
            if(isDetail==detail&&mode==0)return Task.FromResult<GetItemResponse>(null!);
            var json=isDetail?OasisJson.Serialize(new AvatarDetail{Id=id,IsActive=true}):OasisJson.Serialize(new Avatar{Id=id,IsActive=true});
            return Task.FromResult(new GetItemResponse{HttpStatusCode=isDetail==detail&&mode==1?HttpStatusCode.NoContent:HttpStatusCode.OK,
                Item=isDetail==detail&&mode==2?null!:new(){{"id",new AttributeValue{S=id.ToString()}},{"data_json",new AttributeValue{S=json}}}});
        }
        public override Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken token=default)
        {Transactions++;return Task.FromResult(new TransactWriteItemsResponse{HttpStatusCode=HttpStatusCode.OK});}
    }

    [DataTestMethod]
    [DataRow(false,0,false)][DataRow(false,0,true)][DataRow(false,1,false)][DataRow(false,1,true)][DataRow(false,2,false)][DataRow(false,2,true)]
    [DataRow(true,0,false)][DataRow(true,0,true)][DataRow(true,1,false)][DataRow(true,1,true)][DataRow(true,2,false)][DataRow(true,2,true)]
    public async Task LifecycleRequiresAuthoritativeReadAcknowledgementsBeforeTransaction(bool detail,int mode,bool softDelete)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var probe=new ReadProbe(detail,mode);
        field.SetValue(provider,probe);provider.IsProviderActivated=true;
        try {
            var result=await provider.DeleteAvatarAsync(Guid.NewGuid(),softDelete);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);
            Assert.IsInstanceOfType<InvalidDataException>(result.Exception,result.Exception?.ToString());
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception));Assert.AreEqual(0,probe.Transactions);
            Assert.AreEqual(detail?2:1,probe.Reads);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
