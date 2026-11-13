using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class NearPreparedLifecycleTests
{
    [TestMethod][TestCategory("RetainedSignedInspection")]
    public Task PublicNftBridgeWalletAndEncryptedHistoryUseOneExistingNativeOwner()=>RunLifecycle(false);

    [TestMethod][TestCategory("RetainedSignedInspection")]
    public Task PublicNftDepositIdsRejectStaleSourceAndReleasedReplay()=>RunLifecycle(true);

    private async Task RunLifecycle(bool uniqueDeposit)
    {
        string Required(string name)=>Environment.GetEnvironmentVariable(name)??throw new InvalidOperationException(name+" required");
        Assert.AreEqual("http://127.0.0.1:3030",Required("NEAROASIS_RPCENDPOINT"));Assert.AreEqual("oasis-near-local",Required("NEAROASIS_NETWORKID"));Assert.AreEqual("test.near",Required("NEAROASIS_ACCOUNTID"));
        var root=new DirectoryInfo(Environment.CurrentDirectory);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Scripts","run_near_provider_evidence.ps1")))root=root.Parent;Assert.IsNotNull(root);
        var stem=uniqueDeposit?"near-prepared-nft-deposit":"near-prepared-lifecycle";var folder=Path.Combine(root.FullName,"TestResults","NearRuntime");
        var pending=Path.Combine(folder,stem+".pending.json");var journal=Path.Combine(folder,stem+".attempts.jsonl");var complete=Path.Combine(folder,stem+".complete.json");
        var published=Path.Combine(root.FullName,"Docs","WIP","HyperDrive-Providers","NativeEvidence",stem+".complete.json");
        Assert.IsFalse(new[]{pending,journal,complete,published}.Any(File.Exists),"Reconcile originals; never replay a pending/completed cohort");
        var provider=new NEAROASIS(Required("NEAROASIS_RPCENDPOINT"),"oasis-near-local","oasis-near-local","test.near","test.near",Required("NEAROASIS_PRIVATEKEY"));
        var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);
        using(var file=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new{plannedNativeActions=uniqueDeposit?23:18,synthetic=true,account="test.near",purpose="public NFT/wallet prepared lifecycle"}));file.Flush(true);
        }
        var rawSdk=typeof(NEAROASIS).GetField("_sdk",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
        Task<string> NativeCall(string method,object args)=>(Task<string>)rawSdk.GetType().GetMethod("CallAsync")!.Invoke(rawSdk,new object?[]{method,args,null,"0",null,null})!;
        async Task<NextGenSoftware.OASIS.Common.OASISResult<string>> NativeReject(string method,object args,string reason){
            try{await NativeCall(method,args);}
            catch(NearMutationRejectedException rejected){StringAssert.Contains(rejected.NativeStatusJson,reason);return new(){IsError=true,Exception=rejected,Message=rejected.Message};}
            Assert.Fail("The native contract unexpectedly accepted an invalid custody operation");return new();
        }
        var attempts=new List<UsageProviderAttempt>();var nativeRejections=new HashSet<string>();var sequence=0;
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
        var mint=await Own(()=>provider.MintNFTAsync(new MintWeb3NFTRequest{Title="Owned lifecycle Café tree",Description="synthetic retained native acceptance",Symbol="OASIS",MetaData=new(){{"fixture",stem}}}),1);
        Assert.IsFalse(mint.IsError,mint.Exception?.ToString()??mint.Message);var token=mint.Result.Web3NFT.NFTTokenAddress;
        var loaded=await provider.LoadOnChainNFTDataAsync(token);Assert.IsFalse(loaded.IsError,loaded.Message);Assert.AreEqual("Owned lifecycle Café tree",loaded.Result.Title);Assert.AreEqual("test.near",loaded.Result.SendToAddressAfterMinting);
        var locked=await Own(()=>provider.LockNFTAsync(new LockWeb3NFTRequest{NFTTokenAddress=token}),1);Assert.IsFalse(locked.IsError,locked.Exception?.ToString()??locked.Message);Assert.AreEqual("True",locked.Result.Web3NFT.MetaData["near:locked"]);
        var unlocked=await Own(()=>provider.UnlockNFTAsync(new UnlockWeb3NFTRequest{NFTTokenAddress=token}),1);Assert.IsFalse(unlocked.IsError,unlocked.Exception?.ToString()??unlocked.Message);Assert.AreEqual("False",unlocked.Result.Web3NFT.MetaData["near:locked"]);
        var withdrawnNft=await Own(()=>provider.WithdrawNFTAsync(token,token,"test.near",Required("NEAROASIS_PRIVATEKEY")),1);Assert.IsFalse(withdrawnNft.IsError,withdrawnNft.Exception?.ToString()??withdrawnNft.Message);Assert.IsTrue(withdrawnNft.Result.IsSuccessful);
        if(uniqueDeposit){
            var current=await provider.LoadOnChainNFTDataAsync(token);Assert.IsFalse(current.IsError,current.Message);
            Assert.AreNotEqual(locked.Result.Web3NFT.MetaData["near:lock-id"].ToString(),current.Result.MetaData["near:lock-id"].ToString());
            var stale=await Own(()=>provider.DepositNFTAsync(token,token,"test.near",locked.Result.TransactionResult),0);
            Assert.IsTrue(stale.IsError,"Old-cycle source must never release a newer deposit");Assert.IsFalse(stale.Result.IsSuccessful);
            foreach(var rejected in new[]{await Own(()=>NativeReject("nft_set_locked",new{token_id=token,locked=false,lock_id=locked.Result.Web3NFT.MetaData["near:lock-id"].ToString()},"Current unreleased owned NFT deposit required"),1),
                await Own(()=>NativeReject("nft_burn",new{token_id=token},"Locked NFT cannot be burned"),1)}){
                Assert.IsInstanceOfType(rejected.Exception,typeof(NearMutationRejectedException));nativeRejections.Add(((NearMutationRejectedException)rejected.Exception).OriginalTransactionId);
            }
            var preserved=await provider.LoadOnChainNFTDataAsync(token);Assert.IsFalse(preserved.IsError,preserved.Message);Assert.AreEqual("True",preserved.Result.MetaData["near:locked"]);Assert.AreEqual(current.Result.MetaData["near:lock-id"],preserved.Result.MetaData["near:lock-id"]);

        }
        var depositedNft=await Own(()=>provider.DepositNFTAsync(token,token,"test.near",withdrawnNft.Result.TransactionId),3);Assert.IsFalse(depositedNft.IsError,depositedNft.Exception?.ToString()??depositedNft.Message);Assert.IsTrue(depositedNft.Result.IsSuccessful);
        var nftHash=depositedNft.Result.TransactionId;Assert.AreEqual("test.near",(await provider.LoadOnChainNFTDataAsync(token)).Result.SendToAddressAfterMinting);
        if(uniqueDeposit){
            var replay=await Own(()=>provider.DepositNFTAsync(token,token,"test.near",withdrawnNft.Result.TransactionId),0);
            Assert.IsTrue(replay.IsError,"Released source must not authorize a second deposit");Assert.IsFalse(replay.Result.IsSuccessful);
        }
        var burned=await Own(()=>provider.BurnNFTAsync(new BurnWeb3NFTRequest{TokenId=token,OwnerSeedPhrase=string.Empty,OwnerPublicKey=string.Empty,OwnerPrivateKey=string.Empty}),1);Assert.IsFalse(burned.IsError,burned.Exception?.ToString()??burned.Message);Assert.IsNull((await provider.LoadOnChainNFTDataAsync(token)).Result);
        if(uniqueDeposit){
            await Own(()=>NativeCall("nft_mint",new{token_id=token,owner_id="test.near",metadata_json="{}"}),1);
            var relock=await Own(()=>NativeReject("nft_set_locked",new{token_id=token,locked=true,lock_id=locked.Result.Web3NFT.MetaData["near:lock-id"].ToString()},"Unlocked owned NFT and unused deposit ID required"),1);
            Assert.IsInstanceOfType(relock.Exception,typeof(NearMutationRejectedException));nativeRejections.Add(((NearMutationRejectedException)relock.Exception).OriginalTransactionId);
            var reminted=await provider.LoadOnChainNFTDataAsync(token);Assert.IsFalse(reminted.IsError,reminted.Message);Assert.AreEqual("False",reminted.Result.MetaData["near:locked"]);
            await Own(()=>NativeCall("nft_burn",new{token_id=token}),1);Assert.IsNull((await provider.LoadOnChainNFTDataAsync(token)).Result);
        }
        const decimal oneYocto=0.000000000000000000000001m;
        var sent=await Own(()=>provider.SendTransactionAsync("test.near","test.near",oneYocto,"owned lifecycle native send"),2);Assert.IsFalse(sent.IsError,sent.Exception?.ToString()??sent.Message);
        var deposit=await Own(()=>provider.DepositAsync(oneYocto,"test.near"),2);Assert.IsFalse(deposit.IsError,deposit.Exception?.ToString()??deposit.Message);Assert.IsTrue(deposit.Result.IsSuccessful);
        var withdraw=await Own(()=>provider.WithdrawAsync(oneYocto,"test.near",Required("NEAROASIS_PRIVATEKEY")),2);Assert.IsFalse(withdraw.IsError,withdraw.Exception?.ToString()??withdraw.Message);Assert.IsTrue(withdraw.Result.IsSuccessful);
        var parentHashes=new[]{nftHash,sent.Result.TransactionResult,deposit.Result.TransactionId,withdraw.Result.TransactionId};Assert.AreEqual(4,parentHashes.Distinct().Count());
        var history=await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress="test.near"});Assert.IsFalse(history.IsError,history.Exception?.ToString()??history.Message);
        foreach(var hash in parentHashes){var id=new Guid(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(hash)).Take(16).ToArray());var row=history.Result.Single(row=>row.TransactionId==id);Assert.AreEqual("test.near",row.FromWalletAddress);Assert.AreEqual("test.near",row.ToWalletAddress);}
        Assert.AreEqual(uniqueDeposit?19:14,attempts.Count);
        var reader=new NEAROASIS(Required("NEAROASIS_RPCENDPOINT"),"oasis-near-local","oasis-near-local","test.near","test.near","read-only-invalid-signing-key");var readerActive=await reader.ActivateProviderAsync();Assert.IsFalse(readerActive.IsError,readerActive.Message);
        var originals=new List<JsonElement>();var outcomes=new List<JsonElement>();var expectedCiphertexts=new Dictionary<string,string>();
        async Task Observe(UsageProviderAttempt attempt){
            var recovered=await reader.RecoverNativePreparationAsync(attempt);Assert.IsFalse(recovered.IsError,recovered.Exception?.ToString()??recovered.Message);
            var observed=await reader.ReadPreparedNativeOutcomeAsync(recovered.Result);Assert.IsFalse(observed.IsError,observed.Exception?.ToString()??observed.Message);Assert.IsTrue(observed.Result.GetProperty("status").TryGetProperty(nativeRejections.Contains(attempt.ProviderRequestId)?"Failure":"SuccessValue",out _));
            originals.Add(recovered.Result.Clone());outcomes.Add(observed.Result.Clone());
            var intent=recovered.Result.GetProperty("intent");if(intent.GetProperty("kind").GetString()=="functionCall"&&intent.GetProperty("methodName").GetString()=="put"){
                var args=JsonSerializer.Deserialize<JsonElement>(Convert.FromBase64String(intent.GetProperty("argsBase64").GetString()!));expectedCiphertexts.Add(args.GetProperty("key").GetString()!,args.GetProperty("value").GetString()!);
            }
        }
        foreach(var attempt in attempts)await Observe(attempt);Assert.AreEqual(4,expectedCiphertexts.Count);
        var keys=parentHashes.Select(hash=>"wallet-transaction:test.near:"+hash).ToArray();CollectionAssert.AreEquivalent(keys,expectedCiphertexts.Keys.ToArray());
        var sdk=typeof(NEAROASIS).GetField("_sdk",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;var type=sdk.GetType();
        async Task<object> Snapshot(string key){var task=(Task)type.GetMethod("ReadSnapshotAsync")!.Invoke(sdk,new object[]{key,CancellationToken.None})!;await task;return task.GetType().GetProperty("Result")!.GetValue(task)!;}
        string? Field(object snapshot,string name)=>(string?)snapshot.GetType().GetProperty(name)!.GetValue(snapshot);
        var hashes=new Dictionary<string,string>();foreach(var key in keys){var snapshot=await Snapshot(key);Assert.AreEqual(expectedCiphertexts[key],Field(snapshot,"Ciphertext"));Assert.IsFalse(Field(snapshot,"Ciphertext")!.Contains("test.near"));hashes[key]=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(expectedCiphertexts[key])));}
        foreach(var key in keys){
            Assert.AreEqual(expectedCiphertexts[key],Field(await Snapshot(key),"Ciphertext"));
            var hash=await Own(()=>(Task<string>)type.GetMethod("DeleteAsync")!.Invoke(sdk,new object[]{key})!,1);Assert.AreEqual(attempts[^1].ProviderRequestId,hash);await Observe(attempts[^1]);Assert.IsNull(Field(await Snapshot(key),"Ciphertext"));
        }
        Assert.AreEqual(uniqueDeposit?23:18,attempts.Count);Assert.AreEqual(uniqueDeposit?23:18,attempts.Select(row=>row.ProviderRequestId).Distinct().Count());Assert.AreEqual(uniqueDeposit?3:0,nativeRejections.Count);
        using(var file=new FileStream(complete,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            file.Write(JsonSerializer.SerializeToUtf8Bytes(new{token,keys,hashes,parentHashes,attempts,outcomes,nativeRejections,physicalFsyncOwner=true,syntheticAdmission=true,publicNftAndWalletInterfaces=true,
                sameRetainedAccount=true,encryptedWalletHistory=true,verifiedFixturesAbsent=true,nftBurned=true,uniqueDepositProtocolVerified=uniqueDeposit,hostedMongoVerified=false,publicFunds=false}));file.Flush(true);
        }
        var finalRecords=File.ReadAllLines(journal).Select(line=>JsonSerializer.Deserialize<UsageProviderAttempt>(line)!).ToArray();CollectionAssert.AreEqual(attempts.Select(row=>row.ProviderRequestId).ToArray(),finalRecords.Select(row=>row.ProviderRequestId).ToArray());File.Delete(journal);File.Delete(pending);
        Console.WriteLine("NEAR_PREPARED_PUBLIC_LIFECYCLE_VERIFIED "+JsonSerializer.Serialize(new{token,keys,originals=attempts.Select(row=>row.ProviderRequestId).ToArray()}));
    }
}
