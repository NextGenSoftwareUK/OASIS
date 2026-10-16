using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    public sealed class NftBoundaryDeployment : ContractDeploymentMessage
    {
        public NftBoundaryDeployment() : base((string)ReadNftFixture()["bytecode"]!) { }
    }

    private static JObject ReadNftFixture() => JObject.Parse(File.ReadAllText(Path.Combine(
        Path.GetDirectoryName(Environment.GetEnvironmentVariable("WEB3CORE_NATIVE_FIXTURE"))!, "web3-nft-boundary.json")));

    [TestMethod]
    public async Task ActualPolygonNftTransfersPreserveUint256IdsAndRejectWrongSigner()
    {
        Assert.AreEqual("http://127.0.0.1:18545", Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_RPC"));
        var key = Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY")!;
        var account = new Account(key, 31337);
        var web3 = new Web3(account, "http://127.0.0.1:18545");
        Assert.AreEqual(new BigInteger(31337), (await web3.Eth.ChainId.SendRequestAsync()).Value);
        var receipt = await web3.Eth.GetContractDeploymentHandler<NftBoundaryDeployment>()
            .SendRequestAndWaitForReceiptAsync(new NftBoundaryDeployment { Gas = new HexBigInteger(8000000) });
        Assert.IsTrue(receipt.Succeeded());
        var contract = web3.Eth.GetContract(ReadNftFixture()["abi"]!.ToString(), receipt.ContractAddress);
        var recipient = "0x0000000000000000000000000000000000000001";
        var provider = new PolygonOASIS.PolygonOASIS("http://127.0.0.1:18545", key, receipt.ContractAddress, 31337);
        try
        {
            var activated = await provider.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Message);
            foreach (var id in new[] { BigInteger.Zero, BigInteger.One << 63, (BigInteger.One << 256) - 1 })
            {
                var seeded = await contract.GetFunction("seedToken").SendTransactionAndWaitForReceiptAsync(
                    account.Address, new HexBigInteger(500000), null, null, account.Address, id);
                Assert.IsTrue(seeded.Succeeded());
                foreach (var quantity in new[] { BigInteger.Zero, new BigInteger(2) })
                {
                    // Direct contract callers must obey the same ERC-721 invariant.
                    var rejected = await Assert.ThrowsExceptionAsync<Nethereum.JsonRpc.Client.RpcResponseException>(
                        () => contract.GetFunction("sendNFT").SendTransactionAndWaitForReceiptAsync(
                        account.Address, new HexBigInteger(500000), null, null,
                        account.Address, recipient, id, "None", "None", quantity, "invalid quantity"));
                    StringAssert.Contains(rejected.Message, "ERC721 amount must equal one");
                    Assert.AreEqual(account.Address.ToLowerInvariant(),
                        (await contract.GetFunction("ownerOf").CallAsync<string>(id)).ToLowerInvariant());
                }
                var invalidQuantity = await provider.SendNFTAsync(new SendWeb3NFTRequest {
                    FromWalletAddress = account.Address, ToWalletAddress = recipient,
                    TokenId = id.ToString(System.Globalization.CultureInfo.InvariantCulture), Amount = 2 });
                Assert.IsTrue(invalidQuantity.IsError);
                Assert.IsNull(invalidQuantity.Result);
                Assert.AreEqual("ERC-721 transfer amount must equal one.", invalidQuantity.Message);
                var transfer = await provider.SendNFTAsync(new SendWeb3NFTRequest {
                    FromWalletAddress = account.Address, ToWalletAddress = recipient,
                    TokenId = id.ToString(System.Globalization.CultureInfo.InvariantCulture), Amount = 1,
                    MemoText = "owned native token boundary" });
                Assert.IsFalse(transfer.IsError, transfer.Message);
                Assert.IsTrue(transfer.IsSaved);
                var transferReceipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transfer.Result.TransactionResult);
                Assert.IsTrue(transferReceipt.Succeeded());
                Assert.AreEqual(recipient, (await contract.GetFunction("ownerOf").CallAsync<string>(id)).ToLowerInvariant());

                // Configured signer is no longer the owner: no second successful transfer is allowed.
                var unauthorized = await provider.SendNFTAsync(new SendWeb3NFTRequest {
                    FromWalletAddress = recipient, ToWalletAddress = account.Address,
                    TokenId = id.ToString(System.Globalization.CultureInfo.InvariantCulture), Amount = 1 });
                Assert.IsTrue(unauthorized.IsError);
                Assert.IsFalse(unauthorized.IsSaved);
                Assert.AreEqual(recipient, (await contract.GetFunction("ownerOf").CallAsync<string>(id)).ToLowerInvariant());
            }
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
