using System.Net;
using System.Reflection;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class DynamoEncryptedReplayNativeTests
{
    private sealed class Resolver:IHolonMetadataKeyResolver
    {
        public bool DenyOldPreparation;public List<HolonMetadataKeyRequest> Requests=new();
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request) {
            Requests.Add(request);
            if(request.KeyId is not ("key-a" or "key-b")
                ||DenyOldPreparation&&request.KeyId=="key-a"&&request.Purpose==HolonMetadataKeyPurpose.DecryptPreparation)
                return new(){IsError=true,ErrorCode="OWNED_KEY_DENIED"};
            return new(new HolonMetadataKeyMaterial(Enumerable.Repeat(request.KeyId=="key-a"?(byte)17:(byte)34,32).ToArray()));
        }
    }
    private sealed class Client:AmazonDynamoDBClient
    {
        public int Transactions;
        public Client():base(new BasicAWSCredentials("ownedhistorytest","ownedhistorytest"),
            new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",MaxErrorRetry=0}){}
        public override Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken token=default) {
            Interlocked.Increment(ref Transactions);return base.TransactWriteItemsAsync(request,token);
        }
    }
    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,true,false)][DataRow(true,false,false)][DataRow(true,true,false)]
    [DataRow(false,false,true)][DataRow(false,true,true)][DataRow(true,false,true)][DataRow(true,true,true)]
    public async Task NativeV2FreshLogicalReplayUsesOriginalProtectedPreparation(bool aws,bool synchronous,bool batch)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        var journal=Path.Combine(work,$"encrypted-replay-{aws}-{synchronous}-{batch}.pending.json");Assert.IsFalse(File.Exists(journal));
        var authority=aws?ProviderType.AWSOASIS:ProviderType.DynamoDBOASIS;
        var operation=Guid.NewGuid();var actor=Guid.NewGuid();
        var ids=Enumerable.Range(0,batch?4:2).Select(_=>Guid.NewGuid()).ToArray();
        var key=authority+":"+operation.ToString("N");
        const decimal exact=1.000000000000000000000000001m;
        HolonMetadataEncryptionPolicy Policy(Resolver resolver,string activeKey="key-a",string context="aws-root-context")
            =>new(context,activeKey,resolver,new[]{"search-tag"});
        Holon[] Inputs(Resolver resolver)=>Enumerable.Range(0,batch?2:1).Select(index=>new Holon {
            Id=ids[index*2],Name="owned encrypted root "+index,IsActive=true,
            MetaData=new(){{"private-note","root private value"},{"search-tag","public-search-tag"},{"exact",exact}},
            Children=new List<IHolon>{new Holon {
                Id=ids[index*2+1],ParentHolonId=ids[index*2],Name="owned encrypted child",IsActive=true,
                MetadataEncryptionOverride=Policy(resolver,"key-b","aws-child-context"),
                MetaData=new(){{"private-note","child private value"},{"exact",exact}}
            }}
        }).ToArray();
        DynamoDBOASIS Provider()=>aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"):
            new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        HolonManager Manager(DynamoDBOASIS provider,Resolver resolver,string activeKey="key-a") {
            var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}
            }};
            var providers=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
            return new HolonManager(provider,dna,providers){MetadataEncryptionPolicy=Policy(resolver,activeKey)};
        }
        async Task<OASISResult<IEnumerable<IHolon>>> Save(HolonManager manager,Holon[] roots) {
            var options=new CosmicWriteOptions(operation,ids.ToDictionary(id=>id,_=>(Guid?)null));
            if(batch)return synchronous?manager.SaveHolons(roots,actor,options,providerType:authority):
                await manager.SaveHolonsAsync(roots,actor,options,providerType:authority);
            var result=synchronous?manager.SaveHolon(roots[0],actor,options,providerType:authority):
                await manager.SaveHolonAsync(roots[0],actor,options,providerType:authority);
            return new(result.Result==null?null:new[]{result.Result}) {
                IsError=result.IsError,Message=result.Message,Exception=result.Exception,ErrorCode=result.ErrorCode
            };
        }
        using var verifier=new Client();
        async Task<Dictionary<string,AttributeValue>> Read(string table,string identity) {
            var result=await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new(){S=identity}}},ConsistentRead=true});
            Assert.AreEqual(HttpStatusCode.OK,result.HttpStatusCode);return result.Item;
        }
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var firstProvider=Provider();var firstOriginal=(AmazonDynamoDBClient)field.GetValue(firstProvider)!;
        using var firstClient=new Client();Guid[] versions;Guid?[] revisions;
        try {
            Assert.IsFalse((await firstProvider.ActivateProviderAsync()).IsError);
            foreach(var id in ids)Assert.AreEqual(0,(await Read("OasisHolons",id.ToString())).Count);
            foreach(var table in new[]{"OasisHolonWritePreparations","OasisHolonWriteReceipts"})Assert.AreEqual(0,(await Read(table,key)).Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,operation,actor,authority=authority.ToString(),state="before-encrypted-v2-save"}));file.Flush(true);
            }
            field.SetValue(firstProvider,firstClient);var resolver=new Resolver();var input=Inputs(resolver);
            var saved=await Save(Manager(firstProvider,resolver),input);Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            Assert.AreEqual(2,firstClient.Transactions,"One preparation claim and one graph transaction.");
            versions=input.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}).Select(row=>row.VersionId).ToArray();
            revisions=input.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}).Select(row=>CosmicConcurrency.GetProviderRevisionId(row,authority)).ToArray();
        } finally {field.SetValue(firstProvider,firstOriginal);await firstProvider.DeActivateProviderAsync();}
        var stored=new Dictionary<Guid,string>();
        foreach(var id in ids) {
            var json=(await Read("OasisHolons",id.ToString()))["data_json"].S;
            Assert.IsFalse(json.Contains("root private value")||json.Contains("child private value"));Assert.IsTrue(json.Contains("oasis2:"));
            stored.Add(id,json);
        }
        var preparation=await Read("OasisHolonWritePreparations",key);var envelopes=preparation["records"].L.Select(row=>row.M["envelope"].S).ToArray();
        Assert.AreEqual(ids.Length,envelopes.Length);
        foreach(var envelope in envelopes) {
            Assert.IsTrue(envelope.StartsWith("oasis-preparation1:"));
            Assert.IsFalse(envelope.Contains("root private value")||envelope.Contains("child private value"));
        }
        var receipt=(await Read("OasisHolonWriteReceipts",key))["result_json"].S;
        var restarted=Provider();var original=(AmazonDynamoDBClient)field.GetValue(restarted)!;using var restartedClient=new Client();
        try {
            Assert.IsFalse((await restarted.ActivateProviderAsync()).IsError);field.SetValue(restarted,restartedClient);
            var keys=new Resolver();var fresh=Inputs(keys);var retry=Manager(restarted,keys,"key-b");
            var changed=Inputs(keys);changed[0].MetaData["private-note"]="different logical private intent";
            var denied=await Save(retry,changed);Assert.IsTrue(denied.IsError);Assert.AreEqual(0,restartedClient.Transactions);
            var deniedKeys=new Resolver{DenyOldPreparation=true};
            var unauthorized=await Save(Manager(restarted,deniedKeys,"key-b"),Inputs(deniedKeys));
            Assert.IsTrue(unauthorized.IsError);Assert.AreEqual(0,restartedClient.Transactions);
            var replayed=await Save(retry,fresh);Assert.IsFalse(replayed.IsError,replayed.Message+replayed.Exception);
            CollectionAssert.AreEqual(versions,fresh.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}).Select(row=>row.VersionId).ToArray());
            CollectionAssert.AreEqual(revisions,fresh.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}).Select(row=>CosmicConcurrency.GetProviderRevisionId(row,authority)).ToArray());
            Assert.AreEqual(0,restartedClient.Transactions);
            Assert.IsTrue(keys.Requests.Any(request=>request.KeyId=="key-a"&&request.Purpose==HolonMetadataKeyPurpose.DecryptPreparation));
            foreach(var root in fresh) {
                Assert.AreEqual("root private value",root.MetaData["private-note"].ToString());
                Assert.AreEqual("child private value",root.Children.Single().MetaData["private-note"].ToString());
                using var json=JsonDocument.Parse(StoredRecordJson.Serialize(root.MetaData));
                Assert.AreEqual(exact,json.RootElement.GetProperty("exact").GetDecimal());
                Assert.IsFalse(HolonChangeTracking.HasChanges(root));
            }
            foreach(var id in ids)Assert.AreEqual(stored[id],(await Read("OasisHolons",id.ToString()))["data_json"].S);
            Assert.AreEqual(receipt,(await Read("OasisHolonWriteReceipts",key))["result_json"].S);
            CollectionAssert.AreEqual(envelopes,(await Read("OasisHolonWritePreparations",key))["records"].L.Select(row=>row.M["envelope"].S).ToArray());
            foreach(var id in ids)await Remove("OasisHolons",id.ToString(),"data_json",new(){S=stored[id]});
            await Remove("OasisHolonWriteReceipts",key,"result_json",new(){S=receipt});
            await Remove("OasisHolonWritePreparations",key,"records",preparation["records"]);
            File.Delete(journal);
        } finally {field.SetValue(restarted,original);await restarted.DeActivateProviderAsync();}

        async Task Remove(string table,string identity,string property,AttributeValue expected) {
            var ack=await verifier.DeleteItemAsync(new DeleteItemRequest{TableName=table,Key=new(){{"id",new(){S=identity}}},
                ConditionExpression="#value=:expected",ExpressionAttributeNames=new(){{"#value",property}},
                ExpressionAttributeValues=new(){{":expected",expected}},ReturnValues=ReturnValue.ALL_OLD});
            Assert.AreEqual(HttpStatusCode.OK,ack.HttpStatusCode);Assert.IsTrue(ack.Attributes.Count>0);
            Assert.AreEqual(0,(await Read(table,identity)).Count);
        }
    }
}
