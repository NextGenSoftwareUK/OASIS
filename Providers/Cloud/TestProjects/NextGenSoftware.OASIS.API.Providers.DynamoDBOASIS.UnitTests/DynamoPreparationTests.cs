using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoPreparationTests
{
    private sealed class Client:AmazonDynamoDBClient
    {
        public Dictionary<string,Dictionary<string,AttributeValue>> Rows=new();
        public int Reads,Writes;public bool LoseAck;
        public Client():base(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig {
            ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",MaxErrorRetry=0}){}
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken token=default) {
            Reads++;Rows.TryGetValue(request.TableName+"/"+request.Key["id"].S,out var item);
            return Task.FromResult(new GetItemResponse{HttpStatusCode=HttpStatusCode.OK,Item=item??new()});
        }
        public override Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken token=default) {
            Writes++;Assert.AreEqual(2,request.TransactItems.Count);
            var check=request.TransactItems[0].ConditionCheck;var put=request.TransactItems[1].Put;
            Assert.AreEqual("OasisHolonWriteReceipts",check.TableName);
            Assert.AreEqual("attribute_not_exists(#id)",check.ConditionExpression);
            Assert.AreEqual("attribute_not_exists(#id)",put.ConditionExpression);
            if(Rows.ContainsKey(check.TableName+"/"+check.Key["id"].S)||Rows.ContainsKey(put.TableName+"/"+put.Item["id"].S))
                throw new TransactionCanceledException("owned no-execution") {CancellationReasons=new(){new(){Code="ConditionalCheckFailed"}}};
            Rows.Add(put.TableName+"/"+put.Item["id"].S,put.Item);
            if(LoseAck)throw new IOException("owned committed ACK loss");
            return Task.FromResult(new TransactWriteItemsResponse{HttpStatusCode=HttpStatusCode.OK});
        }
    }
    private static HolonWritePreparation Candidate(Guid operation,Guid actor,string suffix="first",ProviderType authority=ProviderType.DynamoDBOASIS)
        =>new(operation,actor,authority,new[]{new ProtectedHolonPreparation(Guid.NewGuid(),"oasis-preparation1:"+suffix)});
    private static (DynamoDBOASIS Provider,AmazonDynamoDBClient Original) Setup(Client client) {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.NonPublic|BindingFlags.Instance)!;
        var old=(AmazonDynamoDBClient)field.GetValue(provider)!;field.SetValue(provider,client);return(provider,old);
    }
    [DataTestMethod]
    [DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeSizedPreparationIsNotRejectedByInventedPadding(bool sync,bool unicode)
    {
        using var client=new Client();var(provider,old)=Setup(client);using var owned=old;
        // Both payloads leave over 400 native bytes for this fixed S/L/M schema.
        // The former 1024+256/record padding rejected them without an SDK call.
        var suffix=unicode?new string('é',200*1024-256):new string('x',400*1024-512);
        var candidate=Candidate(Guid.NewGuid(),Guid.NewGuid(),suffix);
        var claimed=sync?provider.ClaimHolonWritePreparation(candidate):await provider.ClaimHolonWritePreparationAsync(candidate);
        Assert.IsFalse(claimed.IsError,claimed.Message+claimed.Exception);Assert.AreEqual(1,client.Writes);
        var recovered=await provider.ClaimHolonWritePreparationAsync(candidate);
        Assert.IsFalse(recovered.IsError,recovered.Message+recovered.Exception);Assert.AreEqual(candidate.Records[0].Envelope,recovered.Result.Records[0].Envelope);
        Assert.AreEqual(1,client.Writes);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task DefinitelyOversizedPreparationRejectsBeforeAnySdkRead(bool sync)
    {
        using var client=new Client();var(provider,old)=Setup(client);using var owned=old;
        var candidate=Candidate(Guid.NewGuid(),Guid.NewGuid(),new string('x',400*1024));
        var result=sync?provider.ClaimHolonWritePreparation(candidate):await provider.ClaimHolonWritePreparationAsync(candidate);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(0,client.Reads);Assert.AreEqual(0,client.Writes);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void SmallNativeNumbersDoNotAddThirtyEightPaddingBytes(bool negative)
    {
        var method=typeof(DynamoDBOASIS).GetMethod("MeasureGraphItem",BindingFlags.NonPublic|BindingFlags.Static)!;
        var item=new Dictionary<string,AttributeValue>{{"id",new(){S=Guid.NewGuid().ToString()}},{"data",new(){S=new string('x',400*1024-60)}},{"rank",new(){N=negative?"-1":"1"}}};
        var lower=(long)method.Invoke(null,new object[]{item})!;
        Assert.IsTrue(lower<=400L*1024);
        Assert.AreEqual(400L*1024-14,lower);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task FirstWinnerIsRetainedAndAnotherActorReceivesNoEnvelope(bool sync) {
        using var client=new Client();var(provider,old)=Setup(client);using var owned=old;
        var op=Guid.NewGuid();var actor=Guid.NewGuid();var candidate=Candidate(op,actor);
        var first=sync?provider.ClaimHolonWritePreparation(candidate):await provider.ClaimHolonWritePreparationAsync(candidate);
        Assert.IsFalse(first.IsError,first.Exception?.ToString());
        var receiptKey="OasisHolonWriteReceipts/DynamoDBOASIS:"+op.ToString("N");
        client.Rows.Add(receiptKey,new(){{"id",new(){S="DynamoDBOASIS:"+op.ToString("N")}}});
        var replay=await provider.ClaimHolonWritePreparationAsync(Candidate(op,actor,"different"));
        Assert.IsFalse(replay.IsError,replay.Exception?.ToString());
        Assert.AreEqual(candidate.Records[0].HolonId,replay.Result.Records[0].HolonId);
        Assert.AreEqual(candidate.Records[0].Envelope,replay.Result.Records[0].Envelope);
        var denied=await provider.ClaimHolonWritePreparationAsync(Candidate(op,Guid.NewGuid()));
        Assert.AreEqual("COSMIC_PREPARATION_ACTOR_MISMATCH",denied.ErrorCode);Assert.IsNull(denied.Result);
        Assert.AreEqual(1,client.Writes);
    }
    [TestMethod]
    public async Task ReceiptWithoutOriginalPreparationCannotClaimNewCiphertext() {
        using var client=new Client();var(provider,old)=Setup(client);using var owned=old;
        var candidate=Candidate(Guid.NewGuid(),Guid.NewGuid());var key="DynamoDBOASIS:"+candidate.OperationId.ToString("N");
        client.Rows.Add("OasisHolonWriteReceipts/"+key,new(){{"id",new(){S=key}}});
        var result=await provider.ClaimHolonWritePreparationAsync(candidate);
        Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",result.ErrorCode);
        Assert.IsInstanceOfType(result.Exception,typeof(INativeMutationReconciliationRequired));
        Assert.IsNull(result.Result);Assert.AreEqual(1,client.Rows.Count);Assert.AreEqual(1,client.Writes);
    }
    [TestMethod]
    public async Task LostClaimReturnRequiresRecoveryAndDoesNotResubmit() {
        using var client=new Client{LoseAck=true};var(provider,old)=Setup(client);using var owned=old;
        var candidate=Candidate(Guid.NewGuid(),Guid.NewGuid());
        var unknown=await provider.ClaimHolonWritePreparationAsync(candidate);
        Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",unknown.ErrorCode);
        Assert.IsInstanceOfType(unknown.Exception,typeof(INativeMutationReconciliationRequired));
        var recovered=await provider.ClaimHolonWritePreparationAsync(candidate);
        Assert.IsFalse(recovered.IsError,recovered.Exception?.ToString());Assert.AreEqual(1,client.Writes);
        Assert.AreEqual(candidate.Records[0].Envelope,recovered.Result.Records[0].Envelope);
    }
    [DataTestMethod][DataRow("authority")][DataRow("capacity")][DataRow("cancel")]
    public async Task AdmissionRejectsBeforeSdkRead(string kind) {
        using var client=new Client();var(provider,old)=Setup(client);using var owned=old;
        var candidate=Candidate(Guid.NewGuid(),Guid.NewGuid(),kind=="capacity"?new string('x',410*1024):"owned",
            kind=="authority"?ProviderType.AWSOASIS:ProviderType.DynamoDBOASIS);
        var result=await provider.ClaimHolonWritePreparationAsync(candidate,kind=="cancel"?new CancellationToken(true):default);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(0,client.Reads);Assert.AreEqual(0,client.Writes);
    }
    [TestMethod]
    public async Task StoredDuplicateRecordIdsAreNotAcceptedAsWinner() {
        using var client=new Client();var(provider,old)=Setup(client);using var owned=old;
        var candidate=Candidate(Guid.NewGuid(),Guid.NewGuid());
        Assert.IsFalse((await provider.ClaimHolonWritePreparationAsync(candidate)).IsError);
        var key="OasisHolonWritePreparations/DynamoDBOASIS:"+candidate.OperationId.ToString("N");
        var records=client.Rows[key]["records"].L;records.Add(records[0]);
        var rejected=await provider.ClaimHolonWritePreparationAsync(candidate);
        Assert.IsTrue(rejected.IsError);Assert.IsNull(rejected.Result);Assert.AreEqual(1,client.Writes);
    }
}
