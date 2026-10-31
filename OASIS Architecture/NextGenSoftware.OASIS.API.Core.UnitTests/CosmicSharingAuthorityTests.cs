using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharingCannotUseAReplicaRecipientListWhenWriteAuthorityIsOffline(bool revoke)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var recipient = Guid.NewGuid();
        var id = Guid.NewGuid();
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, ErrorCode = "PRIMARY_GRANT_AUTHORITY_OFFLINE" });
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCode = "WRONG_AUTHORITY_MUTATION_ATTEMPTED" });
        var replica = new Mock<IOASISStorageProvider>();
        replica.SetupAllProperties();
        replica.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.LocalFileOASIS);
        replica.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        replica.Object.IsProviderActivated = true;
        var divergent = new Holon { Id = id, CreatedByAvatarId = actor };
        HolonAccess.RecordShares(divergent, new[] { recipient });
        CosmicConcurrency.SetProviderRevisionId(divergent, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        replica.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(divergent) { IsLoaded = true });
        fixture.Providers.RegisterProvider(replica.Object);
        fixture.Providers.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS) })
            .IsError.Should().BeFalse();
        fixture.Providers.IsAutoFailOverEnabled = true;
        replica.Invocations.Clear();
        var manager = new Core.Managers.HolonManager(fixture.Provider.Object, fixture.Manager.OASISDNA, fixture.Providers);
        var avatar = new Mock<IAvatar>();
        avatar.SetupGet(value => value.Id).Returns(actor);

        var result = revoke
            ? await HolonAccess.RevokeAsync(manager, id, new[] { recipient }, avatar.Object)
            : await HolonAccess.ShareAsync(manager, id, new[] { Guid.NewGuid() }, avatar.Object);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("PRIMARY_GRANT_AUTHORITY_OFFLINE");
        replica.Invocations.Should().BeEmpty();
        fixture.Provider.As<IConditionalHolonStorageProvider>().Verify(provider =>
            provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharingUsesTheAuthenticatedCallerSnapshotAcrossItsAwait(bool mutateRole)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var foreign = Guid.NewGuid();
        var currentId = actor;
        var id = Guid.NewGuid();
        var avatar = new Mock<IAvatar>();
        avatar.SetupGet(value => value.Id).Returns(() => currentId);
        var stored = new Holon { Id = id, CreatedByAvatarId = foreign };
        CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .Callback(() =>
            {
                if (mutateRole) avatar.SetupGet(value => value.AvatarType).Returns(new EnumValue<AvatarType>(AvatarType.Wizard));
                else currentId = foreign;
            })
            .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCode = "ACTOR_SNAPSHOT_BYPASS" });

        var result = await HolonAccess.ShareAsync(fixture.Manager, id, new[] { Guid.NewGuid() }, avatar.Object);

        result.ErrorCode.Should().Be("COSMIC_SHARE_FORBIDDEN");
        fixture.Provider.As<IConditionalHolonStorageProvider>().Verify(provider =>
            provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AuthorityObservationKeepsItsProviderAfterDefaultChanges()
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var id = Guid.NewGuid();
        var revision = Guid.NewGuid();
        var stored = new Holon { Id = id, Name = "pinned", CreatedByAvatarId = actor };
        CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, revision);
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        var observed = await fixture.Manager.LoadHolonForWriteAsync(id, actor);
        observed.IsError.Should().BeFalse(observed.Message);
        observed.Result.Authority.Should().Be(ProviderType.SQLLiteDBOASIS);
        observed.Result.ActorId.Should().Be(actor);
        HolonChangeTracking.HasChanges(observed.Result.Record, includeChildren: false).Should().BeFalse();
        var other = new Mock<IOASISStorageProvider>();
        other.SetupAllProperties();
        other.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.LocalFileOASIS);
        other.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        other.Object.IsProviderActivated = true;
        other.Setup(provider => provider.ActivateProvider()).Returns(new OASISResult<bool>(true));
        fixture.Providers.RegisterProvider(other.Object);
        fixture.Providers.SetAndActivateCurrentStorageProvider(other.Object).IsError.Should().BeFalse();
        other.Invocations.Clear();
        ConditionalHolonWriteRequest? submitted = null;
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCode = "BOUND_PRIMARY_WRITE" });
        observed.Result.Record.Name = "changed";

        var saved = await fixture.Manager.SaveHolonAsync(observed.Result.Record, actor,
            new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false, providerType: observed.Result.Authority);

        saved.ErrorCode.Should().Be("BOUND_PRIMARY_WRITE");
        submitted!.Write.Authority.Should().Be(ProviderType.SQLLiteDBOASIS);
        submitted.Write.Conditions.Single().ExpectedRevisionId.Should().Be(revision);
        other.Invocations.Where(call => call.Method.Name.Contains("LoadHolon") || call.Method.Name.Contains("SaveHolon")
            || call.Method.Name.Contains("Conditional")).Should().BeEmpty();
    }

    [Fact]
    public async Task AuthorityObservationRejectsLegacyBeforeStorageAccess()
    {
        var fixture = Fixture(false);
        fixture.Provider.Invocations.Clear();

        var observed = await fixture.Manager.LoadHolonForWriteAsync(Guid.NewGuid(), Guid.NewGuid());

        observed.ErrorCode.Should().Be("COSMIC_AUTHORITY_READ_V2_REQUIRED");
        fixture.Provider.Invocations.Should().BeEmpty();
    }

}
