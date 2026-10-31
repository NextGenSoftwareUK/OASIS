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
    [InlineData(false, "actor")][InlineData(true, "actor")]
    [InlineData(false, "authority")][InlineData(true, "authority")]
    [InlineData(false, "mutation")][InlineData(true, "mutation")]
    [InlineData(false, "unpinned")][InlineData(true, "unpinned")]
    [InlineData(false, "replicate")][InlineData(true, "replicate")]
    [InlineData(false, "payload")][InlineData(true, "payload")]
    [InlineData(false, "valid")][InlineData(true, "valid")]
    public async Task DirectPreparationLookupCannotEnrollMutationOrChangeAuthority(bool synchronous, string mode)
    {
        var f = Fixture(true, preparationRecovery: true);
        var lookup = new HolonWritePreparationLookup(Guid.NewGuid(), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS);
        var request = new StorageOperationRequest
        {
            Operation = "ReadHolonWritePreparation", Payload = mode == "payload" ? null! : lookup,
            AvatarId = mode == "actor" ? Guid.NewGuid() : lookup.ActorId,
            PreferredProvider = mode == "authority" ? ProviderType.MongoDBOASIS : lookup.Authority,
            RequirePreferredProvider = mode != "unpinned", AutoReplicate = mode == "replicate"
        };
        if (mode == "mutation") request.ConditionalWrite = new ConditionalHolonWrite(lookup.OperationId, lookup.ActorId,
            lookup.Authority, new[] { new HolonWriteCondition(Guid.NewGuid(), null) });
        f.Provider.As<IHolonWritePreparationRecoveryStorageProvider>()
            .Setup(provider => provider.ReadHolonWritePreparationAsync(lookup.OperationId, lookup.ActorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<HolonWritePreparation> { IsLoaded = true });
        f.Provider.Invocations.Clear();
        var router = new Managers.OASISHyperDrive.OASISHyperDrive(f.Providers);
        var result = synchronous ? router.RouteRequest<HolonWritePreparation>(request)
            : await router.RouteRequestAsync<HolonWritePreparation>(request);
        result.IsError.Should().Be(mode != "valid", result.Message);
        if (mode != "valid")
        {
            result.ErrorCode.Should().Be("COSMIC_PREPARATION_LOOKUP_INVALID");
            f.Provider.Invocations.Should().BeEmpty();
        }
        else f.Provider.As<IHolonWritePreparationRecoveryStorageProvider>().Verify(provider =>
            provider.ReadHolonWritePreparationAsync(lookup.OperationId, lookup.ActorId, It.IsAny<CancellationToken>()), Times.Once);
    }
    [Theory]
    [InlineData(ConditionalHolonOperation.Save)]
    [InlineData(ConditionalHolonOperation.SoftDelete)]
    [InlineData(ConditionalHolonOperation.HardDelete)]
    public async Task StoredOnlyRecoveryRoutesAuthenticatedOriginalWithoutReExecuting(ConditionalHolonOperation kind)
    {
        var f = Fixture(true, conditional: true, recovery: true, preparationRecovery: true);
        var actor = Guid.NewGuid();
        var root = new Holon { Id = Guid.NewGuid(), Name = "original private value" };
        var write = new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.SQLLiteDBOASIS,
            new[] { new HolonWriteCondition(root.Id, kind == ConditionalHolonOperation.Save ? null : Guid.NewGuid()) });
        var policy = MetadataPolicy(new MetadataResolver());
        var stored = new HolonConditionalPreparation(new[] { root }, write, false, false, 0, _ => policy, kind).Seal().Candidate;
        f.Provider.As<IHolonWritePreparationRecoveryStorageProvider>()
            .Setup(provider => provider.ReadHolonWritePreparationAsync(write.OperationId, actor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<HolonWritePreparation>(stored) { IsLoaded = true });
        ConditionalHolonWriteRequest? observed = null;
        f.Provider.As<IConditionalHolonWriteRecoveryStorageProvider>()
            .Setup(provider => provider.ReadConditionalHolonWriteOutcomeAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => observed = request)
            .ReturnsAsync(new OASISResult<ConditionalHolonWriteOutcome>(new ConditionalHolonWriteOutcome(write.OperationId, actor,
                write.Authority, kind, ConditionalHolonReceiptState.Committed)) { IsLoaded = true });
        root.Name = "caller changed after original operation";
        var result = await f.Manager.RecoverConditionalHolonWriteOutcomeAsync(write.OperationId, actor, write.Authority,
            _ => policy, (name, assembly) => name == typeof(Holon).FullName && assembly == typeof(Holon).Assembly.GetName().Name ? typeof(Holon) : null!);
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.ReceiptState.Should().Be(ConditionalHolonReceiptState.Committed);
        observed.Should().NotBeNull();
        observed!.Roots.Single().Name.Should().Be("original private value");
        observed.Roots.Single().Should().NotBeSameAs(root);
        observed.Operation.Should().Be(kind);
        observed.Write.Conditions.Single().ExpectedRevisionId.Should().Be(write.Conditions.Single().ExpectedRevisionId);
        f.Provider.As<IHolonWritePreparationRecoveryStorageProvider>().Verify(
            provider => provider.ReadHolonWritePreparationAsync(write.OperationId, actor, It.IsAny<CancellationToken>()), Times.Once);
        f.Provider.Invocations.Where(call => call.Method.Name.Contains("ClaimHolon") || call.Method.Name.Contains("ExecuteConditional")
            || call.Method.Name.Contains("SaveHolon") || call.Method.Name.Contains("DeleteHolon")).Should().BeEmpty();
    }

    [Theory]
    [InlineData("operation")][InlineData("actor")][InlineData("default")][InlineData("legacy")]
    [InlineData("unsupported")][InlineData("binding-operation")][InlineData("binding-actor")][InlineData("binding-authority")]
    [InlineData("absent")]
    public async Task LookupRejectsInvalidBindingAndNeverClaims(string mode)
    {
        var f = Fixture(mode != "legacy", preparationRecovery: mode != "unsupported");
        var operation = Guid.NewGuid(); var actor = Guid.NewGuid();
        var authority = ProviderType.SQLLiteDBOASIS;
        if (mode.StartsWith("binding", StringComparison.Ordinal))
        {
            var stored = new HolonWritePreparation(mode == "binding-operation" ? Guid.NewGuid() : operation,
                mode == "binding-actor" ? Guid.NewGuid() : actor,
                mode == "binding-authority" ? ProviderType.MongoDBOASIS : authority,
                new[] { new ProtectedHolonPreparation(Guid.NewGuid(), "oasis-preparation1:opaque") });
            f.Provider.As<IHolonWritePreparationRecoveryStorageProvider>()
                .Setup(provider => provider.ReadHolonWritePreparationAsync(operation, actor, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OASISResult<HolonWritePreparation>(stored) { IsLoaded = true });
        }
        if (mode == "absent") f.Provider.As<IHolonWritePreparationRecoveryStorageProvider>()
            .Setup(provider => provider.ReadHolonWritePreparationAsync(operation, actor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<HolonWritePreparation> { IsLoaded = true });
        f.Provider.Invocations.Clear();
        var result = await f.Manager.ReadHolonWritePreparationAsync(mode == "operation" ? Guid.Empty : operation,
            mode == "actor" ? Guid.Empty : actor, mode == "default" ? ProviderType.Default : authority);
        result.IsError.Should().Be(mode != "absent");
        result.Result.Should().BeNull();
        if (mode.StartsWith("binding", StringComparison.Ordinal)) result.ErrorCode.Should().Be("COSMIC_PREPARATION_BINDING_INVALID");
        if (mode == "unsupported") result.ErrorCode.Should().Be("COSMIC_PREPARATION_LOOKUP_UNSUPPORTED");
        if (mode is "operation" or "actor" or "default" or "legacy") f.Provider.Invocations.Should().BeEmpty();
        f.Provider.Invocations.Where(call => call.Method.Name.Contains("ClaimHolon") || call.Method.Name.Contains("ExecuteConditional")
            || call.Method.Name.Contains("SaveHolon") || call.Method.Name.Contains("DeleteHolon")).Should().BeEmpty();
    }

    [Theory][InlineData("missing-policy")][InlineData("missing-intent")][InlineData("denied-type")]
    public async Task StoredOnlyRecoveryCannotInferOutcomeWithoutAuthorizedIntent(string mode)
    {
        var f = Fixture(true, recovery: true, preparationRecovery: true);
        var actor = Guid.NewGuid(); var root = new Holon { Id = Guid.NewGuid() };
        var write = new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.SQLLiteDBOASIS,
            new[] { new HolonWriteCondition(root.Id, null) });
        var policy = MetadataPolicy(new MetadataResolver());
        var stored = new HolonConditionalPreparation(new[] { root }, write, false, false, 0, _ => policy).Seal().Candidate;
        f.Provider.As<IHolonWritePreparationRecoveryStorageProvider>()
            .Setup(provider => provider.ReadHolonWritePreparationAsync(write.OperationId, actor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<HolonWritePreparation>(mode == "missing-intent" ? null! : stored) { IsLoaded = true });
        var result = await f.Manager.RecoverConditionalHolonWriteOutcomeAsync(write.OperationId, actor, write.Authority,
            mode == "missing-policy" ? null! : _ => policy, (_, _) => null!);
        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.ErrorCode.Should().Be(mode == "missing-policy" ? "COSMIC_PREPARATION_POLICY_REQUIRED"
            : mode == "missing-intent" ? "COSMIC_PREPARATION_NOT_FOUND" : "COSMIC_PREPARATION_INVALID");
        f.Provider.As<IConditionalHolonWriteRecoveryStorageProvider>().Verify(provider =>
            provider.ReadConditionalHolonWriteOutcomeAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
