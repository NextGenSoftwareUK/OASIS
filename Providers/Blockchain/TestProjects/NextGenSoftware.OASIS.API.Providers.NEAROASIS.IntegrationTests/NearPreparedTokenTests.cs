using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class NearPreparedTokenTests
{
    [TestMethod][TestCategory("RetainedSignedInspection")]
    public async Task PublicTokenCustodyPreservesExactSupplyAndOriginals()
    {
        string Required(string name)=>Environment.GetEnvironmentVariable(name)??throw new InvalidOperationException(name+" required");
        Assert.AreEqual("http://127.0.0.1:3030",Required("NEAROASIS_RPCENDPOINT"));Assert.AreEqual("oasis-near-local",Required("NEAROASIS_NETWORKID"));Assert.AreEqual("test.near",Required("NEAROASIS_ACCOUNTID"));
        var root=new DirectoryInfo(Environment.CurrentDirectory);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Scripts","run_near_provider_evidence.ps1")))root=root.Parent;Assert.IsNotNull(root);
        const string stem="near-prepared-token";var folder=Path.Combine(root.FullName,"TestResults","NearRuntime");
        var pending=Path.Combine(folder,stem+".pending.json");var journal=Path.Combine(folder,stem+".attempts.jsonl");var complete=Path.Combine(folder,stem+".complete.json");
        var published=Path.Combine(root.FullName,"Docs","WIP","HyperDrive-Providers","NativeEvidence",stem+".complete.json");
        Assert.IsFalse(new[]{pending,journal,complete,published}.Any(File.Exists),"Reconcile originals; never replay a pending/completed cohort");
        var provider=new NEAROASIS(Required("NEAROASIS_RPCENDPOINT"),"oasis-near-local","oasis-near-local","test.near","test.near",Required("NEAROASIS_PRIVATEKEY"));
        var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);
        using(var file=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new{plannedNativeActions=8,synthetic=true,account="test.near",purpose="public token escrow native acceptance"}));file.Flush(true);
        }
        var attempts=new List<UsageProviderAttempt>();var sequence=0;
        async Task<T> Own<T>(Func<Task<T>> operation,int physicalCount){
            var store=DispatchProxy.Create<IUsageOutboxStore,NearPreparedNativeTests.FlushedOwner>();var owner=(NearPreparedNativeTests.FlushedOwner)(object)store;owner.Journal=journal;
            var execution=(UsageExecutionHandle)typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)
                .Single(value=>value.GetParameters().Length==5).Invoke(new object?[]{new UsageOutboxOperation{Id=Guid.NewGuid().ToString("D"),ExecutionOwner="native-near-public-lifecycle",Policy=new UsageEndpointPolicy()},store,new Web4UsageLedgerOptions(),null,null});
            using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var result=await ((IPreparedNativeStorageProvider)provider).ExecutePreparedStorageAsync(operation,execution,stem+"-"+(sequence++),"test.near",timeout.Token);
            var cause=result?.GetType().GetProperty("Exception")?.GetValue(result) as Exception;
            if(owner.Calls!=physicalCount)throw new InvalidOperationException("Unexpected physical native count; retain all originals",cause);
            var retained=File.ReadAllLines(journal).Select(line=>JsonSerializer.Deserialize<UsageProviderAttempt>(line)!).ToList();
            Assert.AreEqual(attempts.Count+physicalCount,retained.Count);CollectionAssert.AreEqual(attempts.Select(row=>row.ProviderRequestId).ToArray(),retained.Take(attempts.Count).Select(row=>row.ProviderRequestId).ToArray());attempts=retained;return result;
        }
        var symbol="ESCROW-"+Guid.NewGuid().ToString("N");var id=Guid.NewGuid();var rejectedId=Guid.NewGuid();
        var sdk=typeof(NEAROASIS).GetField("_sdk",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
        Task<JsonElement> View(string method,object args)=>(Task<JsonElement>)sdk.GetType().GetMethod("ViewAsync")!.Invoke(sdk,new object?[]{method,args,null})!;
        async Task Balance(decimal expected){var value=await provider.GetCustomTokenBalanceAsync(symbol,"test.near");Assert.IsFalse(value.IsError,value.Message);Assert.AreEqual(expected,value.Result);}
        async Task Supply(string expected){var value=await View("token_get",new{symbol});Assert.AreEqual(expected,value.GetProperty("supply").GetString());}
        void Success<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result)=>Assert.IsFalse(result.IsError,result.Exception?.ToString()??result.Message);
        Success(await Own(()=>provider.MintTokenAsync(new MintWeb3TokenRequest{Symbol=symbol,Title="Exact native escrow fixture",Amount=10m}),1));
        await Balance(10m);await Supply("1000000000");
        Success(await Own(()=>provider.LockTokenAsync(new LockWeb3TokenRequest{TokenAddress=symbol,LockId=id,Amount=3m,EscrowContractAddress="test.near"}),1));
        await Balance(7m);await Supply("1000000000");
        Assert.IsTrue((await Own(()=>provider.LockTokenAsync(new LockWeb3TokenRequest{TokenAddress=symbol,LockId=id,Amount=1m}),0)).IsError);
        Success(await Own(()=>provider.BurnTokenAsync(new BurnWeb3TokenRequest{TokenAddress=symbol,Amount=2m,OwnerPrivateKey="",OwnerPublicKey="",OwnerSeedPhrase=""}),1));
        await Balance(5m);await Supply("800000000");
        Success(await Own(()=>provider.LockTokenAsync(new LockWeb3TokenRequest{TokenAddress=symbol}),1));
        var nativeRejected=await Own(()=>provider.LockTokenAsync(new LockWeb3TokenRequest{TokenAddress=symbol,LockId=rejectedId,Amount=1m}),1);
        Assert.IsTrue(nativeRejected.IsError);Assert.IsNotNull(nativeRejected.Exception);
        Assert.AreEqual(JsonValueKind.Null,(await View("token_lock_get",new{lock_id=rejectedId.ToString("N")})).ValueKind);
        await Balance(5m);await Supply("800000000");
        Success(await Own(()=>provider.UnlockTokenAsync(new UnlockWeb3TokenRequest{TokenAddress=symbol,LockId=id,EscrowContractAddress="test.near"}),1));
        await Balance(8m);await Supply("800000000");
        Assert.IsTrue((await View("token_lock_get",new{lock_id=id.ToString("N")})).GetProperty("released").GetBoolean());
        Assert.IsTrue((await Own(()=>provider.UnlockTokenAsync(new UnlockWeb3TokenRequest{TokenAddress=symbol,LockId=id}),0)).IsError);
        Success(await Own(()=>provider.UnlockTokenAsync(new UnlockWeb3TokenRequest{TokenAddress=symbol}),1));
        Success(await Own(()=>provider.BurnTokenAsync(new BurnWeb3TokenRequest{TokenAddress=symbol,Amount=8m,OwnerPrivateKey="",OwnerPublicKey="",OwnerSeedPhrase=""}),1));
        await Balance(0m);await Supply("0");
        Assert.AreEqual(8,attempts.Count);Assert.AreEqual(8,attempts.Select(row=>row.ProviderRequestId).Distinct().Count());
        var reader=new NEAROASIS(Required("NEAROASIS_RPCENDPOINT"),"oasis-near-local","oasis-near-local","test.near","test.near","read-only-invalid-signing-key");Success(await reader.ActivateProviderAsync());
        var originals=new List<JsonElement>();var outcomes=new List<JsonElement>();
        for(var i=0;i<attempts.Count;i++){
            var recovered=await reader.RecoverNativePreparationAsync(attempts[i]);Success(recovered);
            var observed=await reader.ReadPreparedNativeOutcomeAsync(recovered.Result);Success(observed);
            Assert.AreEqual("FINAL",observed.Result.GetProperty("final_execution_status").GetString());
            Assert.IsTrue(observed.Result.GetProperty("status").TryGetProperty(i==4?"Failure":"SuccessValue",out _));
            originals.Add(recovered.Result.Clone());outcomes.Add(observed.Result.Clone());
        }
        using(var file=new FileStream(complete,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new{symbol,id,rejectedId,attempts,originals,outcomes,physicalFsyncOwner=true,syntheticAdmission=true,
                publicTokenInterfaces=true,sameRetainedAccount=true,finalSpendable="0",finalSupply="0",preservedReleasedTombstone=true,hostedMongoVerified=false,publicFunds=false}));file.Flush(true);
        }
        var finalRecords=File.ReadAllLines(journal).Select(line=>JsonSerializer.Deserialize<UsageProviderAttempt>(line)!).ToArray();
        CollectionAssert.AreEqual(attempts.Select(row=>row.ProviderRequestId).ToArray(),finalRecords.Select(row=>row.ProviderRequestId).ToArray());File.Delete(journal);File.Delete(pending);
        Console.WriteLine("NEAR_PREPARED_TOKEN_VERIFIED "+JsonSerializer.Serialize(new{symbol,id,originals=attempts.Select(row=>row.ProviderRequestId).ToArray()}));
    }
}
