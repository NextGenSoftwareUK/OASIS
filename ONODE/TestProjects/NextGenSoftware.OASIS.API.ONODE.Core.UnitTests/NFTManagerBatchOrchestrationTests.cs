using System.Reflection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using CoreManagers = NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

[Collection("NFT provider registry")]
public sealed class NFTManagerBatchOrchestrationTests
{
    [Theory]
    [InlineData(false, 0, false)]
    [InlineData(true, 0, false)]
    [InlineData(false, 2, false)]
    [InlineData(false, 2, true)]
    public async Task FullBatchRetainsEveryAssetAndReportsMetadataFailure(bool failStorage, int failMintAt, bool partialAsset)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
        {
            HyperDriveMode = HyperDriveModes.V2,
            StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false,
                AutoFailOverEnabled = false, AutoReplicationEnabled = false, AutoLoadBalanceEnabled = false }
        }};
        var storage = new Mock<IOASISStorageProvider>();
        storage.SetupAllProperties();
        storage.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.LocalFileOASIS);
        storage.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        storage.Object.ProviderCapabilities = new List<EnumValue<ProviderCategory>>();
        storage.Object.IsProviderActivated = true;
        var saved = new List<IHolon>();
        storage.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .Returns((IHolon holon, bool children, bool recursive, int depth, bool continueOnError, bool onProvider) =>
            {
                saved.Add(holon);
                return Task.FromResult(new OASISResult<IHolon>
                    { Result = failStorage ? null! : holon, IsError = failStorage, Message = failStorage ? "storage unavailable" : "saved" });
            });
        var registry = new CoreManagers.ProviderManager(null!, dna);
        registry.RegisterProvider(storage.Object);
        var manager = new NFTManager((IOASISStorageProvider)null!, Guid.NewGuid(), dna);
        manager.Data = (CoreManagers.HolonManager)Activator.CreateInstance(typeof(CoreManagers.HolonManager),
            BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { storage.Object, dna, registry }, null)!;
        var minted = 0;
        var provider = new Mock<IOASISNFTProvider>(MockBehavior.Strict);
        provider.Setup(x => x.MintNFTAsync(It.IsAny<IMintWeb3NFTRequest>())).Returns((IMintWeb3NFTRequest request) =>
        {
            Assert.Equal(1, request.NumberToMint);
            Assert.Equal(1m, request.Amount);
            minted++;
            if (minted == failMintAt)
                return Task.FromResult(new OASISResult<IWeb3NFTTransactionResponse>
                {
                    IsError = true, Message = "uncertain delivery",
                    Result = new Web3NFTTransactionResponse { TransactionResult = $"mint-{minted}",
                        Web3NFT = partialAsset ? new Web3NFT { NFTTokenAddress = $"asset-{minted}",
                            MintTransactionHash = $"mint-{minted}",
                            MetaData = new Dictionary<string, string> { ["native-id"] = $"asset-{minted}" } } : null! }
                });
            return Task.FromResult(new OASISResult<IWeb3NFTTransactionResponse>
            {
                Result = new Web3NFTTransactionResponse { TransactionResult = $"mint-{minted}",
                    Web3NFT = new Web3NFT { NFTTokenAddress = $"asset-{minted}",
                        MetaData = new Dictionary<string, string> { ["native-id"] = $"asset-{minted}" } } }
            });
        });
        var original = new MintWeb4NFTRequest
        {
            NumberToMint = 3, Amount = 3, ImageUrl = "https://example.invalid/image.png",
            JSONMetaDataURL = "https://example.invalid/metadata.json", JSONMetaData = "{}",
            OnChainProvider = new EnumValue<ProviderType>(ProviderType.HashgraphOASIS),
            OffChainProvider = new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),
            NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(NFTOffChainMetaType.ExternalJSONURL),
            NFTStandardType = new EnumValue<NFTStandardType>(NFTStandardType.ERC721),
            MetaData = new Dictionary<string, string>(), Tags = new List<string>()
        };
        var web3 = new MintWeb3NFTRequest
        {
            NumberToMint = 3, Amount = 3, Price = 0, Discount = 0,
            OnChainProvider = ProviderType.HashgraphOASIS, OffChainProvider = ProviderType.LocalFileOASIS,
            NFTOffChainMetaType = NFTOffChainMetaType.ExternalJSONURL, NFTStandardType = NFTStandardType.ERC721,
            MetaData = new Dictionary<string, string>(), Tags = new List<string>()
        };
        var method = typeof(NFTManager).GetMethod("MintNFTInternalAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var result = await (Task<OASISResult<IWeb4NFT>>)method.Invoke(manager, new object?[]
        {
            new OASISResult<IWeb4NFT>(), original, web3, original,
            new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),
            new OASISResult<IOASISNFTProvider>(provider.Object), null, false, ResponseFormatType.FormattedText, true
        })!;

        var expectedCalls = failMintAt == 0 ? 3 : failMintAt;
        var expectedAssets = failMintAt == 0 ? 3 : failMintAt - 1 + (partialAsset ? 1 : 0);
        Assert.Equal(expectedCalls, minted);
        Assert.Equal(expectedAssets, result.Result.Web3NFTs.Count);
        Assert.Equal(expectedAssets, result.Result.NewlyMintedWeb3NFTs.Count);
        Assert.Equal(failMintAt == 0 ? 3 : failMintAt - 1, result.SavedCount);
        Assert.Equal(failMintAt == 0 ? 4 : failMintAt - 1, saved.Count);
        Assert.Equal(failStorage || failMintAt != 0, result.IsError);
        Assert.Equal(!failStorage && failMintAt == 0, result.IsSaved);
        if (result.IsError)
            Assert.Contains("Reconcile", result.Message);
        if (failMintAt != 0)
            Assert.Contains($"mint-{failMintAt}", result.Message);
        Assert.Equal(3, original.NumberToMint);
        Assert.Equal(3, web3.NumberToMint);
        Assert.Equal(3m, web3.Amount);
        Assert.Equal(Enumerable.Range(1, expectedAssets).Select(i => $"asset-{i}"), result.Result.Web3NFTs.Select(x => x.MetaData["native-id"]));
        provider.Verify(x => x.MintNFTAsync(It.IsAny<IMintWeb3NFTRequest>()), Times.Exactly(expectedCalls));
        provider.VerifyNoOtherCalls();
    }
}
