using System.Numerics;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow("avatar")][DataRow("avatar-detail")][DataRow("holon")]
    public async Task AcceptedStorageResponseLossRetainsOriginalSignedHashAndArchive(string kind)
    {
        var folder=Path.GetDirectoryName(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON"))!;
        var journal=Path.Combine(folder,"ethereum-accepted-loss.pending.json");
        if(File.Exists(journal))throw new InvalidOperationException("Reconcile the original Ethereum fault before allocating another fixture.");
        var (healthy,address)=await CreateActivatedProviderAsync(history:true);
        var id=Guid.NewGuid();
        using(var initial=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            var bytes=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{phase="created",id,kind,contract=address})+"\n");initial.Write(bytes);initial.Flush(true);
        }
        void Persist(object evidence){
            using var stream=new FileStream(journal,FileMode.Append,FileAccess.Write,FileShare.None);
            var bytes=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evidence)+"\n");stream.Write(bytes);stream.Flush(true);
        }
        IHolonBase source=kind switch{"avatar"=>new Avatar{Id=id},"avatar-detail"=>new AvatarDetail{Id=id},_=>new Core.Holons.Holon{Id=id}};
        source.Description="accepted-first-marker";
        async Task<(bool Error,bool Saved,object? Value,Exception? Cause,string Message)> Save(EthereumOASIS provider){
            if(kind=="avatar"){var r=await provider.SaveAvatarAsync((IAvatar)source);return(r.IsError,r.IsSaved,r.Result,r.Exception,r.Message);}
            if(kind=="avatar-detail"){var r=await provider.SaveAvatarDetailAsync((IAvatarDetail)source);return(r.IsError,r.IsSaved,r.Result,r.Exception,r.Message);}
            var h=await provider.SaveHolonAsync((IHolon)source,saveChildren:false);return(h.IsError,h.IsSaved,h.Result,h.Exception,h.Message);
        }
        try {
            var seed=await Save(healthy);Assert.IsFalse(seed.Error,seed.Message);Assert.AreEqual(1,source.Version);
            Persist(new{phase="seed-acknowledged",version=source.Version});
            var versionId=source.VersionId;var previous=source.PreviousVersionId;var storageKey=source.ProviderUniqueStorageKey[Core.Enums.ProviderType.EthereumOASIS];
            source.Description="accepted-second-marker";
            OwnedNativePreparationScope Scope(EthereumOASIS provider)=>(OwnedNativePreparationScope)typeof(EthereumOASIS).GetField("_storagePreparation",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(provider)!;
            var probe=new Web3(new Account(PrivateKey,ChainId),Host);var signer=probe.TransactionManager.Account.Address;
            var nonceBefore=await probe.Eth.Transactions.GetTransactionCount.SendRequestAsync(signer,Nethereum.RPC.Eth.DTOs.BlockParameter.CreatePending());
            var persistenceFailure=new IOException("Owned preparation persistence failure");
            var blocked=await Scope(healthy).RunAsync(()=>Save(healthy),(_,_)=>Task.FromException(persistenceFailure));
            Assert.IsTrue(blocked.Error);Assert.IsFalse(blocked.Saved);Assert.AreSame(persistenceFailure,blocked.Cause!.InnerException);Assert.AreEqual(1,source.Version);
            using(var cancellation=new CancellationTokenSource()){
                var canceled=await Scope(healthy).RunAsync(()=>Save(healthy),(evidence,_)=>{Persist(new{phase="cancel-before-submit",evidence});cancellation.Cancel();return Task.CompletedTask;},cancellation.Token);
                Assert.IsTrue(canceled.Error);Assert.IsFalse(canceled.Saved);Assert.IsInstanceOfType(canceled.Cause!.InnerException,typeof(OperationCanceledException));
            }
            Assert.AreEqual(nonceBefore.Value,(await probe.Eth.Transactions.GetTransactionCount.SendRequestAsync(signer,Nethereum.RPC.Eth.DTOs.BlockParameter.CreatePending())).Value);
            Assert.AreEqual(1,(await Read(0)).Version);Assert.AreEqual(1,source.Version);
            string? preparedHash=null;string accepted;
            JsonElement providerProof=default;
            await using(var proxy=new Web3AcceptedWriteProxy(raw=>{
                var bytes=raw.HexToByteArray();preparedHash="0x"+Convert.ToHexString(Sha3Keccack.Current.CalculateHash(bytes)).ToLowerInvariant();
                Assert.AreEqual(preparedHash,providerProof.GetProperty("transactionHash").GetString());
                CollectionAssert.AreEqual(raw.HexToByteArray(),Convert.FromBase64String(providerProof.GetProperty("signedTransactionBase64").GetString()!));
                Persist(new{phase="before-fault-forward",transactionHash=preparedHash,signedTransactionBase64=Convert.ToBase64String(bytes)});
                return Task.CompletedTask;
            })){
                var fault=new EthereumOASIS("http://127.0.0.1:18546",PrivateKey,ChainId,address){IsVersionControlEnabled=true};
                Assert.IsFalse((await fault.ActivateProviderAsync()).IsError);
                try {
                    var failed=await Scope(fault).RunAsync(()=>Save(fault),(evidence,_)=>{
                        Assert.AreEqual(0,proxy.Methods.Count(method=>method=="eth_sendRawTransaction"));
                        providerProof=evidence.Clone();Persist(new{phase="provider-prepared-before-send",evidence});return Task.CompletedTask;
                    });
                    Assert.IsTrue(failed.Error);Assert.IsFalse(failed.Saved);Assert.IsNull(failed.Value);
                    Assert.IsInstanceOfType(failed.Cause,typeof(EthereumStorageReceiptReconciliationException));
                    accepted=proxy.AcceptedHashes.Single();
                    Assert.AreEqual(preparedHash,accepted);Assert.AreEqual(accepted,((EthereumStorageReceiptReconciliationException)failed.Cause!).OriginalTransactionId);
                    Assert.AreEqual(1,proxy.Methods.Count(method=>method=="eth_sendRawTransaction"));
                    Assert.AreEqual(1,source.Version);Assert.AreEqual(versionId,source.VersionId);Assert.AreEqual(previous,source.PreviousVersionId);
                    Assert.AreEqual(storageKey,source.ProviderUniqueStorageKey[Core.Enums.ProviderType.EthereumOASIS]);
                }finally{await fault.DeActivateProviderAsync();}
            }
            var web3=new Web3(new Account(PrivateKey,ChainId),Host);
            var receipt=await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(accepted);
            Assert.IsNotNull(receipt);Assert.AreEqual(BigInteger.One,receipt.Status.Value);Assert.AreEqual(accepted,receipt.TransactionHash);
            async Task<IHolonBase> Read(int version)=>kind switch {
                "avatar"=>(await healthy.LoadAvatarAsync(id,version)).Result,
                "avatar-detail"=>(await healthy.LoadAvatarDetailAsync(id,version)).Result,
                _=>(await healthy.LoadHolonAsync(id,loadChildren:false,version:version)).Result
            };
            var current=await Read(0);Assert.AreEqual(2,current.Version);Assert.AreEqual("accepted-second-marker",current.Description);
            var archived=await Read(1);Assert.AreEqual(1,archived.Version);Assert.AreEqual("accepted-first-marker",archived.Description);
            Persist(new{phase="original-reconciled",transactionHash=accepted,version=current.Version});
            var service=new NextGenSoftwareOASISService(web3,address);var numeric=new BigInteger(HashUtility.GetNumericHash(id.ToString()));
            if(kind=="avatar"){var deleted=await healthy.DeleteAvatarAsync(id,softDelete:false);Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsTrue(deleted.Result);Assert.AreEqual(BigInteger.Zero,(await service.GetAvatarByIdQueryAsync(numeric)).ReturnValue1.EntityId);}
            else if(kind=="avatar-detail"){
                var deleted=await service.ContractHandler.SendRequestAndWaitForReceiptAsync(new ContractDefinition.DeleteAvatarDetailByIdentityFunction{EntityId=numeric,Identity=id.ToString()});
                Assert.AreEqual(BigInteger.One,deleted.Status.Value);Assert.AreEqual(BigInteger.Zero,(await service.GetAvatarDetailByIdQueryAsync(numeric)).ReturnValue1.EntityId);
            }else{var deleted=await healthy.DeleteHolonAsync(id);Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsTrue(deleted.IsDeleted);Assert.AreEqual(BigInteger.Zero,(await service.GetHolonByIdQueryAsync(numeric)).ReturnValue1.EntityId);}
            if(Environment.GetEnvironmentVariable("OASIS_WEB3_PREPARED_OUTBOX_REQUIRED")=="1")await VerifyEthereumPreparedOutboxAsync(healthy,address,kind,Persist);
            File.Delete(journal); // Only after original receipt/archive reconciliation and exact fixture/outbox verification.
            Console.WriteLine(JsonSerializer.Serialize(new{proof="ETHEREUM_ACCEPTED_RESPONSE_LOSS_VERIFIED",kind,id,transactionHash=accepted,
                gasUsed=receipt.GasUsed.Value.ToString(),gasPriceWei=receipt.EffectiveGasPrice?.Value.ToString(),
                feeWei=receipt.EffectiveGasPrice==null?null:(receipt.GasUsed.Value*receipt.EffectiveGasPrice.Value).ToString(),fixtureAbsent=true}));
        }finally{await healthy.DeActivateProviderAsync();} // Unresolved originals/journal are deliberately retained.
    }
}
