using System.Net;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public sealed class DynamoExportSelectorVersionTests
{
    private sealed class Probe:AmazonDynamoDBClient
    {
        internal int Reads;
        internal readonly Guid Owner=Guid.NewGuid(), Own=Guid.NewGuid(), Foreign=Guid.NewGuid();
        internal Probe():base(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:1",AuthenticationRegion="us-east-1",MaxErrorRetry=0}){}
        public override Task<ScanResponse> ScanAsync(ScanRequest request,CancellationToken token=default)
        {
            Assert.IsTrue(++Reads<20,"Bounded controlled scan count exceeded.");Assert.IsTrue(request.ConsistentRead);
            var records=request.TableName=="OasisAvatars"
                ?new IHolonBase[]{new Avatar{Id=Owner,Username="export-owner",Email="export@example.invalid",Version=1}}
                :new[]{new Holon{Id=Own,CreatedByAvatarId=Owner,Name="own-v3",Version=3},new Holon{Id=Foreign,CreatedByAvatarId=Guid.NewGuid(),Name="foreign-v3",Version=3}};
            return Task.FromResult(new ScanResponse{HttpStatusCode=HttpStatusCode.OK,Items=records.Select(row=>new Dictionary<string,AttributeValue>{{"id",new(){S=row.Id.ToString()}},{"data_json",new(){S=OasisJson.Serialize(row)}}}).ToList(),LastEvaluatedKey=new()});
        }
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken token=default)=>throw new InvalidOperationException("Unexpected point I/O in controlled export.");
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task ExportHolonVersionDoesNotRequireIdentityVersion(bool sync,bool email)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1","http://127.0.0.1:1");using var client=new Probe();
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;var original=field.GetValue(provider);field.SetValue(provider,client);
        try{
            var result=email
                ?sync?provider.ExportAllDataForAvatarByEmail("export@example.invalid",3):await provider.ExportAllDataForAvatarByEmailAsync("export@example.invalid",3)
                :sync?provider.ExportAllDataForAvatarByUsername("export-owner",3):await provider.ExportAllDataForAvatarByUsernameAsync("export-owner",3);
            Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);
            Assert.AreEqual(client.Own,result.Result.Single().Id);Assert.AreEqual(3,result.Result.Single().Version);Assert.AreEqual("own-v3",result.Result.Single().Name);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NegativeExportVersionFailsBeforeSdkReads(bool sync,bool email)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1","http://127.0.0.1:1");using var client=new Probe();
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;var original=field.GetValue(provider);field.SetValue(provider,client);
        try{
            var result=email
                ?sync?provider.ExportAllDataForAvatarByEmail("export@example.invalid",-1):await provider.ExportAllDataForAvatarByEmailAsync("export@example.invalid",-1)
                :sync?provider.ExportAllDataForAvatarByUsername("export-owner",-1):await provider.ExportAllDataForAvatarByUsernameAsync("export-owner",-1);
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);Assert.AreEqual(0,client.Reads);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
