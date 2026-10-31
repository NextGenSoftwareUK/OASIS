using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class CosmosReviewedBankInterfaceTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public async Task PublicBankInterfacesPreserveAmountsRangeStatusAndFinalFees()
    {
        var root=Environment.GetEnvironmentVariable("OASIS_HYPERDRIVE_TEST_ROOT")!;
        var journal=Path.Combine(root,"reviewed-bank-pending.json");Assert.False(File.Exists(journal));
        string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        const string oldHash="9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81";
        Assert.Equal(oldHash,Hash(Path.Combine(root,"replication-pending.json")));
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        using var executionDeadline=new CancellationTokenSource(TimeSpan.FromSeconds(180));var token=executionDeadline.Token;
        var fixtureId=Guid.NewGuid();var deployment=await CosmosReviewedFinalFeeTests.CreateReviewedProviderAsync(fixtureId,token);
        using var provider=deployment.Provider;IOASISBlockchainStorageProvider api=provider;
        var backend=typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(provider)!;
        var invoke=backend.GetType().GetMethod("InvokeAsync",BindingFlags.NonPublic|BindingFlags.Instance)!;
        Task<JsonElement> Invoke(string operation,object args)=>(Task<JsonElement>)invoke.Invoke(backend,new object[]{operation,args,token})!;
        var primary=(await Invoke("restoreKey",new{})).GetProperty("address").GetString()!;
        // Public, unfunded-by-default DEVELOPMENT identity shared with offline signing fixtures.
        // Never use this known key on a public chain or put generated wallet secrets in evidence.
        var developmentKey=Convert.ToHexString(Enumerable.Repeat((byte)7,32).ToArray());
        var secondary=(await Invoke("restoreKey",new{privateKey=developmentKey})).GetProperty("address").GetString()!;
        Assert.NotEqual(primary,secondary);
        foreach(var generated in new[]{api.GenerateKeyPair(),await api.GenerateKeyPairAsync()}){
            Assert.False(generated.IsError,generated.Message);Assert.True(!string.IsNullOrWhiteSpace(generated.Result.PrivateKey));
            var restored=await api.RestoreAccountAsync(generated.Result.PrivateKey,token);
            Assert.False(restored.IsError,restored.Message);Assert.True(restored.Result.PublicKey==generated.Result.PublicKey);
        }
        var created=await api.CreateAccountAsync(token);Assert.False(created.IsError,created.Message);
        Assert.True(!string.IsNullOrWhiteSpace(created.Result.SeedPhrase)&&created.Result.PrivateKey==created.Result.SeedPhrase);
        var restoredCreated=await api.RestoreAccountAsync(created.Result.SeedPhrase,token);
        Assert.False(restoredCreated.IsError,restoredCreated.Message);Assert.True(restoredCreated.Result.PublicKey==created.Result.PublicKey);
        async Task<decimal> Balance(string address,CancellationToken cancellation){
            var result=await provider.GetAccountBalanceAsync(address,cancellation);Assert.False(result.IsError,result.Message);return result.Result;
        }
        var primaryBefore=await Balance(primary,token);var secondaryBefore=await Balance(secondary,token);
        foreach(var address in new[]{primary,secondary}){
            var request=new GetWeb3WalletBalanceRequest{WalletAddress=address};
            var sync=api.GetBalance(request);var asyncResult=await api.GetBalanceAsync(request);
            Assert.False(sync.IsError,sync.Message);Assert.False(asyncResult.IsError,asyncResult.Message);
            Assert.Equal((double)await Balance(address,token),sync.Result);Assert.Equal(sync.Result,asyncResult.Result);
        }
        var store=new MongoUsageOutboxStore(connection,"usage_cosmos_independent_routing");
        var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new CosmosIndependentRoutingTests.TestAuthority(user));
        var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.invalid","WEB6","test-only-key");
        var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6"}).BeginAsync("test-only",user,fixtureId.ToString("D"),new string('b',64),
            "POST /reviewed-bank-interface-fixture",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=.25m,MaximumNativeFeeCostUsd=.25m,MaximumProviderAttempts=16},token);
        var now=DateTime.UtcNow.AddSeconds(-1);
        execution=execution.WithNativePrices(new[]{new NativeFeePriceQuote(Guid.Parse(execution.OperationId),"CosmosBlockChainOASIS","oasis-cosmos-local","stake","local-test-stake",0,.000001m,
            "synthetic-local-price","fixture-v1","fixture-v1",now,now,now,now.AddMinutes(5))});
        using var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.Read);var complete=false;
        try{
            await JsonSerializer.SerializeAsync(file,new{operationId=execution.OperationId,primary,secondary,primaryBefore,secondaryBefore,review=deployment.Review,
                database="usage_cosmos_independent_routing",developmentIdentity="known-byte07-local-only",fundingStake="200000",netPrincipalToSecondary="190000",
                recovery="Inspect original signed bank attempts and both wallet balances. Never replay an unknown transfer or replace this intent."},cancellationToken:token);file.Flush(true);
            async Task<T> Owned<T>(string stage,string sender,Func<Task<OASISResult<T>>> action){
                var result=await provider.ExecutePreparedStorageAsync(action,execution,stage,sender,token);Assert.False(result.IsError,result.Message);Assert.NotNull(result.Result);return result.Result;
            }
            var fund=await Owned("fund",primary,()=>Task.FromResult(api.SendToken(new SendWeb3TokenRequest{
                FromTokenAddress="stake",FromWalletAddress=primary,ToWalletAddress=secondary,Amount=200000,MemoText="owned-bank-fund"})));
            var withdraw=await Owned("withdraw",secondary,()=>api.WithdrawAsync(10000,secondary,developmentKey));Assert.True(withdraw.IsSuccessful);Assert.Equal(BridgeTransactionStatus.Completed,withdraw.Status);
            var deposit=await Owned("deposit",primary,()=>api.DepositAsync(10000,secondary));Assert.True(deposit.IsSuccessful);Assert.Equal(BridgeTransactionStatus.Completed,deposit.Status);
            var sent=await Owned("send",secondary,()=>api.SendTokenAsync(new SendWeb3TokenRequest{
                FromTokenAddress="stake",FromWalletAddress=secondary,ToWalletAddress=primary,Amount=10000,OwnerPrivateKey=developmentKey,MemoText="owned-bank-return"}));
            var expected=new[]{new{Hash=fund.TransactionResult,From=primary,To=secondary,Units="200000",Memo="owned-bank-fund"},
                new{Hash=withdraw.TransactionId,From=secondary,To=primary,Units="10000",Memo="OASIS bridge withdrawal"},
                new{Hash=deposit.TransactionId,From=primary,To=secondary,Units="10000",Memo="OASIS bridge deposit"},
                new{Hash=sent.TransactionResult,From=secondary,To=primary,Units="10000",Memo="owned-bank-return"}};
            Assert.Equal(4,expected.Select(row=>row.Hash).Distinct().Count());
            using var recoveryDeadline=new CancellationTokenSource(TimeSpan.FromSeconds(180));var recoveryToken=recoveryDeadline.Token;
            var reader=new RegisteredNativeFeeReader(ProviderType.CosmosBlockChainOASIS,"oasis-cosmos-local",new IOASISProvider[]{provider});
            var resolver=new PreparedNativeUsageRecoveryResolver(store,new[]{reader});
            var operation=await store.GetAsync(execution.OperationId,recoveryToken);Assert.Equal(4,operation.ProviderAttempts.Count);
            Assert.True(resolver.CanResolve(operation));Assert.Null(await resolver.ResolveAsync(operation,recoveryToken));
            operation=await new MongoUsageOutboxStore(connection,"usage_cosmos_independent_routing").GetAsync(execution.OperationId,recoveryToken);
            Assert.Equal(4,operation.Receipts.Count);Assert.False(resolver.CanResolve(operation));Assert.Null(operation.ObservedOutcome);Assert.Null(operation.Settlement);
            var fees=new Dictionary<string,decimal>{{primary,0},{secondary,0}};var heights=new Dictionary<string,ulong>();
            foreach(var attempt in operation.ProviderAttempts){
                var receipt=Assert.Single(operation.Receipts.Where(row=>row.ProviderRequestId==attempt.ProviderRequestId));
                Assert.NotNull(receipt.NativeFeeAudit);receipt.NativeFeeAudit.Validate(attempt.Provider,attempt.ProviderRequestId,"oasis-cosmos-local");
                using var audit=JsonDocument.Parse(receipt.NativeFeeAudit.PayloadJson);
                Assert.True(audit.RootElement.GetProperty("NativeExecutionSucceeded").GetBoolean());Assert.Equal(0,audit.RootElement.GetProperty("NativeExecutionCode").GetInt32());
                Assert.Equal(deployment.Review.EvidenceBasis,receipt.CostSource);heights.Add(attempt.ProviderRequestId,audit.RootElement.GetProperty("ReceiptHeight").GetUInt64());
                foreach(var fee in receipt.NativeFeeValuations){Assert.Equal("stake",fee.Denomination);fees[attempt.NativePreparation.Sender]+=decimal.Parse(fee.NativeNetUnits,CultureInfo.InvariantCulture);}
                var status=await api.GetTransactionStatusAsync(attempt.ProviderRequestId,recoveryToken);Assert.False(status.IsError,status.Message);Assert.Equal(BridgeTransactionStatus.Completed,status.Result);
            }
            var primaryAfter=await Balance(primary,recoveryToken);var secondaryAfter=await Balance(secondary,recoveryToken);
            Assert.Equal(190000+fees[primary],primaryBefore-primaryAfter);Assert.Equal(190000-fees[secondary],secondaryAfter-secondaryBefore);
            Assert.Equal(fees.Values.Sum(),primaryBefore+secondaryBefore-primaryAfter-secondaryAfter);
            foreach(var address in new[]{primary,secondary}){
                var request=new GetWeb3TransactionsRequest{WalletAddress=address,FromBlock=(ulong)deployment.Observation.CommittedHeight};
                foreach(var history in new[]{api.GetTransactions(request),await api.GetTransactionsAsync(request)}){
                    Assert.False(history.IsError,history.Message);Assert.Equal(4,history.Result.Count);
                    foreach(var transfer in expected){var row=Assert.Single(history.Result.Where(row=>row.NativeTransactionId==transfer.Hash));
                        Assert.Equal(transfer.From,row.FromWalletAddress);Assert.Equal(transfer.To,row.ToWalletAddress);Assert.Equal(transfer.Units,row.RawAmount);
                        Assert.Equal("Success",row.NativeExecutionResult);Assert.Equal("stake",row.Token);
                        Assert.Equal(transfer.Memo,row.MemoText);Assert.Equal(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(transfer.Memo)),row.MemoBase64);
                        Assert.Equal(address==transfer.From?TransactionType.Debit:TransactionType.Credit,row.TransactionType);
                    }
                }
                var height=heights[fund.TransactionResult];
                var bounded=await api.GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress=address,FromBlock=height,ToBlock=height});
                Assert.False(bounded.IsError,bounded.Message);
                Assert.Equal(heights.Where(row=>row.Value==height).Select(row=>row.Key).Order(),bounded.Result.Select(row=>row.NativeTransactionId).Order());
                var empty=await api.GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress=address,ToBlock=0});Assert.False(empty.IsError,empty.Message);Assert.Empty(empty.Result);
            }
            Assert.Equal(oldHash,Hash(Path.Combine(root,"replication-pending.json")));
            var report=JsonSerializer.Serialize(new{operation,expected,heights,primary,secondary,primaryBefore,secondaryBefore,primaryAfter,secondaryAfter,fees,
                retainedDevelopmentWalletFunds=true,generatedWalletsFunded=false,logicalOutcomeResolved=false,hostedAcceptance=false});
            await File.WriteAllTextAsync(Path.Combine(root,"reviewed-bank-interface-current.json"),report,recoveryToken);output.WriteLine(report);complete=true;
        }finally{
            developmentKey=string.Empty;
            if(complete){file.Dispose();File.Delete(journal);}else output.WriteLine("Preserved bank originals; do not replay: "+journal);
        }
    }
}
