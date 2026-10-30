using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicAncestorAccessTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly Guid Group = Guid.NewGuid();

    private static CosmicQueryCapabilities Capabilities(bool ancestors, int depth = 16)
    {
        var features = CosmicQueryFeatures.And | CosmicQueryFeatures.Or | CosmicQueryFeatures.Metadata | CosmicQueryFeatures.MetadataExists
            | CosmicQueryFeatures.BooleanConstant;
        if (ancestors) features |= CosmicQueryFeatures.AncestorExists;
        var fields = new[]
        {
            new CosmicQueryFieldCapability("IsPublic", typeof(bool), CosmicQueryComparisonOperator.Equal),
            new CosmicQueryFieldCapability("CreatedByAvatarId", typeof(Guid), CosmicQueryComparisonOperator.Equal),
        };
        return new CosmicQueryCapabilities(features, fields, Array.Empty<CosmicQueryMetadataCapability>(),
            ancestors ? new CosmicQueryTraversalCapability(depth, 100) : null!);
    }

    private static CosmicQuery<Holon> InheritedQuery()
    {
        var access = CosmicQueryReadAccess.OwnerPublicOrShared(Actor, new[] { Group }, includeInherited: true);
        access.IsError.Should().BeFalse(access.Message);
        var bound = CosmicQuery<Holon>.Create().WithReadAccess(access.Result);
        bound.IsError.Should().BeFalse(bound.Message);
        return bound.Result;
    }

    [Fact]
    public void TheInheritedRuleContainsABoundedAncestorNode()
    {
        var ancestor = InheritedQuery().Predicates.OfType<CosmicQueryLogical>().Select(l => l.Right).OfType<CosmicQueryAnyAncestor>().Single();
        ancestor.Traversal.MaximumDepth.Should().Be(16);
    }

    [Fact]
    public void AProviderWithoutAncestorSupportRejectsTheRuleInsteadOfReturningLess()
    {
        var result = Capabilities(ancestors: false).Admit(InheritedQuery());
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
    }

    [Fact]
    public void AProviderThatDeclaresAncestorsAdmitsIt()
    {
        var result = Capabilities(ancestors: true).Admit(InheritedQuery());
        result.IsError.Should().BeFalse(result.Message);
    }

    [Fact]
    public void AProviderWhoseCertifiedDepthIsTooShortRejectsIt()
    {
        Capabilities(ancestors: true, depth: 8).Admit(InheritedQuery()).IsError.Should().BeTrue();
    }

    [Fact]
    public void WithoutInheritanceNoAncestorSupportIsNeeded()
    {
        var access = CosmicQueryReadAccess.OwnerPublicOrShared(Actor, new[] { Group }).Result;
        var query = CosmicQuery<Holon>.Create().WithReadAccess(access).Result;
        Capabilities(ancestors: false).Admit(query).IsError.Should().BeFalse();
    }

    [Fact]
    public void AncestorSupportRequiresCertifiedBounds()
    {
        Assert.Throws<ArgumentException>(() => new CosmicQueryCapabilities(CosmicQueryFeatures.AncestorExists,
            Array.Empty<CosmicQueryFieldCapability>(), Array.Empty<CosmicQueryMetadataCapability>(), null!));
    }

    [Fact]
    public void InheritedReadGrantsAreIndexedOnTheGrantingHolon()
    {
        var holon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Owner };
        holon.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty.With(b => b
            .SetInherit(HolonPermissionSet.Key(Actor), HolonPermission.Read)
            .SetInherit(HolonPermissionSet.Key(Group), HolonPermission.Write)          // no read: not indexed
            .SetInherit(HolonPermissionSet.Public, HolonPermission.Read)).Encode().Result;
        holon.MetaData[HolonReadIndex.InheritKeyFor(Guid.NewGuid())] = "true";          // stale

        HolonReadIndex.Rebuild(holon);

        holon.MetaData.Should().ContainKey(HolonReadIndex.InheritKeyFor(Actor));
        holon.MetaData.Should().ContainKey(HolonReadIndex.InheritPublicKey);
        holon.MetaData.Should().NotContainKey(HolonReadIndex.InheritKeyFor(Group));
        holon.MetaData.Keys.Count(k => k.StartsWith(HolonReadIndex.InheritPrefix)).Should().Be(2);
    }

    [Theory]
    [InlineData("INHERIT_WITH_0123456789abcdef0123456789abcdef", true)]
    [InlineData("INHERIT_WITH_public", true)]
    [InlineData("INHERIT_WITH_nonsense", false)]
    public void InheritFlagsAreRecognisedAsIndexKeys(string key, bool expected) =>
        HolonReadIndex.IsIndexKey(key).Should().Be(expected);

    [Fact]
    public void AccessFlagsCanOnlyBeChangedWithManage() =>
        HolonFieldProjection.IsAccessMetadata("INHERIT_WITH_0123").Should().BeTrue();

    [Fact]
    public void TheDetachedRowCheckAcceptsRowsWithAParentOnlyInInheritedMode()
    {
        var child = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Owner, ParentHolonId = Guid.NewGuid() };
        var orphan = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Owner };
        var inherited = CosmicQueryReadAccess.OwnerPublicOrShared(Actor, null, includeInherited: true).Result;
        var plain = CosmicQueryReadAccess.OwnerPublicOrShared(Actor, null).Result;

        inherited.CanRead(child).Should().BeTrue();
        inherited.CanRead(orphan).Should().BeFalse();
        plain.CanRead(child).Should().BeFalse();
    }

    [Fact]
    public void TheTwoRulesAreDifferentPolicies()
    {
        var plain = CosmicQueryReadAccess.OwnerPublicOrShared(Actor, null).Result;
        var inherited = CosmicQueryReadAccess.OwnerPublicOrShared(Actor, null, includeInherited: true).Result;
        CosmicQuery<Holon>.Create().WithReadAccess(plain).Result.WithReadAccess(inherited).ErrorCode.Should().Be("CQL_ACCESS_REBIND_FORBIDDEN");
    }
}
