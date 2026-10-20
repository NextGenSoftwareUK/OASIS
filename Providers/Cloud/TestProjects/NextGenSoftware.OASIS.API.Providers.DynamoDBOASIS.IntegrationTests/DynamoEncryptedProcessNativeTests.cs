using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class DynamoEncryptedProcessNativeTests
{
    private sealed class Resolver:IHolonMetadataKeyResolver
    {
        public bool UsedOldPreparation;
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request) {
            if(request.KeyId is not ("key-a" or "key-b"))return new(){IsError=true};
            if(request.KeyId=="key-a"&&request.Purpose==HolonMetadataKeyPurpose.DecryptPreparation)UsedOldPreparation=true;
            return new(new HolonMetadataKeyMaterial(Enumerable.Repeat(request.KeyId=="key-a"?(byte)17:(byte)34,32).ToArray()));
        }
    }
    private sealed class Client:AmazonDynamoDBClient
    {
        public int Transactions;
        public Client():base(new BasicAWSCredentials("ownedhistorytest","ownedhistorytest"),new AmazonDynamoDBConfig {
            ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",MaxErrorRetry=0}){}
        public override Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken token=default) {
            Transactions++;return base.TransactWriteItemsAsync(request,token);
        }
    }
    private sealed record Intent(Guid Operation,Guid Actor,Guid Root,Guid Child,int WriterProcess);
    private sealed record Proof(Guid RootVersion,Guid ChildVersion,Guid? RootRevision,Guid? ChildRevision,
        string RootHash,string ChildHash,string ReceiptHash,string[] Envelopes);
    private static string Work=>Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
    private static string Journal(bool aws)=>Path.Combine(Work,$"encrypted-process-{aws}.pending.json");
    private static string ProofPath(bool aws)=>Path.Combine(Work,$"encrypted-process-{aws}.written.json");
    private static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static async Task Persist<T>(string path,T value) {
        using var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);
        await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(value));file.Flush(true);
    }
    private static DynamoDBOASIS Provider(bool aws)=>aws?
        new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"):
        new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
    private static Holon Input(Intent intent)=>new(){Id=intent.Root,Name="owned process root",IsActive=true,
        MetaData=new(){{"private-note","owned process root secret"}},
        Children=new List<IHolon>{new Holon{Id=intent.Child,ParentHolonId=intent.Root,Name="owned process child",IsActive=true,
            MetaData=new(){{"private-note","owned process child secret"}}}}};
    private static HolonManager Manager(DynamoDBOASIS provider,Resolver keys,string active) {
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}
        }};
        var providers=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
        return new HolonManager(provider,dna,providers){MetadataEncryptionPolicy=new HolonMetadataEncryptionPolicy("aws-process-context",active,keys,Array.Empty<string>())};
    }
    private static async Task<Dictionary<string,AttributeValue>> Read(Client client,string table,string id) {
        var response=await client.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new(){S=id}}},ConsistentRead=true});
        Assert.AreEqual(HttpStatusCode.OK,response.HttpStatusCode);return response.Item;
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task PhaseOneCommitEncryptedOperation(bool aws) {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        Assert.IsFalse(File.Exists(Journal(aws)));Assert.IsFalse(File.Exists(ProofPath(aws)));
        var authority=aws?ProviderType.AWSOASIS:ProviderType.DynamoDBOASIS;
        var intent=new Intent(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Environment.ProcessId);
        var key=authority+":"+intent.Operation.ToString("N");var provider=Provider(aws);
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var client=new Client();
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);field.SetValue(provider,client);
            foreach(var id in new[]{intent.Root,intent.Child})Assert.AreEqual(0,(await Read(client,"OasisHolons",id.ToString())).Count);
            foreach(var table in new[]{"OasisHolonWritePreparations","OasisHolonWriteReceipts"})Assert.AreEqual(0,(await Read(client,table,key)).Count);
            await Persist(Journal(aws),intent);var input=Input(intent);
            var saved=await Manager(provider,new Resolver(),"key-a").SaveHolonAsync(input,intent.Actor,
                new CosmicWriteOptions(intent.Operation,new Dictionary<Guid,Guid?>{{intent.Root,null},{intent.Child,null}}),providerType:authority);
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.AreEqual(2,client.Transactions);
            var root=(await Read(client,"OasisHolons",intent.Root.ToString()))["data_json"].S;
            var child=(await Read(client,"OasisHolons",intent.Child.ToString()))["data_json"].S;
            Assert.IsFalse(root.Contains("owned process root secret"));Assert.IsFalse(child.Contains("owned process child secret"));
            await Persist(ProofPath(aws),new Proof(input.VersionId,input.Children.Single().VersionId,
                CosmicConcurrency.GetProviderRevisionId(input,authority),CosmicConcurrency.GetProviderRevisionId(input.Children.Single(),authority),
                Hash(root),Hash(child),Hash((await Read(client,"OasisHolonWriteReceipts",key))["result_json"].S),
                (await Read(client,"OasisHolonWritePreparations",key))["records"].L.Select(row=>row.M["envelope"].S).ToArray()));
        } finally {field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task PhaseTwoRecoverInDifferentProcess(bool aws) {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        // Runner checks a complete fresh PhaseOne TRX before launching this separate testhost process.
        var intent=JsonSerializer.Deserialize<Intent>(await File.ReadAllTextAsync(Journal(aws)))!;
        var proof=JsonSerializer.Deserialize<Proof>(await File.ReadAllTextAsync(ProofPath(aws)))!;
        Assert.AreNotEqual(intent.WriterProcess,Environment.ProcessId,"A genuinely different test process is required.");
        var authority=aws?ProviderType.AWSOASIS:ProviderType.DynamoDBOASIS;var key=authority+":"+intent.Operation.ToString("N");
        var provider=Provider(aws);var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var client=new Client();
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);field.SetValue(provider,client);
            var input=Input(intent);var keys=new Resolver();
            var replay=await Manager(provider,keys,"key-b").SaveHolonAsync(input,intent.Actor,
                new CosmicWriteOptions(intent.Operation,new Dictionary<Guid,Guid?>{{intent.Root,null},{intent.Child,null}}),providerType:authority);
            Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);Assert.AreEqual(0,client.Transactions);Assert.IsTrue(keys.UsedOldPreparation);
            Assert.AreEqual(proof.RootVersion,input.VersionId);Assert.AreEqual(proof.ChildVersion,input.Children.Single().VersionId);
            Assert.AreEqual(proof.RootRevision,CosmicConcurrency.GetProviderRevisionId(input,authority));
            Assert.AreEqual(proof.ChildRevision,CosmicConcurrency.GetProviderRevisionId(input.Children.Single(),authority));
            Assert.AreEqual("owned process root secret",input.MetaData["private-note"].ToString());
            Assert.AreEqual("owned process child secret",input.Children.Single().MetaData["private-note"].ToString());
            var root=await Read(client,"OasisHolons",intent.Root.ToString());var child=await Read(client,"OasisHolons",intent.Child.ToString());
            var receipt=await Read(client,"OasisHolonWriteReceipts",key);var preparation=await Read(client,"OasisHolonWritePreparations",key);
            Assert.AreEqual(proof.RootHash,Hash(root["data_json"].S));Assert.AreEqual(proof.ChildHash,Hash(child["data_json"].S));
            Assert.AreEqual(proof.ReceiptHash,Hash(receipt["result_json"].S));
            CollectionAssert.AreEqual(proof.Envelopes,preparation["records"].L.Select(row=>row.M["envelope"].S).ToArray());
            Assert.AreEqual(intent.Actor.ToString("D"),preparation["actor"].S);
            await Remove("OasisHolons",intent.Root.ToString(),"data_json",root["data_json"]);
            await Remove("OasisHolons",intent.Child.ToString(),"data_json",child["data_json"]);
            await Remove("OasisHolonWriteReceipts",key,"result_json",receipt["result_json"]);
            await Remove("OasisHolonWritePreparations",key,"records",preparation["records"]);
            await Persist(Path.Combine(Work,$"encrypted-process-{aws}.verified.json"),new {
                intent.Operation,writerProcess=intent.WriterProcess,readerProcess=Environment.ProcessId,verifiedUtc=DateTime.UtcNow,
                proofHash=Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(ProofPath(aws))))
            });
            File.Delete(Journal(aws));
        } finally {field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
        async Task Remove(string table,string id,string property,AttributeValue expected) {
            var response=await client.DeleteItemAsync(new DeleteItemRequest{TableName=table,Key=new(){{"id",new(){S=id}}},
                ConditionExpression="#value=:expected",ExpressionAttributeNames=new(){{"#value",property}},
                ExpressionAttributeValues=new(){{":expected",expected}},ReturnValues=ReturnValue.ALL_OLD});
            Assert.AreEqual(HttpStatusCode.OK,response.HttpStatusCode);Assert.IsTrue(response.Attributes.Count>0);
            Assert.AreEqual(0,(await Read(client,table,id)).Count);
        }
    }
}
