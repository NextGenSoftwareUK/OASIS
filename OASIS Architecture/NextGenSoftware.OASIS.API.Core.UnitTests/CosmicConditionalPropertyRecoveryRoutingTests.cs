using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

/// <summary>
/// Provider-boundary contract for the V2 operation "ReadConditionalHolonPropertyWriteOutcome":
/// HolonManager.ReadConditionalHolonPropertyWriteOutcomeAsync must route the captured original
/// selection-bound intent to the authority's IConditionalHolonPropertyStorageProvider, never execute
/// or replay a mutation, reject a different actor before provider I/O, and reject a returned outcome
/// whose binding does not match the original operation.
/// </summary>
public partial class CosmicTrackingTests
{
    private static ConditionalHolonPropertyWriteRequest PropertyRecoveryRequest(Guid actor, Holon source) =>
        new ConditionalHolonPropertyWriteRequest(
            new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.SQLLiteDBOASIS,
                new[] { new HolonWriteCondition(source.Id, Guid.NewGuid()) }), ConditionalHolonOperation.Save, new[] { source }, saveChildren: false),
            new Dictionary<Guid, IReadOnlyList<string>> { [source.Id] = new[] { "/Name" } });

    [Fact]
    public async Task PropertyRecoveryRoutesTheOriginalSelectionWithoutExecutingMutation()
    {
        var fixture = Fixture(true, conditional: true, property: true);
        var actor = Guid.NewGuid();
        var source = new Holon { Id = Guid.NewGuid(), Name = "original private value" };
        var request = PropertyRecoveryRequest(actor, source);
        ConditionalHolonPropertyWriteRequest? observed = null;
        fixture.Provider.As<IConditionalHolonPropertyStorageProvider>()
            .Setup(provider => provider.ReadConditionalHolonPropertyWriteOutcomeAsync(It.IsAny<ConditionalHolonPropertyWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonPropertyWriteRequest, CancellationToken>((original, _) => observed = original)
            .ReturnsAsync(new OASISResult<ConditionalHolonWriteOutcome>(new ConditionalHolonWriteOutcome(
                request.WriteRequest.Write.OperationId, actor, ProviderType.SQLLiteDBOASIS, ConditionalHolonOperation.Save,
                ConditionalHolonReceiptState.Committed)) { IsLoaded = true });

        var result = await fixture.Manager.ReadConditionalHolonPropertyWriteOutcomeAsync(request, actor);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.OperationId.Should().Be(request.WriteRequest.Write.OperationId);
        observed.Should().NotBeNull();
        observed!.WriteRequest.Write.Should().BeSameAs(request.WriteRequest.Write);
        observed.Selections[source.Id].Should().Equal("/Name");
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("ExecuteConditional") || call.Method.Name.Contains("DeleteHolon")
            || call.Method.Name.Contains("SaveHolon")).Should().BeEmpty();
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("binding")]
    public async Task PropertyRecoveryRejectsAnotherActorOrAMismatchedOutcome(string mode)
    {
        var fixture = Fixture(true, conditional: true, property: true);
        var actor = Guid.NewGuid();
        var source = new Holon { Id = Guid.NewGuid() };
        var request = PropertyRecoveryRequest(actor, source);
        fixture.Provider.As<IConditionalHolonPropertyStorageProvider>()
            .Setup(provider => provider.ReadConditionalHolonPropertyWriteOutcomeAsync(It.IsAny<ConditionalHolonPropertyWriteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<ConditionalHolonWriteOutcome>(new ConditionalHolonWriteOutcome(
                Guid.NewGuid(), actor, ProviderType.SQLLiteDBOASIS, ConditionalHolonOperation.Save, ConditionalHolonReceiptState.Committed)));
        fixture.Provider.Invocations.Clear();

        var result = await fixture.Manager.ReadConditionalHolonPropertyWriteOutcomeAsync(request, mode == "actor" ? Guid.NewGuid() : actor);

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.ErrorCode.Should().Be(mode == "binding" ? "COSMIC_OPERATION_STATUS_BINDING_INVALID" : "COSMIC_OPERATION_STATUS_INTENT_INVALID");
        if (mode == "actor") fixture.Provider.Invocations.Should().BeEmpty();
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("ExecuteConditional") || call.Method.Name.Contains("DeleteHolon")
            || call.Method.Name.Contains("SaveHolon")).Should().BeEmpty();
    }
}
