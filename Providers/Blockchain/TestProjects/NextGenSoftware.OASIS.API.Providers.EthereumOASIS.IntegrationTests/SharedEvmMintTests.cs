using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task SharedNativeCoinSendBindsExactWeiAndAccountsForReceiptFee(bool synchronous,bool self)
    {
        var (storage,address)=await CreateActivatedProviderAsync();var provider=new SharedMintFixture(Host,"0x"+new string('2',64),address);
        var owner=new Nethereum.Web3.Accounts.Account(PrivateKey,ChainId).Address;var web3=new Web3(Host);
        try
        {
            Assert.IsFalse(provider.IsProviderActivated);
            var initialRequest=new GetWeb3WalletBalanceRequest{WalletAddress=owner};
            var initial=synchronous ? provider.GetBalance(initialRequest):await provider.GetBalanceAsync(initialRequest);
            Assert.IsFalse(initial.IsError,initial.Exception?.ToString()??initial.Message);Assert.IsTrue(initial.IsLoaded);Assert.IsTrue(provider.IsProviderActivated);
            Assert.AreEqual((double)(await web3.Eth.GetBalance.SendRequestAsync(owner)).Value/1e18,initial.Result);
            var nonce=(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value;
            var rejected=await provider.SendTokenAsync(new SendWeb3TokenRequest{FromTokenAddress=null!,ToWalletAddress=address,OwnerPrivateKey=PrivateKey,Amount=0.000000000000000001m});
            Assert.IsTrue(rejected.IsError);Assert.IsNull(rejected.Result);Assert.IsFalse(NativeMutationReconciliation.IsRequired(rejected.Exception));
            Assert.AreEqual(nonce,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value,"Nonpayable recipient preflight must not spend nonce or fees.");
            foreach(var amount in new[]{0.000000000000000001m,1.234567890123456789m})
            {
                var recipient=self ? owner:"0x2222222222222222222222222222222222222222";
                var before=(await web3.Eth.GetBalance.SendRequestAsync(owner)).Value;var targetBefore=(await web3.Eth.GetBalance.SendRequestAsync(recipient)).Value;
                var request=new SendWeb3TokenRequest{FromTokenAddress=null!,FromWalletAddress=owner,ToWalletAddress=recipient,OwnerPrivateKey=PrivateKey,Amount=amount};
                var sent=synchronous ? provider.SendToken(request):await provider.SendTokenAsync(request);
                Assert.IsFalse(sent.IsError,sent.Exception?.ToString()??sent.Message);Assert.IsTrue(sent.IsSaved);
                var transaction=await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(sent.Result.TransactionResult);
                var receipt=await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(sent.Result.TransactionResult);
                var wei=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ExactEvmTokenAmount.ToUnits(amount,18);
                Assert.AreEqual(wei,transaction.Value.Value);Assert.AreEqual(owner.ToLowerInvariant(),transaction.From.ToLowerInvariant());Assert.AreEqual(recipient.ToLowerInvariant(),transaction.To.ToLowerInvariant());
                Assert.IsTrue(receipt.Succeeded());Assert.IsNotNull(receipt.EffectiveGasPrice);var fee=receipt.GasUsed.Value*receipt.EffectiveGasPrice.Value;
                var historyRequest=new GetWeb3TransactionsRequest{WalletAddress=owner,FromBlock=(ulong)receipt.BlockNumber.Value,ToBlock=(ulong)receipt.BlockNumber.Value};
                var history=synchronous ? provider.GetTransactions(historyRequest):await provider.GetTransactionsAsync(historyRequest);
                Assert.IsFalse(history.IsError,history.Exception?.ToString()??history.Message);Assert.IsTrue(history.IsLoaded);
                var entry=history.Result.Single(row=>row.NativeTransactionId==sent.Result.TransactionResult);
                Assert.AreEqual(wei.ToString(),entry.RawAmount);Assert.AreEqual("wei",entry.AmountUnit);Assert.AreEqual("succeeded",entry.NativeExecutionResult);
                Assert.AreEqual(before-(self ? BigInteger.Zero:wei)-fee,(await web3.Eth.GetBalance.SendRequestAsync(owner)).Value);
                var balanceRequest=new GetWeb3WalletBalanceRequest{WalletAddress=owner};
                var displayed=synchronous ? provider.GetBalance(balanceRequest):await provider.GetBalanceAsync(balanceRequest);
                Assert.IsFalse(displayed.IsError,displayed.Exception?.ToString()??displayed.Message);Assert.IsTrue(displayed.IsLoaded);Assert.AreEqual(1,displayed.LoadedCount);
                Assert.AreEqual((double)(await web3.Eth.GetBalance.SendRequestAsync(owner)).Value/1e18,displayed.Result);
                if(!self) Assert.AreEqual(targetBefore+wei,(await web3.Eth.GetBalance.SendRequestAsync(recipient)).Value);
            }
        }
        finally {await provider.DeActivateProviderAsync();await storage.DeActivateProviderAsync();}
    }

    [DataTestMethod]
    [DataRow(false,false,false)] [DataRow(true,false,false)] [DataRow(false,true,false)] [DataRow(true,true,false)]
    [DataRow(false,false,true)] [DataRow(true,false,true)] [DataRow(false,true,true)] [DataRow(true,true,true)]
    public async Task SharedEvmTransferVerifiesExactUnitsSenderRecipientSupplyAndEvent(bool synchronous,bool self,bool noop)
    {
        var (storage,address)=await CreateActivatedProviderAsync();var provider=new SharedMintFixture(Host,"0x"+new string('2',64),address);
        var owner=new Nethereum.Web3.Accounts.Account(PrivateKey,ChainId).Address;
        try
        {
            foreach(var decimals in noop ? new byte[]{6}:new byte[]{6,30})
            {
                var token=await CreateTokenAsync(decimals,noop);var recipient=self ? owner:"0x2222222222222222222222222222222222222222";
                var ownerBefore=await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner);
                var recipientBefore=await token.GetFunction("balanceOf").CallAsync<BigInteger>(recipient);
                var supply=await token.GetFunction("totalSupply").CallAsync<BigInteger>();var raw=2345678*BigInteger.Pow(10,decimals-6);
                var request=new SendWeb3TokenRequest{FromTokenAddress=token.Address,FromWalletAddress=owner,ToWalletAddress=recipient,Amount=2.345678m,
                    OwnerPrivateKey=synchronous ? PrivateKey:null!,FromWalletPrivateKey=synchronous ? PrivateKey.Replace("0x",""):PrivateKey};
                var sent=synchronous ? provider.SendToken(request):await provider.SendTokenAsync(request);
                if(noop)
                {
                    Assert.IsTrue(sent.IsError);Assert.IsNull(sent.Result);Assert.IsFalse(sent.IsSaved);
                    var cause=(Web3MutationReconciliationException)sent.Exception;Assert.IsTrue(cause.ObservedReceipt.Succeeded());
                    Assert.AreEqual(cause.ObservedReceipt.TransactionHash,cause.OriginalTransactionId);
                    if(self) Assert.IsInstanceOfType(cause.InnerException,typeof(InvalidDataException),"Unchanged self-transfer balances cannot certify a no-op without its exact event.");
                }
                else
                {
                    Assert.IsFalse(sent.IsError,sent.Exception?.ToString()??sent.Message);Assert.IsTrue(sent.IsSaved);
                    var native=await new Web3(Host).Eth.Transactions.GetTransactionByHash.SendRequestAsync(sent.Result.TransactionResult);
                    Assert.AreEqual(token.Address.ToLowerInvariant(),native.To.ToLowerInvariant());Assert.AreEqual(owner.ToLowerInvariant(),native.From.ToLowerInvariant());
                }
                var delta=noop || self ? BigInteger.Zero:raw;
                Assert.AreEqual(ownerBefore-delta,await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));
                Assert.AreEqual(recipientBefore+delta,await token.GetFunction("balanceOf").CallAsync<BigInteger>(recipient));
                Assert.AreEqual(supply,await token.GetFunction("totalSupply").CallAsync<BigInteger>());
            }
        }
        finally {await provider.DeActivateProviderAsync();await storage.DeActivateProviderAsync();}
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task SharedEvmBurnUsesOnlyRequestedAmountAndVerifiesSupply(bool synchronous,bool noop)
    {
        var (storage,address)=await CreateActivatedProviderAsync();var provider=new SharedMintFixture(Host,"0x"+new string('2',64),address);
        var owner=new Nethereum.Web3.Accounts.Account(PrivateKey,ChainId).Address;var web3=new Web3(Host);
        try
        {
            foreach(var decimals in noop ? new byte[]{6}:new byte[]{6,30})
            {
                var token=await CreateTokenAsync(decimals,noop);var before=await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner);
                var supply=await token.GetFunction("totalSupply").CallAsync<BigInteger>();var raw=2345678*BigInteger.Pow(10,decimals-6);
                Assert.IsTrue(before>raw,"Fixture must distinguish exact burn from burning the whole balance.");
                if(decimals==6)
                {
                    var nonce=(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value;
                    var invalid=await provider.BurnTokenAsync(new BurnWeb3TokenRequest{TokenAddress=token.Address,OwnerPrivateKey=PrivateKey,OwnerPublicKey="",OwnerSeedPhrase="",Amount=0.0000001m});
                    Assert.IsTrue(invalid.IsError);Assert.IsNull(invalid.Result);Assert.IsFalse(NativeMutationReconciliation.IsRequired(invalid.Exception));
                    Assert.AreEqual(nonce,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value);
                }
                var request=new BurnWeb3TokenRequest{TokenAddress=token.Address,OwnerPrivateKey=PrivateKey,OwnerPublicKey=owner,OwnerSeedPhrase="",Amount=2.345678m};
                var burned=synchronous ? provider.BurnToken(request):await provider.BurnTokenAsync(request);
                if(noop)
                {
                    Assert.IsTrue(burned.IsError);Assert.IsNull(burned.Result);Assert.IsFalse(burned.IsSaved);
                    var cause=(Web3MutationReconciliationException)burned.Exception;Assert.IsTrue(cause.ObservedReceipt.Succeeded());
                    Assert.AreEqual(cause.ObservedReceipt.TransactionHash,cause.OriginalTransactionId);
                }
                else
                {
                    Assert.IsFalse(burned.IsError,burned.Exception?.ToString()??burned.Message);Assert.IsTrue(burned.IsSaved);
                    var native=await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(burned.Result.TransactionResult);
                    Assert.AreEqual(token.Address.ToLowerInvariant(),native.To.ToLowerInvariant());Assert.AreEqual(owner.ToLowerInvariant(),native.From.ToLowerInvariant());
                }
                Assert.AreEqual(before-(noop ? BigInteger.Zero:raw),await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));
                Assert.AreEqual(supply-(noop ? BigInteger.Zero:raw),await token.GetFunction("totalSupply").CallAsync<BigInteger>());
            }
        }
        finally {await provider.DeActivateProviderAsync();await storage.DeActivateProviderAsync();}
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task SharedEvmMintUsesRequestedContractRecipientUnitsAndNativeEffects(bool synchronous,bool noop)
    {
        var (storage,address)=await CreateActivatedProviderAsync();
        var provider=new SharedMintFixture(Host,"0x"+new string('2',64),address){MultipleWallets=!noop};
        try
        {
            foreach(var decimals in noop ? new byte[]{6}:new byte[]{6,30})
            {
                var token=await CreateTokenAsync(decimals,noop);const string recipient="0x2222222222222222222222222222222222222222";
                Assert.AreNotEqual(address,token.Address);
                var before=await token.GetFunction("balanceOf").CallAsync<BigInteger>(recipient);
                var supply=await token.GetFunction("totalSupply").CallAsync<BigInteger>();
                var request=new MintWeb3TokenRequest{MintedByAvatarId=Guid.NewGuid(),TokenAddress=token.Address,ToWalletAddress=recipient,Amount=2.345678m};
                var minted=synchronous ? provider.MintToken(request):await provider.MintTokenAsync(request);
                Assert.AreEqual(request.MintedByAvatarId,provider.Avatar);
                var raw=2345678*BigInteger.Pow(10,decimals-6);
                if(noop)
                {
                    Assert.IsTrue(minted.IsError);Assert.IsNull(minted.Result);Assert.IsFalse(minted.IsSaved);
                    var cause=(Web3MutationReconciliationException)minted.Exception;Assert.IsTrue(NativeMutationReconciliation.IsRequired(cause));
                    Assert.AreEqual(cause.ObservedReceipt.TransactionHash,cause.OriginalTransactionId);Assert.IsTrue(cause.ObservedReceipt.Succeeded());
                }
                else
                {
                    Assert.IsFalse(minted.IsError,minted.Exception?.ToString()??minted.Message);Assert.IsTrue(minted.IsSaved);
                    var receipt=await new Web3(Host).Eth.Transactions.GetTransactionReceipt.SendRequestAsync(minted.Result.TransactionResult);Assert.IsTrue(receipt.Succeeded());
                    var native=await new Web3(Host).Eth.Transactions.GetTransactionByHash.SendRequestAsync(minted.Result.TransactionResult);
                    Assert.AreEqual(token.Address.ToLowerInvariant(),native.To.ToLowerInvariant());
                    Assert.AreEqual(new Nethereum.Web3.Accounts.Account(PrivateKey,ChainId).Address.ToLowerInvariant(),native.From.ToLowerInvariant());
                }
                Assert.AreEqual(before+(noop ? BigInteger.Zero:raw),await token.GetFunction("balanceOf").CallAsync<BigInteger>(recipient));
                Assert.AreEqual(supply+(noop ? BigInteger.Zero:raw),await token.GetFunction("totalSupply").CallAsync<BigInteger>());
            }
            Assert.AreEqual(noop ? 0:2,provider.DefaultReads,"Only multiple distinct authorized accounts require default selection.");
        }
        finally {await provider.DeActivateProviderAsync();await storage.DeActivateProviderAsync();}
    }

    private sealed class SharedMintFixture(string host,string configuredKey,string address):Web3CoreOASISBaseProvider(host,configuredKey,address,31337)
    {
        public Guid Avatar;public bool MultipleWallets;public int DefaultReads;
        protected override OASISResult<List<string>> ResolveErc20MintSigningKeys(Guid avatarId)
        {Avatar=avatarId;return new OASISResult<List<string>>{Result=MultipleWallets ? new(){"0x"+new string('2',64),PrivateKey,PrivateKey[2..]}:new(){PrivateKey}};}
        protected override Task<OASISResult<string>> ResolveDefaultErc20MintWalletAsync(Guid avatarId)
        {Assert.AreEqual(Avatar,avatarId);DefaultReads++;return Task.FromResult(new OASISResult<string>{Result=new Nethereum.Web3.Accounts.Account(PrivateKey,ChainId).Address});}
    }
}
