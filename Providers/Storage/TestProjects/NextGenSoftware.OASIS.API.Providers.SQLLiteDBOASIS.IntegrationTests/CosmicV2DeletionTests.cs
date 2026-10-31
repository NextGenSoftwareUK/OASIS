using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task V2RootSoftDeleteDoesNotWriteChildrenOrInvalidateTheirTokens(bool synchronous, bool byKey)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CosmicManager(provider, true); var actor = Guid.NewGuid();
        var child = new Holon { Name = "owned deletion child", IsPublic = true };
        var root = new Holon { Name = "owned deletion root", IsPublic = true, Children = new List<IHolon> { child } };
        var seeded = await manager.SaveHolonAsync(root, actor, providerType: ProviderType.SQLLiteDBOASIS);
        seeded.IsError.Should().BeFalse(seeded.Message + seeded.Exception);
        using var control = new DataContext(TestConnectionString);
        var childBefore = control.Holons.AsNoTracking().Single(x => x.Id == child.Id.ToString());
        var rootBefore = control.Holons.AsNoTracking().Single(x => x.Id == root.Id.ToString());
        var key = root.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS];
        var deleted = synchronous
            ? byKey ? manager.DeleteHolon(key, actor, providerType: ProviderType.SQLLiteDBOASIS)
                : manager.DeleteHolon(root.Id, actor, providerType: ProviderType.SQLLiteDBOASIS)
            : byKey ? await manager.DeleteHolonAsync(key, actor, providerType: ProviderType.SQLLiteDBOASIS)
                : await manager.DeleteHolonAsync(root.Id, actor, providerType: ProviderType.SQLLiteDBOASIS);
        deleted.IsError.Should().BeFalse(deleted.Message + deleted.Exception); deleted.IsDeleted.Should().BeTrue();
        var persisted = control.Holons.AsNoTracking().Single(x => x.Id == root.Id.ToString());
        var childAfter = control.Holons.AsNoTracking().Single(x => x.Id == child.Id.ToString());
        persisted.DeletedByAvatarId.Should().Be(actor.ToString()); persisted.DeletedDate.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
        persisted.Version.Should().Be(rootBefore.Version + 1); persisted.PreviousVersionId.Should().Be(rootBefore.VersionId);
        persisted.ProviderRevisionId.Should().NotBe(rootBefore.ProviderRevisionId);
        childAfter.Should().BeEquivalentTo(childBefore);
        root.DeletedByAvatarId.Should().BeEmpty(); child.DeletedByAvatarId.Should().BeEmpty();
        control.Holons.AsNoTracking().Count().Should().Be(2);
    }
}
