using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Providers.AptosOASIS;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using System.Text;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS.IntegrationTests;

[TestClass]
public class AptosOASISIntegrationTests
{
    private static string RequiredEnvironment(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new AssertFailedException($"Required real-runtime setting {name} is missing.");

    private static AptosOASIS CreateProvider() => new(
        RequiredEnvironment("OASIS_APTOS_RPC_ENDPOINT"),
        "local",
        RequiredEnvironment("OASIS_APTOS_PRIVATE_KEY"),
        RequiredEnvironment("OASIS_APTOS_CONTRACT_ADDRESS"));

    [TestMethod]
    public async Task OfficialSdk_ActivatesAgainstRealAptosLedger()
    {
        using var provider = CreateProvider();
        var result = await provider.ActivateProviderAsync();

        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(result.Result);
        Assert.IsTrue(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task OfficialSdk_PerformsRealHolonCreateReadUpdateDelete()
    {
        using var provider = CreateProvider();
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);

        var holon = new Holon
        {
            Id = Guid.NewGuid(),
            Name = "aptos-runtime-create",
            Description = "written through the Aptos Labs SDK"
        };

        var created = await provider.SaveHolonAsync(holon);
        Assert.IsFalse(created.IsError, created.Message);

        var loaded = await provider.LoadHolonAsync(holon.Id);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.AreEqual(holon.Name, loaded.Result.Name);

        holon.Name = "aptos-runtime-update";
        var updated = await provider.SaveHolonAsync(holon);
        Assert.IsFalse(updated.IsError, updated.Message);
        var reloaded = await provider.LoadHolonAsync(holon.Id);
        Assert.IsFalse(reloaded.IsError, reloaded.Message);
        Assert.AreEqual("aptos-runtime-update", reloaded.Result.Name);

        var deleted = await provider.DeleteHolonAsync(holon.Id);
        Assert.IsFalse(deleted.IsError, deleted.Message);
        Assert.AreEqual(holon.Id, deleted.Result.Id);
    }

    [TestMethod]
    public async Task OfficialSdk_PerformsRealAvatarAndDetailCrudAndLookup()
    {
        using var provider = CreateProvider();
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);

        var marker = Guid.NewGuid().ToString("N");
        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = $"aptos-{marker}",
            Email = $"{marker}@aptos.test"
        };
        var detail = new AvatarDetail
        {
            Id = avatar.Id,
            Username = avatar.Username,
            Email = avatar.Email,
            Karma = 42
        };

        var savedAvatar = await provider.SaveAvatarAsync(avatar);
        var savedDetail = await provider.SaveAvatarDetailAsync(detail);
        Assert.IsFalse(savedAvatar.IsError, savedAvatar.Message);
        Assert.IsFalse(savedDetail.IsError, savedDetail.Message);

        var byId = await provider.LoadAvatarAsync(avatar.Id);
        var byUsername = await provider.LoadAvatarByUsernameAsync(avatar.Username);
        var byEmail = await provider.LoadAvatarByEmailAsync(avatar.Email);
        var loadedDetail = await provider.LoadAvatarDetailAsync(detail.Id);
        Assert.IsFalse(byId.IsError, byId.Message);
        Assert.IsFalse(byUsername.IsError, byUsername.Message);
        Assert.IsFalse(byEmail.IsError, byEmail.Message);
        Assert.IsFalse(loadedDetail.IsError, loadedDetail.Message);
        Assert.AreEqual(avatar.Id, byId.Result.Id);
        Assert.AreEqual(avatar.Id, byUsername.Result.Id);
        Assert.AreEqual(avatar.Id, byEmail.Result.Id);
        Assert.AreEqual(42, loadedDetail.Result.Karma);

        var deleted = await provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
        Assert.IsFalse(deleted.IsError, deleted.Message);
        Assert.IsTrue(deleted.Result);
        var missing = await provider.LoadAvatarAsync(avatar.Id);
        Assert.IsFalse(missing.IsError, missing.Message);
        Assert.IsNull(missing.Result);
    }

    [TestMethod]
    public async Task OfficialSdk_EnumeratesFiltersSearchesAndDeletesStoredRecords()
    {
        using var provider = CreateProvider();
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var marker = Guid.NewGuid().ToString("N");
        var parentId = Guid.NewGuid();
        var holon = new Holon
        {
            Id = Guid.NewGuid(),
            ParentHolonId = parentId,
            Name = $"searchable-{marker}",
            Description = "official Aptos Move table search evidence",
            MetaData = new Dictionary<string, object> { ["runtime"] = marker }
        };
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = $"delete-{marker}", Email = $"delete-{marker}@aptos.test" };
        Assert.IsFalse((await provider.SaveHolonAsync(holon)).IsError);
        Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);

        var all = await provider.LoadAllHolonsAsync();
        var children = await provider.LoadHolonsForParentAsync(parentId);
        var metadata = await provider.LoadHolonsByMetaDataAsync("runtime", marker);
        var search = await provider.SearchAsync(new SearchParams
        {
            SearchOnlyForCurrentAvatar = false,
            SearchGroups = new List<ISearchGroupBase> { new SearchTextGroup { SearchQuery = marker, SearchHolons = true } }
        });
        Assert.IsFalse(all.IsError, all.Message);
        Assert.IsTrue(all.Result.Any(x => x.Id == holon.Id));
        Assert.IsTrue(children.Result.Any(x => x.Id == holon.Id));
        Assert.IsTrue(metadata.Result.Any(x => x.Id == holon.Id));
        Assert.IsFalse(search.IsError, search.Message);
        Assert.IsTrue(search.Result.SearchResultHolons.Any(x => x.Id == holon.Id));

        var deleteByUsername = await provider.DeleteAvatarByUsernameAsync(avatar.Username, softDelete: false);
        Assert.IsFalse(deleteByUsername.IsError, deleteByUsername.Message);
        Assert.IsTrue(deleteByUsername.Result);
        Assert.IsFalse((await provider.DeleteHolonAsync(holon.Id)).IsError);
    }

    [TestMethod]
    public async Task OfficialSdk_RejectsUnreachableFullnode()
    {
        using var provider = new AptosOASIS(
            "http://127.0.0.1:1/v1",
            "unreachable",
            RequiredEnvironment("OASIS_APTOS_PRIVATE_KEY"),
            RequiredEnvironment("OASIS_APTOS_CONTRACT_ADDRESS"));

        var result = await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.Result);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task OfficialSdk_SignsAndCommitsNativeAptTransfer()
    {
        using var provider = CreateProvider();
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var recipient = await provider.GenerateKeyPairAsync();
        Assert.IsFalse(recipient.IsError, recipient.Message);
        var sent = await provider.SendTokenAsync(new SendWeb3TokenRequest
        {
            ToWalletAddress = recipient.Result.WalletAddressLegacy,
            Amount = 0.00000001m
        });
        Assert.IsFalse(sent.IsError, sent.Message);
        Assert.IsFalse(string.IsNullOrWhiteSpace(sent.Result.TransactionResult));
        var balance = await provider.GetBalanceAsync(new GetWeb3WalletBalanceRequest { WalletAddress = RequiredEnvironment("OASIS_APTOS_ACCOUNT_ADDRESS") });
        Assert.IsFalse(balance.IsError, balance.Message);
        Assert.IsTrue(balance.Result > 0d);
    }

    [TestMethod]
    public async Task OfficialSdk_PerformsMoveBackedNftLifecycle()
    {
        using var provider = CreateProvider();
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var marker = Guid.NewGuid().ToString("N");
        var minted = await provider.MintNFTAsync(new MintWeb3NFTRequest
        {
            Title = $"Aptos NFT {marker}", Symbol = "OASIS", JSONMetaData = $"{{\"marker\":\"{marker}\"}}",
            MetaData = new Dictionary<string, string> { ["marker"] = marker }, Tags = new List<string> { "aptos", "runtime" }
        });
        Assert.IsFalse(minted.IsError, minted.Message);
        var key = minted.Result.Web3NFT.NFTTokenAddress;
        Assert.IsFalse(string.IsNullOrWhiteSpace(minted.Result.TransactionResult));
        var loaded = await provider.LoadOnChainNFTDataAsync(key);
        Assert.AreEqual($"Aptos NFT {marker}", loaded.Result.Title);

        var sent = await provider.SendNFTAsync(new SendWeb3NFTRequest { TokenId = key, ToWalletAddress = "0x123" });
        Assert.IsFalse(sent.IsError, sent.Message);
        Assert.AreEqual("0x123", sent.Result.Web3NFT.SendToAddressAfterMinting);
        var locked = await provider.LockNFTAsync(new LockWeb3NFTRequest { NFTTokenAddress = key, LockedByAvatarId = Guid.NewGuid() });
        Assert.IsFalse(locked.IsError, locked.Message);
        Assert.AreEqual("True", locked.Result.Web3NFT.MetaData["aptos:locked"]);
        var unlocked = await provider.UnlockNFTAsync(new UnlockWeb3NFTRequest { NFTTokenAddress = key, UnlockedByAvatarId = Guid.NewGuid() });
        Assert.IsFalse(unlocked.IsError, unlocked.Message);
        Assert.AreEqual("False", unlocked.Result.Web3NFT.MetaData["aptos:locked"]);
        var burned = await provider.BurnNFTAsync(new BurnWeb3NFTRequest { NFTTokenAddress = key, OwnerPublicKey = "", OwnerPrivateKey = "", OwnerSeedPhrase = "" });
        Assert.IsFalse(burned.IsError, burned.Message);
        Assert.IsNull((await provider.LoadOnChainNFTDataAsync(key)).Result);
    }

    [TestMethod]
    public async Task OfficialSdk_PerformsMoveBackedCustomTokenLifecycle()
    {
        using var provider = CreateProvider();
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var symbol = $"OASIS-{Guid.NewGuid():N}";
        var owner = RequiredEnvironment("OASIS_APTOS_ACCOUNT_ADDRESS");
        var recipient = await provider.GenerateKeyPairAsync();
        Assert.IsFalse(recipient.IsError, recipient.Message);

        var minted = await provider.MintTokenAsync(new MintWeb3TokenRequest
        {
            Symbol = symbol,
            Title = "OASIS Aptos runtime token",
            Description = "Move-backed custom-token lifecycle evidence",
            Amount = 100m,
            MetaData = new Dictionary<string, string> { ["evidence"] = "real-localnet" }
        });
        Assert.IsFalse(minted.IsError, minted.Message);
        Assert.AreEqual(100m, (await provider.GetCustomTokenBalanceAsync(symbol, owner)).Result);

        var sent = await provider.SendTokenAsync(new SendWeb3TokenRequest
        {
            FromTokenAddress = symbol,
            FromWalletAddress = owner,
            ToWalletAddress = recipient.Result.WalletAddressLegacy,
            Amount = 10m
        });
        Assert.IsFalse(sent.IsError, sent.Message);
        Assert.AreEqual(90m, (await provider.GetCustomTokenBalanceAsync(symbol, owner)).Result);
        Assert.AreEqual(10m, (await provider.GetCustomTokenBalanceAsync(symbol, recipient.Result.WalletAddressLegacy)).Result);

        var locked = await provider.LockTokenAsync(new LockWeb3TokenRequest { TokenAddress = symbol });
        Assert.IsFalse(locked.IsError, locked.Message);
        var rejectedWhileLocked = await provider.SendTokenAsync(new SendWeb3TokenRequest
        {
            FromTokenAddress = symbol,
            FromWalletAddress = owner,
            ToWalletAddress = recipient.Result.WalletAddressLegacy,
            Amount = 1m
        });
        Assert.IsTrue(rejectedWhileLocked.IsError);

        var unlocked = await provider.UnlockTokenAsync(new UnlockWeb3TokenRequest { TokenAddress = symbol });
        Assert.IsFalse(unlocked.IsError, unlocked.Message);
        var sentAfterUnlock = await provider.SendTokenAsync(new SendWeb3TokenRequest
        {
            FromTokenAddress = symbol,
            FromWalletAddress = owner,
            ToWalletAddress = recipient.Result.WalletAddressLegacy,
            Amount = 1m
        });
        Assert.IsFalse(sentAfterUnlock.IsError, sentAfterUnlock.Message);

        var burned = await provider.BurnTokenAsync(new BurnWeb3TokenRequest
        {
            TokenAddress = symbol,
            OwnerPublicKey = "",
            OwnerPrivateKey = "",
            OwnerSeedPhrase = ""
        });
        Assert.IsFalse(burned.IsError, burned.Message);
        Assert.IsTrue((await provider.GetCustomTokenBalanceAsync(symbol, owner)).IsError);
    }

    [TestMethod]
    public async Task OfficialSdk_PerformsBridgeTransferAndReportsCommittedStatus()
    {
        using var provider = CreateProvider();
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var recipient = await provider.GenerateKeyPairAsync();
        Assert.IsFalse(recipient.IsError, recipient.Message);

        var deposited = await provider.DepositAsync(0.00000001m, recipient.Result.WalletAddressLegacy);
        Assert.IsFalse(deposited.IsError, deposited.Message);
        Assert.IsTrue(deposited.Result.IsSuccessful);
        var status = await provider.GetTransactionStatusAsync(deposited.Result.TransactionId);
        Assert.IsFalse(status.IsError, status.Message);
        Assert.AreEqual(BridgeTransactionStatus.Completed, status.Result);

        var rejected = await provider.WithdrawAsync(
            0.00000001m,
            RequiredEnvironment("OASIS_APTOS_ACCOUNT_ADDRESS"),
            recipient.Result.PrivateKey);
        Assert.IsTrue(rejected.IsError, "A private key for another Aptos account must be rejected.");

        var withdrawn = await provider.WithdrawAsync(
            0.00000001m,
            RequiredEnvironment("OASIS_APTOS_ACCOUNT_ADDRESS"),
            RequiredEnvironment("OASIS_APTOS_PRIVATE_KEY"));
        Assert.IsFalse(withdrawn.IsError, withdrawn.Message);
        Assert.AreEqual(BridgeTransactionStatus.Completed, (await provider.GetTransactionStatusAsync(withdrawn.Result.TransactionId)).Result);
    }

    [TestMethod]
    public async Task OfficialSdk_ExecutesGenericMoveEntryFunction()
    {
        using var provider = CreateProvider();
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var marker = Guid.NewGuid().ToString("N");
        var executed = await provider.SendSmartContractFunctionAsync(
            RequiredEnvironment("OASIS_APTOS_CONTRACT_ADDRESS"),
            "oasis::upsert_record",
            Encoding.UTF8.GetBytes("generic-evidence"),
            Encoding.UTF8.GetBytes(marker),
            Encoding.UTF8.GetBytes($"{{\"marker\":\"{marker}\"}}"));
        Assert.IsFalse(executed.IsError, executed.Message);
        Assert.AreEqual(BridgeTransactionStatus.Completed, (await provider.GetTransactionStatusAsync(executed.Result)).Result);
    }
}
