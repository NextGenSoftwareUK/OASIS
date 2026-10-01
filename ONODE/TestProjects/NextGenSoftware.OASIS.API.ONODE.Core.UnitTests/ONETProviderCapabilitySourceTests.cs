using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

public sealed class ONETProviderCapabilitySourceTests
{
    [Fact]
    public void AdvertisesOnlyExplicitlyPermittedActivatedHealthyProviders()
    {
        var mongo = Provider(ProviderType.MongoDBOASIS, true);
        var localFile = Provider(ProviderType.LocalFileOASIS, true);
        var holo = Provider(ProviderType.HoloOASIS, false);
        var source = new ONETProviderCapabilitySource(() => new[] { mongo, localFile, holo }, type =>
            type == ProviderType.LocalFileOASIS
                ? new ProviderPerformanceMetrics { ProviderType = type, TotalRequests = 3, FailedRequests = 3, ErrorRate = 1.0 }
                : new ProviderPerformanceMetrics { ProviderType = type, TotalRequests = 3, SuccessfulRequests = 3 });

        var capabilities = source.GetEligibleCapabilities(new[] { "MongoDBOASIS", "LocalFileOASIS", "HoloOASIS" });

        capabilities.Should().ContainSingle().Which.ProviderType.Should().Be("MongoDBOASIS");
    }

    [Fact]
    public void EmptyRemoteAllowListAdvertisesNoProvider()
    {
        var source = new ONETProviderCapabilitySource(() => new[] { Provider(ProviderType.MongoDBOASIS, true) }, _ => null);
        source.GetEligibleCapabilities(Array.Empty<string>()).Should().BeEmpty();
    }

    private static IOASISProvider Provider(ProviderType type, bool active)
    {
        var provider = new Mock<IOASISProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(type);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.ProviderCapabilities = new List<EnumValue<ProviderCategory>>
            { new EnumValue<ProviderCategory>(ProviderCategory.Storage) };
        provider.Object.IsProviderActivated = active;
        return provider.Object;
    }
}
