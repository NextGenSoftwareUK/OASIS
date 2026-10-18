using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class CosmosReviewedTokenInterfaceTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task CustomTokenInterfacesRetainExactSupplyOwnershipAndOriginalFees(bool synchronous)
    {
        var root=Environment.GetEnvironmentVariable("OASIS_HYPERDRIVE_TEST_ROOT")!;
        var journal=Path.Combine(root,"reviewed-token-pending.json");Assert.False(File.Exists(journal));
        const string oldHash="9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81";
        string OldHash()=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,"replication-pending.json"))));
        Assert.Equal(oldHash,OldHash());
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(240));var token=deadline.Token;
        var id=Guid.NewGuid();var deployment=await CosmosReviewedFinalFeeTests.CreateReviewedProviderAsync(id,token);
        using var provider=deployment.Provider;IOASISBlockchainStorageProvider api=provider;
        var backend=typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(provider)!;
        var invoke=backend.GetType().GetMethod("InvokeAsync",BindingFlags.NonPublic|BindingFlags.Instance)!;
        Task<JsonElement> Invoke(string operation,object args)=>(Task<JsonElement>)invoke.Invoke(backend,new object[]{operation,args,token})!;
        var primaryKey=await Invoke("restoreKey",new{});var primary=primaryKey.GetProperty("address").GetString()!;
        // Reuse the bank fixture's publicly known local-only actor. Never fund this key on a public chain.
        var developmentKey=Convert.ToHexString(Enumerable.Repeat((byte)7,32).ToArray());
        var secondaryKey=await Invoke("restoreKey",new{privateKey=developmentKey});var secondary=secondaryKey.GetProperty("address").GetString()!;
        async Task<decimal> Balance(string address,CancellationToken cancellation){var value=await provider.GetAccountBalanceAsync(address,cancellation);Assert.False(value.IsError,value.Message);return value.Result;}
        var primaryBefore=await Balance(primary,token);var secondaryBefore=await Balance(secondary,token);
        Assert.True(secondaryBefore>=50000,"Reuse the retained byte07 development wallet; do not fund or replace it implicitly.");
        var symbol="OWNED"+id.ToString("N");
        var store=new MongoUsageOutboxStore(connection,"usage_cosmos_independent_routing");
        var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new CosmosIndependentRoutingTests.TestAuthority(user));
        var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.invalid","WEB6","test-only-key");
        var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6"}).BeginAsync("test-only",user,id.ToString("D"),new string('a',64),
            "POST /reviewed-token-interface-fixture",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=.4m,MaximumNativeFeeCostUsd=.4m,MaximumProviderAttempts=8},token);
        var now=DateTime.UtcNow.AddSeconds(-1);
        execution=execution.WithNativePrices(new[]{new NativeFeePriceQuote(Guid.Parse(execution.OperationId),"CosmosBlockChainOASIS","oasis-cosmos-local","stake","local-test-stake",0,.000001m,
            "synthetic-local-price","fixture-v1","fixture-v1",now,now,now,now.AddMinutes(10))});
        using var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.Read);var complete=false;
        try{
            await JsonSerializer.SerializeAsync(file,new{operationId=execution.OperationId,symbol,synchronous,primary,secondary,primaryBefore,secondaryBefore,review=deployment.Review,
                database="usage_cosmos_independent_routing",developmentIdentity="known-byte07-local-only",recovery="Inspect original signed token attempts, exact symbol supply/balances and native fees; never replay an uncertain operation."},cancellationToken:token);file.Flush(true);
            async Task<string> Owned(string stage,string sender,Func<Task<OASISResult<ITransactionResponse>>> action){
                var value=await provider.ExecutePreparedStorageAsync(action,execution,stage,sender,token);Assert.False(value.IsError,value.Message);Assert.NotNull(value.Result);return value.Result.TransactionResult;
            }
            async Task AssertState(decimal owner,decimal recipient,string units){
                var first=await provider.GetCustomTokenBalanceAsync(symbol,primary);var second=await provider.GetCustomTokenBalanceAsync(symbol,secondary);
                Assert.False(first.IsError,first.Message);Assert.False(second.IsError,second.Message);Assert.Equal(owner,first.Result);Assert.Equal(recipient,second.Result);
                var state=await provider.GetCustomTokenAsync(symbol);Assert.False(state.IsError,state.Message);Assert.Equal(units,state.Result.GetProperty("supply").GetString());
            }
            var hashes=new List<string>();
            var mint=new MintWeb3TokenRequest{Symbol=symbol,Title="Owned exact decimal token",Amount=12.5m};
            hashes.Add(await Owned("mint",primary,()=>synchronous?Task.FromResult(api.MintToken(mint)):api.MintTokenAsync(mint)));
            await AssertState(12.5m,0,"1250000000");
            var freeze=new LockWeb3TokenRequest{TokenAddress=symbol};
            hashes.Add(await Owned("freeze",primary,()=>synchronous?Task.FromResult(api.LockToken(freeze)):api.LockTokenAsync(freeze)));
            var frozen=await provider.GetCustomTokenAsync(symbol);Assert.False(frozen.IsError,frozen.Message);Assert.True(frozen.Result.GetProperty("locked").GetBoolean());
            var unlock=new UnlockWeb3TokenRequest{TokenAddress=symbol};
            hashes.Add(await Owned("unfreeze",primary,()=>synchronous?Task.FromResult(api.UnlockToken(unlock)):api.UnlockTokenAsync(unlock)));
            var unfrozen=await provider.GetCustomTokenAsync(symbol);Assert.False(unfrozen.IsError,unfrozen.Message);Assert.False(unfrozen.Result.GetProperty("locked").GetBoolean());
            var send=new SendWeb3TokenRequest{FromTokenAddress=symbol,FromWalletAddress=primary,ToWalletAddress=secondary,Amount=1.25m};
            hashes.Add(await Owned("send",primary,()=>synchronous?Task.FromResult(api.SendToken(send)):api.SendTokenAsync(send)));
            await AssertState(11.25m,1.25m,"1250000000");
            var burnRecipient=new BurnWeb3TokenRequest{TokenAddress=symbol,Amount=1.25m,OwnerPublicKey=secondaryKey.GetProperty("publicKey").GetString()!,OwnerPrivateKey=developmentKey,OwnerSeedPhrase=""};
            hashes.Add(await Owned("burn-recipient",secondary,()=>synchronous?Task.FromResult(api.BurnToken(burnRecipient)):api.BurnTokenAsync(burnRecipient)));
            await AssertState(11.25m,0,"1125000000");
            var burnOwner=new BurnWeb3TokenRequest{TokenAddress=symbol,Amount=11.25m,OwnerPublicKey=primaryKey.GetProperty("publicKey").GetString()!,OwnerPrivateKey="",OwnerSeedPhrase=""};
            hashes.Add(await Owned("burn-owner",primary,()=>synchronous?Task.FromResult(api.BurnToken(burnOwner)):api.BurnTokenAsync(burnOwner)));
            await AssertState(0,0,"0");Assert.Equal(6,hashes.Distinct().Count());
            using var recoveryDeadline=new CancellationTokenSource(TimeSpan.FromSeconds(180));var recoveryToken=recoveryDeadline.Token;
            var resolver=new PreparedNativeUsageRecoveryResolver(store,new[]{new RegisteredNativeFeeReader(ProviderType.CosmosBlockChainOASIS,"oasis-cosmos-local",new IOASISProvider[]{provider})});
            var operation=await store.GetAsync(execution.OperationId,recoveryToken);Assert.Equal(6,operation.ProviderAttempts.Count);Assert.Null(await resolver.ResolveAsync(operation,recoveryToken));
            operation=await new MongoUsageOutboxStore(connection,"usage_cosmos_independent_routing").GetAsync(execution.OperationId,recoveryToken);
            Assert.Equal(6,operation.Receipts.Count);Assert.False(resolver.CanResolve(operation));Assert.Null(operation.ObservedOutcome);Assert.Null(operation.Settlement);
            var fees=new Dictionary<string,decimal>{{primary,0},{secondary,0}};
            foreach(var attempt in operation.ProviderAttempts){
                Assert.Contains(attempt.ProviderRequestId,hashes);var receipt=Assert.Single(operation.Receipts.Where(row=>row.ProviderRequestId==attempt.ProviderRequestId));
                Assert.Equal(true,receipt.NativeExecutionSucceeded);Assert.NotNull(receipt.NativeFeeAudit);receipt.Validate();
                foreach(var fee in receipt.NativeFeeValuations){Assert.Equal("stake",fee.Denomination);fees[attempt.NativePreparation.Sender]+=decimal.Parse(fee.NativeNetUnits,CultureInfo.InvariantCulture);}
                var status=await api.GetTransactionStatusAsync(attempt.ProviderRequestId,recoveryToken);Assert.False(status.IsError,status.Message);Assert.Equal(Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,status.Result);
            }
            var primaryAfter=await Balance(primary,recoveryToken);var secondaryAfter=await Balance(secondary,recoveryToken);
            Assert.Equal(fees[primary],primaryBefore-primaryAfter);Assert.Equal(fees[secondary],secondaryBefore-secondaryAfter);Assert.Equal(oldHash,OldHash());
            var report=JsonSerializer.Serialize(new{operation,symbol,synchronous,hashes,primary,secondary,primaryBefore,secondaryBefore,primaryAfter,secondaryAfter,fees,finalSupply="0",logicalOutcomeResolved=false,hostedAcceptance=false});
            await File.WriteAllTextAsync(Path.Combine(root,$"reviewed-token-{(synchronous?"sync":"async")}-current.json"),report,recoveryToken);output.WriteLine(report);complete=true;
        }finally{
            developmentKey=string.Empty;
            if(complete){file.Dispose();File.Delete(journal);}else output.WriteLine("Preserved token originals; do not replay: "+journal);
        }
    }
}
