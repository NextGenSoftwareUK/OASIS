using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory]
    [InlineData("COSMIC_CONCURRENCY_CONFLICT")]
    [InlineData("COSMIC_WRITE_OUTCOME_UNKNOWN")]
    public async Task SharingPreservesConditionalFailureWithoutOrdinaryWrite(string code)
    {
        var fixture = Fixture(true, conditional: true);
        var owner = Guid.NewGuid();
        var holon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = owner };
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(holon.Id, It.IsAny<bool>(), true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(holon) { IsLoaded = true });
        // The accepted provider token is deliberately distinct from VersionId.
        CosmicConcurrency.SetProviderRevisionId(holon, Core.Enums.ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true, ErrorCode = code, Message = "controlled rejection" });
        var actor = new Mock<IAvatar>();
        actor.SetupGet(avatar => avatar.Id).Returns(owner);

        var result = await HolonAccess.ShareAsync(fixture.Manager, holon.Id, new[] { Guid.NewGuid() }, actor.Object);

        result.IsError.Should().BeTrue();
        result.Result.Should().BeFalse();
        result.ErrorCode.Should().Be(code);
        fixture.Provider.As<IConditionalHolonStorageProvider>().Verify(provider =>
            provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        fixture.Provider.Invocations.Where(invocation => invocation.Method.Name is "SaveHolon" or "SaveHolonAsync").Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharingRejectsInconclusiveOrDifferentRecordBeforeSaving(bool wrongIdentity)
    {
        var fixture = Fixture(true);
        var owner = Guid.NewGuid();
        var requested = Guid.NewGuid();
        var holon = new Holon { Id = wrongIdentity ? Guid.NewGuid() : requested, CreatedByAvatarId = owner };
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(requested, It.IsAny<bool>(), true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(holon) { IsLoaded = true, IsWarning = !wrongIdentity });
        var actor = new Mock<IAvatar>();
        actor.SetupGet(avatar => avatar.Id).Returns(owner);

        var result = await HolonAccess.ShareAsync(fixture.Manager, requested, new[] { Guid.NewGuid() }, actor.Object);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("COSMIC_SHARE_LOOKUP_INVALID");
        fixture.Provider.Invocations.Where(invocation => invocation.Method.Name.Contains("Save") || invocation.Method.Name.Contains("Conditionally")).Should().BeEmpty();
    }

    [Fact]
    public async Task SharingCannotDowngradeAnUnsupportedConditionalProvider()
    {
        var fixture = Fixture(true);
        var owner = Guid.NewGuid();
        var holon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = owner };
        CosmicConcurrency.SetProviderRevisionId(holon, Core.Enums.ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(holon.Id, It.IsAny<bool>(), true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(holon) { IsLoaded = true });
        var actor = new Mock<IAvatar>();
        actor.SetupGet(avatar => avatar.Id).Returns(owner);

        var result = await HolonAccess.ShareAsync(fixture.Manager, holon.Id, new[] { Guid.NewGuid() }, actor.Object);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("COSMIC_CONDITIONAL_WRITE_UNSUPPORTED");
        fixture.Provider.Invocations.Where(invocation => invocation.Method.Name is "SaveHolon" or "SaveHolonAsync").Should().BeEmpty();
    }

    [Fact]
    public void ReplacingSharingRecipientsRemovesRevokedIndexFlags()
    {
        var holon = new Holon();
        var revoked = Guid.NewGuid();
        var retained = Guid.NewGuid();
        HolonAccess.RecordShares(holon, new[] { revoked, retained });
        holon.MetaData["unrelated"] = "preserve";

        HolonAccess.RecordShares(holon, new[] { retained });

        HolonAccess.GetSharedAvatarIds(holon).Should().Equal(retained);
        holon.MetaData.Should().NotContainKey(HolonAccess.SharedWithIndexKey(revoked));
        holon.MetaData[HolonAccess.SharedWithIndexKey(retained)].Should().Be("true");
        holon.MetaData["unrelated"].Should().Be("preserve");
    }
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SharingAndRevocationBindOnlyRootActorOperationAndProviderRevision(bool revoke, bool explicitRevision)
    {
        var fixture = Fixture(true, conditional: true);
        var owner = Guid.NewGuid();
        var target = Guid.NewGuid();
        var retained = Guid.NewGuid();
        var accepted = Guid.NewGuid();
        var expected = explicitRevision ? Guid.NewGuid() : accepted;
        var operation = Guid.NewGuid();
        var holon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = owner, VersionId = Guid.NewGuid() };
        HolonAccess.RecordShares(holon, revoke ? new[] { target, retained } : new[] { retained });
        CosmicConcurrency.SetProviderRevisionId(holon, Core.Enums.ProviderType.SQLLiteDBOASIS, accepted);
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(holon.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(holon) { IsLoaded = true });
        ConditionalHolonWriteRequest? submitted = null;
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConditionalHolonWriteRequest, CancellationToken>((request, _) => submitted = request)
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { IsError = true,
                ErrorCode = "COSMIC_CONCURRENCY_CONFLICT", Message = "preserve rejected intent" });
        var actor = new Mock<IAvatar>();
        actor.SetupGet(avatar => avatar.Id).Returns(owner);
        var options = new CosmicWriteOptions(operation, explicitRevision
            ? new Dictionary<Guid, Guid?> { [holon.Id] = expected } : null);

        var result = revoke
            ? await HolonAccess.RevokeAsync(fixture.Manager, holon.Id, new[] { target }, actor.Object, options)
            : await HolonAccess.ShareAsync(fixture.Manager, holon.Id, new[] { target }, actor.Object, options);

        result.ErrorCode.Should().Be("COSMIC_CONCURRENCY_CONFLICT");
        submitted.Should().NotBeNull();
        submitted!.SaveChildren.Should().BeFalse();
        submitted.Write.ActorId.Should().Be(owner);
        submitted.Write.OperationId.Should().Be(operation);
        submitted.Write.Conditions.Should().ContainSingle().Which.ExpectedRevisionId.Should().Be(expected);
        var staged = submitted.Roots.Should().ContainSingle().Subject;
        HolonAccess.GetSharedAvatarIds(staged).Should().BeEquivalentTo(revoke ? new[] { retained } : new[] { retained, target });
        staged.MetaData.ContainsKey(HolonAccess.SharedWithIndexKey(target)).Should().Be(!revoke);
        fixture.Provider.Verify(provider => provider.LoadHolonAsync(holon.Id, false, true, 0, true, false, 0), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharingAndRevocationRejectNonOwnerBeforeAnyWrite(bool revoke)
    {
        var fixture = Fixture(true, conditional: true);
        var holon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid() };
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(holon.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(holon) { IsLoaded = true });
        var actor = new Mock<IAvatar>();
        actor.SetupGet(avatar => avatar.Id).Returns(Guid.NewGuid());

        var result = revoke
            ? await HolonAccess.RevokeAsync(fixture.Manager, holon.Id, new[] { Guid.NewGuid() }, actor.Object)
            : await HolonAccess.ShareAsync(fixture.Manager, holon.Id, new[] { Guid.NewGuid() }, actor.Object);

        result.ErrorCode.Should().Be("COSMIC_SHARE_FORBIDDEN");
        fixture.Provider.Invocations.Where(invocation => invocation.Method.Name.Contains("Save")
            || invocation.Method.Name.Contains("Conditional")).Should().BeEmpty();
    }

}
