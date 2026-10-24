using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public sealed class DynamoSizeBoundaryNativeTests
{
    [DataTestMethod]
    [DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeAcceptsNearLimitPreparationWithoutArbitraryPadding(bool aws,bool unicode)
    {
        const string endpoint="http://127.0.0.1:18000";
        Assert.AreEqual(endpoint,Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        var journal=Path.Combine(work,$"size-boundary-{aws}-{unicode}.pending.json");Assert.IsFalse(File.Exists(journal));
        var authority=aws?ProviderType.AWSOASIS:ProviderType.DynamoDBOASIS;
        var actor=Guid.NewGuid();var operation=Guid.NewGuid();var record=Guid.NewGuid();
        var envelope="oasis-preparation1:"+(unicode?new string('é',200*1024-256):new string('x',400*1024-512));
        var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(envelope)));
        var key=authority+":"+operation.ToString("N");
        var provider=aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest",endpoint)
            :new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1",endpoint);
        using var verifier=new AmazonDynamoDBClient(new BasicAWSCredentials("ownedhistorytest","ownedhistorytest"),
            new AmazonDynamoDBConfig {ServiceURL=endpoint,AuthenticationRegion="us-east-1",MaxErrorRetry=0});
        async Task<Dictionary<string,AttributeValue>> Read(string table)
        {
            var response=await verifier.GetItemAsync(new GetItemRequest {TableName=table,Key=new(){{"id",new(){S=key}}},ConsistentRead=true});
            Assert.AreEqual(HttpStatusCode.OK,response.HttpStatusCode);return response.Item;
        }
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            Assert.AreEqual(0,(await Read("OasisHolonWritePreparations")).Count);Assert.AreEqual(0,(await Read("OasisHolonWriteReceipts")).Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
            {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{actor,operation,record,authority=authority.ToString(),envelopeSha256=hash,envelopeUtf8Bytes=Encoding.UTF8.GetByteCount(envelope),state="before-near-limit-claim"}));file.Flush(true);
            }
            // Opaque synthetic envelope: native transport sizing, not cipher authenticity.
            var candidate=new HolonWritePreparation(operation,actor,authority,new[]{new ProtectedHolonPreparation(record,envelope)});
            var result=await provider.ClaimHolonWritePreparationAsync(candidate);Assert.IsFalse(result.IsError,result.Message+result.Exception);
            Assert.AreEqual(envelope,result.Result.Records.Single().Envelope);
            var retry=provider.ClaimHolonWritePreparation(candidate);Assert.IsFalse(retry.IsError,retry.Message+retry.Exception);Assert.AreEqual(envelope,retry.Result.Records.Single().Envelope);
            var stored=await Read("OasisHolonWritePreparations");Assert.AreEqual(4,stored.Count);
            Assert.AreEqual(key,stored["id"].S);Assert.AreEqual(actor.ToString("D"),stored["actor"].S);Assert.AreEqual(authority.ToString(),stored["authority"].S);
            var fields=stored["records"].L.Single().M;Assert.AreEqual(2,fields.Count);Assert.AreEqual(record.ToString("D"),fields["id"].S);
            var storedHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fields["envelope"].S)));
            Assert.AreEqual(hash,storedHash);
            var removed=await verifier.DeleteItemAsync(new DeleteItemRequest {TableName="OasisHolonWritePreparations",Key=new(){{"id",new(){S=key}}},
                ConditionExpression="#actor=:actor AND #records=:records AND #authority=:authority",
                ExpressionAttributeNames=new(){{"#actor","actor"},{"#records","records"},{"#authority","authority"}},
                ExpressionAttributeValues=new(){{":actor",stored["actor"]},{":records",stored["records"]},{":authority",stored["authority"]}},ReturnValues=ReturnValue.ALL_OLD});
            Assert.AreEqual(HttpStatusCode.OK,removed.HttpStatusCode);Assert.AreEqual(envelope,removed.Attributes["records"].L.Single().M["envelope"].S);
            Assert.AreEqual(0,(await Read("OasisHolonWritePreparations")).Count);Assert.AreEqual(0,(await Read("OasisHolonWriteReceipts")).Count);
            File.Delete(journal);
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}
