using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using SaveHolonRequest = NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.SaveHolonRequest;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class HostedHolonWriteEndpointTests
{
    [Fact]
    public async Task HostedCreateBindsRequestActorOperationAndProviderToOneConditionalWrite()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var actor = Guid.NewGuid();
        var operation = Guid.NewGuid();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = actor };
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        var provider = new Mock<IOASISStorageProvider>();
        provider.As<IConditionalHolonStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.IsProviderActivated = true;
        provider.Setup(value => value.ActivateProvider()).Returns(new OASISResult<bool>(true));
        ConditionalHolonWriteRequest? submitted = null;
        provider.As<IConditionalHolonStorageProvider>()
            .Setup(value => value.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>
                { IsError = true, ErrorCode = CosmicConcurrencyException.Code, Message = "Controlled conflict" });
        var registry = new ProviderManager(null, dna);
        registry.RegisterProvider(provider.Object);
        Assert.False(registry.SetAndActivateCurrentStorageProvider(provider.Object).IsError);
        var manager = new HolonManager(null, dna, registry);

        var result = await HostedHolonWriteEndpoint.SaveAsync(context, HyperDriveModes.V2,
            new SaveHolonRequest { Holon = new Holon { Name = "request-owned create" },
                OperationId = operation, ProviderType = "SQLLiteDBOASIS", SaveChildren = false }, () => manager);

        Assert.Equal(CosmicConcurrencyException.Code, result.ErrorCode);
        Assert.NotNull(submitted);
        Assert.Equal(actor, submitted.Write.ActorId);
        Assert.Equal(operation, submitted.Write.OperationId);
        Assert.Equal(ProviderType.SQLLiteDBOASIS, submitted.Write.Authority);
        Assert.Null(Assert.Single(submitted.Write.Conditions).ExpectedRevisionId);
        provider.As<IConditionalHolonStorageProvider>().Verify(value =>
            value.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false, true, "Default", HyperDriveModes.V2, "COSMIC_WRITE_ACTOR_REQUIRED")]
    [InlineData(true, false, "Default", HyperDriveModes.V2, "COSMIC_WRITE_INTENT_INVALID")]
    [InlineData(true, true, "All", HyperDriveModes.V2, "COSMIC_WRITE_INTENT_INVALID")]
    [InlineData(true, true, "not-a-provider", HyperDriveModes.V2, "COSMIC_WRITE_INTENT_INVALID")]
    [InlineData(true, true, "Default", "unknown", "COSMIC_OWNED_WRITE_V2_REQUIRED")]
    public async Task InvalidHostedIntentNeverAcquiresManager(bool authenticated, bool hasOperation,
        string provider, string engineMode, string expectedCode)
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        if (authenticated) context.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = Guid.NewGuid() };
        var acquisitions = 0;

        var result = await HostedHolonWriteEndpoint.SaveAsync(context, engineMode, new SaveHolonRequest
        {
            Holon = new Holon { Name = "conditional create" },
            OperationId = hasOperation ? Guid.NewGuid() : null,
            ProviderType = provider,
            SaveChildren = false
        }, () => { acquisitions++; throw new InvalidOperationException("Premature manager acquisition"); });

        Assert.True(result.IsError);
        Assert.Equal(expectedCode, result.ErrorCode);
        Assert.Equal(0, acquisitions);
    }
}
