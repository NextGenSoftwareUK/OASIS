using System.Net;
using System.Reflection;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public sealed class DynamoPointReadAcknowledgementTests
{
    private sealed class Probe(int mode):AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{RegionEndpoint=RegionEndpoint.USEast1,MaxErrorRetry=0})
    {
        public int Reads;
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken token=default)
        {
            Reads++;Assert.IsTrue(request.ConsistentRead);if(mode==0)return Task.FromResult<GetItemResponse>(null!);
            var id=Guid.Parse(request.Key["id"].S);
            var json=request.TableName=="OasisAvatars"?OasisJson.Serialize(new Avatar{Id=id}):request.TableName=="OasisAvatarDetails"?OasisJson.Serialize(new AvatarDetail{Id=id}):OasisJson.Serialize(new Holon{Id=id});
            return Task.FromResult(new GetItemResponse{HttpStatusCode=mode==1?HttpStatusCode.NoContent:HttpStatusCode.OK,
                Item=mode==2?null!:mode==3?new():new(){{"id",new AttributeValue{S=id.ToString()}},{"data_json",new AttributeValue{S=json}}}});
        }
    }
    public static IEnumerable<object[]> Cases(){foreach(var kind in new[]{0,1,2})foreach(var mode in new[]{0,1,2,3})foreach(var sync in new[]{false,true})yield return new object[]{kind,mode,sync};}

    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task OnlyAuthoritativeEmptyItemCanBecomeNotFound(int kind,int mode,bool synchronous)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var probe=new Probe(mode);field.SetValue(provider,probe);
        try {
            void Check<T>(OASISResult<T> result){
                Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
                if(mode==3)Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
                else {Assert.IsInstanceOfType<InvalidDataException>(result.Exception,result.Exception?.ToString());Assert.AreNotEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);}
            }
            var id=Guid.NewGuid();
            if(kind==0)Check(synchronous?provider.LoadAvatar(id):await provider.LoadAvatarAsync(id));
            else if(kind==1)Check(synchronous?provider.LoadAvatarDetail(id):await provider.LoadAvatarDetailAsync(id));
            else Check(synchronous?provider.LoadHolon(id,loadChildren:false):await provider.LoadHolonAsync(id,loadChildren:false));
            Assert.AreEqual(1,probe.Reads);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
