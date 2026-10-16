using System.Reflection;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoIdentityTests
{
    private sealed class CorruptRead : AmazonDynamoDBClient
    {
        public bool WrongKey;
        public CorruptRead():base(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig {ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",UseHttp=true}) { }
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)
        {
            Assert.IsTrue(request.ConsistentRead);
            var requested=Guid.Parse(request.Key["id"].S);var other=Guid.NewGuid();
            var payload=WrongKey ? requested:other;
            var json=request.TableName=="OasisAvatars" ? OasisJson.Serialize(new Avatar {Id=payload,Username="owned-wrong-record"})
                :OasisJson.Serialize(new AvatarDetail {Id=payload,Username="owned-wrong-record"});
            return Task.FromResult(new GetItemResponse {HttpStatusCode=System.Net.HttpStatusCode.OK,Item=new() {
                ["id"]=new() {S=(WrongKey ? other:requested).ToString()},["data_json"]=new() {S=json}}});
        }
    }

    [DataTestMethod]
    [DataRow(false,false,false)] [DataRow(false,true,false)]
    [DataRow(true,false,false)] [DataRow(true,true,false)]
    [DataRow(false,false,true)] [DataRow(false,true,true)]
    [DataRow(true,false,true)] [DataRow(true,true,true)]
    public async Task PointReadRejectsMismatchedNativeOrCanonicalIdentity(bool detail,bool synchronous,bool wrongKey)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var probe=new CorruptRead {WrongKey=wrongKey};field.SetValue(provider,probe);
        try
        {
            var id=Guid.NewGuid();
            if(detail)
            {
                var result=synchronous ? provider.LoadAvatarDetail(id):await provider.LoadAvatarDetailAsync(id);
                Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
                Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
            }
            else
            {
                var result=synchronous ? provider.LoadAvatar(id):await provider.LoadAvatarAsync(id);
                Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
                Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
            }
        }
        finally {field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
