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
    public async Task ProviderKeyOwnedDeletePublishesOnlyAnAcknowledgedConditionalDeletion(bool soft)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var id = Guid.NewGuid();
        var revision = Guid.NewGuid();
        var stored = new Holon { Id = id, CreatedByAvatarId = actor };
        CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, revision);
        fixture.Provider.Setup(provider => provider.LoadHolonAsync("native-key", false, true, 0, true, false, 0))
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

        var result = await fixture.Manager.DeleteOwnedHolonByProviderKeyAsync("native-key", id, actor,
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
    public async Task ProviderKeyOwnedDeleteRejectsMissingIdentityOrActorBeforeStorage(bool missingActor)
    {
        var fixture = Fixture(true, conditional: true);
        var id = missingActor ? Guid.NewGuid() : Guid.Empty;
        var actor = missingActor ? Guid.Empty : Guid.NewGuid();
        fixture.Provider.Invocations.Clear();

        var result = await fixture.Manager.DeleteOwnedHolonByProviderKeyAsync("native-key", id, actor,
            new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = Guid.NewGuid() }));

        result.ErrorCode.Should().Be("COSMIC_OWNED_DELETE_INPUT_INVALID");
        fixture.Provider.Invocations.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProviderKeyOwnedDeleteChecksStoredCreatorBeforeAnyConditionalMutation(bool soft)
    {
        var fixture = Fixture(true, conditional: true);
        var id = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var revision = Guid.NewGuid();
        var stored = new Holon { Id = id, CreatedByAvatarId = Guid.NewGuid() };
        CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, revision);
        fixture.Provider.Setup(provider => provider.LoadHolonAsync("native-key", false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });

        var result = await fixture.Manager.DeleteOwnedHolonByProviderKeyAsync("native-key", id, actor,
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
    public async Task ProviderKeyOwnedDeletePreservesCallerRevisionAndCapturedAuthorityAfterItsRead(bool soft)
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
        fixture.Provider.Setup(provider => provider.LoadHolonAsync("native-key", false, true, 0, true, false, 0))
            .Callback(() => fixture.Providers.SetAndActivateCurrentStorageProvider(other.Object))
            .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        ConditionalHolonWriteRequest? submitted = null;
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCode = CosmicConcurrencyException.Code });
        other.Invocations.Clear();

        var result = await fixture.Manager.DeleteOwnedHolonByProviderKeyAsync("native-key", id, actor,
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
    public async Task ProviderKeyOwnedDeleteRequiresExactlyOneOriginalRevisionBeforeProviderAccess(string conditions)
    {
        var fixture = Fixture(true, conditional: true);
        var id = Guid.NewGuid();
        Dictionary<Guid, Guid?>? revisions = conditions == "missing" ? null
            : conditions == "create-only" ? new() { [id] = null }
            : new() { [id] = Guid.NewGuid(), [Guid.NewGuid()] = Guid.NewGuid() };
        fixture.Provider.Invocations.Clear();

        var result = await fixture.Manager.DeleteOwnedHolonByProviderKeyAsync("native-key", id, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid(), revisions));

        result.ErrorCode.Should().Be("COSMIC_EXPECTED_REVISION_REQUIRED");
        fixture.Provider.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task ProviderKeyOwnedDeletePreservesAuthorityFailureEvidenceAndNeverMutates()
    {
        var fixture = Fixture(true, conditional: true);
        var id = Guid.NewGuid();
        fixture.Provider.Setup(provider => provider.LoadHolonAsync("native-key", false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, ErrorCount = 3, ErrorCode = "AUTHORITY_UNAVAILABLE",
                MetaData = new Dictionary<string, string> { ["original-attempt"] = "retained" } });

        var result = await fixture.Manager.DeleteOwnedHolonByProviderKeyAsync("native-key", id, Guid.NewGuid(),
            new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = Guid.Empty }));

        result.ErrorCount.Should().Be(3);
        result.ErrorCode.Should().Be("AUTHORITY_UNAVAILABLE");
        result.MetaData.Should().ContainKey("original-attempt").WhoseValue.Should().Be("retained");
        fixture.Provider.As<IConditionalHolonStorageProvider>().Verify(provider =>
            provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProviderKeyOwnedDeleteRejectsLegacyBeforeProviderAccess()
    {
        var fixture = Fixture(false);
        var id = Guid.NewGuid();
        fixture.Provider.Invocations.Clear();

        var result = await fixture.Manager.DeleteOwnedHolonByProviderKeyAsync("native-key", id, Guid.NewGuid(),
            new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = Guid.NewGuid() }));

        result.ErrorCode.Should().Be("COSMIC_OWNED_DELETE_V2_REQUIRED");
        fixture.Provider.Invocations.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProviderKeyOwnedDeleteRejectsAnotherLogicalIdentity(bool soft)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var intendedId = Guid.NewGuid();
        var stored = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = actor };
        CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        fixture.Provider.Setup(provider => provider.LoadHolonAsync("native-key", false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });

        var result = await fixture.Manager.DeleteOwnedHolonByProviderKeyAsync("native-key", intendedId, actor,
            new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [intendedId] = Guid.NewGuid() }), soft);

        result.ErrorCode.Should().Be("COSMIC_WRITE_INTENT_INVALID");
        result.IsDeleted.Should().BeFalse();
        fixture.Provider.As<IConditionalHolonStorageProvider>().Verify(provider =>
            provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("oversized")]
    public async Task ProviderKeyOwnedDeleteRejectsInvalidKeyBeforeStorage(string? key)
    {
        var fixture = Fixture(true, conditional: true);
        var id = Guid.NewGuid();
        fixture.Provider.Invocations.Clear();
        var result = await fixture.Manager.DeleteOwnedHolonByProviderKeyAsync(key == "oversized" ? new string('x', 4097) : key,
            id, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = Guid.NewGuid() }));
        result.ErrorCode.Should().Be("COSMIC_OWNED_DELETE_INPUT_INVALID");
        fixture.Provider.Invocations.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task OwnedDeleteCannotBorrowOwnershipFromAnAvailableReplica(bool byKey, bool soft)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var id = Guid.NewGuid();
        var unavailable = new OASISResult<IHolon> { IsError = true, ErrorCount = 2,
            ErrorCode = "OWNER_AUTHORITY_OFFLINE", MetaData = new() { ["original-authority"] = "retained" } };
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0)).ReturnsAsync(unavailable);
        fixture.Provider.Setup(provider => provider.LoadHolonAsync("native-key", false, true, 0, true, false, 0)).ReturnsAsync(unavailable);
        var replica = new Mock<IOASISStorageProvider>();
        replica.SetupAllProperties();
        replica.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.LocalFileOASIS);
        replica.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        replica.Object.IsProviderActivated = true;
        var replicaRecord = new Holon { Id = id, CreatedByAvatarId = actor };
        replica.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(replicaRecord) { IsLoaded = true });
        replica.Setup(provider => provider.LoadHolonAsync("native-key", false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(replicaRecord) { IsLoaded = true });
        fixture.Providers.RegisterProvider(replica.Object);
        fixture.Providers.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS) })
            .IsError.Should().BeFalse();
        fixture.Providers.IsAutoFailOverEnabled = true;
        replica.Invocations.Clear();
        var options = new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [id] = Guid.NewGuid() });
        var result = byKey
            ? await fixture.Manager.DeleteOwnedHolonByProviderKeyAsync("native-key", id, actor, options, soft)
            : await fixture.Manager.DeleteOwnedHolonAsync(id, actor, options, soft);
        result.ErrorCode.Should().Be("OWNER_AUTHORITY_OFFLINE");
        result.ErrorCount.Should().Be(2);
        result.MetaData.Should().ContainKey("original-authority").WhoseValue.Should().Be("retained");
        result.IsDeleted.Should().BeFalse();
        replica.Invocations.Should().BeEmpty();
        fixture.Provider.As<IConditionalHolonStorageProvider>().Verify(provider =>
            provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class UnknownOwnedDeleteOutcome : System.IO.IOException, INativeMutationReconciliationRequired
    {
        public string OriginalTransactionId { get; }
        public UnknownOwnedDeleteOutcome(string original) : base("The acknowledgement was not observed.")
            => OriginalTransactionId = original;
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task OwnedDeletePreservesUncertainOriginalWithoutRetryOrPublishingDeletion(bool byKey, bool soft)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var id = Guid.NewGuid();
        var revision = Guid.NewGuid();
        var operation = Guid.NewGuid();
        var originalTransaction = "controlled-original-transaction";
        var cause = new UnknownOwnedDeleteOutcome(originalTransaction);
        var stored = new Holon { Id = id, CreatedByAvatarId = actor, Version = 7 };
        CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, revision);
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        fixture.Provider.Setup(provider => provider.LoadHolonAsync("native-key", false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        ConditionalHolonWriteRequest? submitted = null;
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCount = 2,
                ErrorCode = "COSMIC_WRITE_RECONCILIATION_REQUIRED", Exception = cause,
                Message = cause.Message, MetaData = new() { ["COSMIC.OperationId"] = operation.ToString("D") } });
        var options = new CosmicWriteOptions(operation, new Dictionary<Guid, Guid?> { [id] = revision });
        var result = byKey
            ? await fixture.Manager.DeleteOwnedHolonByProviderKeyAsync("native-key", id, actor, options, soft)
            : await fixture.Manager.DeleteOwnedHolonAsync(id, actor, options, soft);
        result.IsError.Should().BeTrue();
        result.IsDeleted.Should().BeFalse();
        result.ErrorCode.Should().Be("COSMIC_WRITE_RECONCILIATION_REQUIRED");
        result.ErrorCount.Should().Be(2);
        result.Exception.Should().BeSameAs(cause);
        result.MetaData.Should().ContainKey("COSMIC.OperationId").WhoseValue.Should().Be(operation.ToString("D"));
        NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception).Should().BeTrue();
        NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.OriginalTransactionIds(result.Exception)
            .Should().Equal(originalTransaction);
        submitted.Should().NotBeNull();
        submitted!.Write.OperationId.Should().Be(operation);
        submitted.Write.Conditions.Single().ExpectedRevisionId.Should().Be(revision);
        stored.IsDeleted.Should().BeFalse();
        stored.Version.Should().Be(7);
        CosmicConcurrency.GetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS).Should().Be(revision);
        fixture.Provider.As<IConditionalHolonStorageProvider>().Verify(provider =>
            provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
