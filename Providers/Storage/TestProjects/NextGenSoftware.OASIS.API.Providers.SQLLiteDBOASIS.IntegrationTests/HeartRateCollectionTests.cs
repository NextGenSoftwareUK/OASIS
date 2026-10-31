using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LegacyHeartRateSequenceUpgradePreservesExistingSample(bool synchronous)
    {
        var record=new AvatarDetail {Id=Guid.NewGuid(),Username="legacy-heart",HeartRateData=new List<IHeartRateEntry> {
            new CustomHeartRate {HeartRateValue=73,TimeStamp=new DateTime(2026,10,16,1,0,0,DateTimeKind.Utc)}}};
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveAvatarDetailAsync(record)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString)) control.Database.ExecuteSqlRaw("ALTER TABLE HeartRateEntry DROP COLUMN SequenceIndex");
        (synchronous ? provider.ActivateProvider():await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var sample=(await provider.LoadAvatarDetailAsync(record.Id)).Result.HeartRateData.Should().ContainSingle().Subject;
        sample.HeartRateValue.Should().Be(73);sample.TimeStamp.Ticks.Should().Be(record.HeartRateData[0].TimeStamp.Ticks);
    }

    private sealed class CustomHeartRate : IHeartRateEntry
    {
        public int HeartRateValue {get;set;}
        public DateTime TimeStamp {get;set;}
    }

    [Theory]
    [InlineData(true,false)]
    [InlineData(false,false)]
    [InlineData(true,true)]
    [InlineData(false,true)]
    public async Task OrderedHeartRateCollectionSurvivesUpdatesEmptyAndRecreation(bool synchronous,bool startEmpty)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var record=new AvatarDetail {Id=Guid.NewGuid(),Username="heart-rate-fixture",Email="heart@test.invalid",Karma=37,XP=43};
        var instant=new DateTime(2026,10,16,1,0,0,DateTimeKind.Utc).AddTicks(7);
        foreach(var phase in startEmpty ? new[] {0,1,2,0,3}:new[] {1,2,0,3})
        {
            record.HeartRateData=phase==0 ? new List<IHeartRateEntry>():new List<IHeartRateEntry> {
                new CustomHeartRate {HeartRateValue=70+phase,TimeStamp=instant.AddMinutes(2)},
                new CustomHeartRate {HeartRateValue=80+phase,TimeStamp=instant},
                new CustomHeartRate {HeartRateValue=90+phase,TimeStamp=instant}};
            var result=synchronous ? provider.SaveAvatarDetail(record):await provider.SaveAvatarDetailAsync(record);
            result.IsError.Should().BeFalse(result.Message);
            using var reader=new SQLLiteDBOASIS(TestConnectionString);
            (await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
            var loaded=synchronous ? reader.LoadAvatarDetail(record.Id):await reader.LoadAvatarDetailAsync(record.Id);
            loaded.IsError.Should().BeFalse(loaded.Message);loaded.Result.Karma.Should().Be(37);loaded.Result.XP.Should().Be(43);
            loaded.Result.HeartRateData.Should().NotBeNull();
            loaded.Result.HeartRateData.Select(x=>x.HeartRateValue).Should().Equal(record.HeartRateData.Select(x=>x.HeartRateValue));
            loaded.Result.HeartRateData.Select(x=>x.TimeStamp.Ticks).Should().Equal(record.HeartRateData.Select(x=>x.TimeStamp.Ticks));
        }
    }
}
