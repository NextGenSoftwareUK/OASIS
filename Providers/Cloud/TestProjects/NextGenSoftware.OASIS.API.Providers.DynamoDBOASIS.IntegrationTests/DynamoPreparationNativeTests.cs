using System.Net;
using System.Reflection;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class DynamoPreparationNativeTests
{
    private sealed class Client:AmazonDynamoDBClient
    {
        public int Transactions;public bool LoseAck;
        public Client():base(new BasicAWSCredentials("ownedhistorytest","ownedhistorytest"),
            new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",MaxErrorRetry=0}){}
        public override async Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken token=default) {
            Transactions++;var response=await base.TransactWriteItemsAsync(request,token);
            if(LoseAck){LoseAck=false;throw new IOException("owned preparation ACK loss after native commit");}
            return response;
        }
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeFirstWinnerActorIsolationAckLossAndPriorReceiptGate(bool aws,bool loseAck)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!,$"preparation-{aws}-{loseAck}.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var authority=aws?ProviderType.AWSOASIS:ProviderType.DynamoDBOASIS;
        var actor=Guid.NewGuid();var operation=Guid.NewGuid();var blockedOperation=Guid.NewGuid();var record=Guid.NewGuid();
        // Storage deliberately treats envelopes as opaque. This is NOT runtime cipher authentication proof.
        HolonWritePreparation Candidate(Guid op,Guid owner,string suffix)=>new(op,owner,authority,
            new[]{new ProtectedHolonPreparation(record,"oasis-preparation1:owned-native-opaque-"+suffix)});
        var key=authority+":"+operation.ToString("N");var blockedKey=authority+":"+blockedOperation.ToString("N");
        DynamoDBOASIS provider=aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"):
            new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        using var client=new Client();using var verifier=new Client();
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        async Task<Dictionary<string,AttributeValue>> Read(string table,string id) {
            var response=await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new(){S=id}}},ConsistentRead=true});
            Assert.AreEqual(HttpStatusCode.OK,response.HttpStatusCode);return response.Item;
        }
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var id in new[]{key,blockedKey}) {
                Assert.AreEqual(0,(await Read("OasisHolonWritePreparations",id)).Count);
                Assert.AreEqual(0,(await Read("OasisHolonWriteReceipts",id)).Count);
            }
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{actor,operation,blockedOperation,record,authority=authority.ToString(),state="before-opaque-native-claim"}));file.Flush(true);
            }
            field.SetValue(provider,client);client.LoseAck=loseAck;var candidate=Candidate(operation,actor,"first");
            var first=await provider.ClaimHolonWritePreparationAsync(candidate);
            if(loseAck) {
                Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",first.ErrorCode);
                Assert.IsInstanceOfType<INativeMutationReconciliationRequired>(first.Exception);Assert.AreEqual(1,client.Transactions);
                first=await provider.ClaimHolonWritePreparationAsync(Candidate(operation,actor,"different-new-candidate"));
            }
            Assert.IsFalse(first.IsError,first.Exception?.ToString());Assert.AreEqual(candidate.Records[0].Envelope,first.Result.Records[0].Envelope);
            var replay=provider.ClaimHolonWritePreparation(Candidate(operation,actor,"must-not-overwrite"));
            Assert.IsFalse(replay.IsError,replay.Exception?.ToString());Assert.AreEqual(candidate.Records[0].Envelope,replay.Result.Records[0].Envelope);
            var denied=await provider.ClaimHolonWritePreparationAsync(Candidate(operation,Guid.NewGuid(),"other-actor"));
            Assert.AreEqual("COSMIC_PREPARATION_ACTOR_MISMATCH",denied.ErrorCode);Assert.IsNull(denied.Result);Assert.AreEqual(1,client.Transactions);
            var priorReceipt=new Dictionary<string,AttributeValue>{{"id",new(){S=blockedKey}},{"result_json",new(){S="owned-existing-receipt-gate"}}};
            var inserted=await verifier.PutItemAsync(new PutItemRequest{TableName="OasisHolonWriteReceipts",Item=priorReceipt,
                ConditionExpression="attribute_not_exists(#id)",ExpressionAttributeNames=new(){{"#id","id"}},ReturnValues=ReturnValue.ALL_OLD});
            Assert.AreEqual(HttpStatusCode.OK,inserted.HttpStatusCode);Assert.AreEqual(0,inserted.Attributes.Count);
            var blocked=await provider.ClaimHolonWritePreparationAsync(Candidate(blockedOperation,actor,"must-not-claim"));
            Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",blocked.ErrorCode);
            Assert.IsInstanceOfType<INativeMutationReconciliationRequired>(blocked.Exception);Assert.IsNull(blocked.Result);
            Assert.AreEqual(2,client.Transactions);Assert.AreEqual(0,(await Read("OasisHolonWritePreparations",blockedKey)).Count);
            var stored=await Read("OasisHolonWritePreparations",key);
            Assert.AreEqual(actor.ToString("D"),stored["actor"].S);Assert.AreEqual(authority.ToString(),stored["authority"].S);
            Assert.AreEqual(record.ToString("D"),stored["records"].L.Single().M["id"].S);
            Assert.AreEqual(candidate.Records[0].Envelope,stored["records"].L.Single().M["envelope"].S);
            var removed=await verifier.DeleteItemAsync(new DeleteItemRequest{TableName="OasisHolonWritePreparations",Key=new(){{"id",new(){S=key}}},
                ConditionExpression="#actor=:actor AND #records=:records",ExpressionAttributeNames=new(){{"#actor","actor"},{"#records","records"}},
                ExpressionAttributeValues=new(){{":actor",new(){S=actor.ToString("D")}},{":records",stored["records"]}},ReturnValues=ReturnValue.ALL_OLD});
            Assert.AreEqual(HttpStatusCode.OK,removed.HttpStatusCode);
            Assert.AreEqual(candidate.Records[0].Envelope,removed.Attributes["records"].L.Single().M["envelope"].S);
            var cleaned=await verifier.DeleteItemAsync(new DeleteItemRequest{TableName="OasisHolonWriteReceipts",Key=new(){{"id",new(){S=blockedKey}}},
                ConditionExpression="#json=:expected",ExpressionAttributeNames=new(){{"#json","result_json"}},
                ExpressionAttributeValues=new(){{":expected",new(){S="owned-existing-receipt-gate"}}},ReturnValues=ReturnValue.ALL_OLD});
            Assert.AreEqual(HttpStatusCode.OK,cleaned.HttpStatusCode);Assert.AreEqual("owned-existing-receipt-gate",cleaned.Attributes["result_json"].S);
            foreach(var id in new[]{key,blockedKey}) {
                Assert.AreEqual(0,(await Read("OasisHolonWritePreparations",id)).Count);
                Assert.AreEqual(0,(await Read("OasisHolonWriteReceipts",id)).Count);
            }
            File.Delete(journal);
        } finally {field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}
