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
        record.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty.With(b => b.SetGrant(HolonPermissionSet.Key(actor), granted)).Encode().Result;
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(record.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(record) { IsLoaded = true });
        var edit = new Holon { Id = record.Id, CreatedByAvatarId = record.CreatedByAvatarId, Name = "edited" };
        edit.MetaData[HolonPermissions.MetadataKey] = record.MetaData[HolonPermissions.MetadataKey];
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
        parent.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty.With(b => b.SetGrant(HolonPermissionSet.Key(actor), granted)).Encode().Result;
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
    private static string Stored(Guid actor, Func<HolonPermissionSet.Builder, HolonPermissionSet.Builder> change) =>
        HolonPermissionSet.Empty.With(change).Encode().Result;

    [Theory]
    [InlineData("metadata.username", true)]
    [InlineData("metadata.email", false)]
    public async Task AFieldWriterMayChangeOnlyTheirField(string field, bool allowed)
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var record = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = owner, Name = "Dave" };
        record.MetaData["username"] = "dave";
        record.MetaData["email"] = "dave@example.com";
        record.MetaData[HolonPermissions.MetadataKey] = Stored(actor, b => b.SetField(HolonPermissionSet.Key(actor), "metadata.username", HolonPermission.Read | HolonPermission.Write));
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(record.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(record) { IsLoaded = true });
        var edit = new Holon { Id = record.Id, CreatedByAvatarId = owner, Name = "Dave" };
        edit.MetaData["username"] = "dave";
        edit.MetaData["email"] = "dave@example.com";
        edit.MetaData[HolonPermissions.MetadataKey] = record.MetaData[HolonPermissions.MetadataKey];
        edit.MetaData[field.Substring("metadata.".Length)] = "changed";
        var expected = new Dictionary<Guid, Guid?> { [record.Id] = Guid.NewGuid() };

        var result = await fixture.Manager.SaveOwnedHolonAsync(edit, actor, new CosmicWriteOptions(Guid.NewGuid(), expected));

        if (allowed) result.ErrorCode.Should().NotBe("COSMIC_WRITE_FORBIDDEN");
        else result.ErrorCode.Should().Be("COSMIC_WRITE_FORBIDDEN");
    }

    [Fact]
    public async Task AWriterCannotChangeWhoHasAccess()
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var record = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = owner, Name = "Dave" };
        record.MetaData[HolonPermissions.MetadataKey] = Stored(actor, b => b.SetGrant(HolonPermissionSet.Key(actor), HolonPermission.Read | HolonPermission.Write));
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(record.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(record) { IsLoaded = true });
        var edit = new Holon { Id = record.Id, CreatedByAvatarId = owner, Name = "Dave" };
        edit.MetaData[HolonPermissions.MetadataKey] = Stored(actor, b => b.SetGrant(HolonPermissionSet.Key(actor), HolonPermission.All));
        var expected = new Dictionary<Guid, Guid?> { [record.Id] = Guid.NewGuid() };

        var result = await fixture.Manager.SaveOwnedHolonAsync(edit, actor, new CosmicWriteOptions(Guid.NewGuid(), expected));

        result.ErrorCode.Should().Be("COSMIC_MANAGE_FORBIDDEN");
    }
}
