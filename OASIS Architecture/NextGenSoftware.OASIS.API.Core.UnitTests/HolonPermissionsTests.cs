using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class HolonPermissionsTests
{
    private static Holon Owned(Guid owner) => new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = owner };

    private static void Grant(Holon holon, Guid avatar, HolonPermission permissions) =>
        holon.MetaData[HolonPermissions.MetadataKey] = HolonPermissions.Encode(
            HolonPermissions.With(HolonPermissions.Read(holon).Result, avatar, permissions)).Result;

    [Fact]
    public void CreatorHasEveryPermission()
    {
        var owner = Guid.NewGuid();
        HolonPermissions.For(Owned(owner), owner).Should().Be(HolonPermission.All);
    }

    [Fact]
    public void StrangerHasNothing()
    {
        HolonPermissions.For(Owned(Guid.NewGuid()), Guid.NewGuid()).Should().Be(HolonPermission.None);
    }

    [Fact]
    public void GrantsAreExactlyWhatTheOwnerSet()
    {
        var holon = Owned(Guid.NewGuid());
        var slp = Guid.NewGuid();
        Grant(holon, slp, HolonPermission.Read | HolonPermission.Write);

        HolonPermissions.Allows(holon, slp, HolonPermission.Read).Should().BeTrue();
        HolonPermissions.Allows(holon, slp, HolonPermission.Write).Should().BeTrue();
        HolonPermissions.Allows(holon, slp, HolonPermission.Delete).Should().BeFalse();
        HolonPermissions.Allows(holon, slp, HolonPermission.AddChildren).Should().BeFalse();
        HolonPermissions.Allows(holon, slp, HolonPermission.Manage).Should().BeFalse();
        HolonPermissions.Allows(holon, Guid.NewGuid(), HolonPermission.Read).Should().BeFalse();
    }

    [Fact]
    public void RevokingRemovesTheAvatar()
    {
        var holon = Owned(Guid.NewGuid());
        var slp = Guid.NewGuid();
        Grant(holon, slp, HolonPermission.Read);
        Grant(holon, slp, HolonPermission.None);
        HolonPermissions.For(holon, slp).Should().Be(HolonPermission.None);
    }

    [Fact]
    public void LegacyRecipientListStillGrantsRead()
    {
        var holon = Owned(Guid.NewGuid());
        var friend = Guid.NewGuid();
        holon.MetaData[HolonReadGrants.MetadataKey] = HolonReadGrants.Encode(new[] { friend }).Result;
        HolonPermissions.For(holon, friend).Should().Be(HolonPermission.Read);
    }

    [Fact]
    public void AttachedAvatarCanReadButNotChange()
    {
        var staff = Guid.NewGuid();
        var holon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid(), ParentHolonId = staff };
        HolonPermissions.For(holon, staff).Should().Be(HolonPermission.Read);
    }

    [Fact]
    public void PublicIsReadableOnlyWhenPublicResultsAreRequested()
    {
        var holon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid(), IsPublic = true };
        var anyone = Guid.NewGuid();
        HolonPermissions.For(holon, anyone, includePublic: true).Should().Be(HolonPermission.Read);
        HolonPermissions.For(holon, anyone, includePublic: false).Should().Be(HolonPermission.None);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"nope\":[\"read\"]}")]
    [InlineData("{\"00000000-0000-0000-0000-000000000001\":[\"fly\"]}")]
    [InlineData("{\"00000000-0000-0000-0000-000000000001\":\"read\"}")]
    public void MalformedGrantsAreAnErrorAndGrantNothing(string stored)
    {
        var holon = Owned(Guid.NewGuid());
        holon.MetaData[HolonPermissions.MetadataKey] = stored;
        HolonPermissions.Read(holon).IsError.Should().BeTrue();
        HolonPermissions.For(holon, Guid.Parse("00000000-0000-0000-0000-000000000001")).Should().Be(HolonPermission.None);
    }

    [Fact]
    public void EncodingIsCanonicalAndRoundTrips()
    {
        var a = Guid.Parse("00000000-0000-0000-0000-00000000000a");
        var b = Guid.Parse("00000000-0000-0000-0000-00000000000b");
        var grants = new Dictionary<Guid, HolonPermission> { [b] = HolonPermission.All, [a] = HolonPermission.Read };
        var text = HolonPermissions.Encode(grants).Result;
        text.IndexOf(a.ToString("D")).Should().BeLessThan(text.IndexOf(b.ToString("D")));

        var holon = Owned(Guid.NewGuid());
        holon.MetaData[HolonPermissions.MetadataKey] = text;
        HolonPermissions.Read(holon).Result.Should().BeEquivalentTo(grants);
    }

    [Fact]
    public void ReadVisibilityFilterHonoursGrants()
    {
        var slp = Guid.NewGuid();
        var shared = Owned(Guid.NewGuid());
        Grant(shared, slp, HolonPermission.Read);
        var hidden = Owned(Guid.NewGuid());
        HolonVisibilityProjection.IsVisibleTo(shared, slp, false).Should().BeTrue();
        HolonVisibilityProjection.IsVisibleTo(hidden, slp, false).Should().BeFalse();
    }
}
