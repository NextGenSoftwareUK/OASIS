using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("11111111-1111-1111-1111-111111111111,11111111-1111-1111-1111-111111111111")]
    public async Task HostedWriteRejectsInvalidOrCoalescedOperationHeadersBeforeProviderIo(string header)
    {
        var fixture = Fixture(true, conditional: true);
        var result = await HolonWriteEndpoint.SaveAsync(fixture.Manager,
            new SaveHolonRequest { Holon = new Holon() }, Guid.NewGuid(), false, header);

        result.ErrorCode.Should().Be("COSMIC_WRITE_INTENT_INVALID");
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("Load") ||
            call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional")).Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HostedAliasesAcceptOneOperationHeaderAndRejectContradictoryBodyIntent(bool mismatch)
    {
        var fixture = Fixture(true, conditional: true);
        var operation = Guid.NewGuid();
        ConditionalHolonWriteRequest? submitted = null;
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(
                It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>
            {
                IsError = true, ErrorCode = "CONTROL_HEADER_INTENT"
            });

        var result = await HolonWriteEndpoint.SaveAsync(fixture.Manager, new SaveHolonRequest
        {
            Holon = new Holon(), SaveChildren = false,
            OperationId = mismatch ? Guid.NewGuid() : null
        }, Guid.NewGuid(), false, operation.ToString("D"));

        if (mismatch)
        {
            result.ErrorCode.Should().Be("COSMIC_WRITE_INTENT_INVALID");
            submitted.Should().BeNull();
        }
        else
        {
            result.ErrorCode.Should().Be("CONTROL_HEADER_INTENT");
            submitted!.Write.OperationId.Should().Be(operation);
        }
    }

    [Fact]
    public async Task HostedBatchRejectsForeignLaterRootBeforeAnyWrite()
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var first = new Holon { Id = Guid.NewGuid() };
        var foreign = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = actor };
        foreach (var source in new[] { first, foreign })
        {
            fixture.Provider.Setup(provider => provider.LoadHolonAsync(
                    source.Id, false, true, 0, true, false, 0))
                .ReturnsAsync(new OASISResult<IHolon>(new Holon
                {
                    Id = source.Id, CreatedByAvatarId = source == first ? actor : Guid.NewGuid()
                }) { IsLoaded = true });
        }

        var result = await HolonWriteEndpoint.SaveBatchAsync(fixture.Manager, new SaveHolonsRequest
        {
            Holons = new() { first, foreign }, OperationId = Guid.NewGuid(), SaveChildren = false,
            ExpectedRevisions = new() { [first.Id] = Guid.NewGuid(), [foreign.Id] = Guid.NewGuid() }
        }, actor, false);

        result.ErrorCode.Should().Be("COSMIC_WRITE_FORBIDDEN");
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("Save") ||
            call.Method.Name.Contains("Conditional")).Should().BeEmpty();
    }

    [Fact]
    public void HostedWriteIntentRoundTripsCreateOnlyAndLegacyRevisionWithoutAmbiguity()
    {
        var created = Guid.NewGuid();
        var legacy = Guid.NewGuid();
        var request = new SaveHolonsRequest
        {
            OperationId = Guid.NewGuid(), SaveChildren = false, MaxChildDepth = 3,
            ExpectedRevisions = new() { [created] = null, [legacy] = Guid.Empty }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var restored = System.Text.Json.JsonSerializer.Deserialize<SaveHolonsRequest>(json)!;

        restored.OperationId.Should().Be(request.OperationId);
        restored.ExpectedRevisions[created].Should().BeNull();
        restored.ExpectedRevisions[legacy].Should().Be(Guid.Empty);
        restored.SaveChildren.Should().BeFalse();
        restored.MaxChildDepth.Should().Be(3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HostedWritesRequireStableIntentBeforeProviderIo(bool batch)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var root = new Holon { Id = Guid.NewGuid() };

        var code = batch
            ? (await HolonWriteEndpoint.SaveBatchAsync(fixture.Manager,
                new SaveHolonsRequest { Holons = new() { root } }, actor, false)).ErrorCode
            : (await HolonWriteEndpoint.SaveAsync(fixture.Manager,
                new SaveHolonRequest { Holon = root }, actor, false)).ErrorCode;

        code.Should().Be("COSMIC_WRITE_INTENT_INVALID");
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("Load") ||
            call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional"))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task HostedSingleWriteCannotForgeSelectedChildOwnership()
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var root = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = actor };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, CreatedByAvatarId = actor };
        root.Children.Add(child);

        foreach (var source in new[] { root, child })
        {
            fixture.Provider.Setup(provider => provider.LoadHolonAsync(
                    source.Id, false, true, 0, true, false, 0))
                .ReturnsAsync(new OASISResult<IHolon>(new Holon
                {
                    Id = source.Id, CreatedByAvatarId = source == root ? actor : Guid.NewGuid()
                }) { IsLoaded = true });
        }

        var result = await HolonWriteEndpoint.SaveAsync(fixture.Manager, new SaveHolonRequest
        {
            Holon = root, OperationId = Guid.NewGuid(),
            ExpectedRevisions = new() { [root.Id] = Guid.NewGuid(), [child.Id] = Guid.NewGuid() }
        }, actor, false);

        result.ErrorCode.Should().Be("COSMIC_WRITE_FORBIDDEN");
        HolonWriteEndpoint.Status(result).Should().Be(System.Net.HttpStatusCode.Forbidden);
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("Save") ||
            call.Method.Name.Contains("Conditional")).Should().BeEmpty();
    }

    [Fact]
    public async Task HostedWizardRetainsTrustedPolicyButUsesCallerConditionalIntent()
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var root = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid() };
        var revision = Guid.NewGuid();
        var operation = Guid.NewGuid();
        ConditionalHolonWriteRequest? submitted = null;

        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(
                It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>
            {
                IsError = true, ErrorCode = CosmicConcurrencyException.Code
            });

        var result = await HolonWriteEndpoint.SaveAsync(fixture.Manager, new SaveHolonRequest
        {
            Holon = root, OperationId = operation, SaveChildren = false,
            ExpectedRevisions = new() { [root.Id] = revision }
        }, actor, true);

        result.ErrorCode.Should().Be(CosmicConcurrencyException.Code);
        HolonWriteEndpoint.Status(result).Should().Be(System.Net.HttpStatusCode.Conflict);
        submitted!.Write.ActorId.Should().Be(actor);
        submitted.Write.OperationId.Should().Be(operation);
        submitted.Write.Conditions.Single().ExpectedRevisionId.Should().Be(revision);
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("Load")).Should().BeEmpty();
    }
}
