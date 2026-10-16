using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(true,true)] [InlineData(false,true)]
    [InlineData(true,false)] [InlineData(false,false)]
    public async Task LegacyTrackingUpgradePreservesRowsAndExistingTracking(bool synchronous,bool bothMissing)
    {
        var record=new AvatarDetail {Id=Guid.NewGuid(),Username="legacy-tracking",Karma=37,XP=43,
            ActiveQuestId=Guid.NewGuid(),ActiveObjectiveId=Guid.NewGuid()};
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveAvatarDetailAsync(record)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))
        {
            control.Database.ExecuteSqlRaw("ALTER TABLE AvatarDetail DROP COLUMN ActiveObjectiveId");
            if(bothMissing) control.Database.ExecuteSqlRaw("ALTER TABLE AvatarDetail DROP COLUMN ActiveQuestId");
        }
        var activated=synchronous ? provider.ActivateProvider():await provider.ActivateProviderAsync();
        activated.IsError.Should().BeFalse(activated.Message);
        var loaded=await provider.LoadAvatarDetailAsync(record.Id);
        loaded.IsError.Should().BeFalse(loaded.Message);
        loaded.Result.Karma.Should().Be(37);loaded.Result.XP.Should().Be(43);
        loaded.Result.ActiveQuestId.Should().Be(bothMissing ? null:record.ActiveQuestId);
        loaded.Result.ActiveObjectiveId.Should().BeNull();
        record.ActiveQuestId=Guid.NewGuid();record.ActiveObjectiveId=Guid.NewGuid();
        var saved=synchronous ? provider.SaveAvatarDetail(record):await provider.SaveAvatarDetailAsync(record);
        saved.IsError.Should().BeFalse(saved.Message);
        using var reader=new SQLLiteDBOASIS(TestConnectionString);
        (await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var read=(await reader.LoadAvatarDetailAsync(record.Id)).Result;
        read.ActiveQuestId.Should().Be(record.ActiveQuestId);read.ActiveObjectiveId.Should().Be(record.ActiveObjectiveId);
    }
}
