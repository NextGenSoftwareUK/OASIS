using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CqlSharedReadsTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly Guid Group = Guid.NewGuid();

    private sealed class Fake : ICqlSharedReads
    {
        public bool SharedReads { get; init; } = true;
        public bool InheritedReads { get; init; } = true;
        public bool CanWalk { get; init; } = true;
        public Guid[] Groups { get; init; } = { Group };
        public IReadOnlyCollection<Guid> GroupsOf(Guid actor) => Groups;
        public bool ProviderWalksAncestors(ProviderType? provider) => CanWalk;
    }

    private static CosmicQueryReadAccess Access(ICqlSharedReads shared, ProviderType? provider = ProviderType.MongoDBOASIS) =>
        CqlV1Compiler.DefaultReadAccess(Actor, provider, shared).Result;

    private static Holon SharedWith(Guid grantee)
    {
        var holon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid() };
        holon.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty
            .With(b => b.SetGrant(HolonPermissionSet.Key(grantee), HolonPermission.Read)).Encode().Result;
        return holon;
    }

    private static bool HasAncestorNode(CosmicQueryReadAccess access)
    {
        var query = CosmicQuery<Holon>.Create().WithReadAccess(access).Result;
        var pending = new Stack<CosmicQueryPredicate>(query.Predicates);
        while (pending.Count != 0)
        {
            var node = pending.Pop();
            if (node is CosmicQueryAnyAncestor) return true;
            if (node is CosmicQueryLogical logical) { pending.Push(logical.Left); pending.Push(logical.Right); }
        }
        return false;
    }

    [Fact]
    public void WithNoHostPolicyQueriesKeepTheOwnerAndPublicRule()
    {
        var access = Access(null);
        access.CanRead(SharedWith(Actor)).Should().BeFalse();
        HasAncestorNode(access).Should().BeFalse();
    }

    [Fact]
    public void SharedReadsOffKeepsTheOwnerAndPublicRule() =>
        Access(new Fake { SharedReads = false }).CanRead(SharedWith(Actor)).Should().BeFalse();

    [Fact]
    public void SharedReadsOnReturnsWhatWasSharedWithTheCallerAndTheirGroups()
    {
        var access = Access(new Fake { CanWalk = false });
        access.CanRead(SharedWith(Actor)).Should().BeTrue();
        access.CanRead(SharedWith(Group)).Should().BeTrue();
        access.CanRead(SharedWith(Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public void AProviderThatCannotWalkUpKeepsThePlainSharedRule_NotARejectedQuery()
    {
        var access = Access(new Fake { CanWalk = false }, ProviderType.AzureCosmosDBOASIS);
        HasAncestorNode(access).Should().BeFalse();
    }

    [Fact]
    public void AProviderThatCanWalkUpGetsInheritedReads() =>
        HasAncestorNode(Access(new Fake { CanWalk = true })).Should().BeTrue();

    [Fact]
    public void InheritedReadsSwitchedOffIsHonouredEvenWhereTheProviderCanWalk() =>
        HasAncestorNode(Access(new Fake { InheritedReads = false, CanWalk = true })).Should().BeFalse();

    [Fact]
    public void TheDnaDefaultsTurnSharedAndInheritedReadsOn()
    {
        var settings = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS() }.OASIS.Cql;
        settings.SharedReads.Should().BeTrue();
        settings.InheritedReads.Should().BeTrue();
        settings.GroupCacheSeconds.Should().Be(15);
    }

    // ── the group lookup the host uses ─────────────────────────────────────────

    private static Holon MakeGroup(params Guid[] members)
    {
        var group = new Holon { Id = Guid.NewGuid(), CustomHolonType = HolonGroup.CustomType };
        HolonGroup.SetMembers(group, members).IsError.Should().BeFalse();
        return group;
    }

    [Fact]
    public void GroupsOfFollowsGroupsOfGroupsAndSurvivesALoop()
    {
        var person = Guid.NewGuid();
        var team = MakeGroup(person);
        var org = MakeGroup(team.Id);
        HolonGroup.SetMembers(team, new[] { person, org.Id }); // team and org contain each other
        var byMember = new Dictionary<Guid, IHolon[]> { [person] = new IHolon[] { team }, [team.Id] = new IHolon[] { org }, [org.Id] = new IHolon[] { team } };

        var groups = HolonAccessResolver.GroupsOf(person, member => byMember.TryGetValue(member, out var g) ? g : null);

        groups.Should().BeEquivalentTo(new[] { team.Id, org.Id });
    }

    [Fact]
    public void GroupsOfIgnoresAHolonThatOnlyClaimsMembership()
    {
        var person = Guid.NewGuid();
        var claim = new Holon { Id = Guid.NewGuid(), CustomHolonType = HolonGroup.CustomType };
        claim.MetaData[HolonGroup.MemberIndexKey(person)] = HolonGroup.MemberIndexValue; // flag without a member list
        HolonAccessResolver.GroupsOf(person, _ => new IHolon[] { claim }).Should().BeEmpty();
    }

    [Fact]
    public void GroupsOfIsBounded()
    {
        var person = Guid.NewGuid();
        var many = Enumerable.Range(0, 200).Select(_ => MakeGroup(person)).Cast<IHolon>().ToArray();
        HolonAccessResolver.GroupsOf(person, member => member == person ? many : null).Count.Should().BeLessThanOrEqualTo(HolonAccessResolver.MaximumGroups + many.Length);
    }
}
