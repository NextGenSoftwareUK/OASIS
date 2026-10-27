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
    [InlineData(ConditionalHolonOperation.Save)]
    [InlineData(ConditionalHolonOperation.SoftDelete)]
    [InlineData(ConditionalHolonOperation.HardDelete)]
    public async Task RecoveryRoutesCapturedOriginalIntentWithoutExecutingMutation(ConditionalHolonOperation operation)
    {
        var fixture = Fixture(true, conditional: true, recovery: true);
        var actor = Guid.NewGuid();
        var source = new Holon { Id = Guid.NewGuid(), Name = "original private value" };
        var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.SQLLiteDBOASIS,
            new[] { new HolonWriteCondition(source.Id, Guid.NewGuid()) }), operation, new[] { source }, saveChildren: false);
        ConditionalHolonWriteRequest? observed = null;
        fixture.Provider.As<IConditionalHolonWriteRecoveryStorageProvider>()
            .Setup(provider => provider.ReadConditionalHolonWriteOutcomeAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((original, _) =>
            {
                observed = original;
                source.Name = "changed after capture";
            })
            .ReturnsAsync(new OASISResult<ConditionalHolonWriteOutcome>(new ConditionalHolonWriteOutcome(
                request.Write.OperationId, actor, ProviderType.SQLLiteDBOASIS, operation, ConditionalHolonReceiptState.Committed)) { IsLoaded = true });
        var result = await fixture.Manager.ReadConditionalHolonWriteOutcomeAsync(request, actor);
        result.IsError.Should().BeFalse(result.Message);
        result.IsLoaded.Should().BeTrue();
        result.Result.OperationId.Should().Be(request.Write.OperationId);
        observed.Should().NotBeNull();
        observed!.Roots.Single().Should().NotBeSameAs(source);
        observed.Roots.Single().Name.Should().Be("original private value");
        observed.Write.Should().BeSameAs(request.Write);
        source.Version.Should().Be(0);
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("ExecuteConditional") || call.Method.Name.Contains("DeleteHolon")
            || call.Method.Name.Contains("SaveHolon")).Should().BeEmpty();
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("legacy")]
    [InlineData("binding")]
    [InlineData("unsupported")]
    public async Task RecoveryRejectsInvalidActorCapabilityOrReturnedBinding(string mode)
    {
        var fixture = Fixture(mode != "legacy", conditional: true, recovery: mode != "unsupported");
        var actor = Guid.NewGuid();
        var source = new Holon { Id = Guid.NewGuid() };
        var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.SQLLiteDBOASIS,
            new[] { new HolonWriteCondition(source.Id, Guid.NewGuid()) }), ConditionalHolonOperation.HardDelete,
            new[] { source }, saveChildren: false);
        if (mode == "binding") fixture.Provider.As<IConditionalHolonWriteRecoveryStorageProvider>()
            .Setup(provider => provider.ReadConditionalHolonWriteOutcomeAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<ConditionalHolonWriteOutcome>(new ConditionalHolonWriteOutcome(
                Guid.NewGuid(), actor, ProviderType.SQLLiteDBOASIS, request.Operation, ConditionalHolonReceiptState.Committed)));
        fixture.Provider.Invocations.Clear();
        var result = await fixture.Manager.ReadConditionalHolonWriteOutcomeAsync(request, mode == "actor" ? Guid.NewGuid() : actor);
        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.ErrorCode.Should().Be(mode == "binding" ? "COSMIC_OPERATION_STATUS_BINDING_INVALID"
            : mode == "unsupported" ? "COSMIC_OPERATION_STATUS_UNSUPPORTED" : "COSMIC_OPERATION_STATUS_INTENT_INVALID");
        if (mode is "actor" or "legacy") fixture.Provider.Invocations.Should().BeEmpty();
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("ExecuteConditional") || call.Method.Name.Contains("DeleteHolon")
            || call.Method.Name.Contains("SaveHolon")).Should().BeEmpty();
    }
}
