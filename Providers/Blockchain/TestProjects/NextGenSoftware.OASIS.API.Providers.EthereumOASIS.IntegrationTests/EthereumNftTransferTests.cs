using System.Numerics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [TestMethod]
    public async Task BurnUsesNativeIdNotApplicationGuidAndRequiresExactBurnEvent()
    {
        Assert.AreEqual("http://127.0.0.1:18545",Host);Assert.AreEqual(new BigInteger(31337),ChainId);
        var (provider,_)=await CreateActivatedProviderAsync();
        var account=new Account(PrivateKey,ChainId);var web3=new Web3(account,Host);
        var folder=Path.GetDirectoryName(Environment.GetEnvironmentVariable("WEB3CORE_NATIVE_FIXTURE"))!;
        try
        {
            foreach(var noOp in new[] {false,true})
            {
                using var fixture=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder,noOp ? "web3-nft-noop.json":"web3-nft-boundary.json")));
                var deployment=await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
                    fixture.RootElement.GetProperty("abi").GetRawText(),fixture.RootElement.GetProperty("bytecode").GetString(),account.Address,new HexBigInteger(8000000));
                var contract=web3.Eth.GetContract(fixture.RootElement.GetProperty("abi").GetRawText(),deployment.ContractAddress);
                foreach(var id in new[] {BigInteger.Zero,(BigInteger.One<<256)-1})
                {
                    Assert.AreEqual(BigInteger.One,(await contract.GetFunction("seedToken").SendTransactionAndWaitForReceiptAsync(
                        account.Address,new HexBigInteger(500000),null,null,account.Address,id)).Status.Value);
                    var request=new BurnWeb3NFTRequest {Web3NFTId=Guid.NewGuid(),TokenId=id.ToString(),
                        NFTTokenAddress=deployment.ContractAddress,OwnerPrivateKey=PrivateKey,OwnerPublicKey="",OwnerSeedPhrase=""};
                    var burned=id.IsZero ? provider.BurnNFT(request):await provider.BurnNFTAsync(request);
                    if(noOp)
                    {
                        Assert.IsTrue(burned.IsError);Assert.IsFalse(burned.IsSaved);
                        Assert.IsInstanceOfType(burned.Exception,typeof(INativeMutationReconciliationRequired));
                        var hash=((INativeMutationReconciliationRequired)burned.Exception).OriginalTransactionId;
                        Assert.AreEqual(BigInteger.One,(await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(hash)).Status.Value);
                        Assert.AreEqual(account.Address.ToLowerInvariant(),(await contract.GetFunction("ownerOf").CallAsync<string>(id)).ToLowerInvariant());
                    }
                    else
                    {
                        Assert.IsFalse(burned.IsError,burned.Message);Assert.IsTrue(burned.IsSaved);Assert.IsTrue(burned.IsDeleted);
                        Assert.AreEqual(id.ToString(),burned.Result.Web3NFT.TokenId);
                        Assert.AreEqual(deployment.ContractAddress,burned.Result.Web3NFT.NFTTokenAddress);
                        var absent=await Assert.ThrowsExceptionAsync<SmartContractCustomErrorRevertException>(
                            ()=>contract.GetFunction("ownerOf").CallAsync<string>(id));
                        var selector=Nethereum.Util.Sha3Keccack.Current.CalculateHash("ERC721NonexistentToken(uint256)").Substring(0,8);
                        StringAssert.Contains(absent.ExceptionEncodedData.ToLowerInvariant(),selector.ToLowerInvariant());
                        Assert.IsTrue(absent.ExceptionEncodedData.EndsWith(Convert.ToHexString(id.ToByteArray(isUnsigned:true,isBigEndian:true)).PadLeft(64,'0'),StringComparison.OrdinalIgnoreCase));
                    }
                }
                var nonce=(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address)).Value;
                foreach(var invalid in new string?[] {null,"","-1","+1"," 1",(BigInteger.One<<256).ToString(),Guid.NewGuid().ToString()})
                {
                    var missing=await provider.BurnNFTAsync(new BurnWeb3NFTRequest {Web3NFTId=Guid.NewGuid(),TokenId=invalid,
                        NFTTokenAddress=deployment.ContractAddress,OwnerPrivateKey=PrivateKey,OwnerPublicKey="",OwnerSeedPhrase=""});
                    Assert.IsTrue(missing.IsError);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",missing.ErrorCode);
                }
                Assert.AreEqual(nonce,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address)).Value);
            }
        }
        finally {await provider.DeActivateProviderAsync();}
    }

    [TestMethod]
    public async Task DirectErc721SendUsesOwnerKeyExactUint256AndReceiptOwnership()
    {
        Assert.AreEqual("http://127.0.0.1:18545",Host);
        Assert.AreEqual(new BigInteger(31337),ChainId);
        var (provider,_) = await CreateActivatedProviderAsync();
        var account=new Account(PrivateKey,ChainId);var web3=new Web3(account,Host);
        var folder=Path.GetDirectoryName(Environment.GetEnvironmentVariable("WEB3CORE_NATIVE_FIXTURE"))!;
        using var fixture=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder,"web3-nft-boundary.json")));
        var deployed=await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
            fixture.RootElement.GetProperty("abi").GetRawText(),fixture.RootElement.GetProperty("bytecode").GetString(),
            account.Address,new HexBigInteger(8000000));
        Assert.AreEqual(BigInteger.One,deployed.Status.Value);
        var contract=web3.Eth.GetContract(fixture.RootElement.GetProperty("abi").GetRawText(),deployed.ContractAddress);
        const string recipient="0x0000000000000000000000000000000000000001";
        try
        {
            foreach(var id in new[] {BigInteger.Zero,BigInteger.One<<63,(BigInteger.One<<256)-1})
            {
                Assert.IsTrue((await contract.GetFunction("seedToken").SendTransactionAndWaitForReceiptAsync(
                    account.Address,new HexBigInteger(500000),null,null,account.Address,id)).Status.Value == BigInteger.One);
                var request=new SendWeb3NFTRequest {TokenAddress=deployed.ContractAddress,TokenId=id.ToString(),
                    FromWalletAddress=account.Address,ToWalletAddress=recipient,OwnerPrivateKey=PrivateKey,Amount=1};
                var sent=id.IsZero ? provider.SendNFT(request):await provider.SendNFTAsync(request);
                Assert.IsFalse(sent.IsError,sent.Message);Assert.IsTrue(sent.IsSaved);
                Assert.AreEqual(recipient,(await contract.GetFunction("ownerOf").CallAsync<string>(id)).ToLowerInvariant());
                Assert.AreEqual(BigInteger.One,(await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(sent.Result.TransactionResult)).Status.Value);
                Assert.AreEqual(deployed.ContractAddress,sent.Result.Web3NFT.NFTTokenAddress);
            }
            var nonce=(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address)).Value;
            foreach(var invalid in new string?[] {null,"","-1","+1"," 1",(BigInteger.One<<256).ToString(),Guid.NewGuid().ToString()})
            {
                var rejected=await provider.SendNFTAsync(new SendWeb3NFTRequest {TokenAddress=deployed.ContractAddress,
                    TokenId=invalid,FromWalletAddress=account.Address,ToWalletAddress=recipient,OwnerPrivateKey=PrivateKey,Amount=1});
                Assert.IsTrue(rejected.IsError);Assert.IsFalse(rejected.IsSaved);
            }
            foreach(var amount in new[] {0m,2m,0.5m})
                Assert.IsTrue((await provider.SendNFTAsync(new SendWeb3NFTRequest {TokenAddress=deployed.ContractAddress,
                    TokenId="0",FromWalletAddress=account.Address,ToWalletAddress=recipient,OwnerPrivateKey=PrivateKey,Amount=amount})).IsError);
            Assert.IsTrue((await provider.SendNFTAsync(new SendWeb3NFTRequest {TokenAddress=deployed.ContractAddress,
                TokenId="0",FromWalletAddress=recipient,ToWalletAddress=account.Address,OwnerPrivateKey=PrivateKey,Amount=1})).IsError);
            Assert.AreEqual(nonce,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address)).Value);
            Assert.IsTrue((await provider.SendNFTAsync(new SendWeb3NFTRequest {TokenAddress=deployed.ContractAddress,
                TokenId="0",FromWalletAddress=account.Address,ToWalletAddress=recipient,Amount=1})).IsError);
            Assert.AreEqual(nonce,(await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(account.Address)).Value);
            using var noOp=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(folder,"web3-nft-noop.json")));
            var noOpReceipt=await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
                noOp.RootElement.GetProperty("abi").GetRawText(),noOp.RootElement.GetProperty("bytecode").GetString(),
                account.Address,new HexBigInteger(8000000));
            var noOpContract=web3.Eth.GetContract(noOp.RootElement.GetProperty("abi").GetRawText(),noOpReceipt.ContractAddress);
            Assert.AreEqual(BigInteger.One,(await noOpContract.GetFunction("seedToken").SendTransactionAndWaitForReceiptAsync(
                account.Address,new HexBigInteger(500000),null,null,account.Address,17)).Status.Value);
            var unchanged=await provider.SendNFTAsync(new SendWeb3NFTRequest {TokenAddress=noOpReceipt.ContractAddress,
                TokenId="17",FromWalletAddress=account.Address,ToWalletAddress=recipient,OwnerPrivateKey=PrivateKey,Amount=1});
            Assert.IsTrue(unchanged.IsError);Assert.IsFalse(unchanged.IsSaved);
            Assert.IsInstanceOfType(unchanged.Exception,typeof(INativeMutationReconciliationRequired));
            var identity=((INativeMutationReconciliationRequired)unchanged.Exception).OriginalTransactionId;
            Assert.AreEqual(BigInteger.One,(await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(identity)).Status.Value);
            Assert.AreEqual(account.Address.ToLowerInvariant(),(await noOpContract.GetFunction("ownerOf").CallAsync<string>(17)).ToLowerInvariant());
        }
        finally {await provider.DeActivateProviderAsync();}
    }
}
