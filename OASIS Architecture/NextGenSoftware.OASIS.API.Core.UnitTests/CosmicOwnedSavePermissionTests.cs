using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory]
    [InlineData(HolonPermission.Read, false)]
    [InlineData(HolonPermission.Write, true)]
    [InlineData(HolonPermission.Write | HolonPermission.Read, true)]
    public async Task OwnedSaveOfSomeoneElsesHolonNeedsAWriteGrant(HolonPermission granted, bool allowed)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var record = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid() };
        record.MetaData[HolonPermissions.MetadataKey] = HolonPermissions.Encode(
            new Dictionary<Guid, HolonPermission> { [actor] = granted }).Result;
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(record.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(record) { IsLoaded = true });
        var edit = new Holon { Id = record.Id, CreatedByAvatarId = record.CreatedByAvatarId, Name = "edited" };
        var expected = new Dictionary<Guid, Guid?> { [record.Id] = Guid.NewGuid() };

        var result = await fixture.Manager.SaveOwnedHolonAsync(edit, actor, new CosmicWriteOptions(Guid.NewGuid(), expected));

        if (allowed) result.ErrorCode.Should().NotBe("COSMIC_WRITE_FORBIDDEN");
        else result.ErrorCode.Should().Be("COSMIC_WRITE_FORBIDDEN");
    }

    [Theory]
    [InlineData(HolonPermission.Read, false)]
    [InlineData(HolonPermission.AddChildren, true)]
    public async Task CreatingUnderARestrictedHolonNeedsAddChildren(HolonPermission granted, bool allowed)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var parent = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid() };
        parent.MetaData[HolonPermissions.MetadataKey] = HolonPermissions.Encode(
            new Dictionary<Guid, HolonPermission> { [actor] = granted }).Result;
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(parent.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(parent) { IsLoaded = true });
        var child = new Holon { Id = Guid.Empty, ParentHolonId = parent.Id, Name = "new" };

        var result = await fixture.Manager.SaveOwnedHolonAsync(child, actor, new CosmicWriteOptions(Guid.NewGuid()));

        if (allowed) result.ErrorCode.Should().NotBe("COSMIC_ADD_CHILDREN_FORBIDDEN");
        else result.ErrorCode.Should().Be("COSMIC_ADD_CHILDREN_FORBIDDEN");
    }

    [Fact]
    public async Task CreatingUnderAnUnrestrictedHolonKeepsWorking()
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var parent = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid() };
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(parent.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(parent) { IsLoaded = true });
        var child = new Holon { Id = Guid.Empty, ParentHolonId = parent.Id, Name = "new" };

        var result = await fixture.Manager.SaveOwnedHolonAsync(child, actor, new CosmicWriteOptions(Guid.NewGuid()));

        result.ErrorCode.Should().NotBe("COSMIC_ADD_CHILDREN_FORBIDDEN");
    }
}
