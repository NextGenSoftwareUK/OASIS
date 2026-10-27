using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory]
    [InlineData(false, false, false)][InlineData(false, true, false)]
    [InlineData(true, false, false)][InlineData(true, true, false)]
    [InlineData(false, false, true)][InlineData(false, true, true)]
    [InlineData(true, false, true)][InlineData(true, true, true)]
    public async Task DeletePreparationPrecedesDispatchAndSurvivesAnUncertainOutcome(bool synchronous, bool soft, bool protect)
    {
        var f = Fixture(true, conditional: true, preparation: true, recovery: true, preparationRecovery: true);
        var actor = Guid.NewGuid(); var operation = Guid.NewGuid(); var revision = Guid.NewGuid();
        var source = new Holon { Id = Guid.NewGuid(), Name = "private delete original", Version = 7, VersionId = Guid.NewGuid() };
        source.Children.Add(new Holon { Id = Guid.NewGuid(), Name = "unselected child" });
        CosmicConcurrency.SetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS, revision);
        HolonChangeTracking.AcceptChanges(source);
        var policy = MetadataPolicy(new MetadataResolver());
        if (protect) f.Manager.PreparationEncryptionPolicy = policy;
        HolonWritePreparation? stored = null;
        OASISResult<HolonWritePreparation> Claim(HolonWritePreparation value) { stored = value; return new(value); }
        f.Provider.As<IHolonWritePreparationStorageProvider>()
            .Setup(provider => provider.ClaimHolonWritePreparation(It.IsAny<HolonWritePreparation>()))
            .Returns((HolonWritePreparation value) => Claim(value));
        f.Provider.As<IHolonWritePreparationStorageProvider>()
            .Setup(provider => provider.ClaimHolonWritePreparationAsync(It.IsAny<HolonWritePreparation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HolonWritePreparation value, CancellationToken _) => Claim(value));
        OASISResult<IEnumerable<IHolon>> Uncertain(ConditionalHolonWriteRequest request)
        {
            (stored != null).Should().Be(protect, "protection must be durable before mutation dispatch");
            request.Operation.Should().Be(soft ? ConditionalHolonOperation.SoftDelete : ConditionalHolonOperation.HardDelete);
            request.Roots.Single().Children.Should().BeEmpty();
            return new() { IsError = true, ErrorCode = "COSMIC_WRITE_RECONCILIATION_REQUIRED", Exception = new IOException("lost acknowledgement") };
        }
        f.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWrite(It.IsAny<ConditionalHolonWriteRequest>()))
            .Returns((ConditionalHolonWriteRequest request) => Uncertain(request));
        f.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConditionalHolonWriteRequest request, CancellationToken _) => Uncertain(request));
        var options = new CosmicWriteOptions(operation, new Dictionary<Guid, Guid?> { [source.Id] = revision });
        var deleted = synchronous ? f.Manager.DeleteHolon(source, actor, options, soft, ProviderType.SQLLiteDBOASIS)
            : await f.Manager.DeleteHolonAsync(source, actor, options, soft, ProviderType.SQLLiteDBOASIS);
        deleted.ErrorCode.Should().Be("COSMIC_WRITE_RECONCILIATION_REQUIRED");
        source.IsDeleted.Should().BeFalse(); source.Version.Should().Be(7);
        source.Children.Single().Name.Should().Be("unselected child");
        if (!protect) return;
        stored.Should().NotBeNull(); stored!.Records.Should().ContainSingle();
        stored.Records.Single().Envelope.Should().NotContain("private delete original");
        f.Provider.As<IHolonWritePreparationRecoveryStorageProvider>()
            .Setup(provider => provider.ReadHolonWritePreparationAsync(operation, actor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<HolonWritePreparation>(stored) { IsLoaded = true });
        ConditionalHolonWriteRequest? recovered = null;
        f.Provider.As<IConditionalHolonWriteRecoveryStorageProvider>()
            .Setup(provider => provider.ReadConditionalHolonWriteOutcomeAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => recovered = request)
            .ReturnsAsync(new OASISResult<ConditionalHolonWriteOutcome>(new ConditionalHolonWriteOutcome(operation, actor,
                ProviderType.SQLLiteDBOASIS, soft ? ConditionalHolonOperation.SoftDelete : ConditionalHolonOperation.HardDelete,
                ConditionalHolonReceiptState.Committed)) { IsLoaded = true });
        var status = await f.Manager.RecoverConditionalHolonWriteOutcomeAsync(operation, actor, ProviderType.SQLLiteDBOASIS,
            _ => policy, (name, assembly) => name == typeof(Holon).FullName && assembly == typeof(Holon).Assembly.GetName().Name ? typeof(Holon) : null!);
        status.IsError.Should().BeFalse(status.Message + status.Exception);
        recovered!.Roots.Single().Name.Should().Be("private delete original");
        recovered.Write.Conditions.Single().ExpectedRevisionId.Should().Be(revision);
        var mutationName = synchronous ? "ExecuteConditionalHolonWrite" : "ExecuteConditionalHolonWriteAsync";
        f.Provider.Invocations.Count(call => call.Method.Name == mutationName).Should().Be(1);
    }

    [Theory][InlineData(false, false)][InlineData(false, true)][InlineData(true, false)][InlineData(true, true)]
    public async Task ProtectedDeleteRejectsMissingPreparationCapabilityBeforeMutation(bool synchronous, bool soft)
    {
        var f = Fixture(true, conditional: true);
        f.Manager.PreparationEncryptionPolicy = MetadataPolicy(new MetadataResolver());
        var source = new Holon { Id = Guid.NewGuid() };
        var options = new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [source.Id] = Guid.NewGuid() });
        var result = synchronous ? f.Manager.DeleteHolon(source, Guid.NewGuid(), options, soft, ProviderType.SQLLiteDBOASIS)
            : await f.Manager.DeleteHolonAsync(source, Guid.NewGuid(), options, soft, ProviderType.SQLLiteDBOASIS);
        result.ErrorCode.Should().Be("COSMIC_PREPARATION_UNSUPPORTED");
        source.IsDeleted.Should().BeFalse();
        f.Provider.Invocations.Where(call => call.Method.Name.Contains("ExecuteConditional")).Should().BeEmpty();
    }

    [Theory][InlineData(false, false)][InlineData(false, true)][InlineData(true, false)][InlineData(true, true)]
    public async Task DeleteCannotReuseAnOperationSealedAsSave(bool synchronous, bool soft)
    {
        var f = Fixture(true, conditional: true, preparation: true);
        var actor = Guid.NewGuid(); var operation = Guid.NewGuid(); var revision = Guid.NewGuid();
        var source = new Holon { Id = Guid.NewGuid(), Name = "same value" };
        CosmicConcurrency.SetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS, revision);
        HolonChangeTracking.AcceptChanges(source);
        var policy = MetadataPolicy(new MetadataResolver()); f.Manager.PreparationEncryptionPolicy = policy;
        var write = new ConditionalHolonWrite(operation, actor, ProviderType.SQLLiteDBOASIS, new[] { new HolonWriteCondition(source.Id, revision) });
        var originalSave = new HolonConditionalPreparation(new[] { source }, write, false, false, 0, _ => policy).Seal().Candidate;
        f.Provider.As<IHolonWritePreparationStorageProvider>()
            .Setup(provider => provider.ClaimHolonWritePreparation(It.IsAny<HolonWritePreparation>())).Returns(new OASISResult<HolonWritePreparation>(originalSave));
        f.Provider.As<IHolonWritePreparationStorageProvider>()
            .Setup(provider => provider.ClaimHolonWritePreparationAsync(It.IsAny<HolonWritePreparation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<HolonWritePreparation>(originalSave));
        var options = new CosmicWriteOptions(operation, new Dictionary<Guid, Guid?> { [source.Id] = revision });
        var result = synchronous ? f.Manager.DeleteHolon(source, actor, options, soft, ProviderType.SQLLiteDBOASIS)
            : await f.Manager.DeleteHolonAsync(source, actor, options, soft, ProviderType.SQLLiteDBOASIS);
        result.ErrorCode.Should().Be("COSMIC_OPERATION_ID_REUSED");
        f.Provider.Invocations.Where(call => call.Method.Name.Contains("ExecuteConditional")).Should().BeEmpty();
    }
}
