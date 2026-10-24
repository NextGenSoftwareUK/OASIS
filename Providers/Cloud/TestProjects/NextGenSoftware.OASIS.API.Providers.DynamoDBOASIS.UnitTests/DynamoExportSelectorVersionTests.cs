using System.Net;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Newtonsoft.Json.Linq;
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
        internal int ArchiveMode=-1;
        internal readonly Guid Owner=Guid.NewGuid(), Own=Guid.NewGuid(), Foreign=Guid.NewGuid();
        internal Probe():base(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:1",AuthenticationRegion="us-east-1",MaxErrorRetry=0}){}
        public override Task<ScanResponse> ScanAsync(ScanRequest request,CancellationToken token=default)
        {
            Assert.IsTrue(++Reads<20,"Bounded controlled scan count exceeded.");Assert.IsTrue(request.ConsistentRead);
            var records=request.TableName=="OasisAvatars"
                ?new IHolonBase[]{new Avatar{Id=Owner,Username="export-owner",Email="export@example.invalid",Version=1}}
                :new[]{new Holon{Id=Own,CreatedByAvatarId=Owner,Name="own-v3",Version=3},new Holon{Id=Foreign,CreatedByAvatarId=Guid.NewGuid(),Name="foreign-v3",Version=3}};
            var items=records.Select(row=>{
                if(ArchiveMode==3&&row is Holon)row.DeletedDate=DateTime.UtcNow;
                var document=OasisJson.ParseStoredObject(OasisJson.Serialize(row));
                if(ArchiveMode>=0&&row is Holon){
                    var archived=new Holon{Id=row.Id,CreatedByAvatarId=row.CreatedByAvatarId,Name="archived",Version=1};
                    var entries=new JArray(OasisJson.ParseStoredObject(OasisJson.Serialize(archived)));
                    if(ArchiveMode==2)entries.Add(entries[0]!.DeepClone());
                    document[EmbeddedRecordHistory.PropertyName]=new JObject{{"schema",1},{"records",entries}};
                }
                return new Dictionary<string,AttributeValue>{{"id",new(){S=row.Id.ToString()}},{"data_json",new(){S=document.ToString(Newtonsoft.Json.Formatting.None)}}};
            }).ToList();
            return Task.FromResult(new ScanResponse{HttpStatusCode=HttpStatusCode.OK,Items=items,LastEvaluatedKey=new()});
        }
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken token=default)=>throw new InvalidOperationException("Unexpected point I/O in controlled export.");
    }

    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)]
    [DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
    public async Task AllVersionExportsValidateArchiveAndFilterOwnerPerVersion(int mode,bool sync)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1","http://127.0.0.1:1");using var client=new Probe{ArchiveMode=mode};
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;var original=field.GetValue(provider);field.SetValue(provider,client);
        try{
            var result=mode==1
                ?sync?provider.ExportAllDataForAvatarById(client.Owner,-1):await provider.ExportAllDataForAvatarByIdAsync(client.Owner,-1)
                :sync?provider.ExportAll(-1):await provider.ExportAllAsync(-1);
            if(mode==2){Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);Assert.IsInstanceOfType<InvalidDataException>(result.Exception);return;}
            Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);
            var rows=result.Result.ToArray();Assert.AreEqual(mode==0?4:2,rows.Length);Assert.AreEqual(rows.Length,result.LoadedCount);
            Assert.IsTrue(rows.All(row=>!row.IsDeleted));
            if(mode==1)Assert.IsTrue(rows.All(row=>row.Id==client.Own&&row.CreatedByAvatarId==client.Owner));
            var own=rows.Where(row=>row.Id==client.Own).ToArray();Assert.AreEqual(1,own[0].Version);
            if(mode!=3)Assert.AreEqual(3,own[1].Version);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod][DataRow(false,false,3)][DataRow(false,true,3)][DataRow(true,false,3)][DataRow(true,true,3)]
    [DataRow(false,false,-1)][DataRow(false,true,-1)][DataRow(true,false,-1)][DataRow(true,true,-1)]
    public async Task ExportHolonVersionDoesNotRequireIdentityVersion(bool sync,bool email,int version)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1","http://127.0.0.1:1");using var client=new Probe();
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;var original=field.GetValue(provider);field.SetValue(provider,client);
        try{
            var result=email
                ?sync?provider.ExportAllDataForAvatarByEmail("export@example.invalid",version):await provider.ExportAllDataForAvatarByEmailAsync("export@example.invalid",version)
                :sync?provider.ExportAllDataForAvatarByUsername("export-owner",version):await provider.ExportAllDataForAvatarByUsernameAsync("export-owner",version);
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
                ?sync?provider.ExportAllDataForAvatarByEmail("export@example.invalid",-2):await provider.ExportAllDataForAvatarByEmailAsync("export@example.invalid",-2)
                :sync?provider.ExportAllDataForAvatarByUsername("export-owner",-2):await provider.ExportAllDataForAvatarByUsernameAsync("export-owner",-2);
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);Assert.AreEqual(0,client.Reads);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
