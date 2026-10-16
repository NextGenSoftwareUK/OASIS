using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false,"normal")] [DataRow(true,"normal")]
    [DataRow(false,"preapproved")] [DataRow(true,"preapproved")]
    [DataRow(false,"bad-runtime")] [DataRow(true,"bad-runtime")]
    [DataRow(false,"approve-loss")] [DataRow(true,"approve-loss")]
    [DataRow(false,"lock-loss")] [DataRow(true,"lock-loss")]
    [DataRow(false,"unlock-loss")] [DataRow(true,"unlock-loss")]
    public async Task NativeEscrowProviderUsesExplicitCustodyAndPreservesEachStage(bool synchronous,string scenario)
    {
        var (storage, storageAddress) = await CreateActivatedProviderAsync();
        var web3 = new Web3(new Account(PrivateKey,ChainId),Host);
        var owner = web3.TransactionManager.Account.Address;
        var token = await CreateTokenAsync(6);
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(
            Path.GetDirectoryName(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON"))!,"token-escrow.json")));
        var abi = json.RootElement.GetProperty("abi").GetRawText();
        var deployed = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi,
            json.RootElement.GetProperty("bytecode").GetString(),owner,new HexBigInteger(4000000));
        var escrow = web3.Eth.GetContract(abi,deployed.ContractAddress);
        var provider = new EscrowFixtureProvider(Host,PrivateKey,ChainId,storageAddress) {
            TokenEscrowContractAddress = scenario=="bad-runtime" ? token.Address : escrow.Address,
            LoseStage = scenario.EndsWith("-loss",StringComparison.Ordinal) ? scenario.Split('-')[0] : null };
        try
        {
            var id = Guid.NewGuid();var actor = Guid.NewGuid();
            var nativeId = SHA256.HashData(Encoding.UTF8.GetBytes(id.ToString("N")));
            var request = new LockWeb3TokenRequest { LockId=id,LockedByAvatarId=actor,Web3TokenId=Guid.NewGuid(),
                Amount=1.25m,TokenAddress=token.Address,EscrowContractAddress=provider.TokenEscrowContractAddress,
                FromWalletAddress=owner,FromWalletPrivateKey=PrivateKey };
            var initial = await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner);
            var units = new BigInteger(1250000);
            if (scenario=="preapproved") await token.GetFunction("approve").SendTransactionAndWaitForReceiptAsync(owner,
                new HexBigInteger(200000),null,null,escrow.Address,units);
            var nonce = (await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value;
            var locked = synchronous ? provider.LockToken(request) : await provider.LockTokenAsync(request);
            if (scenario=="bad-runtime")
            {
                Assert.IsTrue(locked.IsError);Assert.IsNull(locked.Result);
                Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",locked.ErrorCode);
                Assert.AreEqual(nonce,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value);
                Assert.AreEqual(initial,await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));return;
            }
            if (scenario is "approve-loss" or "lock-loss")
            {
                Assert.IsTrue(locked.IsError);Assert.IsFalse(locked.IsSaved);Assert.IsNull(locked.Result);
                var evidence=(EthereumEscrowReconciliationException)locked.Exception;
                Assert.IsTrue(NativeMutationReconciliation.IsRequired(evidence));
                Assert.AreEqual(provider.Receipt!.TransactionHash,evidence.OriginalTransactionId);
                Assert.AreSame(provider.Cause,evidence.InnerException);
                Assert.AreEqual(scenario=="approve-loss" ? 0:1,evidence.ConfirmedTransactions.Count);
                Assert.AreEqual(scenario.Split('-')[0],evidence.UncertainStage);
                var record=await escrow.GetFunction("deposits").CallDeserializingToObjectAsync<EthereumEscrowDeposit>(nativeId);
                Assert.AreEqual(scenario=="approve-loss" ? BigInteger.Zero:units,record.Amount);
                Assert.AreEqual(scenario=="approve-loss" ? initial:initial-units,await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));
                Assert.AreEqual(nonce+(scenario=="approve-loss" ? 1:2),(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value);
                return;
            }
            Assert.IsFalse(locked.IsError,locked.Message+" "+locked.Exception);Assert.IsTrue(locked.IsSaved);
            var response=(EthereumEscrowTransactionResponse)locked.Result;
            Assert.AreEqual(id,response.LockId);Assert.AreEqual(scenario=="preapproved" ? 1:2,response.NativeTransactions.Count);
            foreach(var hash in response.NativeTransactions)
                Assert.AreEqual(BigInteger.One,(await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(hash)).Status.Value);
            var unlockRequest=new UnlockWeb3TokenRequest {LockId=id,UnlockedByAvatarId=actor,TokenAddress=token.Address,EscrowContractAddress=escrow.Address};
            provider.SigningKey=Nethereum.Signer.EthECKey.GenerateKey().GetPrivateKey();
            var outsider=new Account(provider.SigningKey,ChainId).Address;
            var funding=await web3.TransactionManager.SendTransactionAsync(new TransactionInput {
                From=owner,To=outsider,Value=new HexBigInteger(BigInteger.Pow(10,18)),Gas=new HexBigInteger(21000) });
            Assert.AreEqual(BigInteger.One,(await web3.TransactionManager.TransactionReceiptService.PollForReceiptAsync(funding)).Status.Value);
            var beforeDenied=(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value;
            var outsiderNonce=(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(outsider)).Value;
            var denied=await provider.UnlockTokenAsync(unlockRequest);
            Assert.IsTrue(denied.IsError);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,denied.ErrorCode);
            Assert.AreEqual(beforeDenied,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value);
            Assert.AreEqual(outsiderNonce,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(outsider)).Value);
            Assert.IsFalse((await escrow.GetFunction("deposits").CallDeserializingToObjectAsync<EthereumEscrowDeposit>(nativeId)).Released);
            provider.SigningKey=PrivateKey;
            var released=synchronous ? provider.UnlockToken(unlockRequest):await provider.UnlockTokenAsync(unlockRequest);
            if(scenario=="unlock-loss")
            {
                Assert.IsTrue(released.IsError);Assert.IsNull(released.Result);
                var evidence=(EthereumEscrowReconciliationException)released.Exception;
                Assert.AreEqual("unlock",evidence.UncertainStage);Assert.AreSame(provider.Cause,evidence.InnerException);
                Assert.AreEqual(provider.Receipt!.TransactionHash,evidence.OriginalTransactionId);Assert.AreEqual(0,evidence.ConfirmedTransactions.Count);
            }
            else
            {
                Assert.IsFalse(released.IsError,released.Message+" "+released.Exception);Assert.IsTrue(released.IsSaved);
                Assert.AreEqual(1,((EthereumEscrowTransactionResponse)released.Result).NativeTransactions.Count);
            }
            Assert.IsTrue((await escrow.GetFunction("deposits").CallDeserializingToObjectAsync<EthereumEscrowDeposit>(nativeId)).Released);
            Assert.AreEqual(initial,await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));
            Assert.AreEqual(BigInteger.Zero,await token.GetFunction("balanceOf").CallAsync<BigInteger>(escrow.Address));
            nonce=(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value;
            var replay=await provider.UnlockTokenAsync(unlockRequest);
            Assert.IsTrue(replay.IsError);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,replay.ErrorCode);
            var reuse=await provider.LockTokenAsync(request);
            Assert.IsTrue(reuse.IsError);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,reuse.ErrorCode);
            Assert.AreEqual(nonce,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value);
            if(scenario=="preapproved")
            {
                await token.GetFunction("approve").SendTransactionAndWaitForReceiptAsync(owner,new HexBigInteger(200000),null,null,escrow.Address,units);
                request.LockId=Guid.NewGuid();request.Amount=2.5m;
                nonce=(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value;
                var partial=await provider.LockTokenAsync(request);
                Assert.IsTrue(partial.IsError);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",partial.ErrorCode);
                Assert.IsFalse(NativeMutationReconciliation.IsRequired(partial.Exception));
                Assert.AreEqual(nonce,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value);
                Assert.AreEqual(initial,await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));
            }
        }
        finally {await provider.DeActivateProviderAsync();await storage.DeActivateProviderAsync();}
    }

    [TestMethod]
    public async Task MissingEscrowInputsFailBeforeActivationOrKeyLookup()
    {
        var provider=new EscrowFixtureProvider(null!,PrivateKey,ChainId,"0x"+new string('1',40));
        var locked=await provider.LockTokenAsync(null!);var released=await provider.UnlockTokenAsync(null!);
        Assert.IsTrue(locked.IsError);Assert.IsTrue(released.IsError);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",locked.ErrorCode);Assert.AreEqual(locked.ErrorCode,released.ErrorCode);
        Assert.AreEqual(0,provider.KeyCalls);Assert.IsFalse(provider.IsProviderActivated);
        provider.TokenEscrowContractAddress="0x"+new string('2',40);
        var redirected=await provider.LockTokenAsync(new LockWeb3TokenRequest {
            LockId=Guid.NewGuid(),LockedByAvatarId=Guid.NewGuid(),Amount=1,TokenAddress="0x"+new string('4',40),
            EscrowContractAddress="0x"+new string('3',40) });
        Assert.IsTrue(redirected.IsError);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",redirected.ErrorCode);
        Assert.AreEqual(0,provider.KeyCalls);Assert.IsFalse(provider.IsProviderActivated);
    }

    private sealed class EscrowFixtureProvider(string host,string key,BigInteger chain,string storage) : EthereumOASIS(host,key,chain,storage)
    {
        public string? LoseStage;public TransactionReceipt? Receipt;public int KeyCalls;
        public string SigningKey=PrivateKey;
        public readonly IOException Cause=new("Owned escrow response loss after actual confirmation.");
        protected override OASISResult<List<string>> ResolveTokenSigningKeys(Guid avatarId)
        {KeyCalls++;return new OASISResult<List<string>>(new List<string>{SigningKey});}
        protected override async Task<TransactionReceipt> PollEscrowReceiptAsync(Web3 web3,string hash,string stage)
        {var receipt=await base.PollEscrowReceiptAsync(web3,hash,stage);if(stage==LoseStage){Receipt=receipt;throw Cause;}return receipt;}
    }
}
