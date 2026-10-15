using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private sealed class FixtureAuthority(string user) : IUsageReservationAuthority
    {
        public Task<SubscriptionAuthorizationResult> AuthorizeUsageAsync(string bearer,UsageAuthorizationRequest request,CancellationToken token)
            =>Task.FromResult(new SubscriptionAuthorizationResult {Allowed=true,OperationId=request.OperationId,UserId=user,Status="authorized",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(10)});
        public Task<UsageStartResult> StartUsageAsync(UsageStartRequest request,CancellationToken token)
            =>Task.FromResult(new UsageStartResult {Started=true,OperationId=request.OperationId});
        public Task<UsageSettlementResult> SettleUsageAsync(UsageSettlementRequest request,CancellationToken token)
            =>throw new NotSupportedException("This local fixture does not authorize settlement or fee valuation.");
    }

    private static async Task VerifyRealOutboxAsync(PolygonOASIS.PolygonOASIS provider,Web3CoreOASIS native)
    {
        var uri=Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI");
        Assert.IsTrue(uri?.StartsWith("mongodb://127.0.0.1:27029/",StringComparison.Ordinal)==true,"Owned existing Mongo replica required.");
        const string database="oasis_web3_prepared_native";
        var client=new MongoClient(uri);
        Assert.IsFalse((await (await client.ListDatabaseNamesAsync()).ToListAsync()).Contains(database),"Retained outbox evidence exists; refusing overwrite.");
        var verified=false;
        try
        {
            var store=new MongoUsageOutboxStore(uri,database);await store.InitializeAsync(default);
            var user=Guid.NewGuid().ToString();var options=new Web4UsageLedgerOptions {Service="WEB4",PersistenceTimeout=TimeSpan.FromSeconds(5)};
            var executor=new Web4UsageExecution(new FixtureAuthority(user),store,options);
            var policy=new UsageEndpointPolicy {MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=0,PricingCatalogueVersion="local-test"};
            var handle=await executor.BeginAsync("local-fixture-bearer",user,"web3-native-preparation",new string('a',64),"local native storage fixture",policy);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var field=typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",flags)!;var original=field.GetValue(provider);
            var address=(string)typeof(Web3CoreOASISBaseProvider).GetField("_contractAddress",flags)!.GetValue(provider)!;
            var id=Guid.NewGuid();var numeric=(uint)Core.Utilities.HashUtility.GetNumericHash(id.ToString());var beforeSubmissionChecks=0;
            await using(var proxy=new Web3AcceptedWriteProxy(async signed=>
            {
                var reopened=new MongoUsageOutboxStore(uri,database);
                var pending=await reopened.GetAsync(handle.OperationId,default);
                Assert.AreEqual(1,pending.ProviderAttempts.Count);
                var prepared=pending.ProviderAttempts.Single();
                var bytes=signed.HexToByteArray();
                Assert.AreEqual(Convert.ToBase64String(bytes),prepared.NativePreparation.SignedTransactionBase64);
                Assert.AreEqual("0x"+Convert.ToHexString(Nethereum.Util.Sha3Keccack.Current.CalculateHash(bytes)).ToLowerInvariant(),prepared.ProviderRequestId);
                Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await native.GetAvatarDetailByIdAsync(numeric)));
                beforeSubmissionChecks++;
            }))
            {
                var sdk=new Web3CoreOASIS(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY")!,"http://127.0.0.1:18546",address,Web3CoreOASISBaseProviderHelper.Abi,31337);
                field.SetValue(provider,sdk);
                try
                {
                    var result=await provider.ExecutePreparedStorageAsync(()=>provider.SaveAvatarDetailAsync(new AvatarDetail {Id=id,IsActive=true,XP=872}),handle,"owned-native-outbox",sdk.SigningAddress);
                    Assert.IsTrue(result.IsError);Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
                    Assert.AreEqual(1,beforeSubmissionChecks);Assert.AreEqual(1,proxy.AcceptedHashes.Count);
                    var reopened=new MongoUsageOutboxStore(uri,database);var persisted=await reopened.GetAsync(handle.OperationId,default);
                    var attempt=persisted.ProviderAttempts.Single();
                    Assert.AreEqual(proxy.AcceptedHashes.Single(),attempt.ProviderRequestId);
                    Assert.AreEqual(attempt.ProviderRequestId,((Web3MutationReconciliationException)result.Exception).OriginalTransactionId);
                    Assert.AreEqual("PolygonOASIS",attempt.Provider);Assert.AreEqual("owned-native-outbox.storage.1",attempt.ClientRequestId);
                    var entity=await native.GetAvatarDetailByIdAsync(numeric);
                    Assert.AreEqual(872L,provider.DecodePrivateEntity<AvatarDetail>("detail",entity).XP);
                    var calldata=new Nethereum.Web3.Web3("http://127.0.0.1:18545").Eth.GetContract(Web3CoreOASISBaseProviderHelper.Abi,address).GetFunction("upsertAvatarDetailChecked")
                        .GetData(numeric,Web3CoreOASISBaseProvider.PrivateExternalId(id),false,Nethereum.Util.Sha3Keccack.Current.CalculateHash(Array.Empty<byte>()),entity.Info);
                    Web3SignedPreparation.Verify(Convert.FromBase64String(attempt.NativePreparation.SignedTransactionBase64),attempt.ProviderRequestId,31337,sdk.SigningAddress,address,calldata.HexToByteArray());
                }
                finally {field.SetValue(provider,original);}
            }
            var rejectedHandle=await executor.BeginAsync("local-fixture-bearer",user,"web3-rejected-owner",new string('b',64),"local rejected owner fixture",policy);
            var beforeOwner=await store.GetAsync(rejectedHandle.OperationId,default);
            var operations=client.GetDatabase(database).GetCollection<UsageOutboxOperation>("subscription_usage_outbox_v1")
                .WithWriteConcern(WriteConcern.WMajority.With(journal:true));
            var changedOwner=Guid.NewGuid().ToString();
            var changed=await operations.UpdateOneAsync(row=>row.Id==rejectedHandle.OperationId && row.ExecutionOwner==beforeOwner.ExecutionOwner,
                Builders<UsageOutboxOperation>.Update.Set(row=>row.ExecutionOwner,changedOwner));
            Assert.AreEqual(1L,changed.MatchedCount);
            var deniedId=Guid.NewGuid();var deniedKey=(uint)Core.Utilities.HashUtility.GetNumericHash(deniedId.ToString());
            var sdkCurrent=(Web3CoreOASIS)field.GetValue(provider)!;
            var rpc=new Nethereum.Web3.Web3("http://127.0.0.1:18545");
            var nonceBefore=await rpc.Eth.Transactions.GetTransactionCount.SendRequestAsync(sdkCurrent.SigningAddress,Nethereum.RPC.Eth.DTOs.BlockParameter.CreatePending());
            var denied=await provider.ExecutePreparedStorageAsync(()=>provider.SaveAvatarDetailAsync(new AvatarDetail {Id=deniedId,IsActive=true,XP=999}),
                rejectedHandle,"rejected-owner",sdkCurrent.SigningAddress);
            Assert.IsTrue(denied.IsError);Assert.IsFalse(denied.IsSaved);Assert.IsNull(denied.Result);
            var ownershipError=denied.Exception as UsageProtocolException;Assert.IsNotNull(ownershipError);
            Assert.AreEqual("PROVIDER_ATTEMPT_ALREADY_STARTED",ownershipError.Code);
            Assert.IsFalse(Core.Helpers.NativeMutationReconciliation.IsRequired(ownershipError));
            Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await native.GetAvatarDetailByIdAsync(deniedKey)));
            Assert.AreEqual(nonceBefore.Value,(await rpc.Eth.Transactions.GetTransactionCount.SendRequestAsync(sdkCurrent.SigningAddress,Nethereum.RPC.Eth.DTOs.BlockParameter.CreatePending())).Value);
            var afterOwner=await new MongoUsageOutboxStore(uri,database).GetAsync(rejectedHandle.OperationId,default);
            Assert.AreEqual(changedOwner,afterOwner.ExecutionOwner);Assert.AreEqual(0,afterOwner.ProviderAttempts.Count);
            verified=true;
            Console.WriteLine("WEB3_REAL_OUTBOX_VERIFIED");
        }
        finally {if(verified) await client.DropDatabaseAsync(database);}
    }
}
