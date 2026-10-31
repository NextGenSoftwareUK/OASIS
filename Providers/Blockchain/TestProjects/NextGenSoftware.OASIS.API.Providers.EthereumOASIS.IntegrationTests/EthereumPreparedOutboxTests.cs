using System.Numerics;
using MongoDB.Driver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    private sealed class EthereumFixtureAuthority(string user):IUsageReservationAuthority
    {
        public Task<SubscriptionAuthorizationResult> AuthorizeUsageAsync(string bearer,UsageAuthorizationRequest request,CancellationToken token)
            =>Task.FromResult(new SubscriptionAuthorizationResult{Allowed=true,OperationId=request.OperationId,UserId=user,Status="authorized",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(10)});
        public Task<UsageStartResult> StartUsageAsync(UsageStartRequest request,CancellationToken token)=>Task.FromResult(new UsageStartResult{Started=true,OperationId=request.OperationId});
        public Task<UsageSettlementResult> SettleUsageAsync(UsageSettlementRequest request,CancellationToken token)=>throw new NotSupportedException("Local fixture does not authorize settlement or fee valuation.");
    }
    private static async Task VerifyEthereumPreparedOutboxAsync(EthereumOASIS healthy,string address,string kind,Action<object> journal)
    {
        var uri=Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI");
        Assert.IsTrue(uri?.StartsWith("mongodb://127.0.0.1:27029/",StringComparison.Ordinal)==true,"Retained owned Mongo replica required.");
        const string database="oasis_ethereum_prepared_native";
        var client=new MongoClient(uri);Assert.IsFalse((await(await client.ListDatabaseNamesAsync()).ToListAsync()).Contains(database),"Retained original outbox exists; refusing overwrite.");
        var verified=false;var id=Guid.NewGuid();var originalFlag=healthy.IsVersionControlEnabled;
        journal(new{phase="outbox-fixture-planned",id,kind,contract=address,database});
        try {
            var store=new MongoUsageOutboxStore(uri,database);await store.InitializeAsync(default);
            var user=Guid.NewGuid().ToString();var executor=new Web4UsageExecution(new EthereumFixtureAuthority(user),store,new Web4UsageLedgerOptions{Service="WEB4",PersistenceTimeout=TimeSpan.FromSeconds(5)});
            var policy=new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=0,PricingCatalogueVersion="local-test"};
            var handle=await executor.BeginAsync("local-fixture-bearer",user,"ethereum-native-preparation",new string('e',64),"local Ethereum outbox fixture",policy);
            journal(new{phase="outbox-owner-admitted",id,operationId=handle.OperationId,database});
            IHolonBase source=kind switch{"avatar"=>new Avatar{Id=id},"avatar-detail"=>new AvatarDetail{Id=id},_=>new Core.Holons.Holon{Id=id}};
            var beforeSend=0;string accepted;
            await using(var proxy=new Web3AcceptedWriteProxy(async raw=>{
                var hash="0x"+Convert.ToHexString(Sha3Keccack.Current.CalculateHash(raw.HexToByteArray())).ToLowerInvariant();
                var reopened=new MongoUsageOutboxStore(uri,database);var stored=await reopened.GetAsync(handle.OperationId,default);
                var attempt=stored.ProviderAttempts.Single();Assert.AreEqual(hash,attempt.ProviderRequestId);
                Assert.AreEqual("EthereumOASIS",attempt.Provider);Assert.AreEqual("31337",attempt.NativePreparation.ChainId);
                CollectionAssert.AreEqual(raw.HexToByteArray(),Convert.FromBase64String(attempt.NativePreparation.SignedTransactionBase64));
                beforeSend++;journal(new{phase="real-outbox-before-native-forward",id,transactionHash=hash,operationId=handle.OperationId});
            })){
                var fault=new EthereumOASIS("http://127.0.0.1:18546",PrivateKey,ChainId,address){IsVersionControlEnabled=true};
                Assert.IsFalse((await fault.ActivateProviderAsync()).IsError);
                try {
                    async Task<bool> Save(){
                        if(kind=="avatar"){var r=await fault.SaveAvatarAsync((IAvatar)source);Assert.IsTrue(r.IsError);Assert.IsFalse(r.IsSaved);Assert.IsNull(r.Result);Assert.IsInstanceOfType(r.Exception,typeof(EthereumStorageReceiptReconciliationException));return true;}
                        if(kind=="avatar-detail"){var r=await fault.SaveAvatarDetailAsync((IAvatarDetail)source);Assert.IsTrue(r.IsError);Assert.IsFalse(r.IsSaved);Assert.IsNull(r.Result);Assert.IsInstanceOfType(r.Exception,typeof(EthereumStorageReceiptReconciliationException));return true;}
                        var h=await fault.SaveHolonAsync((IHolon)source,saveChildren:false);Assert.IsTrue(h.IsError);Assert.IsFalse(h.IsSaved);Assert.IsNull(h.Result);Assert.IsInstanceOfType(h.Exception,typeof(EthereumStorageReceiptReconciliationException));return true;
                    }
                    await fault.ExecutePreparedStorageAsync(Save,handle,"owned-ethereum-outbox",fault.Web3Client.TransactionManager.Account.Address);
                    Assert.AreEqual(1,beforeSend);Assert.AreEqual(1,proxy.Methods.Count(method=>method=="eth_sendRawTransaction"));accepted=proxy.AcceptedHashes.Single();
                    var reopened=new MongoUsageOutboxStore(uri,database);var persisted=await reopened.GetAsync(handle.OperationId,default);
                    Assert.AreEqual(accepted,persisted.ProviderAttempts.Single().ProviderRequestId);Assert.AreEqual(0,source.Version);
                    Assert.IsFalse(source.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.EthereumOASIS));
                }finally{await fault.DeActivateProviderAsync();}
            }
            var receipt=await healthy.Web3Client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(accepted);Assert.AreEqual(BigInteger.One,receipt.Status.Value);Assert.AreEqual(accepted,receipt.TransactionHash);
            var service=new NextGenSoftwareOASISService(healthy.Web3Client,address);var key=Core.Utilities.HashUtility.GetNumericHash(id.ToString());
            if(kind=="avatar"){Assert.AreEqual(1,(await healthy.LoadAvatarAsync(id)).Result.Version);Assert.IsFalse((await healthy.DeleteAvatarAsync(id,softDelete:false)).IsError);Assert.AreEqual(BigInteger.Zero,(await service.GetAvatarByIdQueryAsync(key)).ReturnValue1.EntityId);}
            else if(kind=="avatar-detail"){
                Assert.AreEqual(1,(await healthy.LoadAvatarDetailAsync(id)).Result.Version);
                var deleted=await service.ContractHandler.SendRequestAndWaitForReceiptAsync(new ContractDefinition.DeleteAvatarDetailByIdentityFunction{EntityId=key,Identity=id.ToString()});Assert.AreEqual(BigInteger.One,deleted.Status.Value);Assert.AreEqual(BigInteger.Zero,(await service.GetAvatarDetailByIdQueryAsync(key)).ReturnValue1.EntityId);
            }else{Assert.AreEqual(1,(await healthy.LoadHolonAsync(id,loadChildren:false)).Result.Version);Assert.IsFalse((await healthy.DeleteHolonAsync(id)).IsError);Assert.AreEqual(BigInteger.Zero,(await service.GetHolonByIdQueryAsync(key)).ReturnValue1.EntityId);}
            verified=true;Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{proof="ETHEREUM_REAL_OUTBOX_VERIFIED",kind,id,transactionHash=accepted,operationId=handle.OperationId,fixtureAbsent=true}));
        }finally{healthy.IsVersionControlEnabled=originalFlag;if(verified)await client.DropDatabaseAsync(database);}
    }
}
