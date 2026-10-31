using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    [TestMethod]
    public async Task ActualPolygonBurnUsesExactNativeIdentityAndPreservesPreparationOwner()
    {
        Assert.AreEqual("http://127.0.0.1:18545",Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_RPC"));
        var key=Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY")!;
        var account=new Account(key,31337);var web3=new Web3(account,"http://127.0.0.1:18545");
        Assert.AreEqual(new BigInteger(31337),(await web3.Eth.ChainId.SendRequestAsync()).Value);
        var deployment=await web3.Eth.GetContractDeploymentHandler<NftBoundaryDeployment>()
            .SendRequestAndWaitForReceiptAsync(new NftBoundaryDeployment {Gas=new HexBigInteger(8000000)});
        var contract=web3.Eth.GetContract(ReadNftFixture()["abi"]!.ToString(),deployment.ContractAddress);
        var provider=new PolygonOASIS.PolygonOASIS("http://127.0.0.1:18545",key,deployment.ContractAddress,31337);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var sdk=(Web3CoreOASIS)typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
            var preparations=new List<System.Text.Json.JsonElement>();
            foreach(var id in new[] {BigInteger.Zero,(BigInteger.One<<256)-1})
            {
                Assert.IsTrue((await contract.GetFunction("seedToken").SendTransactionAndWaitForReceiptAsync(
                    account.Address,new HexBigInteger(500000),null,null,account.Address,id)).Succeeded());
                var request=new BurnWeb3NFTRequest {Web3NFTId=Guid.NewGuid(),TokenId=id.ToString(),
                    NFTTokenAddress=deployment.ContractAddress,OwnerPrivateKey=key,OwnerPublicKey="",OwnerSeedPhrase=""};
                var burned=await sdk.WithNativePreparationAsync(()=>provider.BurnNFTAsync(request),e=>{preparations.Add(e.Clone());return Task.CompletedTask;});
                Assert.IsFalse(burned.IsError,burned.Message);Assert.IsTrue(burned.IsSaved);Assert.IsTrue(burned.IsDeleted);
                Assert.AreEqual(id.ToString(),burned.Result.Web3NFT.TokenId);
                var prepared=preparations.Last();
                Assert.AreEqual(burned.Result.TransactionResult,prepared.GetProperty("transactionHash").GetString());
                Assert.AreEqual(deployment.ContractAddress.ToLowerInvariant(),prepared.GetProperty("contract").GetString()!.ToLowerInvariant());
                Assert.AreEqual("31337",prepared.GetProperty("chainId").GetString());
                await Assert.ThrowsExceptionAsync<SmartContractCustomErrorRevertException>(()=>contract.GetFunction("ownerOf").CallAsync<string>(id));
            }
            Assert.AreEqual(2,preparations.Count);
            var external=await web3.Eth.GetContractDeploymentHandler<NftBoundaryDeployment>()
                .SendRequestAndWaitForReceiptAsync(new NftBoundaryDeployment {Gas=new HexBigInteger(8000000)});
            var externalContract=web3.Eth.GetContract(ReadNftFixture()["abi"]!.ToString(),external.ContractAddress);
            Assert.IsTrue((await externalContract.GetFunction("seedToken").SendTransactionAndWaitForReceiptAsync(
                account.Address,new HexBigInteger(500000),null,null,account.Address,17)).Succeeded());
            var alternateTarget=new BurnWeb3NFTRequest {TokenId="17",NFTTokenAddress=external.ContractAddress,
                OwnerPrivateKey=key,OwnerPublicKey="",OwnerSeedPhrase=""};
            var nonce=(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address)).Value;
            var rejected=await sdk.WithNativePreparationAsync(()=>provider.BurnNFTAsync(alternateTarget),e=>{preparations.Add(e.Clone());return Task.CompletedTask;});
            Assert.IsTrue(rejected.IsError);Assert.AreEqual(2,preparations.Count);
            Assert.AreEqual(nonce,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address)).Value);
            Assert.IsFalse((await provider.BurnNFTAsync(alternateTarget)).IsError);
            var noOp=await web3.Eth.GetContractDeploymentHandler<NftNoOpDeployment>()
                .SendRequestAndWaitForReceiptAsync(new NftNoOpDeployment {Gas=new HexBigInteger(8000000)});
            var noOpContract=web3.Eth.GetContract(ReadNftFixture("web3-nft-noop.json")["abi"]!.ToString(),noOp.ContractAddress);
            Assert.IsTrue((await noOpContract.GetFunction("seedToken").SendTransactionAndWaitForReceiptAsync(
                account.Address,new HexBigInteger(500000),null,null,account.Address,19)).Succeeded());
            var unchanged=await provider.BurnNFTAsync(new BurnWeb3NFTRequest {TokenId="19",NFTTokenAddress=noOp.ContractAddress,
                OwnerPrivateKey=key,OwnerPublicKey="",OwnerSeedPhrase=""});
            Assert.IsTrue(unchanged.IsError);Assert.IsFalse(unchanged.IsSaved);
            Assert.IsInstanceOfType(unchanged.Exception,typeof(INativeMutationReconciliationRequired));
            Assert.IsTrue((await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(
                ((INativeMutationReconciliationRequired)unchanged.Exception).OriginalTransactionId)).Succeeded());
            Assert.AreEqual(account.Address.ToLowerInvariant(),(await noOpContract.GetFunction("ownerOf").CallAsync<string>(19)).ToLowerInvariant());
        }
        finally {await provider.DeActivateProviderAsync();}
    }
}
