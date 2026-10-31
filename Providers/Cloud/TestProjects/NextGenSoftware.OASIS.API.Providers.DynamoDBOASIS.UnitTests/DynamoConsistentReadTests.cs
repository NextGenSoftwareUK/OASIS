using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using System.Reflection;
namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;
[TestClass]
public class DynamoConsistentReadTests
{
    private sealed class Pages:AmazonDynamoDBClient
    {
        public int Reads;public readonly Guid Id=Guid.NewGuid();public readonly Guid Parent=Guid.NewGuid();
        public Pages():base(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig {ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",UseHttp=true}) {}
        public override Task<ScanResponse> ScanAsync(ScanRequest request,CancellationToken cancellationToken=default)
        {
            Assert.IsTrue(request.ConsistentRead,"Every page must request a strong base-table read.");Assert.IsTrue(string.IsNullOrEmpty(request.IndexName));Reads++;
            if(Reads==1) return Task.FromResult(new ScanResponse {HttpStatusCode=System.Net.HttpStatusCode.OK,Items=new(),LastEvaluatedKey=new() {["id"]=new() {S="owned-cursor"}}});
            Assert.AreEqual("owned-cursor",request.ExclusiveStartKey["id"].S);Assert.AreEqual(2,Reads);
            var json=request.TableName=="OasisAvatars" ? OasisJson.Serialize(new Avatar {Id=Id,IsActive=true})
                :request.TableName=="OasisAvatarDetails" ? OasisJson.Serialize(new AvatarDetail {Id=Id,IsActive=true})
                :OasisJson.Serialize(new Holon {Id=Id,ParentHolonId=Parent,IsActive=true});
            return Task.FromResult(new ScanResponse {HttpStatusCode=System.Net.HttpStatusCode.OK,Items=new() {new() {["id"]=new() {S=Id.ToString()},["data_json"]=new() {S=json}}}});
        }
        public override Task<QueryResponse> QueryAsync(QueryRequest request,CancellationToken cancellationToken=default)
            =>throw new AssertFailedException("A GSI cannot supply strongly consistent parent reads.");
    }
    [DataTestMethod]
    [DataRow(0,false)] [DataRow(0,true)] [DataRow(1,false)] [DataRow(1,true)]
    [DataRow(2,false)] [DataRow(2,true)] [DataRow(3,false)] [DataRow(3,true)]
    public async Task CollectionsUseStrongBaseTablePages(int kind,bool synchronous)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var pages=new Pages();field.SetValue(provider,pages);
        try
        {
            if(kind==0) {var r=synchronous ? provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync();Assert.IsFalse(r.IsError,r.Exception?.ToString());Assert.AreEqual(pages.Id,r.Result.Single().Id);}
            else if(kind==1) {var r=synchronous ? provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync();Assert.IsFalse(r.IsError,r.Exception?.ToString());Assert.AreEqual(pages.Id,r.Result.Single().Id);}
            else {var r=kind==2 ? synchronous ? provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false)
                :synchronous ? provider.LoadHolonsForParent(pages.Parent,loadChildren:false):await provider.LoadHolonsForParentAsync(pages.Parent,loadChildren:false);
                Assert.IsFalse(r.IsError,r.Exception?.ToString());Assert.AreEqual(pages.Id,r.Result.Single().Id);}
            Assert.AreEqual(2,pages.Reads);
        }
        finally {field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
