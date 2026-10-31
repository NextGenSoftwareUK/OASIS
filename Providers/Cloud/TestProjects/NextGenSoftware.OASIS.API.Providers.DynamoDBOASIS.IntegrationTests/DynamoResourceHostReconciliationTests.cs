using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public sealed class DynamoResourceHostReconciliationTests
{
    private static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    [TestMethod][TestCategory("Reconciliation")]
    public async Task CleanupExactInspectedOriginalJoinedWriteFixture()
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        var inspectionPath=Path.Combine(work,"dynamo-resource-host-inspection-current.json");var inspectionBytes=await File.ReadAllBytesAsync(inspectionPath);
        Assert.AreEqual("82689F841D7517D54DDB36EE49C886DB0FDF66CE9F79A9763290D3CD6155D101",Hash(inspectionBytes));
        using var inspection=JsonDocument.Parse(inspectionBytes);var proof=inspection.RootElement;
        var path=Path.Combine(work,"dynamo-resource-host-write-False-False.pending.json");var originalBytes=await File.ReadAllBytesAsync(path);
        Assert.AreEqual(proof.GetProperty("JournalSha256").GetString(),Hash(originalBytes));
        var id=proof.GetProperty("NativeId").GetGuid();var operation=proof.GetProperty("OperationId").GetString()!;
        var uri=Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI")!;var url=new MongoUrl(uri);
        Assert.AreEqual("127.0.0.1",url.Server.Host);Assert.AreEqual(27029,url.Server.Port);Assert.AreEqual("oasisRecoveryRs",url.ReplicaSetName);
        var settings=MongoClientSettings.FromConnectionString(uri);settings.WriteConcern=WriteConcern.WMajority.With(journal:true);
        settings.ReadConcern=ReadConcern.Majority;settings.ReadPreference=ReadPreference.Primary;
        var collection=new MongoClient(settings).GetDatabase("oasis_sdk_resource_join_test").GetCollection<BsonDocument>("subscription_usage_outbox_v1");
        var original=await collection.Find(new BsonDocument("_id",operation)).SingleAsync();
        Assert.AreEqual(proof.GetProperty("MongoBsonSha256").GetString(),Hash(original.ToBson()));
        Assert.AreEqual(1L,await collection.CountDocumentsAsync(new BsonDocument("Service","DynamoResourceHostNativeTests")));
        using var verifier=DynamoHistoryNativeTests.Client();
        var native=await verifier.GetItemAsync(new(){TableName="OasisHolons",Key=new(){{"id",new(){S=id.ToString("D")}}},ConsistentRead=true});
        var json=native.Item["data_json"].S;Assert.AreEqual(proof.GetProperty("NativeJsonSha256").GetString(),Hash(Encoding.UTF8.GetBytes(json)));
        var record=OasisJson.DeserializeStored<Holon>(json);Assert.AreEqual(id,record.Id);
        Assert.AreEqual(proof.GetProperty("Marker").GetString(),record.Name);Assert.AreEqual(proof.GetProperty("UserId").GetString(),record.CreatedByAvatarId.ToString("D"));
        var intentPath=Path.Combine(work,"dynamo-resource-host-original-cleanup.pending.json");
        var intentBytes=JsonSerializer.SerializeToUtf8Bytes(new{OperationId=operation,NativeId=id,InspectionSha256=Hash(inspectionBytes),
            JournalSha256=Hash(originalBytes),NativeJsonSha256=Hash(Encoding.UTF8.GetBytes(json)),MongoBsonSha256=Hash(original.ToBson())});
        using(var file=new FileStream(intentPath,FileMode.CreateNew,FileAccess.Write,FileShare.None)){file.Write(intentBytes);file.Flush(true);}
        await DynamoHistoryNativeTests.RemoveOwned(verifier,"OasisHolons",id,json);
        Assert.AreEqual(1L,(await collection.DeleteOneAsync(original)).DeletedCount);
        Assert.AreEqual(0L,await collection.CountDocumentsAsync(new BsonDocument("_id",operation)));
        Assert.AreEqual(0L,await collection.CountDocumentsAsync(new BsonDocument("Service","DynamoResourceHostNativeTests")));
        var reportPath=Path.Combine(work,"dynamo-resource-host-original-cleanup-current.json");
        using(var file=new FileStream(reportPath,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new{OperationId=operation,NativeId=id,InspectionSha256=Hash(inspectionBytes),
                JournalSha256=Hash(originalBytes),NativeJsonSha256=Hash(Encoding.UTF8.GetBytes(json)),MongoBsonSha256=Hash(original.ToBson()),
                NativeConditionalDeleteAcknowledged=true,NativeIndependentAbsence=true,MongoExactBsonDeleteAcknowledged=true,MongoIndependentAbsence=true}));file.Flush(true);
        }
        CollectionAssert.AreEqual(originalBytes,await File.ReadAllBytesAsync(path));CollectionAssert.AreEqual(intentBytes,await File.ReadAllBytesAsync(intentPath));
        File.Delete(intentPath);File.Delete(path);
    }

    [TestMethod][TestCategory("Reconciliation")]
    public async Task InspectOriginalJoinedWriteWithoutMutationOrReplay()
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        var path=Path.Combine(work,"dynamo-resource-host-write-False-False.pending.json");
        var bytes=await File.ReadAllBytesAsync(path);using var pending=JsonDocument.Parse(bytes);var intent=pending.RootElement;
        Assert.AreEqual("oasis_sdk_resource_join_test",intent.GetProperty("Database").GetString());
        Assert.AreEqual("DynamoResourceHostNativeTests",intent.GetProperty("Service").GetString());
        var id=intent.GetProperty("NativeRecordId").GetGuid();var user=intent.GetProperty("UserId").GetString()!;
        var operation=intent.GetProperty("OperationId").GetString()!;var marker=intent.GetProperty("Marker").GetString();
        Assert.AreEqual("owned-resource-host-"+id.ToString("N"),marker);
        var uri=Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI")!;var url=new MongoUrl(uri);
        Assert.AreEqual("127.0.0.1",url.Server.Host);Assert.AreEqual(27029,url.Server.Port);Assert.AreEqual("oasisRecoveryRs",url.ReplicaSetName);
        var stored=await new MongoUsageOutboxStore(uri,"oasis_sdk_resource_join_test").GetAsync(operation,default);
        Assert.AreEqual(user,stored.UserId);Assert.AreEqual("DynamoResourceHostNativeTests",stored.Service);
        var document=await new MongoClient(uri).GetDatabase("oasis_sdk_resource_join_test").GetCollection<BsonDocument>("subscription_usage_outbox_v1")
            .Find(new BsonDocument("_id",operation)).SingleAsync();
        using var verifier=DynamoHistoryNativeTests.Client();
        var native=await verifier.GetItemAsync(new(){TableName="OasisHolons",Key=new(){{"id",new(){S=id.ToString("D")}}},ConsistentRead=true});
        var json=native.Item["data_json"].S;var record=OasisJson.DeserializeStored<Holon>(json);
        Assert.AreEqual(id,record.Id);Assert.AreEqual(marker,record.Name);Assert.AreEqual(Guid.Parse(user),record.CreatedByAvatarId);
        var report=new{JournalSha256=Hash(bytes),OperationId=operation,NativeId=id,Marker=marker,UserId=user,NativeVersion=record.Version,
            NativeJsonSha256=Hash(Encoding.UTF8.GetBytes(json)),MongoBsonSha256=Hash(document.ToBson()),State=stored.State,
            CaptureCount=stored.ProviderAttempts.Single().ResourceCaptureCount,
            Batches=stored.ResourceObservations.Select(batch=>new{batch.Sequence,batch.NativeOperation,Rows=batch.Observations})};
        var reportPath=Path.Combine(work,"dynamo-resource-host-inspection-current.json");
        using(var file=new FileStream(reportPath,FileMode.CreateNew,FileAccess.Write,FileShare.None)){file.Write(JsonSerializer.SerializeToUtf8Bytes(report));file.Flush(true);}
        CollectionAssert.AreEqual(bytes,await File.ReadAllBytesAsync(path));
    }
}
