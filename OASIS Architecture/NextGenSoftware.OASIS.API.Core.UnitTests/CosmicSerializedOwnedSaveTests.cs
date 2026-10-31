using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task SerializedMembershipUsesTheSameConditionalAdmissionInEverySdkSave(int route)
    {
        var fixture = Fixture(true, conditional: true);
        var root = new Holon { Id = Guid.NewGuid(), Name = "serialized SDK edit" };
        root.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,
            root.Id, new[] { new HolonChildRole("Children", Array.Empty<Guid>()) }));
        var revision = Guid.NewGuid();
        var options = new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [root.Id] = revision });
        var actor = Guid.NewGuid();
        ConditionalHolonWriteRequest? submitted = null;
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWrite(It.IsAny<ConditionalHolonWriteRequest>()))
            .Callback<ConditionalHolonWriteRequest>(request => submitted = request)
            .Returns(new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCode = CosmicConcurrencyException.Code });
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCode = CosmicConcurrencyException.Code });

        var error = route switch
        {
            0 => fixture.Manager.SaveHolon(root, actor, options).ErrorCode,
            1 => (await fixture.Manager.SaveHolonAsync(root, actor, options)).ErrorCode,
            2 => fixture.Manager.SaveHolons(new[] { root }, actor, options).ErrorCode,
            _ => (await fixture.Manager.SaveHolonsAsync(new[] { root }, actor, options)).ErrorCode
        };

        error.Should().Be(CosmicConcurrencyException.Code);
        submitted!.Write.Conditions.Single().ExpectedRevisionId.Should().Be(revision);
        Action ordinary = () => HolonChangeTracking.ValidateChildSelectionForSave(root);
        ordinary.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task CompleteSerializedOwnedGraphReachesOwnershipAndOriginalConditionalRevision(bool batch, bool foreign)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var root = new Holon { Id = Guid.NewGuid(), Name = "serialized edit" };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id };
        root.Children.Add(child);
        root.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,
            root.Id, new[] { new HolonChildRole("Children", new[] { child.Id }) }));
        child.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,
            child.Id, new[] { new HolonChildRole("Children", Array.Empty<Guid>()) }));
        // These instances intentionally have no process-local hydration authority.
        var expected = new Dictionary<Guid, Guid?> { [root.Id] = Guid.NewGuid(), [child.Id] = Guid.NewGuid() };
        foreach (var id in expected.Keys)
            fixture.Provider.Setup(provider => provider.LoadHolonAsync(id, false, true, 0, true, false, 0))
                .ReturnsAsync(new OASISResult<IHolon>(new Holon
                {
                    Id = id, CreatedByAvatarId = foreign && id == child.Id ? Guid.NewGuid() : actor
                }) { IsLoaded = true });

        ConditionalHolonWriteRequest? submitted = null;
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCode = CosmicConcurrencyException.Code });

        var options = new CosmicWriteOptions(Guid.NewGuid(), expected);
        var error = batch
            ? (await fixture.Manager.SaveOwnedHolonsAsync(new[] { root }, actor, options)).ErrorCode
            : (await fixture.Manager.SaveOwnedHolonAsync(root, actor, options)).ErrorCode;

        error.Should().Be(foreign ? "COSMIC_WRITE_FORBIDDEN" : CosmicConcurrencyException.Code);
        if (foreign) submitted.Should().BeNull();
        else submitted!.Write.Conditions.Should().OnlyContain(condition => condition.ExpectedRevisionId == expected[condition.HolonId]);
        // Conditional admission never grants ordinary replacement authority.
        Action ordinary = () => HolonChangeTracking.ValidateChildSelectionForSave(root);
        ordinary.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SerializedConditionalGraphCannotReplaceAnUnmaterializedOrKnownPartialSelection(bool knownPartial)
    {
        var fixture = Fixture(true, conditional: true);
        var root = new Holon { Id = Guid.NewGuid() };
        root.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,
            root.Id, new[] { new HolonChildRole("Children", new[] { Guid.NewGuid() }) }));
        if (knownPartial) HolonChangeTracking.MarkIncompleteChildSelection(root);
        fixture.Provider.Invocations.Clear();

        var result = await fixture.Manager.SaveOwnedHolonAsync(root, Guid.NewGuid(),
            new CosmicWriteOptions(Guid.NewGuid(), new Dictionary<Guid, Guid?> { [root.Id] = Guid.NewGuid() }));

        result.IsError.Should().BeTrue();
        fixture.Provider.Invocations.Should().BeEmpty();
    }
}
