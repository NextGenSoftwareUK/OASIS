using Microsoft.Extensions.DependencyInjection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cosmic;
using SaveHolonRequest = NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.SaveHolonRequest;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public partial class HostedCosmicRecoveryTests
{
    private static (HolonManager Manager, Mock<IOASISStorageProvider> Provider, Holon Stored) WriteFixture(Guid actor)
    {
        var provider = new Mock<IOASISStorageProvider>(); provider.SetupAllProperties();
        provider.As<IConditionalHolonStorageProvider>(); provider.As<IHolonWritePreparationStorageProvider>();
        provider.Object.ProviderType = new EnumValue<ProviderType>(Authority);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage); provider.Object.IsProviderActivated = true;
        provider.Setup(value => value.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        var registry = new ProviderManager(null, dna); registry.RegisterProvider(provider.Object); registry.SetAndActivateCurrentStorageProvider(provider.Object);
        var stored = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = actor, Name = "owned delete original" };
        CosmicConcurrency.SetProviderRevisionId(stored, Authority, Guid.NewGuid());
        provider.Setup(value => value.LoadHolonAsync(stored.Id, It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        return (new HolonManager(null, dna, registry), provider, stored);
    }

    [Theory][InlineData("single")][InlineData("batch")][InlineData("soft")][InlineData("hard")]
    public async Task ConfiguredHostGrantEnrollsEveryWriteShapeWithoutChangingSharedManager(string shape)
    {
        var actor = Guid.NewGuid(); var operation = Guid.NewGuid(); var fixture = WriteFixture(actor);
        var selected = new List<Guid>();
        var policy = new Policy((a, p) => new(new CosmicOperationRecoveryGrant(a, p, record =>
        {
            Assert.NotEqual(Guid.Empty, record); selected.Add(record);
            return new HolonMetadataEncryptionPolicy("host-" + a.ToString("N"), "key-v1", new Keys());
        }, new[] { typeof(Holon) })));
        HolonWritePreparation? captured = null;
        fixture.Provider.As<IHolonWritePreparationStorageProvider>()
            .Setup(value => value.ClaimHolonWritePreparationAsync(It.IsAny<HolonWritePreparation>(), It.IsAny<CancellationToken>()))
            .Callback<HolonWritePreparation, CancellationToken>((value, _) => captured = value)
            .ReturnsAsync(new OASISResult<HolonWritePreparation> { IsError = true, ErrorCode = "TEST_CAPTURED_ORIGINAL" });
        string? code;
        if (shape == "batch")
        {
            var result = await HolonWriteEndpoint.SaveBatchAsync(fixture.Manager, new SaveHolonsRequest
                { Holons = new() { new Holon { Name = "private root a" }, new Holon { Name = "private root b" } },
                    OperationId = operation, SaveChildren = false }, actor, true, null, policy);
            code = result.ErrorCode;
        }
        else if (shape == "single")
            code = (await HolonWriteEndpoint.SaveAsync(fixture.Manager, new SaveHolonRequest
                { Holon = new Holon { Name = "private root" }, OperationId = operation, SaveChildren = false }, actor, true, null, policy)).ErrorCode;
        else
            code = (await HolonWriteEndpoint.DeleteAsync(fixture.Manager, new DeleteHolonRequest
                { Id = fixture.Stored.Id, OperationId = operation, SoftDelete = shape == "soft",
                    ExpectedRevisions = new() { [fixture.Stored.Id] = CosmicConcurrency.GetProviderRevisionId(fixture.Stored, Authority) } },
                actor, true, null, policy)).ErrorCode;
        Assert.Equal("TEST_CAPTURED_ORIGINAL", code); Assert.NotNull(captured);
        Assert.Equal(actor, captured.ActorId); Assert.Equal(operation, captured.OperationId); Assert.Equal(Authority, captured.Authority);
        Assert.Equal(shape == "batch" ? 2 : 1, captured.Records.Count);
        Assert.Equal(captured.Records.Select(value => value.HolonId).Order(), selected.Order());
        Assert.All(captured.Records, record => Assert.DoesNotContain("private root", record.Envelope));
        Assert.Null(fixture.Manager.PreparationPolicyResolver); Assert.False(fixture.Stored.IsDeleted);
        Assert.DoesNotContain(fixture.Provider.Invocations, call => call.Method.Name.Contains("ExecuteConditional"));
        Assert.Equal(1, policy.Calls);
    }

    [Theory][InlineData("denied")][InlineData("actor")][InlineData("authority")][InlineData("type")]
    public async Task HostPolicyDenialNeverUsesAnUnprotectedWrite(string mode)
    {
        var actor = Guid.NewGuid(); var fixture = WriteFixture(actor);
        var protection = new HolonMetadataEncryptionPolicy("unused-fallback", "key-v1", new Keys());
        fixture.Manager.PreparationEncryptionPolicy = protection;
        var policy = new Policy((a, p) => mode == "denied" ? new() { IsError = true, ErrorCode = "COSMIC_RECOVERY_FORBIDDEN" }
            : new(new CosmicOperationRecoveryGrant(mode == "actor" ? Guid.NewGuid() : a,
                mode == "authority" ? ProviderType.MongoDBOASIS : p, _ => protection,
                mode == "type" ? new[] { typeof(NextGenSoftware.OASIS.API.Core.Holons.Avatar) } : new[] { typeof(Holon) })));
        fixture.Provider.Invocations.Clear();
        var result = await HolonWriteEndpoint.SaveAsync(fixture.Manager, new SaveHolonRequest
            { Holon = new Holon(), OperationId = Guid.NewGuid(), SaveChildren = false }, actor, true, null, policy);
        Assert.True(result.IsError); Assert.False(result.IsSaved);
        Assert.DoesNotContain(fixture.Provider.Invocations, call => call.Method.Name.Contains("LoadHolon")
            || call.Method.Name.Contains("ClaimHolon") || call.Method.Name.Contains("ExecuteConditional"));
        Assert.Null(fixture.Manager.PreparationPolicyResolver);
    }
}
