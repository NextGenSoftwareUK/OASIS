using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Moq;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

[Collection("NFT provider registry")]
public sealed class NFTStandardValidationTests : IDisposable
{
    private readonly IOASISBlockchainStorageProvider? _ownedProvider;

    public NFTStandardValidationTests()
    {
        if (ProviderManager.Instance.GetProvider(ProviderType.EthereumOASIS) != null)
            return;

        var provider = new Mock<IOASISBlockchainStorageProvider>();
        provider.SetupGet(x => x.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.EthereumOASIS));
        provider.SetupGet(x => x.ProviderCategory).Returns(new EnumValue<ProviderCategory>(ProviderCategory.EVMBlockchain));
        provider.SetupGet(x => x.ProviderCapabilities).Returns(new List<EnumValue<ProviderCategory>>());
        provider.Setup(x => x.DeActivateProvider()).Returns(new OASISResult<bool> { Result = true });
        _ownedProvider = provider.Object;
        Assert.True(ProviderManager.Instance.RegisterProvider(_ownedProvider));
    }

    public void Dispose()
    {
        if (_ownedProvider != null)
            ProviderManager.Instance.UnRegisterProvider(_ownedProvider);
    }

    [Theory]
    [InlineData(NFTStandardType.SuiObject, ProviderType.SuiOASIS, true)]
    [InlineData(NFTStandardType.SuiObject, ProviderType.EthereumOASIS, false)]
    [InlineData(NFTStandardType.SuiObject, ProviderType.SolanaOASIS, false)]
    [InlineData(NFTStandardType.SuiObject, ProviderType.Default, false)]
    [InlineData(NFTStandardType.ERC721, ProviderType.SuiOASIS, false)]
    [InlineData(NFTStandardType.ERC1155, ProviderType.SuiOASIS, false)]
    [InlineData(NFTStandardType.SPL, ProviderType.SuiOASIS, false)]
    [InlineData(NFTStandardType.ERC721, ProviderType.EthereumOASIS, true)]
    [InlineData(NFTStandardType.ERC1155, ProviderType.EthereumOASIS, true)]
    [InlineData(NFTStandardType.SPL, ProviderType.SolanaOASIS, true)]
    [InlineData(NFTStandardType.ERC721, ProviderType.SolanaOASIS, false)]
    [InlineData(NFTStandardType.SPL, ProviderType.EthereumOASIS, false)]
    [InlineData((NFTStandardType)999, ProviderType.SuiOASIS, false)]
    [InlineData((NFTStandardType)999, ProviderType.EthereumOASIS, false)]
    public void StandardMustMatchTheActualChainModel(NFTStandardType standard, ProviderType provider, bool expected)
    {
        // Supplying DNA and no storage provider avoids activating any external service.
        var manager = new NFTManager((IOASISStorageProvider)null!, Guid.NewGuid(), new OASISDNA());
        var result = manager.IsNFTStandardTypeValid(standard, provider);
        Assert.Equal(!expected, result.IsError);
        Assert.Equal(expected, result.Result);
    }

    [Fact]
    public void UnrelatedProviderWithNoSecondaryCapabilitiesDoesNotBreakEvmValidation()
    {
        var registry = new ProviderManager(null!, new OASISDNA());
        var storage = new Mock<IOASISStorageProvider>();
        storage.SetupGet(x => x.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.LocalFileOASIS));
        storage.SetupGet(x => x.ProviderCategory).Returns(new EnumValue<ProviderCategory>(ProviderCategory.Storage));
        storage.SetupGet(x => x.ProviderCapabilities).Returns((List<EnumValue<ProviderCategory>>)null!);
        Assert.True(registry.RegisterProvider(storage.Object));
        try
        {
            Assert.Empty(registry.GetAllEVMBlockchainProviders());
            Assert.Empty(registry.GetAllBlockchainProviders());
        }
        finally
        {
            registry.UnRegisterProvider(storage.Object);
        }
    }

    [Fact]
    public void ExistingSerializedEnumValuesAreUnchanged()
    {
        Assert.Equal(0, (int)NFTStandardType.ERC721);
        Assert.Equal(1, (int)NFTStandardType.ERC1155);
        Assert.Equal(2, (int)NFTStandardType.SPL);
        Assert.Equal(3, (int)NFTStandardType.SuiObject);
    }
}

[CollectionDefinition("NFT provider registry", DisableParallelization = true)]
public sealed class NFTProviderRegistryCollection;
