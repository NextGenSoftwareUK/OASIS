using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedSaveRejectsForeignSelectedChildEvenWithForgedPayloadCreator(bool unchanged)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var root = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = actor };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, CreatedByAvatarId = actor };
        root.Children.Add(child);
        if (unchanged) root.AcceptChanges();
        var expected = new Dictionary<Guid, Guid?> { [root.Id] = Guid.NewGuid(), [child.Id] = Guid.NewGuid() };
        foreach (var id in new[] { root.Id, child.Id })
        {
            var record = new Holon { Id = id, CreatedByAvatarId = id == root.Id ? actor : Guid.NewGuid() };
            fixture.Provider.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0))
                .ReturnsAsync(new OASISResult<IHolon>(record) { IsLoaded = true });
        }

        var result = await fixture.Manager.SaveOwnedHolonAsync(root, actor, new CosmicWriteOptions(Guid.NewGuid(), expected));

        result.ErrorCode.Should().Be("COSMIC_WRITE_FORBIDDEN");
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional")).Should().BeEmpty();
        child.CreatedByAvatarId.Should().Be(actor);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedSaveBindsCallerRevisionInsteadOfFreshOwnershipLookup(bool explicitRevision)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var claimed = Guid.NewGuid();
        var fresh = Guid.NewGuid();
        var source = new Holon { Id = Guid.NewGuid(), Name = "stale client edit" };
        CosmicConcurrency.SetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS, claimed);
        var stored = new Holon { Id = source.Id, CreatedByAvatarId = actor };
        CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, fresh);
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(source.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        var explicitClaim = Guid.NewGuid();
        var expected = explicitRevision ? explicitClaim : claimed;
        ConditionalHolonWriteRequest? submitted = null;
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true,
                ErrorCode = CosmicConcurrencyException.Code, Message = "stale caller revision" });

        var result = await fixture.Manager.SaveOwnedHolonAsync(source, actor,
            new CosmicWriteOptions(Guid.NewGuid(), explicitRevision
                ? new Dictionary<Guid, Guid?> { [source.Id] = explicitClaim } : null), saveChildren: false);

        result.ErrorCode.Should().Be(CosmicConcurrencyException.Code);
        submitted!.Write.Conditions.Single().ExpectedRevisionId.Should().Be(expected);
        submitted.Write.ActorId.Should().Be(actor);
        source.Name.Should().Be("stale client edit");
        CosmicConcurrency.GetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS).Should().Be(claimed);
    }

    [Fact]
    public async Task OwnedSaveRejectsBlindExistingUpdateWithoutRevision()
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var source = new Holon { Id = Guid.NewGuid() };
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(source.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon { Id = source.Id, CreatedByAvatarId = actor }) { IsLoaded = true });

        var result = await fixture.Manager.SaveOwnedHolonAsync(source, actor, new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false);

        result.ErrorCode.Should().Be("COSMIC_EXPECTED_REVISION_REQUIRED");
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional")).Should().BeEmpty();
    }

    [Fact]
    public async Task OwnedSaveNodeLimitFailsBeforeAnyStorageAccessAndReleasesLeases()
    {
        var fixture = Fixture(true, conditional: true);
        var root = new Holon { Children = new List<IHolon> { new Holon(), new Holon() } };
        fixture.Provider.Invocations.Clear();

        var result = await fixture.Manager.SaveOwnedHolonAsync(root, Guid.NewGuid(),
            new CosmicWriteOptions(Guid.NewGuid()), maximumHolons: 2);

        result.IsError.Should().BeTrue();
        fixture.Provider.Invocations.Should().BeEmpty();
        root.Id.Should().BeEmpty();
        using var released = Core.Managers.HolonSavePlan.Create(new[] { root }, true, true, 0);
    }

    [Fact]
    public async Task OwnedSaveDoesNotEnterLegacyPipeline()
    {
        var fixture = Fixture(false, conditional: true);
        fixture.Provider.Invocations.Clear();

        var result = await fixture.Manager.SaveOwnedHolonAsync(new Holon(), Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()));

        result.ErrorCode.Should().Be("COSMIC_OWNED_WRITE_V2_REQUIRED");
        fixture.Provider.Invocations.Should().BeEmpty();
    }
    [Fact]
    public async Task OwnedSaveCannotAuthorizeAgainstAFailoverReplica()
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var source = new Holon { Id = Guid.NewGuid() };
        CosmicConcurrency.SetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(source.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, ErrorCode = "OWNER_AUTHORITY_OFFLINE" });
        var replica = new Mock<IOASISStorageProvider>();
        replica.SetupAllProperties();
        replica.Object.ProviderType = new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.LocalFileOASIS);
        replica.Object.ProviderCategory = new NextGenSoftware.Utilities.EnumValue<ProviderCategory>(ProviderCategory.Storage);
        replica.Object.IsProviderActivated = true;
        replica.Setup(provider => provider.LoadHolonAsync(source.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon { Id = source.Id, CreatedByAvatarId = actor }) { IsLoaded = true });
        fixture.Providers.RegisterProvider(replica.Object);
        fixture.Providers.SetAndReplaceAutoFailOverListForProviders(new[]
        { new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.LocalFileOASIS) }).IsError.Should().BeFalse();
        fixture.Providers.IsAutoFailOverEnabled = true;
        replica.Invocations.Clear();

        var result = await fixture.Manager.SaveOwnedHolonAsync(source, actor,
            new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false);

        result.ErrorCode.Should().Be("COSMIC_WRITE_OWNERSHIP_LOOKUP_FAILED");
        replica.Invocations.Should().BeEmpty();
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional")).Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedSaveNamedCreationUsesOnlyAnAuthoritativeAbsence(bool codedAbsence)
    {
        var fixture = Fixture(true, conditional: true);
        var source = new Holon { Id = Guid.NewGuid() };
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(source.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = codedAbsence,
                ErrorCode = codedAbsence ? OASISResultErrorCodes.RecordNotFound : null });
        ConditionalHolonWriteRequest? submitted = null;
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCode = "OWNED_CREATE_CONTROL" });

        var result = await fixture.Manager.SaveOwnedHolonAsync(source, Guid.NewGuid(),
            new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false);

        result.ErrorCode.Should().Be("OWNED_CREATE_CONTROL");
        submitted!.Write.Conditions.Single().ExpectedRevisionId.Should().BeNull();
    }

    [Fact]
    public async Task OwnedSaveNotFoundWithTransportExceptionCannotBecomeCreation()
    {
        var fixture = Fixture(true, conditional: true);
        var source = new Holon { Id = Guid.NewGuid() };
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(source.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, ErrorCode = OASISResultErrorCodes.RecordNotFound,
                Exception = new IOException("inconclusive transport") });

        var result = await fixture.Manager.SaveOwnedHolonAsync(source, Guid.NewGuid(),
            new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false);

        result.ErrorCode.Should().Be("COSMIC_WRITE_OWNERSHIP_LOOKUP_FAILED");
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional")).Should().BeEmpty();
    }

}
