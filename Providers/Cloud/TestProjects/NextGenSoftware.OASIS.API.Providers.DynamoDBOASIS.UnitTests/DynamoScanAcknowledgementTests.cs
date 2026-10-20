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
public sealed class DynamoScanAcknowledgementTests
{
    private sealed class Probe(int mode):AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{RegionEndpoint=RegionEndpoint.USEast1,MaxErrorRetry=0})
    {
        public int Reads;public readonly Guid Id=Guid.NewGuid();public readonly Guid Parent=Guid.NewGuid();
        public override Task<ScanResponse> ScanAsync(ScanRequest request,CancellationToken token=default)
        {
            Reads++;Assert.IsTrue(request.ConsistentRead);Assert.IsTrue(string.IsNullOrEmpty(request.IndexName));
            if(Reads>2)throw new InvalidOperationException("Repeated cursor was not rejected within its first repetition.");
            if(mode==0)return Task.FromResult<ScanResponse>(null!);
            var json=request.TableName=="OasisAvatars"?OasisJson.Serialize(new Avatar{Id=Id,Username="selected",Email="selected@example.invalid"}):request.TableName=="OasisAvatarDetails"?OasisJson.Serialize(new AvatarDetail{Id=Id,Username="selected",Email="selected@example.invalid"}):OasisJson.Serialize(new Holon{Id=Id,ParentHolonId=Parent});
            var items=new List<Dictionary<string,AttributeValue>>{new(){{"id",new AttributeValue{S=Id.ToString()}},{"data_json",new AttributeValue{S=json}}}};
            return Task.FromResult(new ScanResponse{
                HttpStatusCode=mode==1||mode==5&&Reads==2?HttpStatusCode.BadGateway:HttpStatusCode.OK,
                Items=mode==2?null!:mode==3||mode==4?new():items,
                LastEvaluatedKey=mode==3||mode==5&&Reads==1?new(){{"id",new AttributeValue{S="next"}}}:new()});
        }
    }
    public static IEnumerable<object[]> Cases(){foreach(var kind in new[]{0,1,2,3,4,5})foreach(var mode in new[]{0,1,2,3,4,5})foreach(var sync in new[]{false,true})yield return new object[]{kind,mode,sync};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task EveryPageNeedsAcknowledgementAndProgressWithoutPartialSuccess(int kind,int mode,bool synchronous)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var probe=new Probe(mode);field.SetValue(provider,probe);
        try {
            void Check<T>(OASISResult<T> result){
                if(mode==4&&kind<4){Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);return;}
                Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
                if(mode==4)Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
                else {Assert.IsInstanceOfType<InvalidDataException>(result.Exception,result.Exception?.ToString());Assert.AreNotEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);}
            }
            if(kind==0)Check(synchronous?provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync());
            else if(kind==1)Check(synchronous?provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync());
            else if(kind==2)Check(synchronous?provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false));
            else if(kind==3)Check(synchronous?provider.LoadHolonsForParent(probe.Parent,loadChildren:false):await provider.LoadHolonsForParentAsync(probe.Parent,loadChildren:false));
            else if(kind==4)Check(synchronous?provider.LoadAvatarByUsername("selected"):await provider.LoadAvatarByUsernameAsync("selected"));
            else Check(synchronous?provider.LoadAvatarDetailByEmail("selected@example.invalid"):await provider.LoadAvatarDetailByEmailAsync("selected@example.invalid"));
            Assert.AreEqual(mode==3||mode==5?2:1,probe.Reads);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
