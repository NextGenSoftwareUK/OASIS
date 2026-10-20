using System.Net;
using System.Reflection;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public sealed class DynamoIdentityLifecycleRaceTests
{
    private sealed class Selected(Guid id):DynamoDBOASIS("owned","owned","us-east-1")
    {
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string value,int version=0)=>Profile();
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string value,int version=0)=>Profile();
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string value,int version=0)=>Detail();
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string value,int version=0)=>Detail();
        private Task<OASISResult<IAvatar>> Profile()=>Task.FromResult(new OASISResult<IAvatar>{Result=new Avatar{Id=id,Username="owned-original",Email="owned-original@example.invalid"},IsLoaded=true});
        private Task<OASISResult<IAvatarDetail>> Detail()=>Task.FromResult(new OASISResult<IAvatarDetail>{Result=new AvatarDetail{Id=id,Username="owned-original",Email="owned-original@example.invalid"},IsLoaded=true});
    }
    private sealed class Probe(Guid id,bool detail):AmazonDynamoDBClient(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{RegionEndpoint=RegionEndpoint.USEast1,MaxErrorRetry=0})
    {
        public int Reads,Transactions;
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)
        {
            Reads++;
            var expected=detail?"OasisAvatarDetails":"OasisAvatars";
            if(request.TableName!=expected)return Task.FromResult(new GetItemResponse{HttpStatusCode=HttpStatusCode.OK,Item=new()});
            Assert.IsTrue(request.ConsistentRead);
            IHolonBase row=detail?new AvatarDetail{Id=id,Username="different-owner",Email="different-owner@example.invalid",IsActive=true}:new Avatar{Id=id,Username="different-owner",Email="different-owner@example.invalid",IsActive=true};
            return Task.FromResult(new GetItemResponse{HttpStatusCode=HttpStatusCode.OK,Item=new(){{"id",new(){S=id.ToString()}},{"username",new(){S="different-owner"}},{"email",new(){S="different-owner@example.invalid"}},{"data_json",new(){S=OasisJson.Serialize(row)}}}});
        }
        public override Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken cancellationToken=default)
        {Transactions++;return Task.FromResult(new TransactWriteItemsResponse{HttpStatusCode=HttpStatusCode.OK});}
    }
    public static IEnumerable<object[]> Cases(){foreach(var detail in new[]{false,true})foreach(var email in new[]{false,true})foreach(var soft in new[]{false,true})foreach(var sync in new[]{false,true})yield return new object[]{detail,email,soft,sync};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task ReassignedIdentityCannotDeleteCanonicalRecord(bool detail,bool email,bool soft,bool sync)
    {
        var id=Guid.NewGuid();var provider=new Selected(id);var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var probe=new Probe(id,detail);field.SetValue(provider,probe);provider.IsProviderActivated=true;
        try{
            var result=detail?email?sync?provider.DeleteAvatarDetailByEmail("owned-original@example.invalid",soft):await provider.DeleteAvatarDetailByEmailAsync("owned-original@example.invalid",soft)
                :sync?provider.DeleteAvatarDetailByUsername("owned-original",soft):await provider.DeleteAvatarDetailByUsernameAsync("owned-original",soft)
                :email?sync?provider.DeleteAvatarByEmail("owned-original@example.invalid",soft):await provider.DeleteAvatarByEmailAsync("owned-original@example.invalid",soft)
                :sync?provider.DeleteAvatarByUsername("owned-original",soft):await provider.DeleteAvatarByUsernameAsync("owned-original",soft);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
            Assert.IsInstanceOfType<StorageLifecycleConflictException>(result.Exception);Assert.AreEqual(1,probe.Reads);Assert.AreEqual(0,probe.Transactions);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
