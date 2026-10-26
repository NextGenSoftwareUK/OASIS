using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedDeletePublishesOnlyAnAcknowledgedConditionalDeletion(bool soft)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var id = Guid.NewGuid();
        var revision = Guid.NewGuid();
        var stored = new Holon { Id = id, CreatedByAvatarId = actor };
        CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, revision);
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConditionalHolonWriteRequest request, CancellationToken _) =>
            {
                var acknowledged = request.Roots.Single();
                acknowledged.DeletedDate = DateTime.UtcNow;
                acknowledged.DeletedByAvatarId = request.Write.ActorId;
                acknowledged.Version++;
                CosmicConcurrency.SetProviderRevisionId(acknowledged, request.Write.Authority, Guid.NewGuid());
                return new OASISResult<IEnumerable<IHolon>>(request.Roots) { IsDeleted = true };
            });

        var result = await fixture.Manager.DeleteOwnedHolonAsync(id, actor,
            new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = revision }), soft);

        result.IsError.Should().BeFalse(result.Message);
        result.IsDeleted.Should().BeTrue();
        result.Result.IsDeleted.Should().BeTrue();
        result.Result.DeletedByAvatarId.Should().Be(actor);
        fixture.Provider.As<IConditionalHolonStorageProvider>().Verify(provider =>
            provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedDeleteRejectsMissingIdentityOrActorBeforeStorage(bool missingActor)
    {
        var fixture = Fixture(true, conditional: true);
        var id = missingActor ? Guid.NewGuid() : Guid.Empty;
        var actor = missingActor ? Guid.Empty : Guid.NewGuid();
        fixture.Provider.Invocations.Clear();

        var result = await fixture.Manager.DeleteOwnedHolonAsync(id, actor,
            new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = Guid.NewGuid() }));

        result.ErrorCode.Should().Be("COSMIC_OWNED_DELETE_INPUT_INVALID");
        fixture.Provider.Invocations.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedDeleteChecksStoredCreatorBeforeAnyConditionalMutation(bool soft)
    {
        var fixture = Fixture(true, conditional: true);
        var id = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var revision = Guid.NewGuid();
        var stored = new Holon { Id = id, CreatedByAvatarId = Guid.NewGuid() };
        CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, revision);
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });

        var result = await fixture.Manager.DeleteOwnedHolonAsync(id, actor,
            new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = revision }), soft);

        result.ErrorCode.Should().Be("COSMIC_WRITE_FORBIDDEN");
        result.IsDeleted.Should().BeFalse();
        fixture.Provider.As<IConditionalHolonStorageProvider>().Verify(provider =>
            provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedDeletePreservesCallerRevisionAndCapturedAuthorityAfterItsRead(bool soft)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var id = Guid.NewGuid();
        var originalRevision = Guid.NewGuid();
        var newerRevision = Guid.NewGuid();
        var operation = Guid.NewGuid();
        var stored = new Holon { Id = id, Name = "original", CreatedByAvatarId = actor };
        CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, newerRevision);
        var other = new Mock<IOASISStorageProvider>();
        other.SetupAllProperties();
        other.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.LocalFileOASIS);
        other.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        other.Object.IsProviderActivated = true;
        other.Setup(provider => provider.ActivateProvider()).Returns(new OASISResult<bool>(true));
        fixture.Providers.RegisterProvider(other.Object);
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .Callback(() => fixture.Providers.SetAndActivateCurrentStorageProvider(other.Object))
            .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        ConditionalHolonWriteRequest? submitted = null;
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCode = CosmicConcurrencyException.Code });
        other.Invocations.Clear();

        var result = await fixture.Manager.DeleteOwnedHolonAsync(id, actor,
            new CosmicWriteOptions(operation, new Dictionary<Guid, Guid?> { [id] = originalRevision }), soft);

        result.ErrorCode.Should().Be(CosmicConcurrencyException.Code);
        result.IsDeleted.Should().BeFalse();
        submitted.Should().NotBeNull();
        submitted!.Write.ActorId.Should().Be(actor);
        submitted.Write.OperationId.Should().Be(operation);
        submitted.Write.Authority.Should().Be(ProviderType.SQLLiteDBOASIS);
        submitted.Write.Conditions.Single().ExpectedRevisionId.Should().Be(originalRevision);
        submitted.Operation.Should().Be(soft ? ConditionalHolonOperation.SoftDelete : ConditionalHolonOperation.HardDelete);
        stored.IsDeleted.Should().BeFalse();
        other.Invocations.Where(call => call.Method.Name.Contains("LoadHolon") || call.Method.Name.Contains("DeleteHolon")
            || call.Method.Name.Contains("Conditional")).Should().BeEmpty();
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("create-only")]
    [InlineData("outside-record")]
    public async Task OwnedDeleteRequiresExactlyOneOriginalRevisionBeforeProviderAccess(string conditions)
    {
        var fixture = Fixture(true, conditional: true);
        var id = Guid.NewGuid();
        Dictionary<Guid, Guid?>? revisions = conditions == "missing" ? null
            : conditions == "create-only" ? new() { [id] = null }
            : new() { [id] = Guid.NewGuid(), [Guid.NewGuid()] = Guid.NewGuid() };
        fixture.Provider.Invocations.Clear();

        var result = await fixture.Manager.DeleteOwnedHolonAsync(id, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid(), revisions));

        result.ErrorCode.Should().Be("COSMIC_EXPECTED_REVISION_REQUIRED");
        fixture.Provider.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task OwnedDeletePreservesAuthorityFailureEvidenceAndNeverMutates()
    {
        var fixture = Fixture(true, conditional: true);
        var id = Guid.NewGuid();
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, ErrorCount = 3, ErrorCode = "AUTHORITY_UNAVAILABLE",
                MetaData = new Dictionary<string, string> { ["original-attempt"] = "retained" } });

        var result = await fixture.Manager.DeleteOwnedHolonAsync(id, Guid.NewGuid(),
            new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = Guid.Empty }));

        result.ErrorCount.Should().Be(3);
        result.ErrorCode.Should().Be("AUTHORITY_UNAVAILABLE");
        result.MetaData.Should().ContainKey("original-attempt").WhoseValue.Should().Be("retained");
        fixture.Provider.As<IConditionalHolonStorageProvider>().Verify(provider =>
            provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OwnedDeleteRejectsLegacyBeforeProviderAccess()
    {
        var fixture = Fixture(false);
        var id = Guid.NewGuid();
        fixture.Provider.Invocations.Clear();

        var result = await fixture.Manager.DeleteOwnedHolonAsync(id, Guid.NewGuid(),
            new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = Guid.NewGuid() }));

        result.ErrorCode.Should().Be("COSMIC_OWNED_DELETE_V2_REQUIRED");
        fixture.Provider.Invocations.Should().BeEmpty();
    }
}
