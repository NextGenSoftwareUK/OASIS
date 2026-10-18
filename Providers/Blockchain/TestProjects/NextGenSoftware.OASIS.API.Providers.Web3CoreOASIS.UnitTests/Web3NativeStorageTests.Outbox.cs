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
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings(),
                SubscriptionConfig=new NextGenSoftware.OASIS.API.DNA.SubscriptionConfig {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna);manager.RegisterProvider(provider);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
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
                    var preparation=new PreparedStorageExecution(handle,"owned-native-outbox",new Dictionary<Core.Enums.ProviderType,string> {{Core.Enums.ProviderType.PolygonOASIS,sdk.SigningAddress}},Array.Empty<Core.Enums.ProviderType>());
                    var request=new Core.StorageOperationRequest {Operation="SaveAvatarDetail",PreferredProvider=Core.Enums.ProviderType.PolygonOASIS,
                        Payload=new AvatarDetail {Id=id,IsActive=true,XP=872},PreparedExecution=preparation};
                    Assert.IsFalse(Newtonsoft.Json.JsonConvert.SerializeObject(request).Contains("PreparedExecution"));
                    Assert.IsFalse(System.Text.Json.JsonSerializer.Serialize(request).Contains("PreparedExecution"));
                    Assert.IsNull(Newtonsoft.Json.JsonConvert.DeserializeObject<Core.StorageOperationRequest>("{\"PreparedExecution\":{}}")!.PreparedExecution);
                    var result=await router.RouteRequestAsync<Core.Interfaces.IAvatarDetail>(request);
                    Assert.IsTrue(result.IsError);Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
                    Assert.AreEqual(1,beforeSubmissionChecks);Assert.AreEqual(1,proxy.AcceptedHashes.Count);
                    var reopened=new MongoUsageOutboxStore(uri,database);var persisted=await reopened.GetAsync(handle.OperationId,default);
                    var attempt=persisted.ProviderAttempts.Single();
                    Assert.AreEqual(proxy.AcceptedHashes.Single(),attempt.ProviderRequestId);
                    Assert.AreEqual(attempt.ProviderRequestId,((Web3MutationReconciliationException)result.Exception).OriginalTransactionId);
                    Assert.AreEqual("PolygonOASIS",attempt.Provider);Assert.AreEqual("owned-native-outbox.route.1.storage.1",attempt.ClientRequestId);
                    var entity=await native.GetAvatarDetailByIdAsync(numeric);
                    Assert.AreEqual(872L,provider.DecodePrivateEntity<AvatarDetail>("detail",entity).XP);
                    var calldata=new Nethereum.Web3.Web3("http://127.0.0.1:18545").Eth.GetContract(Web3CoreOASISBaseProviderHelper.Abi,address).GetFunction("upsertAvatarDetailChecked")
                        .GetData(numeric,Web3CoreOASISBaseProvider.PrivateExternalId(id),false,Nethereum.Util.Sha3Keccack.Current.CalculateHash(Array.Empty<byte>()),entity.Info);
                    Web3SignedPreparation.Verify(Convert.FromBase64String(attempt.NativePreparation.SignedTransactionBase64),attempt.ProviderRequestId,31337,sdk.SigningAddress,address,calldata.HexToByteArray());
                    var acceptedBeforeRecovery=proxy.AcceptedHashes.Count;
                    var recovered=await provider.GetPreparedNativeReceiptAsync(attempt,calldata);
                    Assert.IsFalse(recovered.IsError,recovered.Message);Assert.IsTrue(recovered.IsLoaded);
                    Assert.AreEqual(attempt.ProviderRequestId,recovered.Result.TransactionHash);Assert.AreEqual(System.Numerics.BigInteger.One,recovered.Result.Status.Value);
                    Assert.AreEqual(acceptedBeforeRecovery,proxy.AcceptedHashes.Count,"Read-only recovery must not resubmit.");
                    var methodsBeforeInvalid=proxy.Methods.Count;
                    Assert.IsInstanceOfType((await provider.GetPreparedNativeReceiptAsync(attempt,"0x00")).Exception,typeof(InvalidDataException));
                    var originalHash=attempt.ProviderRequestId;attempt.ProviderRequestId="0xwrong";
                    Assert.IsInstanceOfType((await provider.GetPreparedNativeReceiptAsync(attempt,calldata)).Exception,typeof(InvalidDataException));
                    attempt.ProviderRequestId=originalHash;
                    Assert.AreEqual(methodsBeforeInvalid,proxy.Methods.Count,"Invalid retained evidence must fail before RPC.");
                    Assert.AreEqual(1,(await reopened.GetAsync(handle.OperationId,default)).ProviderAttempts.Count);
                    await VerifyFreshRecoveryProcessAsync(handle.OperationId,address,calldata);
                    Assert.AreEqual(acceptedBeforeRecovery,proxy.AcceptedHashes.Count);
                    var methodsBeforeDenied=proxy.Methods.Count;
                    request.Payload=new AvatarDetail {Id=Guid.NewGuid(),IsActive=true,XP=999};
                    request.PreparedExecution=new PreparedStorageExecution(handle,"unlisted-provider",new Dictionary<Core.Enums.ProviderType,string>(),Array.Empty<Core.Enums.ProviderType>());
                    manager.IsAutoFailOverEnabled=true;
                    var deniedRoute=router.RouteRequest<Core.Interfaces.IAvatarDetail>(request);
                    Assert.IsTrue(deniedRoute.IsError);Assert.AreEqual(PreparedStorageExecution.RejectedCode,deniedRoute.ErrorCode);
                    Assert.AreEqual(methodsBeforeDenied,proxy.Methods.Count,"Unlisted provider must be rejected before native IO.");
                    var methodsBeforeWrongChain=proxy.Methods.Count;
                    var receiptsBeforeWrongChain=proxy.Methods.Count(method=>method=="eth_getTransactionReceipt");
                    proxy.ReportedChainId="0x1";
                    try
                    {
                        var wrongChain=await provider.GetPreparedNativeReceiptAsync(attempt,calldata);
                        Assert.IsTrue(wrongChain.IsError);Assert.IsInstanceOfType(wrongChain.Exception,typeof(InvalidDataException));
                        Assert.IsFalse(wrongChain.IsLoaded);Assert.IsNull(wrongChain.Result);
                    }
                    finally {proxy.ReportedChainId=null;}
                    Assert.AreEqual(methodsBeforeWrongChain+1,proxy.Methods.Count);
                    Assert.AreEqual(receiptsBeforeWrongChain,proxy.Methods.Count(method=>method=="eth_getTransactionReceipt"));
                    Assert.AreEqual(acceptedBeforeRecovery,proxy.AcceptedHashes.Count);
                }
                finally {field.SetValue(provider,original);}
            }
            await VerifyPreparedReplicationAsync(provider,native,executor,user,policy,store);
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
            var deniedRequest=new Core.StorageOperationRequest {Operation="SaveAvatarDetail",PreferredProvider=Core.Enums.ProviderType.PolygonOASIS,
                Payload=new AvatarDetail {Id=deniedId,IsActive=true,XP=999},PreparedExecution=new PreparedStorageExecution(rejectedHandle,"rejected-owner",
                    new Dictionary<Core.Enums.ProviderType,string> {{Core.Enums.ProviderType.PolygonOASIS,sdkCurrent.SigningAddress}},Array.Empty<Core.Enums.ProviderType>())};
            var denied=await router.RouteRequestAsync<Core.Interfaces.IAvatarDetail>(deniedRequest);
            Assert.IsTrue(denied.IsError);Assert.IsFalse(denied.IsSaved);Assert.IsNull(denied.Result);
            var ownershipError=denied.Exception as UsageProtocolException;Assert.IsNotNull(ownershipError);
            Assert.AreEqual("PROVIDER_ATTEMPT_ALREADY_STARTED",ownershipError.Code);
            Assert.AreEqual(PreparedStorageExecution.RejectedCode,denied.ErrorCode);
            Assert.IsFalse(Core.Helpers.NativeMutationReconciliation.IsRequired(ownershipError));
            Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await native.GetAvatarDetailByIdAsync(deniedKey)));
            Assert.AreEqual(nonceBefore.Value,(await rpc.Eth.Transactions.GetTransactionCount.SendRequestAsync(sdkCurrent.SigningAddress,Nethereum.RPC.Eth.DTOs.BlockParameter.CreatePending())).Value);
            var afterOwner=await new MongoUsageOutboxStore(uri,database).GetAsync(rejectedHandle.OperationId,default);
            Assert.AreEqual(changedOwner,afterOwner.ExecutionOwner);Assert.AreEqual(0,afterOwner.ProviderAttempts.Count);
            var pendingHandle=await executor.BeginAsync("local-fixture-bearer",user,"web3-unsubmitted-original",new string('c',64),"local unresolved receipt fixture",policy);
            const string pendingData="0x0102";
            var signedPending=new Nethereum.Signer.LegacyTransactionSigner().SignTransaction(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY")!,
                new System.Numerics.BigInteger(31337),address,0,1000000,1,3000000,pendingData);
            var pendingBytes=signedPending.HexToByteArray();
            var pendingHash="0x"+Convert.ToHexString(Nethereum.Util.Sha3Keccack.Current.CalculateHash(pendingBytes)).ToLowerInvariant();
            await pendingHandle.BeginPreparedNativeAttemptAsync("PolygonOASIS",null,"never-submitted-fixture",pendingHash,
                new UsageNativePreparation {ChainId="31337",Sender=sdkCurrent.SigningAddress,SignedTransactionBase64=Convert.ToBase64String(pendingBytes)});
            var pendingAttempt=(await new MongoUsageOutboxStore(uri,database).GetAsync(pendingHandle.OperationId,default)).ProviderAttempts.Single();
            var pending=await provider.GetPreparedNativeReceiptAsync(pendingAttempt,pendingData);
            Assert.IsTrue(pending.IsError);Assert.AreEqual("OASIS_NATIVE_OUTCOME_UNRESOLVED",pending.ErrorCode);
            Assert.IsNull(pending.Result);Assert.IsNull(pending.Exception);Assert.IsFalse(pending.IsLoaded);Assert.IsFalse(pending.IsSaved);
            Assert.AreEqual(nonceBefore.Value,(await rpc.Eth.Transactions.GetTransactionCount.SendRequestAsync(sdkCurrent.SigningAddress,Nethereum.RPC.Eth.DTOs.BlockParameter.CreatePending())).Value);

            var revertHandle=await executor.BeginAsync("local-fixture-bearer",user,"web3-reverted-original",new string('d',64),"local reverted receipt fixture",policy);
            var revertId=Guid.NewGuid();var revertKey=(uint)Core.Utilities.HashUtility.GetNumericHash(revertId.ToString());
            var external=Web3CoreOASISBaseProvider.PrivateExternalId(revertId);var invalidExpectedHash=new byte[32];var revertPayload=new byte[] {1};
            var reverted=await Assert.ThrowsExceptionAsync<Web3MutationReconciliationException>(()=>provider.ExecutePreparedStorageAsync(
                ()=>sdkCurrent.UpsertCheckedAsync("detail",revertKey,external,true,invalidExpectedHash,revertPayload),revertHandle,"reverted-original",sdkCurrent.SigningAddress));
            var revertAttempt=(await new MongoUsageOutboxStore(uri,database).GetAsync(revertHandle.OperationId,default)).ProviderAttempts.Single();
            Assert.AreEqual(reverted.OriginalTransactionId,revertAttempt.ProviderRequestId);
            var revertData=rpc.Eth.GetContract(Web3CoreOASISBaseProviderHelper.Abi,address).GetFunction("upsertAvatarDetailChecked")
                .GetData(revertKey,external,true,invalidExpectedHash,revertPayload);
            var revertReceipt=await provider.GetPreparedNativeReceiptAsync(revertAttempt,revertData);
            Assert.IsFalse(revertReceipt.IsError,revertReceipt.Message);Assert.IsTrue(revertReceipt.IsLoaded);Assert.IsFalse(revertReceipt.IsSaved);
            Assert.AreEqual(System.Numerics.BigInteger.Zero,revertReceipt.Result.Status.Value);
            Assert.IsTrue(revertReceipt.Message.Contains("reverted"));
            Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await native.GetAvatarDetailByIdAsync(revertKey)));
            Assert.AreEqual(1,(await store.GetAsync(revertHandle.OperationId,default)).ProviderAttempts.Count);
            verified=true;
            Console.WriteLine("WEB3_REAL_OUTBOX_VERIFIED");
        }
        finally {if(verified) await client.DropDatabaseAsync(database);}
    }
}
