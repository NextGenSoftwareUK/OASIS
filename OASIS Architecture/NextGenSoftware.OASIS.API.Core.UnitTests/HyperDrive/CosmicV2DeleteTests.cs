using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    public static IEnumerable<object[]> V2DeleteRoutes()
    {
        foreach (var sync in new[] { false, true })
        foreach (var key in new[] { false, true })
        foreach (var typed in new[] { false, true }) yield return new object[] { sync, key, typed };
    }

    [Theory]
    [MemberData(nameof(V2DeleteRoutes))]
    public async Task V2SoftDeletionWritesOnlyDetachedRootAndRetainsReadAuthority(bool sync, bool key, bool typed)
    {
        var id = Guid.NewGuid(); var actor = Guid.NewGuid(); const string nativeKey = "delete-root-key";
        var child = new Holon { Id = Guid.NewGuid(), Name = "child unchanged", Version = 9 };
        var source = new Holon { Id = id, Name = "root", Version = 3, VersionId = Guid.NewGuid(), Children = new List<IHolon> { child } };
        var primary = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "root-authority");
        var other = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-provider-changed-during-read");
        var dna = CreateDna(HyperDriveModes.V2);
        var providers = new ProviderManager(null, dna)
        { IsAutoReplicationEnabled = false, IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false };
        foreach (var mock in new[] { primary, other })
        {
            mock.Setup(p => p.ActivateProvider()).Returns(new OASISResult<bool>(true));
            mock.Setup(p => p.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        }
        providers.RegisterProvider(primary.Object); providers.RegisterProvider(other.Object);
        providers.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        OASISResult<IHolon> Read()
        {
            providers.SetAndActivateCurrentStorageProvider(other.Object).IsError.Should().BeFalse();
            return new OASISResult<IHolon>(source);
        }
        primary.Setup(p => p.LoadHolon(id, false, false, 0, true, false, 0)).Returns(Read);
        primary.Setup(p => p.LoadHolonAsync(id, false, false, 0, true, false, 0)).ReturnsAsync(Read);
        primary.Setup(p => p.LoadHolon(nativeKey, false, false, 0, true, false, 0)).Returns(Read);
        primary.Setup(p => p.LoadHolonAsync(nativeKey, false, false, 0, true, false, 0)).ReturnsAsync(Read);
        IHolon? written = null;
        OASISResult<IHolon> Save(IHolon value)
        { written = value; return new OASISResult<IHolon>(value) { IsSaved = true }; }
        primary.Setup(p => p.SaveHolon(It.IsAny<IHolon>(), false, false, 0, true, false))
            .Returns((IHolon value, bool a, bool b, int c, bool d, bool e) => Save(value));
        primary.Setup(p => p.SaveHolonAsync(It.IsAny<IHolon>(), false, false, 0, true, false))
            .ReturnsAsync((IHolon value, bool a, bool b, int c, bool d, bool e) => Save(value));
        var manager = new HolonManager(null, dna, providers);
        OASISResult<IHolon> outcome;
        if (typed)
        {
            var result = sync
                ? key ? manager.DeleteHolon<Holon>(nativeKey, actor) : manager.DeleteHolon<Holon>(id, actor)
                : key ? await manager.DeleteHolonAsync<Holon>(nativeKey, actor) : await manager.DeleteHolonAsync<Holon>(id, actor);
            result.IsError.Should().BeFalse(result.Message); result.IsDeleted.Should().BeTrue();
            outcome = new OASISResult<IHolon>(result.Result);
        }
        else
        {
            outcome = sync ? key ? manager.DeleteHolon(nativeKey, actor) : manager.DeleteHolon(id, actor)
                : key ? await manager.DeleteHolonAsync(nativeKey, actor) : await manager.DeleteHolonAsync(id, actor);
            outcome.IsError.Should().BeFalse(outcome.Message); outcome.IsDeleted.Should().BeTrue();
        }
        written.Should().NotBeNull(); written!.Id.Should().Be(id); written.Children.Should().BeNullOrEmpty();
        written.DeletedByAvatarId.Should().Be(actor); written.Version.Should().Be(4);
        source.DeletedDate.Should().Be(default); source.DeletedByAvatarId.Should().BeEmpty();
        source.Version.Should().Be(3); source.Children.Should().ContainSingle().Which.Should().BeSameAs(child);
        child.Version.Should().Be(9); child.DeletedByAvatarId.Should().BeEmpty();
        outcome.Result.Should().NotBeSameAs(source);
        other.Invocations.Should().NotContain(x => x.Method.Name.StartsWith("SaveHolon"));
    }

    [Theory]
    [MemberData(nameof(V2DeleteRoutes))]
    public async Task V2HardDeletionRequiresNoPrerequisiteRead(bool sync, bool key, bool typed)
    {
        var id = Guid.NewGuid(); var actor = Guid.NewGuid(); const string nativeKey = "native-delete";
        var primary = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "hard-delete-no-read");
        var committed = new OASISResult<IHolon>(new Holon { Id = id }) { IsDeleted = true };
        primary.Setup(p => p.DeleteHolon(id)).Returns(committed);
        primary.Setup(p => p.DeleteHolonAsync(id)).ReturnsAsync(committed);
        primary.Setup(p => p.DeleteHolon(nativeKey)).Returns(committed);
        primary.Setup(p => p.DeleteHolonAsync(nativeKey)).ReturnsAsync(committed);
        var dna = CreateDna(HyperDriveModes.V2);
        var providers = new ProviderManager(null, dna) { IsAutoReplicationEnabled = false, IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false };
        primary.Setup(p => p.ActivateProvider()).Returns(new OASISResult<bool>(true));
        providers.RegisterProvider(primary.Object); providers.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        var manager = new HolonManager(null, dna, providers);
        if (typed)
        {
            var result = sync ? key ? manager.DeleteHolon<Holon>(nativeKey, actor, false) : manager.DeleteHolon<Holon>(id, actor, false)
                : key ? await manager.DeleteHolonAsync<Holon>(nativeKey, actor, false) : await manager.DeleteHolonAsync<Holon>(id, actor, false);
            result.IsError.Should().BeFalse(result.Message); result.IsDeleted.Should().BeTrue(); result.Result.Id.Should().Be(id);
        }
        else
        {
            var result = sync ? key ? manager.DeleteHolon(nativeKey, actor, false) : manager.DeleteHolon(id, actor, false)
                : key ? await manager.DeleteHolonAsync(nativeKey, actor, false) : await manager.DeleteHolonAsync(id, actor, false);
            result.IsError.Should().BeFalse(result.Message); result.IsDeleted.Should().BeTrue(); result.Result.Id.Should().Be(id);
        }
        primary.Invocations.Should().NotContain(x => x.Method.Name.StartsWith("LoadHolon"));
        primary.Invocations.Count(x => x.Method.Name == "DeleteHolon" || x.Method.Name == "DeleteHolonAsync").Should().Be(1);
    }
}
