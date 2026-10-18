using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class CosmosReviewedRoutingRecoveryTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public async Task RecoverExactTimedOutReviewedRoutingWithoutNativeReplay()
    {
        var root=Environment.GetEnvironmentVariable("OASIS_HYPERDRIVE_TEST_ROOT")!;
        var journal=Path.Combine(root,"independent-routing-pending.json");
        string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        const string originalHash="6F155E2977FDB3004F6CA545EE42D2671DFCD1E957A368DBF8671A7C77315E6C";
        const string oldHash="9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81";
        Assert.Equal(originalHash,Hash(journal));Assert.Equal(oldHash,Hash(Path.Combine(root,"replication-pending.json")));
        using var intent=JsonDocument.Parse(File.ReadAllText(journal));
        const string operationId="006f10f8-427d-a81f-be9a-c1413e4a0815";
        Assert.Equal(operationId,intent.RootElement.GetProperty("operationId").GetString());
        var archive=Path.Combine(root,"reviewed-routing-timeout-recovery.json");Assert.False(File.Exists(archive));
        var resolved=Path.Combine(root,"reviewed-routing-timeout-intent-resolved.json");Assert.False(File.Exists(resolved));
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(180));var token=deadline.Token;
        var store=new MongoUsageOutboxStore(connection,"usage_cosmos_independent_routing");
        var before=await store.GetAsync(operationId,token);Assert.NotNull(before);Assert.Null(before.ObservedOutcome);Assert.Null(before.Settlement);
        var originalAttempts=JsonSerializer.Serialize(before.ProviderAttempts);var previous=before.Receipts.ToArray();
        Assert.Equal(10,before.ProviderAttempts.Count(attempt=>attempt.Provider=="CosmosBlockChainOASIS"));
        var data=intent.RootElement.GetProperty("review");
        var review=CosmosFeeModelReview.ForWasmd0703(data.GetProperty("ReviewId").GetString()!,data.GetProperty("ChainId").GetString()!,
            data.GetProperty("GenesisSha256").GetString()!,data.GetProperty("BinarySha256").GetString()!,data.GetProperty("FirstHeight").GetInt64(),data.GetProperty("LastHeight").GetInt64(),
            data.GetProperty("ReviewedAtUtc").GetDateTimeOffset(),data.GetProperty("ExpiresAtUtc").GetDateTimeOffset(),TimeSpan.Parse(data.GetProperty("MaximumObservationAge").GetString()!,System.Globalization.CultureInfo.InvariantCulture));
        using var cosmos=await CosmosBlockChainOASIS.CreateWithReviewedFinalFeesAsync(
            CosmosNativeFeeLimits.ForReviewedStandardSdkChain(new Dictionary<string,string>{{"stake","50000"}},review.ChainId),review,CosmosReviewedFinalFeeTests.InspectAsync,
            "http://127.0.0.1:26657",review.ChainId,contractAddress:intent.RootElement.GetProperty("contract").GetString()!,
            storageEncryptionKeyResolver:()=>SHA256.HashData(Encoding.UTF8.GetBytes("oasis-cosmos-local-storage-fixture-v1")),token:token);
        var reader=new RegisteredNativeFeeReader(ProviderType.CosmosBlockChainOASIS,review.ChainId,new IOASISProvider[]{cosmos});
        var resolver=new PreparedNativeUsageRecoveryResolver(store,new[]{reader});Assert.True(resolver.CanResolve(before));
        Assert.Null(await resolver.ResolveAsync(before,token));
        var after=await new MongoUsageOutboxStore(connection,"usage_cosmos_independent_routing").GetAsync(operationId,token);
        Assert.Equal(10,after.Receipts.Count);Assert.Equal(originalAttempts,JsonSerializer.Serialize(after.ProviderAttempts));
        Assert.Equal(before.NativeFeeExposureUsd,after.NativeFeeExposureUsd);Assert.Null(after.ObservedOutcome);Assert.Null(after.Settlement);
        foreach(var receipt in previous)Assert.Equal(JsonSerializer.Serialize(receipt),JsonSerializer.Serialize(Assert.Single(after.Receipts.Where(row=>row.ProviderRequestId==receipt.ProviderRequestId))));
        foreach(var receipt in after.Receipts){
            Assert.NotNull(receipt.NativeFeeAudit);receipt.NativeFeeAudit.Validate(receipt.Provider,receipt.ProviderRequestId,review.ChainId);
            using var audit=JsonDocument.Parse(receipt.NativeFeeAudit.PayloadJson);
            Assert.True(audit.RootElement.GetProperty("NativeExecutionSucceeded").GetBoolean());Assert.Equal(0,audit.RootElement.GetProperty("NativeExecutionCode").GetInt32());
        }
        var backend=(IKeyValueBackend)typeof(CosmosBlockChainOASIS).GetField("_privateStorage",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(cosmos)!;
        var avatarId=intent.RootElement.GetProperty("avatarId").GetGuid();var leafId=intent.RootElement.GetProperty("holonId").GetGuid();
        foreach(var key in new[]{"oasis/avatar/"+avatarId.ToString("N"),"oasis/avatardetail/"+avatarId.ToString("N"),"oasis/holon/"+leafId.ToString("N")})Assert.Null(await backend.GetAsync(key,token));
        var local=new LocalFileOASIS(storageDirectory:Path.Combine(root,"files"));
        try{Assert.Null((await local.LoadAvatarAsync(avatarId)).Result);Assert.Null((await local.LoadAvatarDetailAsync(avatarId)).Result);Assert.Null((await local.LoadHolonAsync(leafId,loadChildren:false)).Result);}
        finally{await local.DeActivateProviderAsync();}
        Assert.False(resolver.CanResolve(after));Assert.False(cosmos.IsProviderActivated);Assert.Equal(oldHash,Hash(Path.Combine(root,"replication-pending.json")));
        var evidence=JsonSerializer.Serialize(new{originalIntent=intent.RootElement,operation=after,previousReceiptCount=previous.Length,
            newReceiptCount=10-previous.Length,nativeSubmissions=0,originalAttemptsUnchanged=true,ownedRecordsAbsent=true,
            originalLogicalTestSucceeded=false,logicalOutcomeResolved=false,settlementAuthorized=false});
        using(var file=new FileStream(archive,FileMode.CreateNew,FileAccess.Write,FileShare.Read)){await file.WriteAsync(Encoding.UTF8.GetBytes(evidence),token);file.Flush(true);}
        Assert.Equal(originalHash,Hash(journal));File.Move(journal,resolved);
        output.WriteLine("Exact original fees recovered without a signing key or native replay; archive SHA256="+Hash(archive));
    }
}
