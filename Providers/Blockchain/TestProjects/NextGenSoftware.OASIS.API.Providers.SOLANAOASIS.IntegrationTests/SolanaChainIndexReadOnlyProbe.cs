using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Rpc;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class SolanaChainIndexReadOnlyProbe
{
    [TestMethod,TestCategory("SolanaChainIndexReadOnly")]
    public async Task ObserveRetentionAndRangeWithoutIndexPublication()
    {
        const string endpoint="http://127.0.0.1:18899";
        Assert.AreEqual(endpoint,Environment.GetEnvironmentVariable("SOLANA_RPC_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_SOLANA_NATIVE_EVIDENCE")!;
        var output=Path.Combine(work,"solana-chain-index-readonly.json");Assert.IsFalse(File.Exists(output),"Preserve original diagnostic.");
        var rpc=DispatchProxy.Create<IRpcClient,ReadOnlyRpc>();var guard=(ReadOnlyRpc)(object)rpc;
        guard.Inner=ClientFactory.GetClient(endpoint);
        var genesis=await rpc.GetGenesisHashAsync();Assert.IsTrue(genesis.WasSuccessful);
        Assert.AreEqual("F22uw23o1HuahQiQVfd7Lfi6Q51fv4KNoYAvDnDDpEvj",genesis.Result);
        var first=await rpc.GetFirstAvailableBlockAsync();Assert.IsTrue(first.WasSuccessful);
        var secret=Encoders.Base58.DecodeData(Environment.GetEnvironmentVariable("SOLANA_PRIVATE_KEY")!);
        var storageKey=Convert.FromBase64String(Environment.GetEnvironmentVariable("OASIS_SOLANA_STORAGE_ENCRYPTION_KEY")!);
        try{
            var account=new Account(secret,Encoders.Base58.DecodeData(Environment.GetEnvironmentVariable("SOLANA_PUBLIC_KEY")!));Assert.AreEqual(Environment.GetEnvironmentVariable("SOLANA_PUBLIC_KEY"),account.PublicKey.Key);
            var repository=new SolanaRepository(account,rpc,()=> (byte[])storageKey.Clone());
            var source=new CosmicChainQuerySource(ProviderType.SolanaOASIS,genesis.Result,ProviderType.SQLLiteDBOASIS,
                Guid.NewGuid(),1,SolanaRepository.ChainHolonCatalogueHash(account.PublicKey.Key,genesis.Result),0);
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var result=await repository.ObserveFinalizedHolonRangeAsync(source,timeout.Token);
            var retentionRejected=result.IsError&&result.Exception?.Message=="Node retention cannot prove the configured source range; no complete checkpoint returned.";
            if(first.Result>0){Assert.IsTrue(retentionRejected,"Pruned declared coverage must not produce an empty complete index.");Assert.IsNull(result.Result);}
            Assert.AreEqual(0,guard.Writes);
            await using var file=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.None);
            await JsonSerializer.SerializeAsync(file,new {
                scope="Native read-only observer diagnostic, not native CQL/index acceptance",
                genesis=genesis.Result,writer=account.PublicKey.Key,coverageStart=0,firstAvailableBlock=first.Result,
                observerError=result.IsError,retentionRejected,observerReturnedRange=result.Result!=null,
                records=result.Result?.Records.Count,checkpoint=result.Result?.Checkpoint.Position.Slot,
                exceptionType=result.Exception?.GetType().FullName,readCalls=guard.Reads,submissions=guard.Writes,
                indexPublished=false,originalRecordRecoveryVerified=false});file.Flush(true);
        }finally{CryptographicOperations.ZeroMemory(secret);CryptographicOperations.ZeroMemory(storageKey);}
    }
    public class ReadOnlyRpc:DispatchProxy
    {
        public IRpcClient Inner=null!;public int Reads,Writes;
        protected override object? Invoke(MethodInfo? method,object?[]? args){
            if(method!.Name is not ("GetGenesisHashAsync" or "GetFirstAvailableBlockAsync" or "GetSlotAsync" or "GetBlockAsync" or "GetSignaturesForAddressAsync" or "get_NodeAddress")){
                Writes++;throw new InvalidOperationException("Only explicit read-only source observation RPCs are permitted.");
            }
            Reads++;return method.Invoke(Inner,args);
        }
    }
}
