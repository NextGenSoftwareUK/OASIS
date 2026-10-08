using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.NEAROASIS;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests
{
    [TestClass]
    [DoNotParallelize]
    public class NEAROASISIntegrationTests
    {
        private NEAROASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new NEAROASIS(
                Required("NEAROASIS_RPCENDPOINT"),
                Required("NEAROASIS_NETWORKID"),
                Required("NEAROASIS_CHAINID"),
                Required("NEAROASIS_CONTRACTADDRESS"),
                Required("NEAROASIS_ACCOUNTID"),
                Required("NEAROASIS_PRIVATEKEY"));
        }

        private static string Required(string name) => Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException($"{name} is required; use Scripts/run_near_provider_evidence.ps1.");

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            // Arrange
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "TestUser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            var result = await _provider.SaveAvatarAsync(avatar);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
            var loaded = await _provider.LoadAvatarAsync(avatar.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.IsNotNull(loaded.Result);
            Assert.AreEqual(avatar.Username, loaded.Result.Username);
            Assert.AreEqual(avatar.Email, loaded.Result.Email);
            Assert.AreEqual(avatar.Id, (await _provider.LoadAvatarByProviderKeyAsync(avatar.Id.ToString("D"))).Result.Id);
            Assert.AreEqual(avatar.Id, (await _provider.LoadAvatarByUsernameAsync(avatar.Username)).Result.Id);
            Assert.AreEqual(avatar.Id, (await _provider.LoadAvatarByEmailAsync(avatar.Email)).Result.Id);
            Assert.IsTrue((await _provider.LoadAllAvatarsAsync()).Result.Any(item => item.Id == avatar.Id));
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnAvatar()
        {
            // Arrange
            var avatarId = Guid.NewGuid();

            // Act
            var result = await _provider.LoadAvatarAsync(avatarId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNull(result.Result);
        }

        [TestMethod]
        public async Task SaveHolon_ShouldReturnSuccessResult()
        {
            // Arrange
            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = "TestHolon",
                Description = "Test Holon Description"
            };

            // Act
            var result = await _provider.SaveHolonAsync(holon);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
            var loaded = await _provider.LoadHolonAsync(holon.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.IsNotNull(loaded.Result);
            Assert.AreEqual(holon.Name, loaded.Result.Name);
            Assert.AreEqual(holon.Id, (await _provider.LoadHolonByProviderKeyAsync(holon.Id.ToString("D"))).Result.Id);
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnHolon()
        {
            // Arrange
            var holonId = Guid.NewGuid();

            // Act
            var result = await _provider.LoadHolonAsync(holonId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNull(result.Result);
        }

        [TestMethod]
        public async Task Activation_RejectsWrongNetwork()
        {
            var wrong = new NEAROASIS(
                Required("NEAROASIS_RPCENDPOINT"), "not-sandbox", "not-sandbox",
                Required("NEAROASIS_CONTRACTADDRESS"), Required("NEAROASIS_ACCOUNTID"), Required("NEAROASIS_PRIVATEKEY"));
            var result = await wrong.ActivateProviderAsync();
            Assert.IsTrue(result.IsError || !result.Result, "Activation must reject a mismatched NEAR network.");
        }

        [TestMethod]
        public async Task Storage_PerformsAvatarDetailAndHardDeleteLifecycle()
        {
            var marker = Guid.NewGuid().ToString("N");
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = $"near-{marker}", Email = $"{marker}@near.test" };
            var detail = new AvatarDetail { Id = avatar.Id, Username = avatar.Username, Email = avatar.Email, Karma = 42 };
            Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
            Assert.IsFalse((await _provider.SaveAvatarDetailAsync(detail)).IsError);
            var loadedDetail = await _provider.LoadAvatarDetailAsync(avatar.Id);
            Assert.IsFalse(loadedDetail.IsError, loadedDetail.Message);
            Assert.IsNotNull(loadedDetail.Result, loadedDetail.Message);
            Assert.AreEqual(42, loadedDetail.Result.Karma);
            Assert.AreEqual(avatar.Id, (await _provider.LoadAvatarDetailByUsernameAsync(avatar.Username)).Result.Id);
            Assert.AreEqual(avatar.Id, (await _provider.LoadAvatarDetailByEmailAsync(avatar.Email)).Result.Id);
            Assert.IsTrue((await _provider.LoadAllAvatarDetailsAsync()).Result.Any(item => item.Id == avatar.Id));
            var deleted = await _provider.DeleteAvatarAsync(avatar.Id, false);
            Assert.IsFalse(deleted.IsError, deleted.Message);
            Assert.IsTrue(deleted.Result);
            Assert.IsNull((await _provider.LoadAvatarAsync(avatar.Id)).Result);
        }

        [TestMethod]
        public async Task Storage_EnumeratesParentMetadataSearchImportAndExport()
        {
            var marker = Guid.NewGuid().ToString("N");
            var owner = Guid.NewGuid();
            var parent = new Holon { Id = Guid.NewGuid(), Name = $"parent-{marker}" };
            var child = new Holon
            {
                Id = Guid.NewGuid(), Name = $"child-{marker}", CreatedByAvatarId = owner,
                MetaData = new System.Collections.Generic.Dictionary<string, object> { ["runtime"] = marker }
            };
            parent.Children.Add(child);
            var saved = await _provider.SaveHolonAsync(parent, saveChildren: true, recursive: true);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loadedParent = await _provider.LoadHolonAsync(parent.Id, loadChildren: true, recursive: true);
            Assert.IsFalse(loadedParent.IsError, loadedParent.Message);
            Assert.IsNotNull(loadedParent.Result, loadedParent.Message);
            Assert.IsTrue(loadedParent.Result.Children.Any(item => item.Id == child.Id));
            var children = await _provider.LoadHolonsForParentAsync(parent.Id, HolonType.All);
            Assert.IsTrue(children.Result.Any(item => item.Id == child.Id));
            var metadata = await _provider.LoadHolonsByMetaDataAsync("runtime", marker, HolonType.All);
            Assert.IsTrue(metadata.Result.Any(item => item.Id == child.Id));
            var search = await _provider.SearchAsync(new SearchParams
            {
                SearchOnlyForCurrentAvatar = false,
                SearchGroups = new System.Collections.Generic.List<ISearchGroupBase>
                    { new SearchTextGroup { SearchQuery = marker, SearchHolons = true } }
            });
            Assert.IsFalse(search.IsError, search.Message);
            Assert.IsTrue(search.Result.SearchResultHolons.Any(item => item.Id == child.Id));
            Assert.IsTrue((await _provider.ExportAllDataForAvatarByIdAsync(owner)).Result.Any(item => item.Id == child.Id));

            var imported = new Holon { Id = Guid.NewGuid(), Name = $"import-{marker}" };
            Assert.IsTrue((await _provider.ImportAsync(new[] { imported })).Result);
            Assert.IsTrue((await _provider.ExportAllAsync()).Result.Any(item => item.Id == imported.Id));
            Assert.IsFalse((await _provider.DeleteHolonAsync(parent.Id)).IsError);
        }

        [TestMethod]
        public async Task OfficialSdk_GeneratesKeysReadsBalanceAndTransfersNativeNear()
        {
            Assert.IsFalse((await _provider.ActivateProviderAsync()).IsError);
            var generated = await _provider.GenerateKeyPairAsync();
            Assert.IsFalse(generated.IsError, generated.Message);
            Assert.IsTrue(generated.Result.PublicKey.StartsWith("ed25519:"));
            Assert.IsTrue(generated.Result.PrivateKey.StartsWith("ed25519:"));
            Assert.AreEqual(64, generated.Result.WalletAddressLegacy.Length);

            var balance = await _provider.GetBalanceAsync(new GetWeb3WalletBalanceRequest
                { WalletAddress = Required("NEAROASIS_ACCOUNTID") });
            Assert.IsFalse(balance.IsError, balance.Message);
            Assert.IsTrue(balance.Result > 0);
            var sent = await _provider.SendTokenAsync(new SendWeb3TokenRequest
                { ToWalletAddress = generated.Result.WalletAddressLegacy, Amount = 0.00000001m });
            Assert.IsFalse(sent.IsError, sent.Message);
            Assert.IsFalse(string.IsNullOrWhiteSpace(sent.Result.TransactionResult));
            var status = await _provider.GetTransactionStatusAsync(sent.Result.TransactionResult);
            Assert.IsFalse(status.IsError, status.Message);
        }

        [TestMethod]
        public async Task OfficialContract_PerformsCustomTokenLifecycle()
        {
            var symbol = $"NEAR-{Guid.NewGuid():N}";
            var recipient = (await _provider.GenerateKeyPairAsync()).Result.WalletAddressLegacy;
            var minted = await _provider.MintTokenAsync(new MintWeb3TokenRequest
                { Symbol = symbol, Title = "NEAR runtime token", Amount = 100m });
            Assert.IsFalse(minted.IsError, minted.Message);
            Assert.AreEqual(100m, (await _provider.GetCustomTokenBalanceAsync(symbol, Required("NEAROASIS_ACCOUNTID"))).Result);
            var sent = await _provider.SendTokenAsync(new SendWeb3TokenRequest
            {
                FromTokenAddress = symbol, FromWalletAddress = Required("NEAROASIS_ACCOUNTID"),
                ToWalletAddress = recipient, Amount = 10m
            });
            Assert.IsFalse(sent.IsError, sent.Message);
            Assert.AreEqual(90m, (await _provider.GetCustomTokenBalanceAsync(symbol, Required("NEAROASIS_ACCOUNTID"))).Result);
            Assert.AreEqual(10m, (await _provider.GetCustomTokenBalanceAsync(symbol, recipient)).Result);
            Assert.IsFalse((await _provider.LockTokenAsync(new LockWeb3TokenRequest { TokenAddress = symbol })).IsError);
            Assert.IsTrue((await _provider.SendTokenAsync(new SendWeb3TokenRequest
            {
                FromTokenAddress = symbol, FromWalletAddress = Required("NEAROASIS_ACCOUNTID"),
                ToWalletAddress = recipient, Amount = 1m
            })).IsError);
            Assert.IsFalse((await _provider.UnlockTokenAsync(new UnlockWeb3TokenRequest { TokenAddress = symbol })).IsError);
            Assert.IsFalse((await _provider.BurnTokenAsync(new BurnWeb3TokenRequest
                { TokenAddress = symbol, OwnerPublicKey = "", OwnerPrivateKey = "", OwnerSeedPhrase = "" })).IsError);
        }

        [TestMethod]
        public async Task OfficialContract_PerformsNftLifecycle()
        {
            var marker = Guid.NewGuid().ToString("N");
            var minted = await _provider.MintNFTAsync(new MintWeb3NFTRequest
            {
                Title = $"NEAR NFT {marker}", Symbol = "OASIS", JSONMetaData = $"{{\"marker\":\"{marker}\"}}",
                MetaData = new System.Collections.Generic.Dictionary<string, string> { ["marker"] = marker }
            });
            Assert.IsFalse(minted.IsError, minted.Message);
            var tokenId = minted.Result.Web3NFT.NFTTokenAddress;
            Assert.AreEqual($"NEAR NFT {marker}", (await _provider.LoadOnChainNFTDataAsync(tokenId)).Result.Title);
            var locked = await _provider.LockNFTAsync(new LockWeb3NFTRequest { NFTTokenAddress = tokenId });
            Assert.IsFalse(locked.IsError, locked.Message);
            Assert.AreEqual("True", locked.Result.Web3NFT.MetaData["near:locked"]);
            var unlocked = await _provider.UnlockNFTAsync(new UnlockWeb3NFTRequest { NFTTokenAddress = tokenId });
            Assert.IsFalse(unlocked.IsError, unlocked.Message);
            var recipient = (await _provider.GenerateKeyPairAsync()).Result.WalletAddressLegacy;
            var sent = await _provider.SendNFTAsync(new SendWeb3NFTRequest { TokenId = tokenId, ToWalletAddress = recipient });
            Assert.IsFalse(sent.IsError, sent.Message);
            Assert.AreEqual(recipient, sent.Result.Web3NFT.SendToAddressAfterMinting);
            var burnMint = await _provider.MintNFTAsync(new MintWeb3NFTRequest
                { Title = $"NEAR burn NFT {marker}", Symbol = "OASIS" });
            Assert.IsFalse(burnMint.IsError, burnMint.Message);
            var burnTokenId = burnMint.Result.Web3NFT.NFTTokenAddress;
            var burned = await _provider.BurnNFTAsync(new BurnWeb3NFTRequest
                { NFTTokenAddress = burnTokenId, OwnerPublicKey = "", OwnerPrivateKey = "", OwnerSeedPhrase = "" });
            Assert.IsFalse(burned.IsError, burned.Message);
            Assert.IsNull((await _provider.LoadOnChainNFTDataAsync(burnTokenId)).Result);
        }

        [TestMethod]
        public async Task OfficialSdk_PerformsNativeBridgeDepositAndWithdrawal()
        {
            var account = await _provider.GenerateKeyPairAsync();
            Assert.IsFalse(account.IsError, account.Message);
            var deposited = await _provider.DepositAsync(0.001m, account.Result.WalletAddressLegacy);
            Assert.IsFalse(deposited.IsError, deposited.Message);
            Assert.IsTrue(deposited.Result.IsSuccessful);
            Assert.IsFalse(string.IsNullOrWhiteSpace(deposited.Result.TransactionId));
            var withdrawn = await _provider.WithdrawAsync(0.00000001m,
                account.Result.WalletAddressLegacy, account.Result.PrivateKey);
            Assert.IsFalse(withdrawn.IsError, withdrawn.Message);
            Assert.IsTrue(withdrawn.Result.IsSuccessful);
            Assert.IsFalse(string.IsNullOrWhiteSpace(withdrawn.Result.TransactionId));
            var history = await _provider.GetTransactionsAsync(new GetWeb3TransactionsRequest
                { WalletAddress = account.Result.WalletAddressLegacy });
            Assert.IsFalse(history.IsError, history.Message);
            Assert.IsTrue(history.Result.Any(item => item.ToWalletAddress == account.Result.WalletAddressLegacy));
            Assert.IsTrue(history.Result.Any(item => item.FromWalletAddress == account.Result.WalletAddressLegacy));
        }

        [TestMethod]
        public async Task OfficialContract_PerformsNftBridgeLockUnlockAndTransfer()
        {
            var minted = await _provider.MintNFTAsync(new MintWeb3NFTRequest
                { Title = $"NEAR bridge NFT {Guid.NewGuid():N}", Symbol = "OASIS" });
            Assert.IsFalse(minted.IsError, minted.Message);
            var tokenId = minted.Result.Web3NFT.NFTTokenAddress;
            var withdrawn = await _provider.WithdrawNFTAsync(tokenId, tokenId,
                Required("NEAROASIS_ACCOUNTID"), Required("NEAROASIS_PRIVATEKEY"));
            Assert.IsFalse(withdrawn.IsError, withdrawn.Message);
            Assert.IsTrue(withdrawn.Result.IsSuccessful);

            var recipient = (await _provider.GenerateKeyPairAsync()).Result.WalletAddressLegacy;
            var deposited = await _provider.DepositNFTAsync(tokenId, tokenId, recipient,
                withdrawn.Result.TransactionId);
            Assert.IsFalse(deposited.IsError, deposited.Message);
            Assert.IsTrue(deposited.Result.IsSuccessful);
            Assert.AreEqual(recipient, (await _provider.LoadOnChainNFTDataAsync(tokenId)).Result.SendToAddressAfterMinting);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
            {
                _provider.DeActivateProvider();
            }
        }
    }
}
