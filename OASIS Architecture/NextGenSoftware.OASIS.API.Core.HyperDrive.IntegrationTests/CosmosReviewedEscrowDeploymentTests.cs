using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class CosmosReviewedEscrowDeploymentTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public async Task DeployOnceWithOriginalAdmittedUploadAndInstantiation()
    {
        var root=Environment.GetEnvironmentVariable("OASIS_HYPERDRIVE_TEST_ROOT")!;
        var journal=Path.Combine(root,"escrow-deployment-pending.jsonl");
        var descriptor=Path.Combine(root,"escrow-deployment-current.json");
        Assert.False(File.Exists(journal),"Reconcile original deployment; never reupload uncertain code.");
        Assert.False(File.Exists(descriptor),"Reuse the retained deployment; never deploy another copy.");
        const string codeHash="fcae1521a4564ae4548ad25bae886e432ad1049ece035b3f70ae3a4e961e014c";
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        string Fence()=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,"replication-pending.json"))));
        const string fence="9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81";Assert.Equal(fence,Fence());
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(240));var token=deadline.Token;
        var wasm=await File.ReadAllBytesAsync(@"\\wsl.localhost\Ubuntu\var\tmp\oasis-cosmos-contract-target\wasm32-unknown-unknown\release\oasis_cosmos_storage.wasm",token);
        Assert.Equal(codeHash,Convert.ToHexString(SHA256.HashData(wasm)).ToLowerInvariant());Assert.Equal(376152,wasm.Length);
        var id=Guid.NewGuid();
        // Dedicated publication policy, not a change to the50000stake asset ceiling.
        var deployment=await CosmosReviewedFinalFeeTests.CreateReviewedProviderAsync(id,token,"100000");using var provider=deployment.Provider;
        var backend=typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(provider)!;
        var invoke=backend.GetType().GetMethod("InvokeAsync",BindingFlags.NonPublic|BindingFlags.Instance)!;
        Task<JsonElement> Invoke(string operation,object args)=>(Task<JsonElement>)invoke.Invoke(backend,new object[]{operation,args,token})!;
        var sender=(await Invoke("restoreKey",new{})).GetProperty("address").GetString()!;
        async Task<decimal> Balance(CancellationToken cancellation){var value=await provider.GetAccountBalanceAsync(sender,cancellation);Assert.False(value.IsError,value.Message);return value.Result;}
        var before=await Balance(token);var user=Guid.NewGuid().ToString("D");
        var store=new MongoUsageOutboxStore(connection,"usage_cosmos_independent_routing");
        using var http=new HttpClient(new CosmosIndependentRoutingTests.TestAuthority(user));
        var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.invalid","WEB6","test-only-key");
        var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6"}).BeginAsync("test-only",user,id.ToString("D"),new string('d',64),
            "POST /reviewed-escrow-deployment-fixture",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=.2m,MaximumNativeFeeCostUsd=.2m,MaximumProviderAttempts=2},token);
        var now=DateTime.UtcNow.AddSeconds(-1);
        execution=execution.WithNativePrices(new[]{new NativeFeePriceQuote(Guid.Parse(execution.OperationId),"CosmosBlockChainOASIS","oasis-cosmos-local","stake","local-test-stake",0,.000001m,
            "synthetic-local-price","fixture-v1","fixture-v1",now,now,now,now.AddMinutes(10))});
        using var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.Read);var complete=false;
        async Task Append(object entry){await file.WriteAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry)+"\n"),token);file.Flush(true);}
        try{
            await Append(new{stage="intent",operationId=execution.OperationId,database="usage_cosmos_independent_routing",codeHash,sender,balanceBefore=before,review=deployment.Review,
                maximumTransactionStake="100000",maximumOperationStake="200000",maximumAttempts=2,recovery="Inspect original upload/instantiate signed attempts and events. Never retry deployment or replace the old contract."});
            var uploaded=await provider.ExecutePreparedStorageAsync(()=>provider.UploadContractAsync(wasm,token),execution,"upload",sender,token);
            Assert.False(uploaded.IsError,uploaded.Message);Assert.True(uploaded.Result.CodeId>1);
            await Append(new{stage="uploaded",codeId=uploaded.Result.CodeId,transactionHash=uploaded.Result.TransactionHash});
            var created=await provider.ExecutePreparedStorageAsync(()=>provider.InstantiateContractAsync(uploaded.Result.CodeId,new{owner=sender},"oasis-id-bound-escrow-v1-"+id.ToString("N"),token),execution,"instantiate",sender,token);
            Assert.False(created.IsError,created.Message);Assert.NotEqual(Environment.GetEnvironmentVariable("OASIS_COSMOS_TEST_CONTRACT"),created.Result.ContractAddress);
            await Append(new{stage="instantiated",contractAddress=created.Result.ContractAddress,transactionHash=created.Result.TransactionHash});
            var protocol=await Invoke("tokenEscrowInfo",new{targetContractAddress=created.Result.ContractAddress,codeSha256=codeHash});
            Assert.Equal(uploaded.Result.CodeId,protocol.GetProperty("codeId").GetInt64());Assert.True(protocol.GetProperty("immutable").GetBoolean());Assert.Equal(1,protocol.GetProperty("protocol").GetInt32());
            var config=await provider.QueryContractAsync(created.Result.ContractAddress,new{config=new{}},token);Assert.False(config.IsError,config.Message);Assert.Equal(sender,config.Result.GetProperty("owner").GetString());
            using var recoveryDeadline=new CancellationTokenSource(TimeSpan.FromSeconds(180));var recoveryToken=recoveryDeadline.Token;
            var resolver=new PreparedNativeUsageRecoveryResolver(store,new[]{new RegisteredNativeFeeReader(ProviderType.CosmosBlockChainOASIS,"oasis-cosmos-local",new IOASISProvider[]{provider})});
            var operation=await store.GetAsync(execution.OperationId,recoveryToken);Assert.Equal(2,operation.ProviderAttempts.Count);Assert.Null(await resolver.ResolveAsync(operation,recoveryToken));
            operation=await new MongoUsageOutboxStore(connection,"usage_cosmos_independent_routing").GetAsync(execution.OperationId,recoveryToken);
            Assert.Equal(2,operation.Receipts.Count);Assert.False(resolver.CanResolve(operation));Assert.Null(operation.ObservedOutcome);Assert.Null(operation.Settlement);
            var originals=new[]{uploaded.Result.TransactionHash,created.Result.TransactionHash};Assert.Equal(2,originals.Distinct().Count());decimal actualFees=0;
            foreach(var receipt in operation.Receipts){Assert.Contains(receipt.ProviderRequestId,originals);Assert.Equal(true,receipt.NativeExecutionSucceeded);Assert.NotNull(receipt.NativeFeeAudit);receipt.Validate();
                foreach(var fee in receipt.NativeFeeValuations){Assert.Equal("stake",fee.Denomination);actualFees+=decimal.Parse(fee.NativeNetUnits,CultureInfo.InvariantCulture);}
                var status=await provider.GetTransactionStatusAsync(receipt.ProviderRequestId,recoveryToken);Assert.False(status.IsError,status.Message);Assert.Equal(Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,status.Result);
            }
            var after=await Balance(recoveryToken);Assert.Equal(actualFees,before-after);Assert.Equal(fence,Fence());
            var record=new{chain="oasis-cosmos-local",contractAddress=created.Result.ContractAddress,codeId=uploaded.Result.CodeId,codeSha256=codeHash,owner=sender,
                uploadTransaction=uploaded.Result.TransactionHash,instantiateTransaction=created.Result.TransactionHash,operationId=execution.OperationId,database="usage_cosmos_independent_routing",
                balanceBefore=before,balanceAfter=after,actualFeesStake=actualFees,immutable=true,protocol=1,nativeEscrowAcceptance=false,hostedAcceptance=false};
            using(var saved=new FileStream(descriptor,FileMode.CreateNew,FileAccess.Write,FileShare.Read)){await JsonSerializer.SerializeAsync(saved,record,cancellationToken:recoveryToken);saved.Flush(true);}
            output.WriteLine(JsonSerializer.Serialize(new{operation,deployment=record}));complete=true;
        }finally{if(complete){file.Dispose();File.Delete(journal);}else output.WriteLine("Original deployment pending; do not replay: "+journal);}
    }
}
