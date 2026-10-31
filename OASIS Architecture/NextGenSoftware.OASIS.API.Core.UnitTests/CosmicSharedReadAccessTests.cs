using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicSharedReadAccessTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly Guid Group = Guid.NewGuid();

    private static Holon Shared(string grantee, HolonPermission permission)
    {
        var holon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Owner };
        holon.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty.With(b => b.SetGrant(grantee, permission)).Encode().Result;
        return holon;
    }

    [Fact]
    public void BuildsANativePredicateForActorGroupsAndPublic()
    {
        var access = CosmicQueryReadAccess.OwnerPublicOrShared(Actor, new[] { Group });
        access.IsError.Should().BeFalse(access.Message);
        access.Result.ActorId.Should().Be(Actor);
    }

    [Fact]
    public void ReadsOwnPublicAndWholeHolonGrantsToTheActorAndTheirGroups()
    {
        var access = CosmicQueryReadAccess.OwnerPublicOrShared(Actor, new[] { Group }).Result;

        access.CanRead(Shared(HolonPermissionSet.Key(Actor), HolonPermission.Read)).Should().BeTrue();
        access.CanRead(Shared(HolonPermissionSet.Key(Group), HolonPermission.Read)).Should().BeTrue();
        access.CanRead(Shared(HolonPermissionSet.Public, HolonPermission.Read)).Should().BeTrue();
        access.CanRead(new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Actor }).Should().BeTrue();
    }

    [Fact]
    public void AFlagWithoutAStoredGrantNeverGrantsAccess()
    {
        var access = CosmicQueryReadAccess.OwnerPublicOrShared(Actor, null).Result;
        var holon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Owner };
        holon.MetaData[$"SHARED_WITH_{Actor:N}"] = "true";
        access.CanRead(holon).Should().BeFalse();
    }

    [Fact]
    public void WriteOnlyAndFieldOnlyGrantsAreNotServedByCql()
    {
        var access = CosmicQueryReadAccess.OwnerPublicOrShared(Actor, null).Result;
        access.CanRead(Shared(HolonPermissionSet.Key(Actor), HolonPermission.Write)).Should().BeFalse();
        var fieldOnly = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Owner };
        fieldOnly.MetaData[HolonPermissions.MetadataKey] = HolonPermissionSet.Empty
            .With(b => b.SetField(HolonPermissionSet.Key(Actor), "name", HolonPermission.Read)).Encode().Result;
        access.CanRead(fieldOnly).Should().BeFalse();
    }

    [Fact]
    public void AGroupTheActorIsNotInGrantsNothing()
    {
        var access = CosmicQueryReadAccess.OwnerPublicOrShared(Actor, null).Result;
        access.CanRead(Shared(HolonPermissionSet.Key(Group), HolonPermission.Read)).Should().BeFalse();
    }

    [Fact]
    public void DefaultOwnerOrPublicIsUnchanged()
    {
        var access = CosmicQueryReadAccess.OwnerOrPublic(Actor).Result;
        access.CanRead(Shared(HolonPermissionSet.Key(Actor), HolonPermission.Read)).Should().BeFalse();
        access.CanRead(new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Actor }).Should().BeTrue();
    }

    [Fact]
    public void RejectsAnEmptyActorAndTooManyGroups()
    {
        CosmicQueryReadAccess.OwnerPublicOrShared(Guid.Empty, null).IsError.Should().BeTrue();
        CosmicQueryReadAccess.OwnerPublicOrShared(Actor, Enumerable.Range(0, 65).Select(_ => Guid.NewGuid())).IsError.Should().BeTrue();
    }
}
