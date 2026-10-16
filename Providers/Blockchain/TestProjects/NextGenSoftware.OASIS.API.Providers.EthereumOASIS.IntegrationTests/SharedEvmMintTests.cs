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
        var provider=new SharedMintFixture(Host,"0x"+new string('2',64),address);
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
                }
                Assert.AreEqual(before+(noop ? BigInteger.Zero:raw),await token.GetFunction("balanceOf").CallAsync<BigInteger>(recipient));
                Assert.AreEqual(supply+(noop ? BigInteger.Zero:raw),await token.GetFunction("totalSupply").CallAsync<BigInteger>());
            }
        }
        finally {await provider.DeActivateProviderAsync();await storage.DeActivateProviderAsync();}
    }

    private sealed class SharedMintFixture(string host,string configuredKey,string address):Web3CoreOASISBaseProvider(host,configuredKey,address,31337)
    {
        public Guid Avatar;
        protected override OASISResult<List<string>> ResolveErc20MintSigningKeys(Guid avatarId)
        {Avatar=avatarId;return new OASISResult<List<string>>{Result=new(){PrivateKey}};}
    }
}
