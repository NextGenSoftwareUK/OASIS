using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class HolonPermissionsTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Slp = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    private static Holon OwnedBy(Guid owner, Guid? parent = null) =>
        new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = owner, ParentHolonId = parent ?? Guid.NewGuid() };

    private static void Store(IHolonBase holon, Func<HolonPermissionSet.Builder, HolonPermissionSet.Builder> change) =>
        holon.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Read(holon).Result.With(change).Encode().Result;

    private static string G(Guid id) => HolonPermissionSet.Key(id);

    private static HolonAccessResolver Resolver(Guid actor, bool includePublic = true,
        IDictionary<Guid, IHolon> others = null, IEnumerable<IHolon> groups = null, params IHolonBase[] prime)
    {
        var resolver = new HolonAccessResolver(actor, includePublic);
        resolver.Prime(prime, id => others != null && others.TryGetValue(id, out var h) ? h : null, _ => groups);
        return resolver;
    }

    // ── whole-holon grants ────────────────────────────────────────────────────

    [Fact]
    public void CreatorHasEverythingAndStrangerNothing()
    {
        var holon = OwnedBy(Owner);
        HolonPermissions.For(holon, Owner).Should().Be(HolonPermission.All);
        HolonPermissions.For(holon, Stranger).Should().Be(HolonPermission.None);
    }

    [Fact]
    public void GrantsAreExactlyWhatTheOwnerSet()
    {
        var holon = OwnedBy(Owner);
        Store(holon, b => b.SetGrant(G(Slp), HolonPermission.Read | HolonPermission.Write));
        var rights = Resolver(Slp, prime: holon).Resolve(holon);
        rights.Allows(HolonPermission.Read).Should().BeTrue();
        rights.Allows(HolonPermission.Write).Should().BeTrue();
        rights.Allows(HolonPermission.Delete).Should().BeFalse();
        rights.Allows(HolonPermission.Manage).Should().BeFalse();
        Resolver(Stranger, prime: holon).Resolve(holon).CanSee.Should().BeFalse();
    }

    [Fact]
    public void AttachedAvatarReadsButCannotChange()
    {
        var holon = OwnedBy(Owner, parent: Slp);
        var rights = Resolver(Slp, prime: holon).Resolve(holon);
        rights.Allows(HolonPermission.Read).Should().BeTrue();
        rights.Allows(HolonPermission.Write).Should().BeFalse();
    }

    [Fact]
    public void PublicGranteeIsReadableOnlyWhenPublicResultsAreRequested()
    {
        var holon = OwnedBy(Owner);
        Store(holon, b => b.SetGrant(HolonPermissionSet.Public, HolonPermission.Read));
        Resolver(Stranger, includePublic: true, prime: holon).Resolve(holon).CanSee.Should().BeTrue();
        Resolver(Stranger, includePublic: false, prime: holon).Resolve(holon).CanSee.Should().BeFalse();
    }

    [Fact]
    public void LegacyRecipientListStillGrantsRead()
    {
        var holon = OwnedBy(Owner);
        holon.MetaData[HolonReadGrants.MetadataKey] = HolonReadGrants.Encode(new[] { Slp }).Result;
        Resolver(Slp, prime: holon).Resolve(holon).Whole.Should().Be(HolonPermission.Read);
    }

    // ── stored format ─────────────────────────────────────────────────────────

    [Fact]
    public void FlatValuesFromTheFirstVersionAreReadAsGrants()
    {
        var holon = OwnedBy(Owner);
        holon.MetaData[HolonPermissions.MetadataKey] = $"{{\"{G(Slp)}\":[\"read\",\"write\"]}}";
        var set = HolonPermissionSet.Read(holon).Result;
        set.Grants[G(Slp)].Should().Be(HolonPermission.Read | HolonPermission.Write);
        set.Encode().Result.Should().Be($"{{\"{G(Slp)}\":[\"read\",\"write\"]}}");
    }

    [Fact]
    public void StructuredValueRoundTripsInCanonicalOrder()
    {
        var a = Guid.Parse("00000000-0000-0000-0000-00000000000a");
        var b = Guid.Parse("00000000-0000-0000-0000-00000000000b");
        var set = HolonPermissionSet.Empty.With(x => x
            .SetGrant(G(b), HolonPermission.All).SetGrant(G(a), HolonPermission.Read)
            .SetInherit(G(a), HolonPermission.Read)
            .SetField(HolonPermissionSet.Public, "metadata.username", HolonPermission.Read));
        var text = set.Encode().Result;
        text.IndexOf(G(a)).Should().BeLessThan(text.IndexOf(G(b)));

        var holon = OwnedBy(Owner);
        holon.MetaData[HolonPermissions.MetadataKey] = text;
        var back = HolonPermissionSet.Read(holon).Result;
        back.Grants.Should().BeEquivalentTo(set.Grants);
        back.Inherit.Should().BeEquivalentTo(set.Inherit);
        back.Fields["public"]["metadata.username"].Should().Be(HolonPermission.Read);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"nope\":[\"read\"]}")]
    [InlineData("{\"00000000-0000-0000-0000-000000000001\":[\"fly\"]}")]
    [InlineData("{\"00000000-0000-0000-0000-000000000001\":\"read\"}")]
    [InlineData("{\"grants\":{},\"surprise\":{}}")]
    [InlineData("{\"fields\":{\"public\":{\"name\":[\"manage\"]}}}")]
    [InlineData("{\"fields\":{\"public\":{\"metadata.\":[\"read\"]}}}")]
    public void MalformedValuesGrantNothing(string stored)
    {
        var holon = OwnedBy(Owner);
        holon.MetaData[HolonPermissions.MetadataKey] = stored;
        HolonPermissionSet.Read(holon).IsError.Should().BeTrue();
        Resolver(Slp, prime: holon).Resolve(holon).CanSee.Should().BeFalse();
    }

    // ── groups and roles ──────────────────────────────────────────────────────

    private static Holon Group(params Guid[] members)
    {
        var group = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Owner, CustomHolonType = HolonGroup.CustomType };
        HolonGroup.SetMembers(group, members).IsError.Should().BeFalse();
        return group;
    }

    [Fact]
    public void AGrantToAGroupReachesItsMembersOnly()
    {
        var group = Group(Slp);
        var holon = OwnedBy(Owner);
        Store(holon, b => b.SetGrant(G(group.Id), HolonPermission.Read | HolonPermission.Write));

        Resolver(Slp, groups: new[] { group }, prime: holon).Resolve(holon).Allows(HolonPermission.Write).Should().BeTrue();
        Resolver(Stranger, groups: new[] { group }, prime: holon).Resolve(holon).CanSee.Should().BeFalse();
    }

    [Fact]
    public void RemovingAMemberRemovesTheirAccessEverywhere()
    {
        var group = Group(Slp, Stranger);
        var holon = OwnedBy(Owner);
        Store(holon, b => b.SetGrant(G(group.Id), HolonPermission.Read));
        Resolver(Stranger, groups: new[] { group }, prime: holon).Resolve(holon).CanSee.Should().BeTrue();

        HolonGroup.SetMembers(group, new[] { Slp });
        Resolver(Stranger, groups: new[] { group }, prime: holon).Resolve(holon).CanSee.Should().BeFalse();
        group.MetaData.ContainsKey(HolonGroup.MemberIndexKey(Stranger)).Should().BeFalse();
        group.MetaData.ContainsKey(HolonGroup.MemberIndexKey(Slp)).Should().BeTrue();
    }

    [Fact]
    public void AHolonThatMerelyLooksLikeAGroupGrantsNothing()
    {
        var notReally = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Owner, CustomHolonType = HolonGroup.CustomType };
        notReally.MetaData[HolonGroup.MemberIndexKey(Stranger)] = HolonGroup.MemberIndexValue; // flag only, no member list
        var holon = OwnedBy(Owner);
        Store(holon, b => b.SetGrant(G(notReally.Id), HolonPermission.Read));
        Resolver(Stranger, groups: new[] { notReally }, prime: holon).Resolve(holon).CanSee.Should().BeFalse();
    }

    // ── inheritance ───────────────────────────────────────────────────────────

    [Fact]
    public void InheritedGrantsReachEveryDescendantButDirectGrantsDoNot()
    {
        var root = OwnedBy(Owner);
        var middle = OwnedBy(Owner, parent: root.Id);
        var leaf = OwnedBy(Owner, parent: middle.Id);
        Store(root, b => b.SetInherit(G(Slp), HolonPermission.Read | HolonPermission.Write).SetGrant(G(Stranger), HolonPermission.Read));
        var others = new Dictionary<Guid, IHolon> { [root.Id] = root, [middle.Id] = middle };

        var slp = Resolver(Slp, others: others, prime: leaf);
        slp.Resolve(leaf).Allows(HolonPermission.Write).Should().BeTrue();
        slp.Resolve(middle).Allows(HolonPermission.Read).Should().BeTrue();

        var other = Resolver(Stranger, others: others, prime: leaf);
        other.Resolve(leaf).CanSee.Should().BeFalse();
        other.Resolve(root).CanSee.Should().BeTrue();
    }

    [Fact]
    public void AnInheritedGrantDoesNotMakeAnyoneTheOwnerOfDescendants()
    {
        var root = OwnedBy(Owner);
        var leaf = OwnedBy(Owner, parent: root.Id);
        Store(root, b => b.SetInherit(G(Slp), HolonPermission.Read | HolonPermission.Write));
        var rights = Resolver(Slp, others: new Dictionary<Guid, IHolon> { [root.Id] = root }, prime: leaf).Resolve(leaf);
        rights.Allows(HolonPermission.Manage).Should().BeFalse();
        rights.Allows(HolonPermission.Delete).Should().BeFalse();
    }

    [Fact]
    public void AParentThatIsAnAvatarOrCannotBeLoadedEndsTheChain()
    {
        var leaf = OwnedBy(Owner, parent: Guid.NewGuid());
        Resolver(Slp, prime: leaf).Resolve(leaf).CanSee.Should().BeFalse();
    }

    // ── fields ────────────────────────────────────────────────────────────────

    private static Holon Profile()
    {
        var holon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Owner, ParentHolonId = Owner, Name = "Dave", Description = "private notes" };
        holon.MetaData["username"] = "dave";
        holon.MetaData["email"] = "dave@example.com";
        return holon;
    }

    [Fact]
    public void ShareOnlyTheUsernameOnAProfile()
    {
        var holon = Profile();
        Store(holon, b => b.SetField(HolonPermissionSet.Public, "metadata.username", HolonPermission.Read));

        var shown = Resolver(Stranger, prime: holon).Project(holon);

        shown.Should().NotBeNull();
        shown.Id.Should().Be(holon.Id);
        shown.MetaData.Should().ContainKey("username").And.NotContainKey("email").And.NotContainKey(HolonPermissions.MetadataKey);
        shown.Name.Should().BeNullOrEmpty();
        shown.Description.Should().BeNullOrEmpty();
    }

    [Fact]
    public void FieldGrantsCanNameNameAndDescriptionAndWildcards()
    {
        var holon = Profile();
        Store(holon, b => b.SetField(G(Slp), "name", HolonPermission.Read).SetField(G(Slp), "metadata.*", HolonPermission.Read));
        var shown = Resolver(Slp, prime: holon).Project(holon);
        shown.Name.Should().Be("Dave");
        shown.Description.Should().BeNullOrEmpty();
        shown.MetaData.Keys.Should().BeEquivalentTo(new[] { "username", "email" });
    }

    [Fact]
    public void WholeHolonReadReturnsTheHolonItself()
    {
        var holon = Profile();
        Store(holon, b => b.SetGrant(G(Slp), HolonPermission.Read));
        Resolver(Slp, prime: holon).Project(holon).Should().BeSameAs(holon);
    }

    [Fact]
    public void NoGrantMeansNotReturnedAtAll()
    {
        var holon = Profile();
        holon.ParentHolonId = Guid.NewGuid();
        Resolver(Stranger, prime: holon).Project(holon).Should().BeNull();
    }

    [Fact]
    public void FieldWriteAndDeleteAreSeparate()
    {
        var holon = Profile();
        Store(holon, b => b.SetField(G(Slp), "metadata.username", HolonPermission.Read | HolonPermission.Write));
        var rights = Resolver(Slp, prime: holon).Resolve(holon);
        rights.FieldAllows("metadata.username", HolonPermission.Write).Should().BeTrue();
        rights.FieldAllows("metadata.username", HolonPermission.Delete).Should().BeFalse();
        rights.FieldAllows("metadata.email", HolonPermission.Write).Should().BeFalse();
        rights.FieldAllows("name", HolonPermission.Write).Should().BeFalse();
    }

    [Fact]
    public void ChangesListsFieldsAndMetadataKeysWithThePermissionNeeded()
    {
        var stored = Profile();
        var incoming = Profile();
        incoming.Id = stored.Id;
        incoming.Name = "David";
        incoming.MetaData["username"] = "david";
        incoming.MetaData.Remove("email");
        incoming.MetaData["phone"] = "1";

        var changes = HolonFieldProjection.Changes(stored, incoming);

        changes.Should().Contain(("name", HolonPermission.Write));
        changes.Should().Contain(("metadata.username", HolonPermission.Write));
        changes.Should().Contain(("metadata.phone", HolonPermission.Write));
        changes.Should().Contain(("metadata.email", HolonPermission.Delete));
        changes.Should().NotContain(c => c.Field == "description");
    }

    [Fact]
    public void AnUnchangedHolonHasNoChanges()
    {
        var stored = Profile();
        var incoming = Profile();
        incoming.Id = stored.Id;
        HolonFieldProjection.Changes(stored, incoming).Should().BeEmpty();
    }

    [Theory]
    [InlineData("OASIS_PERMISSIONS", true)]
    [InlineData("OASIS_GROUP_MEMBERS", true)]
    [InlineData("SHARED_AVATAR_IDS", true)]
    [InlineData("SHARED_WITH_0123", true)]
    [InlineData("GROUP_MEMBER_0123", true)]
    [InlineData("username", false)]
    public void AccessMetadataNeedsManage(string key, bool access) =>
        HolonFieldProjection.IsAccessMetadata(key).Should().Be(access);

    [Fact]
    public void ProviderLevelVisibilityStillHonoursWholeHolonGrants()
    {
        var holon = OwnedBy(Owner);
        Store(holon, b => b.SetGrant(G(Slp), HolonPermission.Read));
        HolonVisibilityProjection.IsVisibleTo(holon, Slp, false).Should().BeTrue();
        HolonVisibilityProjection.IsVisibleTo(holon, Stranger, false).Should().BeFalse();
    }
    // ── nested groups, inherited fields, credentials ───────────────────────────

    [Fact]
    public void GroupsCanContainGroupsAndAccessFollowsTheNesting()
    {
        var team = Group(Slp);                       // Slp is in "team"
        var org = Group(team.Id);                    // "team" is in "org"
        var holon = OwnedBy(Owner);
        Store(holon, b => b.SetGrant(G(org.Id), HolonPermission.Read));
        var groupsByMember = new Dictionary<Guid, IHolon[]> { [Slp] = new IHolon[] { team }, [team.Id] = new IHolon[] { org } };

        var resolver = new HolonAccessResolver(Slp);
        resolver.Prime(new IHolonBase[] { holon }, _ => null, member => groupsByMember.TryGetValue(member, out var g) ? g : null);

        resolver.Resolve(holon).CanSee.Should().BeTrue();
    }

    [Fact]
    public void NestedGroupsDoNotLoopAndAreBounded()
    {
        var a = Group(Slp);
        var b = Group(a.Id);
        HolonGroup.SetMembers(a, new[] { Slp, b.Id }); // a and b contain each other
        var holon = OwnedBy(Owner);
        Store(holon, x => x.SetGrant(G(b.Id), HolonPermission.Read));
        var groupsByMember = new Dictionary<Guid, IHolon[]> { [Slp] = new IHolon[] { a }, [a.Id] = new IHolon[] { b }, [b.Id] = new IHolon[] { a } };

        var resolver = new HolonAccessResolver(Slp);
        resolver.Prime(new IHolonBase[] { holon }, _ => null, member => groupsByMember.TryGetValue(member, out var g) ? g : null);

        resolver.Resolve(holon).CanSee.Should().BeTrue();
    }

    [Fact]
    public void InheritedFieldGrantsReachDescendantsWithoutGrantingTheWholeHolon()
    {
        var root = OwnedBy(Owner);
        var leaf = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Owner, ParentHolonId = root.Id, Name = "Leaf" };
        leaf.MetaData["note"] = "n";
        leaf.MetaData["secret"] = "s";
        Store(root, b => b.SetInheritField(G(Slp), "metadata.note", HolonPermission.Read));
        var others = new Dictionary<Guid, IHolon> { [root.Id] = root };

        var shown = Resolver(Slp, others: others, prime: leaf).Project(leaf);

        shown.MetaData.Should().ContainKey("note").And.NotContainKey("secret");
        shown.Name.Should().BeNullOrEmpty();
        Resolver(Stranger, others: others, prime: leaf).Project(leaf).Should().BeNull();
    }

    [Fact]
    public void InheritFieldsRoundTrip()
    {
        var holon = OwnedBy(Owner);
        Store(holon, b => b.SetInheritField(HolonPermissionSet.Public, "name", HolonPermission.Read));
        var back = HolonPermissionSet.Read(holon).Result;
        back.InheritFields["public"]["name"].Should().Be(HolonPermission.Read);
        back.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void CredentialsAreNeverSharedEvenWithAWildcard()
    {
        var avatar = new Avatar { Id = Guid.NewGuid(), CreatedByAvatarId = Owner, Username = "dave", Email = "d@example.com", Password = "hash", JwtToken = "jwt" };
        Store(avatar, b => b.SetField(HolonPermissionSet.Public, "*", HolonPermission.Read));

        var shown = Resolver(Stranger, prime: avatar).Project(avatar);

        shown.Username.Should().Be("dave");
        shown.Email.Should().Be("d@example.com");
        shown.Password.Should().BeNullOrEmpty();
        shown.JwtToken.Should().BeNullOrEmpty();
    }

    [Fact]
    public void ShareOnlyTheUsernameOnAnAvatar()
    {
        var avatar = new Avatar { Id = Guid.NewGuid(), CreatedByAvatarId = Owner, Username = "dave", Email = "d@example.com", FirstName = "Dave" };
        Store(avatar, b => b.SetField(HolonPermissionSet.Public, "username", HolonPermission.Read));

        var shown = Resolver(Stranger, prime: avatar).Project(avatar);

        shown.Username.Should().Be("dave");
        shown.Email.Should().BeNullOrEmpty();
        shown.FirstName.Should().BeNullOrEmpty();
    }
    [Fact]
    public void AnInheritedGrantCoversTheHolonThatCarriesItAsWellAsEverythingBelow()
    {
        var root = OwnedBy(Owner);
        Store(root, b => b.SetInherit(G(Slp), HolonPermission.Read));
        Resolver(Slp, prime: root).Resolve(root).Allows(HolonPermission.Read).Should().BeTrue();
        Resolver(Stranger, prime: root).Resolve(root).CanSee.Should().BeFalse();
    }
}
