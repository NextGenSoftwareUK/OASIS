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
    public async Task OwnedSaveValidatesWriteConditionsEvenWhenNothingChanged(bool batch)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var root = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = actor };
        root.AcceptChanges();

        fixture.Provider.Setup(provider => provider.LoadHolonAsync(
                root.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon
            {
                Id = root.Id, CreatedByAvatarId = actor
            }) { IsLoaded = true });

        var options = new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?>
        {
            [root.Id] = Guid.NewGuid(),
            [Guid.NewGuid()] = null // Outside the selected plan: invalid intent.
        });
        var rejected = batch
            ? (await fixture.Manager.SaveOwnedHolonsAsync(new[] { root }, actor, options, saveChildren: false)).IsError
            : (await fixture.Manager.SaveOwnedHolonAsync(root, actor, options, saveChildren: false)).IsError;

        rejected.Should().BeTrue("no-op input still requires valid selected write conditions");
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("Save") ||
            call.Method.Name.Contains("Conditional")).Should().BeEmpty();
    }

    [Fact]
    public async Task OwnedBatchDispatchesOneConditionalOperationWithEveryCallerRevision()
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var operation = Guid.NewGuid();
        var roots = new[]
        {
            new Holon { Id = Guid.NewGuid(), Name = "first" },
            new Holon { Id = Guid.NewGuid(), Name = "second" }
        };
        var expected = roots.ToDictionary(root => root.Id, _ => (Guid?)Guid.NewGuid());

        foreach (var root in roots)
        {
            var stored = new Holon { Id = root.Id, CreatedByAvatarId = actor };
            CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
            fixture.Provider.Setup(provider => provider.LoadHolonAsync(
                    root.Id, false, true, 0, true, false, 0))
                .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        }

        ConditionalHolonWriteRequest? submitted = null;
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(
                It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>
            {
                IsError = true, ErrorCode = "CONTROL_BATCH_CONFLICT"
            });

        var result = await fixture.Manager.SaveOwnedHolonsAsync(
            roots, actor, new CosmicWriteOptions(operation, expected), saveChildren: false);

        result.ErrorCode.Should().Be("CONTROL_BATCH_CONFLICT");
        submitted!.Write.OperationId.Should().Be(operation);
        submitted.Write.ActorId.Should().Be(actor);
        submitted.Write.Authority.Should().Be(ProviderType.SQLLiteDBOASIS);
        submitted.Write.Conditions.ToDictionary(row => row.HolonId, row => row.ExpectedRevisionId)
            .Should().BeEquivalentTo(expected);
        fixture.Provider.As<IConditionalHolonStorageProvider>().Verify(provider =>
            provider.ExecuteConditionalHolonWriteAsync(
                It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OwnedBatchEnforcesCombinedGraphLimitBeforeAnyProviderIo()
    {
        var fixture = Fixture(true, conditional: true);
        var root = new Holon { Id = Guid.NewGuid() };
        root.Children.Add(new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id });

        var result = await fixture.Manager.SaveOwnedHolonsAsync(
            new[] { root }, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()), maximumHolons: 1);

        result.IsError.Should().BeTrue();
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("Load") ||
            call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional"))
            .Should().BeEmpty();
        root.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedBatchAuthorizesEveryRootBeforeAnyMutation(bool unchangedForeignRoot)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var owned = new Holon { Id = Guid.NewGuid(), Name = "valid first edit" };
        var foreign = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = actor };

        // A forged payload creator, or a clean local baseline, must not grant
        // access. Only the stored creator at the write authority is evidence.
        if (unchangedForeignRoot)
            foreign.AcceptChanges();

        foreach (var source in new[] { owned, foreign })
        {
            var stored = new Holon
            {
                Id = source.Id,
                CreatedByAvatarId = source == owned ? actor : Guid.NewGuid()
            };

            fixture.Provider.Setup(provider => provider.LoadHolonAsync(
                    source.Id, false, true, 0, true, false, 0))
                .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
        }

        var expected = new Dictionary<Guid, Guid?>
        {
            [owned.Id] = Guid.NewGuid(),
            [foreign.Id] = Guid.NewGuid()
        };

        var result = await fixture.Manager.SaveOwnedHolonsAsync(
            new[] { owned, foreign }, actor,
            new CosmicWriteOptions(Guid.NewGuid(), expected));

        result.ErrorCode.Should().Be("COSMIC_WRITE_FORBIDDEN");
        fixture.Provider.Invocations.Where(call =>
                call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional"))
            .Should().BeEmpty("a later denied root must prevent earlier commits");
        owned.Name.Should().Be("valid first edit");
    }

    [Fact]
    public async Task OwnedBatchDoesNotTreatAnUncertainSecondLookupAsCreatePermission()
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var first = new Holon { Id = Guid.NewGuid() };
        var uncertain = new Holon { Id = Guid.NewGuid() };

        fixture.Provider.Setup(provider => provider.LoadHolonAsync(
                first.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon
            {
                Id = first.Id, CreatedByAvatarId = actor
            }) { IsLoaded = true });

        fixture.Provider.Setup(provider => provider.LoadHolonAsync(
                uncertain.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true, ErrorCode = "CONTROL_AUTHORITY_UNAVAILABLE"
            });

        var result = await fixture.Manager.SaveOwnedHolonsAsync(
            new[] { first, uncertain }, actor,
            new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?>
            {
                [first.Id] = Guid.NewGuid(), [uncertain.Id] = null
            }));

        result.ErrorCode.Should().Be("COSMIC_WRITE_OWNERSHIP_LOOKUP_FAILED");
        fixture.Provider.Invocations.Where(call =>
                call.Method.Name.Contains("Save") || call.Method.Name.Contains("Conditional"))
            .Should().BeEmpty();
    }
}
