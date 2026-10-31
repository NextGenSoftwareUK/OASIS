using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task SharedEvmEscrowUsesExactDepositOwnershipAndNeverReleasesPoolBalance(bool synchronous,bool badRuntime)
    {
        var (storage,address)=await CreateActivatedProviderAsync();
        var web3=new Web3(new Account(PrivateKey,ChainId),Host);var owner=web3.TransactionManager.Account.Address;
        using var json=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON"))!,"token-escrow.json")));
        var abi=json.RootElement.GetProperty("abi").GetRawText();
        var deployment=await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi,json.RootElement.GetProperty("bytecode").GetString(),owner,new HexBigInteger(4000000));
        var escrow=web3.Eth.GetContract(abi,deployment.ContractAddress);
        var provider=new SharedMintFixture(Host,"0x"+new string('2',64),address){TokenEscrowContractAddress=escrow.Address};
        provider.ProviderType=new(ProviderType.PolygonOASIS);provider.ProviderName="shared-escrow-api-fixture";provider.ProviderCategory=new(ProviderCategory.StorageAndNetwork);
        var dna=new OASISDNA();dna.OASIS.HyperDriveMode=HyperDriveModes.V2;
        var providers=new ProviderManager(null,dna);Assert.IsTrue(providers.RegisterProvider(provider));
        var wallets=new WalletManager(null,dna,providers);
        Assert.IsInstanceOfType<IOASISIdBoundTokenEscrowProvider>(provider);
        try
        {
            foreach(var decimals in badRuntime ? new byte[]{6}:new byte[]{6,30})
            {
                var token=await CreateTokenAsync(decimals);provider.TokenEscrowContractAddress=badRuntime ? token.Address:escrow.Address;
                var id=Guid.NewGuid();var actor=Guid.NewGuid();var units=125*BigInteger.Pow(10,decimals-2);
                var before=await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner);
                var supply=await token.GetFunction("totalSupply").CallAsync<BigInteger>();
                var nonce=(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value;
                var request=new LockWeb4TokenRequest{ProviderType=new EnumValue<ProviderType>(ProviderType.PolygonOASIS),LockId=id,LockedByAvatarId=actor,Web3TokenId=Guid.NewGuid(),Amount=1.25m,
                    TokenAddress=token.Address,EscrowContractAddress=provider.TokenEscrowContractAddress,FromWalletPrivateKey=PrivateKey,FromWalletAddress=owner};
                var locked=synchronous ? wallets.LockToken(request):await wallets.LockTokenAsync(request);
                if(badRuntime)
                {
                    Assert.IsTrue(locked.IsError);Assert.IsNull(locked.Result);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",locked.ErrorCode);
                    Assert.AreEqual(nonce,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value);return;
                }
                Assert.IsFalse(locked.IsError,locked.Exception?.ToString()??locked.Message);Assert.IsTrue(locked.IsSaved);
                var response=(Web3EscrowTransactionResponse)locked.Result;Assert.AreEqual(id,response.LockId);Assert.AreEqual(2,response.NativeTransactions.Count);
                var nativeId=SHA256.HashData(Encoding.UTF8.GetBytes(id.ToString("N")));
                var deposit=await escrow.GetFunction("deposits").CallDeserializingToObjectAsync<EthereumEscrowDeposit>(nativeId);
                Assert.AreEqual(units,deposit.Amount);Assert.AreEqual(owner.ToLowerInvariant(),deposit.Owner.ToLowerInvariant());Assert.IsFalse(deposit.Released);
                Assert.AreEqual(before-units,await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));
                Assert.AreEqual(units,await token.GetFunction("balanceOf").CallAsync<BigInteger>(escrow.Address));
                var unlock=new UnlockWeb4TokenRequest{ProviderType=request.ProviderType,LockId=id,UnlockedByAvatarId=actor,TokenAddress=token.Address,EscrowContractAddress=escrow.Address};
                provider.ExplicitKeys=new(){"0x"+new string('2',64)};
                var beforeDenied=(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value;
                var denied=await provider.UnlockTokenAsync(unlock);Assert.IsTrue(denied.IsError);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,denied.ErrorCode);
                Assert.AreEqual(beforeDenied,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value);
                provider.ExplicitKeys=new(){"0x"+new string('2',64),PrivateKey,PrivateKey[2..]};
                var released=synchronous ? wallets.UnlockToken(unlock):await wallets.UnlockTokenAsync(unlock);
                Assert.IsFalse(released.IsError,released.Exception?.ToString()??released.Message);Assert.IsTrue(released.IsSaved);
                var native=await web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(released.Result.TransactionResult);
                Assert.AreEqual(owner.ToLowerInvariant(),native.From.ToLowerInvariant());
                Assert.AreEqual(before,await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));
                Assert.AreEqual(BigInteger.Zero,await token.GetFunction("balanceOf").CallAsync<BigInteger>(escrow.Address));
                Assert.AreEqual(supply,await token.GetFunction("totalSupply").CallAsync<BigInteger>());
                Assert.IsTrue((await escrow.GetFunction("deposits").CallDeserializingToObjectAsync<EthereumEscrowDeposit>(nativeId)).Released);
                var replayNonce=(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value;
                var replay=await provider.UnlockTokenAsync(unlock);Assert.IsTrue(replay.IsError);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,replay.ErrorCode);
                var duplicate=await provider.LockTokenAsync(request);Assert.IsTrue(duplicate.IsError);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,duplicate.ErrorCode);
                Assert.AreEqual(replayNonce,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value);
            }
        }
        finally {await provider.DeActivateProviderAsync();await storage.DeActivateProviderAsync();}
    }
}
