using System.Reflection;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoIdentityTests
{
    private sealed class CorruptRead : AmazonDynamoDBClient
    {
        public bool WrongKey;
        public bool Malformed;
        public CorruptRead():base(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig {ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",UseHttp=true}) { }
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)
        {
            Assert.IsTrue(request.ConsistentRead);
            var requested=Guid.Parse(request.Key["id"].S);var other=Guid.NewGuid();
            var payload=WrongKey ? requested:other;
            var json=request.TableName=="OasisAvatars" ? OasisJson.Serialize(new Avatar {Id=payload,Username="owned-wrong-record"})
                :request.TableName=="OasisHolons" ? OasisJson.Serialize(new Holon {Id=payload,Name="owned-wrong-record"})
                :OasisJson.Serialize(new AvatarDetail {Id=payload,Username="owned-wrong-record"});
            if(Malformed) json="{ invalid canonical JSON";
            return Task.FromResult(new GetItemResponse {HttpStatusCode=System.Net.HttpStatusCode.OK,Item=new() {
                ["id"]=new() {S=(WrongKey ? other:requested).ToString()},["data_json"]=new() {S=json}}});
        }
        public override async Task<ScanResponse> ScanAsync(ScanRequest request,CancellationToken cancellationToken=default)
        {
            var row=await GetItemAsync(new GetItemRequest {TableName=request.TableName,ConsistentRead=true,Key=new() {["id"]=new() {S=Guid.NewGuid().ToString()}}});
            return new ScanResponse {Items=new() {row.Item}};
        }
    }

    [DataTestMethod]
    [DataRow(0,false,false)] [DataRow(0,true,false)] [DataRow(0,false,true)] [DataRow(0,true,true)]
    [DataRow(1,false,false)] [DataRow(1,true,false)] [DataRow(1,false,true)] [DataRow(1,true,true)]
    [DataRow(2,false,false)] [DataRow(2,true,false)] [DataRow(2,false,true)] [DataRow(2,true,true)]
    public async Task CollectionReadsRejectCorruptRecordsInsteadOfReturningOrSkippingThem(int kind,bool synchronous,bool malformed)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var probe=new CorruptRead {Malformed=malformed};field.SetValue(provider,probe);
        try
        {
            void Check<T>(OASISResult<T> result)
            {
                Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
                Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
                if(malformed) Assert.IsInstanceOfType(result.Exception.InnerException,typeof(Newtonsoft.Json.JsonException));
            }
            if(kind==0) Check(synchronous ? provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync());
            else if(kind==1) Check(synchronous ? provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync());
            else Check(synchronous ? provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false));
        }
        finally {field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
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
