using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
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
        private sealed class NamedGraphOwner:Holon
        {
            public System.Collections.Generic.List<Holon> Components {get;set;}=new();
            public override System.Collections.ObjectModel.ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Components).ToList().AsReadOnly();
            public override System.Collections.Generic.IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
                .Concat(new[]{new HolonChildCollection(nameof(Components),Components,rows=>Components=rows.Cast<Holon>().ToList())}).ToArray();
        }

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
            var marker = Guid.NewGuid().ToString("N");
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"near-user-{marker}",
                Email = $"{marker}@near.test",
                FirstName = "Test",
                LastName = "User"
            };
            avatar.Id = Guid.Empty;

            // Act
            var result = await _provider.SaveAvatarAsync(avatar);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
            Assert.AreNotEqual(Guid.Empty,avatar.Id);
            var loaded = await _provider.LoadAvatarAsync(avatar.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.IsNotNull(loaded.Result);
            Assert.AreEqual(avatar.Username, loaded.Result.Username);
            Assert.AreEqual(avatar.Email, loaded.Result.Email);
            Assert.AreEqual(avatar.Id, (await _provider.LoadAvatarByProviderKeyAsync(avatar.Id.ToString("D"))).Result.Id);
            Assert.AreEqual(avatar.Id, (await _provider.LoadAvatarByUsernameAsync(avatar.Username)).Result.Id);
            Assert.AreEqual(avatar.Id, (await _provider.LoadAvatarByEmailAsync(avatar.Email)).Result.Id);
            Assert.IsTrue((await _provider.LoadAllAvatarsAsync()).Result.Any(item => item.Id == avatar.Id));
            // Distinct encrypted records must never make a username/email lookup pick an arbitrary owner.
            var duplicate = new Avatar { Id=Guid.NewGuid(), Username=avatar.Username, Email=avatar.Email };
            var duplicateSave=await _provider.SaveAvatarAsync(duplicate);
            Assert.IsFalse(duplicateSave.IsError,duplicateSave.Message);
            Assert.IsNotNull(duplicateSave.Result);
            try
            {
                foreach(var ambiguous in new[] {
                    await _provider.LoadAvatarByUsernameAsync(avatar.Username),
                    await _provider.LoadAvatarByEmailAsync(avatar.Email) })
                {
                    Assert.IsTrue(ambiguous.IsError);
                    Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,ambiguous.ErrorCode);
                    Assert.IsNull(ambiguous.Result);
                    Assert.IsFalse(ambiguous.IsLoaded);
                }
            }
            finally
            {
                // Delete only this acknowledged run-owned duplicate, never by its ambiguous identity.
                var removed=await _provider.DeleteAvatarAsync(duplicate.Id,false);
                Assert.IsFalse(removed.IsError,removed.Message);
                Assert.IsTrue(removed.Result);
                var absent=await _provider.LoadAvatarAsync(duplicate.Id);
                Assert.IsFalse(absent.IsError,absent.Message);
                Assert.IsNull(absent.Result);
            }
            Assert.AreEqual(avatar.Id,(await _provider.LoadAvatarByUsernameAsync(avatar.Username)).Result.Id);
            Assert.AreEqual(avatar.Id,(await _provider.LoadAvatarByEmailAsync(avatar.Email)).Result.Id);
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
            var conflicting = new NEAROASIS(Required("NEAROASIS_RPCENDPOINT"),Required("NEAROASIS_NETWORKID"),
                "different-chain",Required("NEAROASIS_CONTRACTADDRESS"),Required("NEAROASIS_ACCOUNTID"),Required("NEAROASIS_PRIVATEKEY"));
            var rejected = await conflicting.ActivateProviderAsync();
            Assert.IsTrue(rejected.IsError);
            StringAssert.Contains(rejected.Message,"networkId and chainId");
            Assert.IsFalse(conflicting.IsProviderActivated);
        }

        [TestMethod]
        public async Task Storage_PerformsAvatarDetailAndHardDeleteLifecycle()
        {
            _provider.IsVersionControlEnabled = true;
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
            var tombstone=await _provider.DeleteAvatarAsync(avatar.Id,true);
            Assert.IsFalse(tombstone.IsError,tombstone.Message);Assert.IsTrue(tombstone.Result);
            var hidden=await _provider.LoadAvatarAsync(avatar.Id);
            Assert.IsFalse(hidden.IsError,hidden.Message);Assert.IsNull(hidden.Result);
            var visible=await _provider.LoadAllAvatarsAsync();
            Assert.IsFalse(visible.IsError,visible.Message);Assert.IsFalse(visible.Result.Any(item=>item.Id==avatar.Id));
            foreach(var missing in new[]{await _provider.LoadAvatarByUsernameAsync(avatar.Username),await _provider.LoadAvatarByEmailAsync(avatar.Email)}){
                Assert.IsTrue(missing.IsError);Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);Assert.IsNull(missing.Result);
            }
            var historical=await _provider.LoadAvatarByUsernameAsync(avatar.Username,1);
            Assert.IsFalse(historical.IsError,historical.Message);Assert.AreEqual(avatar.Id,historical.Result.Id);Assert.IsFalse(historical.Result.IsDeleted);
            var deleted = await _provider.DeleteAvatarAsync(avatar.Id, false);
            Assert.IsFalse(deleted.IsError, deleted.Message);
            Assert.IsTrue(deleted.Result);
            Assert.IsNull((await _provider.LoadAvatarAsync(avatar.Id)).Result);
            var removedHistory=await _provider.LoadAvatarAsync(avatar.Id,1);
            Assert.IsFalse(removedHistory.IsError,removedHistory.Message);Assert.IsNull(removedHistory.Result);
        }

        [TestMethod]
        public async Task Storage_EnumeratesParentMetadataSearchImportAndExport()
        {
            var marker = Guid.NewGuid().ToString("N");
            var owner = Guid.NewGuid();
            var parent = new NamedGraphOwner { Id = Guid.NewGuid(), Name = $"parent-{marker}" };
            var child = new Holon
            {
                Id = Guid.NewGuid(), Name = $"child-{marker}", CreatedByAvatarId = owner,
                MetaData = new System.Collections.Generic.Dictionary<string, object> { ["runtime"] = marker,["canonical:enabled"]=true,["canonical:amount"]=1.5m }
            };
            var grandchild = new Holon { Id = Guid.NewGuid(), Name = $"grandchild-{marker}" };
            child.Children.Add(grandchild);
            parent.Components.Add(child);
            var expectedRoles=NextGenSoftware.OASIS.API.Core.Serialization.HolonChildRoleManifestJson.Serialize(
                HolonChildCollectionState.Capture(parent).CreateManifest(parent.Id,row=>row.Id));
            var saved = await _provider.SaveHolonAsync(parent, saveChildren: true, recursive: true);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loadedParent = await _provider.LoadHolonAsync(parent.Id, loadChildren: true, recursive: true,loadChildrenFromProvider:true);
            Assert.IsFalse(loadedParent.IsError, loadedParent.Message);
            Assert.IsNotNull(loadedParent.Result, loadedParent.Message);
            Assert.IsTrue(loadedParent.Result.Children.Any(item => item.Id == child.Id));
            Assert.AreEqual(expectedRoles,parent.ChildRoleManifestJson);
            Assert.AreEqual(expectedRoles,((Holon)loadedParent.Result).ChildRoleManifestJson);
            var childBefore=await _provider.LoadHolonAsync(child.Id,loadChildren:false);Assert.IsFalse(childBefore.IsError,childBefore.Message);Assert.IsNotNull(childBefore.Result);
            var scalar=new Holon{Id=parent.Id,Name=parent.Name,Version=parent.Version};
            var rootOnly=await _provider.SaveHolonAsync(scalar,saveChildren:false);Assert.IsFalse(rootOnly.IsError,rootOnly.Message);
            var persistedRoot=await _provider.LoadHolonAsync(parent.Id,loadChildren:false);Assert.IsFalse(persistedRoot.IsError,persistedRoot.Message);
            Assert.AreEqual(expectedRoles,scalar.ChildRoleManifestJson);Assert.AreEqual(expectedRoles,((Holon)persistedRoot.Result).ChildRoleManifestJson);
            var childAfter=await _provider.LoadHolonAsync(child.Id,loadChildren:false);Assert.IsFalse(childAfter.IsError,childAfter.Message);Assert.IsNotNull(childAfter.Result);
            Assert.AreEqual(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(childBefore.Result),
                NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(childAfter.Result),"Root-only write must leave the independently loaded child unchanged.");
            var byKey = await _provider.LoadHolonAsync(parent.Id.ToString("D"), loadChildren: true,
                recursive: true, loadChildrenFromProvider: true);
            Assert.IsFalse(byKey.IsError, byKey.Message);
            Assert.AreEqual(child.Id, byKey.Result.Children.Single().Id);
            Assert.AreEqual(grandchild.Id, byKey.Result.Children.Single().Children.Single().Id);
            var shallow = await _provider.LoadHolonAsync(parent.Id.ToString("D"), loadChildren: true,
                recursive: false, loadChildrenFromProvider: true);
            Assert.IsFalse(shallow.IsError, shallow.Message);
            Assert.AreEqual(child.Id, shallow.Result.Children.Single().Id);
            Assert.AreEqual(0, shallow.Result.Children.Single().Children.Count());
            var limited = await _provider.LoadHolonAsync(parent.Id.ToString("D"), loadChildren: true,
                maxChildDepth: 1, loadChildrenFromProvider: true);
            Assert.IsFalse(limited.IsError, limited.Message);
            Assert.AreEqual(0, limited.Result.Children.Single().Children.Count());
            var withoutChildren = await _provider.LoadHolonAsync(parent.Id.ToString("D"), loadChildren: false,
                loadChildrenFromProvider: true);
            Assert.IsFalse(withoutChildren.IsError, withoutChildren.Message);
            Assert.AreEqual(0, withoutChildren.Result.Children.Count());
            var listed = await _provider.LoadAllHolonsAsync(HolonType.All, loadChildren: true,loadChildrenFromProvider:true);
            Assert.IsFalse(listed.IsError, listed.Message);
            Assert.AreEqual(child.Id, listed.Result.Single(item => item.Id == parent.Id).Children.Single().Id);
            var children = await _provider.LoadHolonsForParentAsync(parent.Id, HolonType.All,loadChildrenFromProvider:true);
            Assert.IsTrue(children.Result.Any(item => item.Id == child.Id));
            Assert.AreEqual(grandchild.Id, children.Result.Single(item => item.Id == child.Id).Children.Single().Id);
            var metadata = await _provider.LoadHolonsByMetaDataAsync("runtime", marker, HolonType.All);
            Assert.IsTrue(metadata.Result.Any(item => item.Id == child.Id));
            var previousCulture=System.Globalization.CultureInfo.CurrentCulture;
            try {
                System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
                var canonical=await _provider.LoadHolonsByMetaDataAsync(new() {{"runtime",marker},{"canonical:enabled","True"},{"canonical:amount","1.5"}},MetaKeyValuePairMatchMode.All,HolonType.All);
                Assert.IsFalse(canonical.IsError,canonical.Message);Assert.AreEqual(child.Id,canonical.Result.Single().Id);
                var numeric=_provider.LoadHolonsByMetaData("canonical:amount","1.5",HolonType.All);
                Assert.IsFalse(numeric.IsError,numeric.Message);Assert.IsTrue(numeric.Result.Any(row=>row.Id==child.Id));
                var any=await _provider.LoadHolonsByMetaDataAsync(new() {{"absent","absent"},{"canonical:enabled","True"}},MetaKeyValuePairMatchMode.Any,HolonType.All);
                Assert.IsFalse(any.IsError,any.Message);Assert.IsTrue(any.Result.Any(row=>row.Id==child.Id));
                var wrongCase=await _provider.LoadHolonsByMetaDataAsync("canonical:enabled","true",HolonType.All);
                Assert.IsFalse(wrongCase.IsError,wrongCase.Message);Assert.IsFalse(wrongCase.Result.Any(row=>row.Id==child.Id));
            }
            finally {System.Globalization.CultureInfo.CurrentCulture=previousCulture;}
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
            Assert.IsNull(generated.Result.WalletAddressSegwitP2SH);

            var balance = await _provider.GetBalanceAsync(new GetWeb3WalletBalanceRequest
                { WalletAddress = Required("NEAROASIS_ACCOUNTID") });
            Assert.IsFalse(balance.IsError, balance.Message);
            Assert.IsTrue(balance.Result > 0);
            var preciseBalance = await _provider.GetAccountBalanceAsync(Required("NEAROASIS_ACCOUNTID"));
            Assert.IsFalse(preciseBalance.IsError, preciseBalance.Message);
            Assert.AreEqual(balance.Result, (double)preciseBalance.Result);
            var sent = await _provider.SendTokenAsync(new SendWeb3TokenRequest
                { ToWalletAddress = generated.Result.WalletAddressLegacy, Amount = 0.00000001m });
            Assert.IsFalse(sent.IsError, sent.Message);
            Assert.IsFalse(string.IsNullOrWhiteSpace(sent.Result.TransactionResult));
            var status = await _provider.GetTransactionStatusAsync(sent.Result.TransactionResult);
            Assert.IsFalse(status.IsError, status.Message);
            Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,status.Result);
        }

        [TestMethod]
        public async Task OfficialContract_PerformsCustomTokenLifecycle()
        {
            var symbol = $"NEAR-{Guid.NewGuid():N}";
            var independent = await _provider.CreateAccountAsync();
            Assert.IsFalse(independent.IsError,independent.Message);
            var recipient = Convert.ToHexString(NBitcoin.DataEncoders.Encoders.Base58.DecodeData(independent.Result.PublicKey[8..])).ToLowerInvariant();
            var funded=await _provider.SendTokenAsync(new SendWeb3TokenRequest{ToWalletAddress=recipient,Amount=0.1m});
            Assert.IsFalse(funded.IsError,funded.Message);
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
            var independentSend=await _provider.SendTokenAsync(new SendWeb3TokenRequest
            {
                FromTokenAddress=symbol,FromWalletAddress=recipient,OwnerPrivateKey=independent.Result.PrivateKey,
                ToWalletAddress=Required("NEAROASIS_ACCOUNTID"),Amount=2m
            });
            Assert.IsFalse(independentSend.IsError,independentSend.Message);
            Assert.IsFalse(string.IsNullOrWhiteSpace(independentSend.Result.TransactionResult));
            Assert.AreEqual(92m,(await _provider.GetCustomTokenBalanceAsync(symbol,Required("NEAROASIS_ACCOUNTID"))).Result);
            Assert.AreEqual(8m,(await _provider.GetCustomTokenBalanceAsync(symbol,recipient)).Result);
            Assert.IsFalse((await _provider.LockTokenAsync(new LockWeb3TokenRequest { TokenAddress = symbol })).IsError);
            Assert.IsTrue((await _provider.SendTokenAsync(new SendWeb3TokenRequest
            {
                FromTokenAddress = symbol, FromWalletAddress = Required("NEAROASIS_ACCOUNTID"),
                ToWalletAddress = recipient, Amount = 1m
            })).IsError);
            Assert.IsFalse((await _provider.UnlockTokenAsync(new UnlockWeb3TokenRequest { TokenAddress = symbol })).IsError);
            var holderBurn=await _provider.BurnTokenAsync(new BurnWeb3TokenRequest{TokenAddress=symbol,Amount=1.25m,
                OwnerPrivateKey="",OwnerPublicKey=independent.Result.PublicKey,OwnerSeedPhrase=independent.Result.SeedPhrase});
            Assert.IsFalse(holderBurn.IsError,holderBurn.Message);
            Assert.AreEqual(6.75m,(await _provider.GetCustomTokenBalanceAsync(symbol,recipient)).Result);
            Assert.IsFalse((await _provider.BurnTokenAsync(new BurnWeb3TokenRequest
                { Amount = 0.25m, TokenAddress = symbol, OwnerPublicKey = "", OwnerPrivateKey = "", OwnerSeedPhrase = "" })).IsError);
            Assert.AreEqual(91.75m, (await _provider.GetCustomTokenBalanceAsync(symbol, Required("NEAROASIS_ACCOUNTID"))).Result);
            Assert.AreEqual(6.75m, (await _provider.GetCustomTokenBalanceAsync(symbol, recipient)).Result);
            var namedBurn=await _provider.BurnTokenAsync(new BurnWeb3TokenRequest{TokenAddress=symbol,Amount=0.25m,
                OwnerPrivateKey=Required("NEAROASIS_PRIVATEKEY"),OwnerPublicKey="",OwnerSeedPhrase=""},Required("NEAROASIS_ACCOUNTID"));
            Assert.IsFalse(namedBurn.IsError,namedBurn.Message);
            Assert.AreEqual(91.5m,(await _provider.GetCustomTokenBalanceAsync(symbol,Required("NEAROASIS_ACCOUNTID"))).Result);
        }

        [TestMethod]
        public async Task OfficialContract_PerformsNftLifecycle()
        {
            var marker = Guid.NewGuid().ToString("N");
            var title=$"NEAR NFT Café 🌳 東京 {marker}";
            var minted = await _provider.MintNFTAsync(new MintWeb3NFTRequest
            {
                Title = title, Symbol = "OASIS", JSONMetaData = $"{{\"marker\":\"{marker}\"}}",
                MetaData = new System.Collections.Generic.Dictionary<string, string> { ["marker"] = marker }
            });
            Assert.IsFalse(minted.IsError, minted.Message);
            var tokenId = minted.Result.Web3NFT.NFTTokenAddress;
            Assert.AreEqual(title, (await _provider.LoadOnChainNFTDataAsync(tokenId)).Result.Title);
            var locked = await _provider.LockNFTAsync(new LockWeb3NFTRequest { NFTTokenAddress = tokenId });
            Assert.IsFalse(locked.IsError, locked.Message);
            Assert.AreEqual("True", locked.Result.Web3NFT.MetaData["near:locked"]);
            var unlocked = await _provider.UnlockNFTAsync(new UnlockWeb3NFTRequest { NFTTokenAddress = tokenId });
            Assert.IsFalse(unlocked.IsError, unlocked.Message);
            var independentOwner=await _provider.CreateAccountAsync();
            Assert.IsFalse(independentOwner.IsError,independentOwner.Message);
            var recipient=Convert.ToHexString(NBitcoin.DataEncoders.Encoders.Base58.DecodeData(independentOwner.Result.PublicKey[8..])).ToLowerInvariant();
            var funding=await _provider.SendTokenAsync(new SendWeb3TokenRequest{ToWalletAddress=recipient,Amount=0.1m});
            Assert.IsFalse(funding.IsError,funding.Message);
            var sent = await _provider.SendNFTAsync(new SendWeb3NFTRequest { TokenId = tokenId, ToWalletAddress = recipient });
            Assert.IsFalse(sent.IsError, sent.Message);
            Assert.AreEqual(recipient, sent.Result.Web3NFT.SendToAddressAfterMinting);
            var returned=await _provider.SendNFTAsync(new SendWeb3NFTRequest{TokenId=tokenId,FromWalletAddress=recipient,
                ToWalletAddress=Required("NEAROASIS_ACCOUNTID"),OwnerPrivateKey="",OwnerPublicKey=independentOwner.Result.PublicKey,OwnerSeedPhrase=independentOwner.Result.SeedPhrase});
            Assert.IsFalse(returned.IsError,returned.Message);
            Assert.AreEqual(Required("NEAROASIS_ACCOUNTID"),(await _provider.LoadOnChainNFTDataAsync(tokenId)).Result.SendToAddressAfterMinting);
            var burnMint = await _provider.MintNFTAsync(new MintWeb3NFTRequest
                { Title = $"NEAR burn NFT {marker}", Symbol = "OASIS",SendToAddressAfterMinting=recipient });
            Assert.IsFalse(burnMint.IsError, burnMint.Message);
            var burnTokenId = burnMint.Result.Web3NFT.NFTTokenAddress;
            var burned = await _provider.BurnNFTAsync(new BurnWeb3NFTRequest
                { TokenId = burnTokenId, OwnerPublicKey = independentOwner.Result.PublicKey, OwnerPrivateKey = "", OwnerSeedPhrase = independentOwner.Result.SeedPhrase });
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

        [TestMethod]
        public async Task Storage_VersionsRootsSelectorsAndIndependentChildren()
        {
            _provider.IsVersionControlEnabled = true;
            var marker = Guid.NewGuid().ToString("N");
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = $"v1-{marker}", Email = $"v1-{marker}@near.test" };
            var oldUsername = avatar.Username;
            var oldEmail = avatar.Email;
            Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
            Assert.AreEqual(1, avatar.Version);
            avatar.Username = $"v2-{marker}"; avatar.Email = $"v2-{marker}@near.test";
            var updated = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(updated.IsError, updated.Message);
            Assert.AreEqual(2, avatar.Version);
            Assert.AreEqual(oldUsername, (await _provider.LoadAvatarAsync(avatar.Id, 1)).Result.Username);
            Assert.AreEqual(oldEmail, _provider.LoadAvatarByProviderKey(avatar.Id.ToString("D"), 1).Result.Email);
            Assert.AreEqual(avatar.Id, (await _provider.LoadAvatarByUsernameAsync(oldUsername, 1)).Result.Id);
            Assert.AreEqual(avatar.Id, (await _provider.LoadAvatarByEmailAsync(oldEmail, 1)).Result.Id);
            Assert.IsTrue((await _provider.LoadAllAvatarsAsync(1)).Result.Any(item => item.Id == avatar.Id));

            var detail = new AvatarDetail { Id = avatar.Id, Username = oldUsername, Email = oldEmail, Karma = 12 };
            Assert.IsFalse((await _provider.SaveAvatarDetailAsync(detail)).IsError);
            detail.Username = avatar.Username; detail.Email = avatar.Email; detail.Karma = 34;
            Assert.IsFalse((await _provider.SaveAvatarDetailAsync(detail)).IsError);
            Assert.AreEqual(12L, (await _provider.LoadAvatarDetailAsync(detail.Id, 1)).Result.Karma);
            Assert.AreEqual(12L, (await _provider.LoadAvatarDetailByUsernameAsync(oldUsername, 1)).Result.Karma);
            Assert.AreEqual(12L, _provider.LoadAvatarDetailByEmail(oldEmail, 1).Result.Karma);

            var root = new Holon { Id = Guid.NewGuid(), Name = $"root-v1-{marker}" };
            var child = new Holon { Id = Guid.NewGuid(), Name = $"child-v1-{marker}" };
            root.Children.Add(child);
            Assert.IsFalse((await _provider.SaveHolonAsync(root, saveChildren:true)).IsError);
            child.Name = $"child-v2-{marker}";
            Assert.IsFalse((await _provider.SaveHolonAsync(child)).IsError);
            root.Name = $"root-v2-{marker}";
            Assert.IsFalse((await _provider.SaveHolonAsync(root, saveChildren:false)).IsError);
            var historical = await _provider.LoadHolonAsync(root.Id, loadChildrenFromProvider:true,version:1);
            Assert.IsFalse(historical.IsError, historical.Message);
            Assert.AreEqual($"root-v1-{marker}", historical.Result.Name);
            Assert.AreEqual(child.Name, historical.Result.Children.Single().Name);
            Assert.AreEqual(2, historical.Result.Children.Single().Version);
            Assert.AreEqual($"root-v1-{marker}", _provider.LoadHolonByProviderKey(root.Id.ToString("D"), 1).Result.Name);
            Assert.AreEqual(1, (await _provider.LoadAllHolonsAsync(HolonType.All, version:1)).Result.Single(item=>item.Id==root.Id).Version);
            IOASISStorageProvider storage=_provider;
            var viaInterface=await storage.LoadAllHolonsAsync(HolonType.All,curentChildDepth:0,loadChildrenFromProvider:true,version:1);
            Assert.IsFalse(viaInterface.IsError,viaInterface.Message);
            var selected=viaInterface.Result.Single(item=>item.Id==root.Id);
            Assert.AreEqual(1,selected.Version);Assert.AreEqual(2,selected.Children.Single().Version);
            var syncInterface=storage.LoadAllHolons(HolonType.All,curentChildDepth:0,loadChildrenFromProvider:true,version:1);
            Assert.IsFalse(syncInterface.IsError,syncInterface.Message);Assert.AreEqual(1,syncInterface.Result.Single(item=>item.Id==root.Id).Version);
            var parentInterface=await storage.LoadHolonsForParentAsync(root.Id,HolonType.All,curentChildDepth:0,version:1);
            Assert.IsFalse(parentInterface.IsError,parentInterface.Message);Assert.AreEqual(1,parentInterface.Result.Single(item=>item.Id==child.Id).Version);
            Assert.IsNull((await _provider.LoadHolonAsync(root.Id,version:99)).Result);
            Assert.IsTrue((await _provider.LoadHolonAsync(root.Id,version:-1)).IsError);
            Assert.IsTrue((await _provider.LoadAllAvatarsAsync(-1)).IsError);
            Assert.IsFalse((await _provider.DeleteHolonByIdAsync(root.Id,false)).IsError);
            Assert.IsNull((await _provider.LoadHolonAsync(root.Id,version:1)).Result);
            Assert.IsFalse((await _provider.DeleteAvatarAsync(avatar.Id,false)).IsError);
            Assert.IsNull((await _provider.LoadAvatarAsync(avatar.Id,1)).Result);
        }

        [TestMethod]
        public async Task Storage_BatchSaveHonorsCurrentDepthAndPublishesOnlySelectedNodes()
        {
            var root=new Holon{Name=$"depth-{Guid.NewGuid():N}"};
            var child=new Holon{Name="selected-child"};
            var grandchild=new Holon{Name="unselected-grandchild"};
            root.Children.Add(child);child.Children.Add(grandchild);
            var atDepth=await _provider.SaveHolonsAsync(new[]{root},maxChildDepth:2,curentChildDepth:1);
            Assert.IsFalse(atDepth.IsError,atDepth.Message);
            Assert.AreNotEqual(Guid.Empty,root.Id);
            Assert.AreEqual(Guid.Empty,child.Id);
            Assert.AreEqual(Guid.Empty,child.ParentHolonId);
            Assert.AreEqual(0,(await _provider.LoadHolonAsync(root.Id)).Result.Children.Count());
            var fromRoot=_provider.SaveHolons(new[]{root},maxChildDepth:2,curentChildDepth:0);
            Assert.IsFalse(fromRoot.IsError,fromRoot.Message);
            Assert.AreNotEqual(Guid.Empty,child.Id);
            Assert.AreEqual(root.Id,child.ParentHolonId);
            Assert.AreEqual(Guid.Empty,grandchild.Id);
            Assert.AreEqual(Guid.Empty,grandchild.ParentHolonId);
            var loaded=await _provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);
            Assert.IsFalse(loaded.IsError,loaded.Message);
            Assert.AreEqual(child.Id,loaded.Result.Children.Single().Id);
            Assert.AreEqual(0,loaded.Result.Children.Single().Children.Count());
            var defaults=await _provider.SaveHolonAsync(root);
            Assert.IsFalse(defaults.IsError,defaults.Message);
            Assert.AreNotEqual(Guid.Empty,grandchild.Id);
            Assert.AreEqual(child.Id,grandchild.ParentHolonId);
            Assert.AreEqual(grandchild.Id,(await _provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true)).Result.Children.Single().Children.Single().Id);
        }

        [TestMethod]
        public async Task Storage_SearchHonorsOwnerMetadataAndGroupIntersection()
        {
            var marker=Guid.NewGuid().ToString("N");
            var owner=Guid.NewGuid();
            var selected=new Holon{Id=Guid.NewGuid(),Name=$"search-{marker}",CreatedByAvatarId=owner,
                MetaData=new Dictionary<string,object>{{"search-marker",marker}}};
            var excluded=new Holon{Id=Guid.NewGuid(),Name=selected.Name,CreatedByAvatarId=Guid.NewGuid(),
                MetaData=new Dictionary<string,object>{{"search-marker",marker}}};
            Assert.IsFalse((await _provider.SaveHolonsAsync(new[]{selected,excluded},saveChildren:false)).IsError);
            var query=new SearchParams{AvatarId=owner,SearchOnlyForCurrentAvatar=true,
                FilterByMetaData=new Dictionary<string,string>{{"search-marker",marker}},
                SearchGroups=new List<ISearchGroupBase>{
                    new SearchTextGroup{SearchQuery=marker,SearchHolons=true,HolonType=HolonType.All},
                    new SearchNumberGroup{Number=1,NumberOperator=SearchOperatorType.EqualTo,
                        SearchHolons=true,HolonType=HolonType.All,PreviousSearchGroupOperator=SearchParamGroupOperator.And}}};
            var result=await _provider.SearchAsync(query,loadChildren:false);
            Assert.IsFalse(result.IsError,result.Message);
            Assert.AreEqual(selected.Id,result.Result.SearchResultHolons.Single().Id);
            query.SearchOnlyForCurrentAvatar=false;
            Assert.AreEqual(2,_provider.Search(query,loadChildren:false).Result.SearchResultHolons.Count);
            query.FilterByMetaData["search-marker"]="not-the-marker";
            Assert.AreEqual(0,(await _provider.SearchAsync(query)).Result.NumberOfResults);
        }

        [TestMethod]
        public async Task Storage_NearbyUsesMicrodegreesAndRejectsMalformedStoredCoordinates()
        {
            var marker=Guid.NewGuid().ToString("N");
            var avatar=new Avatar{Id=Guid.NewGuid(),Username=$"nearby-{marker}",
                MetaData=new Dictionary<string,object>{{"Latitude",0d},{"Longitude",0d}}};
            var holon=new Holon{Id=Guid.NewGuid(),Name=$"nearby-{marker}",
                MetaData=new Dictionary<string,object>{{"Latitude",0d},{"Longitude",0d}}};
            Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
            Assert.IsFalse((await _provider.SaveHolonAsync(holon)).IsError);
            var avatars=_provider.GetAvatarsNearMe(0,0,0);
            Assert.IsFalse(avatars.IsError,avatars.Message);
            Assert.IsTrue(avatars.Result.Any(item=>item.Id==avatar.Id));
            var holons=_provider.GetHolonsNearMe(0,0,0,HolonType.All);
            Assert.IsFalse(holons.IsError,holons.Message);
            Assert.IsTrue(holons.Result.Any(item=>item.Id==holon.Id));
            Assert.IsFalse(_provider.GetAvatarsNearMe(1000000,0,1).Result.Any(item=>item.Id==avatar.Id));
            Assert.IsFalse(_provider.GetHolonsNearMe(1000000,0,1,HolonType.All).Result.Any(item=>item.Id==holon.Id));
            var corruptionAcknowledged=false;
            try
            {
                holon.MetaData["Longitude"]="not-a-coordinate";
                var corrupted=await _provider.SaveHolonAsync(holon);
                Assert.IsFalse(corrupted.IsError,corrupted.Message);
                corruptionAcknowledged=true;
                var malformed=_provider.GetHolonsNearMe(0,0,1,HolonType.All);
                Assert.IsTrue(malformed.IsError);
                Assert.IsInstanceOfType<System.IO.InvalidDataException>(malformed.Exception);
                Assert.IsNull(malformed.Result);
            }
            finally
            {
                if(corruptionAcknowledged)
                {
                    holon.MetaData["Longitude"]=0d;
                    var repaired=await _provider.SaveHolonAsync(holon);
                    Assert.IsFalse(repaired.IsError,repaired.Message);
                }
            }
        }

        [TestMethod]
        public async Task OfficialSdk_FinalizedFailedOriginalIsFailedNotCompleted()
        {
            var result=await _provider.GetTransactionStatusAsync(Required("NEAROASIS_FAILED_STATUS_TRANSACTIONID"));
            Assert.IsFalse(result.IsError,result.Message);
            Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Failed,result.Result);
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
