using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(-1)] [InlineData(3)]
    public async Task KarmaHistoryRejectsInvalidPersistedDateKind(int invalidKind)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id=Guid.NewGuid();
        (await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username="history-corruption",
            KarmaAkashicRecords=new List<IKarmaAkashicRecord>{HistoryRecord(id,1,"kind")}})).IsError.Should().BeFalse();
        using(var connection=new SqliteConnection(TestConnectionString))
        {
            await connection.OpenAsync();using var command=connection.CreateCommand();
            command.CommandText="UPDATE KarmaAkashicRecord SET DateKind=$kind";command.Parameters.AddWithValue("$kind",invalidKind);
            await command.ExecuteNonQueryAsync();
        }
        var loaded=await provider.LoadAvatarDetailAsync(id);
        loaded.IsError.Should().BeTrue();
        loaded.Exception.Should().NotBeNull();
    }

    [Fact]
    public async Task KarmaHistoryAdditiveUpgradePreservesLegacyValuesAndOrder()
    {
        var id=Guid.NewGuid();var first=HistoryRecord(id,2,"legacy-first");var second=HistoryRecord(id,1,"legacy-second");
        first.Date=DateTime.SpecifyKind(first.Date,DateTimeKind.Unspecified);
        second.Date=DateTime.SpecifyKind(second.Date,DateTimeKind.Unspecified);
        using(var initial=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await initial.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await initial.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username="history-legacy",
                KarmaAkashicRecords=new List<IKarmaAkashicRecord>{first,second}})).IsError.Should().BeFalse();
        }
        using(var connection=new SqliteConnection(TestConnectionString))
        {
            await connection.OpenAsync();
            foreach(var column in new[]{"SequenceIndex","DateKind","HasKarmaSource","HasKarmaEarntOrLost","HasKarmaTypePositive","HasKarmaTypeNegative","HasProvider"})
            {
                using var command=connection.CreateCommand();command.CommandText="ALTER TABLE KarmaAkashicRecord DROP COLUMN \""+column+"\"";
                await command.ExecuteNonQueryAsync();
            }
        }
        using var upgraded=new SQLLiteDBOASIS(TestConnectionString);
        (await upgraded.ActivateProviderAsync()).IsError.Should().BeFalse();
        var loaded=await upgraded.LoadAvatarDetailAsync(id);
        loaded.IsError.Should().BeFalse(loaded.Message+loaded.Exception);
        loaded.Result.KarmaAkashicRecords.Should().BeEquivalentTo(new[]{first,second},options=>options.WithStrictOrdering());
        loaded.Result.KarmaAkashicRecords.Select(record=>record.Date.Kind).Should().OnlyContain(kind=>kind==DateTimeKind.Unspecified);
    }

    [Theory]
    [InlineData(false,DateTimeKind.Unspecified)] [InlineData(true,DateTimeKind.Unspecified)]
    [InlineData(false,DateTimeKind.Utc)] [InlineData(true,DateTimeKind.Utc)]
    [InlineData(false,DateTimeKind.Local)] [InlineData(true,DateTimeKind.Local)]
    public async Task KarmaHistoryPreservesExplicitOrderDateKindAndNullableEnums(bool asynchronous,DateTimeKind kind)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var detail=new AvatarDetail{Id=Guid.NewGuid(),Username="history-fidelity"};
        var present=HistoryRecord(detail.Id,2,"present");
        present.Date=DateTime.SpecifyKind(present.Date,kind);
        var absent=HistoryRecord(detail.Id,1,"absent");
        absent.Date=DateTime.SpecifyKind(absent.Date,kind);
        absent.KarmaSource=null!;absent.KarmaEarntOrLost=null!;absent.KarmaTypePositive=null!;absent.KarmaTypeNegative=null!;absent.Provider=null!;
        detail.KarmaAkashicRecords=new List<IKarmaAkashicRecord>{present,absent,present};
        var saved=asynchronous?await provider.SaveAvatarDetailAsync(detail):provider.SaveAvatarDetail(detail);
        saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        // Physical identities are not the sequence contract.
        using(var connection=new SqliteConnection(TestConnectionString))
        {
            await connection.OpenAsync();using var command=connection.CreateCommand();
            command.CommandText="UPDATE KarmaAkashicRecord SET Id=-Id";
            await command.ExecuteNonQueryAsync();
        }
        // The external SQL rewrite invalidated the writer's tracked physical identities.
        using var reader=new SQLLiteDBOASIS(TestConnectionString);
        (await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var loaded=await reader.LoadAvatarDetailAsync(detail.Id);
        loaded.IsError.Should().BeFalse(loaded.Message+loaded.Exception);
        loaded.Result.KarmaAkashicRecords.Should().BeEquivalentTo(detail.KarmaAkashicRecords,options=>options.WithStrictOrdering());
        loaded.Result.KarmaAkashicRecords.Select(record=>record.Date.Kind).Should().OnlyContain(value=>value==kind);
        loaded.Result.KarmaAkashicRecords.Select(record=>record.Date.Ticks).Should().Equal(present.Date.Ticks,absent.Date.Ticks,present.Date.Ticks);
        loaded.Result.KarmaAkashicRecords=new List<IKarmaAkashicRecord>{absent,present};
        var updated=asynchronous?await reader.SaveAvatarDetailAsync(loaded.Result):reader.SaveAvatarDetail(loaded.Result);
        updated.IsError.Should().BeFalse(updated.Message+updated.Exception);
        (await reader.LoadAvatarDetailAsync(detail.Id)).Result.KarmaAkashicRecords
            .Should().BeEquivalentTo(loaded.Result.KarmaAkashicRecords,options=>options.WithStrictOrdering());
    }

    [Theory]
    [InlineData("plain")] [InlineData("kind")] [InlineData("absent")] [InlineData("sequence")]
    public async Task KarmaHistoryMigrationGuardsLossyDowngrade(string representation)
    {
        using(var initial=new DataContext(TestConnectionString))await initial.Database.MigrateAsync();
        using(var provider=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var detail=new AvatarDetail{Id=Guid.NewGuid(),Username="history-migration"};
            var record=HistoryRecord(detail.Id,1,"migration");
            record.Date=DateTime.SpecifyKind(record.Date,representation=="kind"?DateTimeKind.Local:DateTimeKind.Unspecified);
            if(representation=="absent")record.Provider=null!;
            detail.KarmaAkashicRecords=new List<IKarmaAkashicRecord>{record};
            if(representation=="sequence")detail.KarmaAkashicRecords.Add(record);
            (await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
        }
        using var context=new DataContext(TestConnectionString);var migrator=context.GetService<IMigrator>();
        const string previous="20261020060000_AddAvatarEnumPresence";
        const string current="20261020070000_AddKarmaHistoryFidelity";
        if(representation!="plain")
        {
            Func<Task> down=()=>migrator.MigrateAsync(previous);
            await down.Should().ThrowAsync<SqliteException>();
            (await context.Database.GetAppliedMigrationsAsync()).Should().Contain(current);
        }
        else
        {
            await migrator.MigrateAsync(previous);
            (await context.Database.GetAppliedMigrationsAsync()).Should().NotContain(current);
            await migrator.MigrateAsync();
        }
    }

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
