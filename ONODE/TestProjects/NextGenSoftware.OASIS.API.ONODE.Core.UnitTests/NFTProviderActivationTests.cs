using System.Reflection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

[Collection("NFT provider registry")]
public sealed class NFTProviderActivationTests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, true)]
    public void ActivationRequiresPositiveResultAndActivatedState(bool success, bool error, bool activated, bool accepted)
    {
        var provider = Provider();
        provider.As<IOASISNFTProvider>();
        var state = false;
        provider.SetupGet(x => x.IsProviderActivated).Returns(() => state);
        provider.Setup(x => x.ActivateProvider()).Returns(() => {
            state = activated;
            return new OASISResult<bool> { Result = success, IsError = error };
        });
        WithRegistry(provider.Object, manager => {
            var result = manager.GetNFTProvider(ProviderType.SuiOASIS);
            Assert.Equal(!accepted, result.IsError);
            if (accepted) Assert.Same(provider.Object, result.Result);
            else Assert.Null(result.Result);
            provider.Verify(x => x.ActivateProvider(), Times.Once);
        });
    }

    [Fact]
    public void NullActivationResultIsAnErrorNotASecondaryException()
    {
        var provider = Provider(); provider.As<IOASISNFTProvider>();
        provider.Setup(x => x.ActivateProvider()).Returns((OASISResult<bool>)null!);
        WithRegistry(provider.Object, manager => Assert.True(manager.GetNFTProvider(ProviderType.SuiOASIS).IsError));
    }

    [Fact]
    public void NonNftProviderIsRejectedWithoutActivation()
    {
        var provider = Provider();
        WithRegistry(provider.Object, manager => {
            var result = manager.GetNFTProvider(ProviderType.SuiOASIS);
            Assert.True(result.IsError); Assert.Null(result.Result);
            provider.Verify(x => x.ActivateProvider(), Times.Never);
        });
    }

    private static Mock<IOASISStorageProvider> Provider()
    {
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupGet(x => x.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.SuiOASIS));
        return provider;
    }

    private static void WithRegistry(IOASISStorageProvider provider, Action<NFTManager> check)
    {
        var field = typeof(ProviderManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
        var original = field.GetValue(null);
        var dna = new OASISDNA();
        var registry = new ProviderManager(null!, dna);
        registry.RegisterProvider(provider);
        try {
            field.SetValue(null, registry);
            check(new NFTManager((IOASISStorageProvider)null!, Guid.NewGuid(), dna));
        } finally { field.SetValue(null, original); }
    }
}
