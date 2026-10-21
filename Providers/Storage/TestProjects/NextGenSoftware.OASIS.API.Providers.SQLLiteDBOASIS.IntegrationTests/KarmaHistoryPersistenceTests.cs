using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private static KarmaAkashicRecord HistoryRecord(Guid owner,int amount,string title)
        =>new(){AvatarId=owner,Date=new DateTime(2026,10,20,12,0,0,DateTimeKind.Utc).AddTicks(amount),Karma=amount,TotalKarma=100+amount,
            KarmaSourceTitle=title,KarmaSourceDesc="owned history",WebLink="https://example.test/karma",
            KarmaSource=new EnumValue<KarmaSourceType>(System.Enum.GetValues<KarmaSourceType>()[0]),
            KarmaEarntOrLost=new EnumValue<KarmaEarntOrLost>(System.Enum.GetValues<KarmaEarntOrLost>()[0]),
            KarmaTypePositive=new EnumValue<KarmaTypePositive>(System.Enum.GetValues<KarmaTypePositive>()[0]),
            KarmaTypeNegative=new EnumValue<KarmaTypeNegative>(System.Enum.GetValues<KarmaTypeNegative>()[0]),
            Provider=new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};

    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task KarmaHistoryPersistsInCreationAndUpdatesAndIsReturnedByAcknowledgement(bool asynchronous,bool existing)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var detail=new AvatarDetail{Id=Guid.NewGuid(),Username="owned-karma-history",Email="history@example.test"};
        if(existing)(await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
        var first=HistoryRecord(detail.Id,2,"first");var second=HistoryRecord(detail.Id,1,"second");
        detail.KarmaAkashicRecords=new List<IKarmaAkashicRecord>{first,second,first};
        var saved=asynchronous?await provider.SaveAvatarDetailAsync(detail):provider.SaveAvatarDetail(detail);
        saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        saved.Result.KarmaAkashicRecords.Select(record=>record.KarmaSourceTitle).Should().Equal("first","second","first");
        var loaded=await provider.LoadAvatarDetailAsync(detail.Id);
        loaded.IsError.Should().BeFalse(loaded.Message+loaded.Exception);
        loaded.Result.KarmaAkashicRecords.Should().BeEquivalentTo(detail.KarmaAkashicRecords,options=>options.WithStrictOrdering());
        loaded.Result.KarmaAkashicRecords=new List<IKarmaAkashicRecord>{second};
        var replaced=asynchronous?await provider.SaveAvatarDetailAsync(loaded.Result):provider.SaveAvatarDetail(loaded.Result);
        replaced.IsError.Should().BeFalse(replaced.Message+replaced.Exception);
        var final=await provider.LoadAvatarDetailAsync(detail.Id);
        final.Result.KarmaAkashicRecords.Should().ContainSingle().Which.KarmaSourceTitle.Should().Be("second");
        final.Result.KarmaAkashicRecords.Clear();
        var cleared=asynchronous?await provider.SaveAvatarDetailAsync(final.Result):provider.SaveAvatarDetail(final.Result);
        cleared.IsError.Should().BeFalse(cleared.Message+cleared.Exception);
        (await provider.LoadAvatarDetailAsync(detail.Id)).Result.KarmaAkashicRecords.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task KarmaHistoryRejectsForeignOwnerWithoutChangingStoredTotals(bool asynchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var detail=new AvatarDetail{Id=Guid.NewGuid(),Username="history-owner",Karma=10};
        (await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
        detail.Karma=99;
        detail.KarmaAkashicRecords=new List<IKarmaAkashicRecord>{HistoryRecord(Guid.NewGuid(),1,"foreign")};
        var rejected=asynchronous?await provider.SaveAvatarDetailAsync(detail):provider.SaveAvatarDetail(detail);
        rejected.IsError.Should().BeTrue();
        var loaded=await provider.LoadAvatarDetailAsync(detail.Id);
        loaded.IsError.Should().BeFalse();
        loaded.Result.Karma.Should().Be(10);
        loaded.Result.KarmaAkashicRecords.Should().BeEmpty();
    }
}
