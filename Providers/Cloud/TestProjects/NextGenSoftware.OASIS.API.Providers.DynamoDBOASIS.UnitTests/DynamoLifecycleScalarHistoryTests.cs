using System.Net;
using System.Reflection;
using System.Text.Json;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public sealed class DynamoLifecycleScalarHistoryTests
{
    private const string Date="2030-01-02T03:04:05.0000000+05:30",Number="9007199254740993.125";
    private sealed class Probe:AmazonDynamoDBClient
    {
        public readonly Dictionary<string,string> Originals=new();
        public readonly Dictionary<string,string> Updates=new();
        public Probe():base(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig{RegionEndpoint=RegionEndpoint.USEast1,MaxErrorRetry=0}){}
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken token=default)
        {
            Assert.IsTrue(request.ConsistentRead);var id=Guid.Parse(request.Key["id"].S);
            using var number=JsonDocument.Parse(Number);
            IHolonBase row=request.TableName=="OasisAvatars"?new Avatar{Id=id,IsActive=true,Version=1}:new AvatarDetail{Id=id,IsActive=true,Version=1};
            row.MetaData=new(){["literal-date"]=Date,["literal-number"]=number.RootElement.Clone()};
            var json=OasisJson.Serialize(row);Originals.Add(request.TableName,json);
            return Task.FromResult(new GetItemResponse{HttpStatusCode=HttpStatusCode.OK,Item=new(){
                ["id"]=new(){S=id.ToString()},["data_json"]=new(){S=json}
            }});
        }
        public override Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken token=default)
        {
            foreach(var item in request.TransactItems){
                Assert.IsNotNull(item.Update);var update=item.Update;
                Assert.AreEqual(Originals[update.TableName],update.ExpressionAttributeValues[":expected"].S);
                Updates.Add(update.TableName,update.ExpressionAttributeValues[":json"].S);
            }
            return Task.FromResult(new TransactWriteItemsResponse{HttpStatusCode=HttpStatusCode.OK});
        }
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task SoftDeletionPreservesExactCurrentAndArchivedMetadata(bool standaloneDetail,bool synchronous)
    {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1"){IsVersionControlEnabled=true};
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=field.GetValue(provider);using var probe=new Probe();field.SetValue(provider,probe);provider.IsProviderActivated=true;
        try{
            var id=Guid.NewGuid();
            var result=standaloneDetail?(synchronous?provider.DeleteAvatarDetail(id):await provider.DeleteAvatarDetailAsync(id)):
                (synchronous?provider.DeleteAvatar(id):await provider.DeleteAvatarAsync(id));
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.Result);
            Assert.AreEqual(standaloneDetail?1:2,probe.Updates.Count);
            var failures=new List<string>();
            foreach(var update in probe.Updates){
                using var json=JsonDocument.Parse(update.Value);var current=json.RootElement;
                Assert.AreEqual(2,current.GetProperty("Version").GetInt32());
                var archived=current.GetProperty(EmbeddedRecordHistory.PropertyName).GetProperty("records")[0];
                Assert.AreEqual(1,archived.GetProperty("Version").GetInt32());
                foreach(var row in new[]{current,archived}){
                    var meta=row.GetProperty("MetaData");
                    var tag=update.Key+"/v"+row.GetProperty("Version").GetInt32();
                    if(meta.GetProperty("literal-date").GetString()!=Date)failures.Add(tag+": literal date changed to "+meta.GetProperty("literal-date").GetString());
                    if(meta.GetProperty("literal-number").GetRawText()!=Number)failures.Add(tag+": precise number changed to "+meta.GetProperty("literal-number").GetRawText());
                }
            }
            Assert.AreEqual(0,failures.Count,string.Join(Environment.NewLine,failures));
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
