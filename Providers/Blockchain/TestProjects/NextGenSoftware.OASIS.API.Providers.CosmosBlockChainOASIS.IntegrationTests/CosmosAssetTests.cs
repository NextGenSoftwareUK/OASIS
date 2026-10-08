using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests;

[TestClass]
public class CosmosAssetTests
{
    private CosmosBlockChainOASIS _provider = null!;
    private string _owner = null!;
    private string _recipient = null!;
    private string _contract = null!;
    private string _recipientKey = null!;
    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Real Cosmos test setting {name} is required; no skipped or mocked evidence.");

    [TestInitialize]
    public async Task Setup()
    {
        _contract = Required("OASIS_COSMOS_TEST_CONTRACT");
        _provider = new CosmosBlockChainOASIS(Required("OASIS_COSMOS_TEST_RPC"), Required("OASIS_COSMOS_TEST_CHAIN"),
            Required("OASIS_COSMOS_TEST_KEY"), _contract, "wasm", "0.025stake", "stake", 0);
        var active = await _provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);
        // The consensus contract identifies its minter; do not substitute a generated wallet.
        var config = await _provider.QueryContractAsync(_contract, new { config = new { } });
        Assert.IsFalse(config.IsError, config.Message);
        _owner = config.Result.GetProperty("owner").GetString()!;
        var wallet = await _provider.GenerateKeyPairAsync();
        Assert.IsFalse(wallet.IsError, wallet.Message);
        _recipient = wallet.Result.WalletAddressLegacy;
        _recipientKey = wallet.Result.PrivateKey;
        var funded = await _provider.SendTransactionAsync(_owner, _recipient, 100000, "Cosmos asset test actor gas");
        Assert.IsFalse(funded.IsError, funded.Message);
    }

    [TestCleanup]
    public void Cleanup() => _provider?.Dispose();

    [TestMethod]
    public async Task CustomTokenSupplyBalancesTransferLockAndBurnAreConsensusState()
    {
        var symbol = "TREE" + Guid.NewGuid().ToString("N");
        var minted = await _provider.MintTokenAsync(new MintWeb3TokenRequest { Symbol = symbol, Title = "Tree token", Amount = 12.5m });
        Assert.IsFalse(minted.IsError, minted.Message);
        var balance = await _provider.GetCustomTokenBalanceAsync(symbol, _owner);
        Assert.IsFalse(balance.IsError, balance.Message);
        Assert.AreEqual(12.5m, balance.Result);
        var send = new SendWeb3TokenRequest { FromTokenAddress = symbol, FromWalletAddress = _owner, ToWalletAddress = _recipient, Amount = 1.25m };
        var locked = await _provider.LockTokenAsync(new LockWeb3TokenRequest { TokenAddress = symbol });
        Assert.IsFalse(locked.IsError, locked.Message);
        var rejected = await _provider.SendTokenAsync(send);
        Assert.IsTrue(rejected.IsError);
        StringAssert.Contains(rejected.Message, "token is locked");
        var unlocked = await _provider.UnlockTokenAsync(new UnlockWeb3TokenRequest { TokenAddress = symbol });
        Assert.IsFalse(unlocked.IsError, unlocked.Message);
        var sent = await _provider.SendTokenAsync(send);
        Assert.IsFalse(sent.IsError, sent.Message);
        Assert.AreEqual(11.25m, (await _provider.GetCustomTokenBalanceAsync(symbol, _owner)).Result);
        Assert.AreEqual(1.25m, (await _provider.GetCustomTokenBalanceAsync(symbol, _recipient)).Result);
        var wrongSender = await _provider.SendTokenAsync(new SendWeb3TokenRequest
        {
            FromTokenAddress = symbol, FromWalletAddress = _owner, ToWalletAddress = _recipient, Amount = 1,
            OwnerPrivateKey = _recipientKey
        });
        Assert.IsTrue(wrongSender.IsError);
        StringAssert.Contains(wrongSender.Message, "does not own");
        var burnedRecipient = await _provider.BurnTokenAsync(new BurnWeb3TokenRequest { TokenAddress = symbol, OwnerPublicKey = _recipient, OwnerPrivateKey = _recipientKey, OwnerSeedPhrase = string.Empty });
        Assert.IsFalse(burnedRecipient.IsError, burnedRecipient.Message);
        var supply = await _provider.GetCustomTokenAsync(symbol);
        Assert.IsFalse(supply.IsError, supply.Message);
        Assert.AreEqual("1125000000", supply.Result.GetProperty("supply").GetString());
        var burnedOwner = await _provider.BurnTokenAsync(new BurnWeb3TokenRequest { TokenAddress = symbol, OwnerPublicKey = _owner, OwnerPrivateKey = string.Empty, OwnerSeedPhrase = string.Empty });
        Assert.IsFalse(burnedOwner.IsError, burnedOwner.Message);
        var zero = await _provider.GetCustomTokenAsync(symbol);
        Assert.IsFalse(zero.IsError, zero.Message);
        Assert.AreEqual("0", zero.Result.GetProperty("supply").GetString());
        Assert.AreEqual(0m, (await _provider.GetCustomTokenBalanceAsync(symbol, _owner)).Result);
    }

    [TestMethod]
    public async Task NftMintReadTransferLockEscrowReleaseAndBurnEnforceActualOwner()
    {
        var minted = await _provider.MintNFTAsync(new MintWeb3NFTRequest { Title = "Living Oak", Symbol = "OAK", NumberToMint = 1 });
        Assert.IsFalse(minted.IsError, minted.Message);
        Assert.IsNotNull(minted.Result.Web3NFT);
        var address = minted.Result.Web3NFT.NFTTokenAddress;
        var tokenId = address.Split('#')[1];
        var loaded = await _provider.LoadOnChainNFTDataAsync(address);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.AreEqual("Living Oak", loaded.Result.Title);
        Assert.AreEqual(_owner, loaded.Result.SendToAddressAfterMinting);
        var locked = await _provider.LockNFTAsync(new LockWeb3NFTRequest { NFTTokenAddress = address });
        Assert.IsFalse(locked.IsError, locked.Message);
        var send = new SendWeb3NFTRequest { TokenAddress = address, TokenId = tokenId, FromWalletAddress = _owner, ToWalletAddress = _recipient, Amount = 1 };
        var rejected = await _provider.SendNFTAsync(send);
        Assert.IsTrue(rejected.IsError);
        StringAssert.Contains(rejected.Message, "NFT is locked");
        var unlocked = await _provider.UnlockNFTAsync(new UnlockWeb3NFTRequest { NFTTokenAddress = address });
        Assert.IsFalse(unlocked.IsError, unlocked.Message);
        var sent = await _provider.SendNFTAsync(send);
        Assert.IsFalse(sent.IsError, sent.Message);
        Assert.AreEqual(_recipient, sent.Result.Web3NFT.SendToAddressAfterMinting);
        var unauthorized = await _provider.BurnNFTAsync(new BurnWeb3NFTRequest { NFTTokenAddress = address, OwnerPublicKey = _owner, OwnerPrivateKey = string.Empty, OwnerSeedPhrase = string.Empty });
        Assert.IsTrue(unauthorized.IsError);
        StringAssert.Contains(unauthorized.Message, "NFT owner required");
        var escrowed = await _provider.WithdrawNFTAsync(address, tokenId, _recipient, _recipientKey);
        Assert.IsFalse(escrowed.IsError, escrowed.Message);
        Assert.IsTrue(escrowed.Result.IsSuccessful);
        Assert.AreEqual(_owner, (await _provider.LoadOnChainNFTDataAsync(address)).Result.SendToAddressAfterMinting);
        var released = await _provider.DepositNFTAsync(address, tokenId, _recipient);
        Assert.IsFalse(released.IsError, released.Message);
        Assert.IsTrue(released.Result.IsSuccessful);
        Assert.AreEqual(BridgeTransactionStatus.Completed, (await _provider.GetTransactionStatusAsync(released.Result.TransactionId)).Result);
        var burned = await _provider.BurnNFTAsync(new BurnWeb3NFTRequest { NFTTokenAddress = address, OwnerPublicKey = _recipient, OwnerPrivateKey = _recipientKey, OwnerSeedPhrase = string.Empty });
        Assert.IsFalse(burned.IsError, burned.Message);
        var absent = await _provider.LoadOnChainNFTDataAsync(address);
        Assert.IsTrue(absent.IsError, "Burned NFT must not return a synthetic basic-info record.");
        Assert.IsNull(absent.Result);
    }

    [TestMethod]
    public async Task GenericContractQueryAndSignedExecutionUseDeployedAbi()
    {
        var key = "oasis/generic/" + Guid.NewGuid().ToString("N");
        var committed = await _provider.ExecuteContractAsync(_contract, new { put = new { key, value = "generic execution evidence" } }, fromWalletAddress: _owner);
        Assert.IsFalse(committed.IsError, committed.Message);
        var query = await _provider.QueryContractAsync(_contract, new { get = new { key } });
        Assert.IsFalse(query.IsError, query.Message);
        Assert.AreEqual("generic execution evidence", query.Result.GetString());
        var status = await _provider.GetTransactionStatusAsync(committed.Result.TransactionResult);
        Assert.IsFalse(status.IsError, status.Message);
        Assert.AreEqual(BridgeTransactionStatus.Completed, status.Result);
        var unauthorized = await _provider.ExecuteContractAsync(_contract, new { put = new { key, value = "must fail" } }, _recipientKey, _recipient);
        Assert.IsTrue(unauthorized.IsError);
        StringAssert.Contains(unauthorized.Message, "storage owner required");
        var deleted = await _provider.ExecuteContractAsync(_contract, new { delete = new { key } }, fromWalletAddress: _owner);
        Assert.IsFalse(deleted.IsError, deleted.Message);
        var missing = await _provider.QueryContractAsync(_contract, new { get = new { key } });
        Assert.IsFalse(missing.IsError, missing.Message);
        Assert.AreEqual(System.Text.Json.JsonValueKind.Null, missing.Result.ValueKind);
    }
}
