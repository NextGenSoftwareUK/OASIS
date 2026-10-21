using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    // Historical migration fixtures predate kind companions. Preserve their
    // clock ticks while explicitly constructing the old Unspecified representation.
    private async Task SeedLegacyHolonAuditRepresentationAsync()
    {
        using var context=new DataContext(TestConnectionString);
        foreach(var row in await context.Holons.ToListAsync())
        {
            row.CreatedDate=DateTime.SpecifyKind(row.CreatedDate,DateTimeKind.Unspecified);
            row.ModifiedDate=DateTime.SpecifyKind(row.ModifiedDate,DateTimeKind.Unspecified);
            row.DeletedDate=DateTime.SpecifyKind(row.DeletedDate,DateTimeKind.Unspecified);
        }
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task AuditDateKindAdditiveUpgradeKeepsLegacyTicksWithoutInventingTimezone()
    {
        var id=Guid.NewGuid();var stamp=new DateTime(2026,10,20,12,30,0,DateTimeKind.Utc).AddTicks(1234567);
        using(var initial=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await initial.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await initial.SaveAvatarAsync(new Avatar{Id=id,Username="legacy-kind",CreatedDate=stamp,ModifiedDate=stamp.AddTicks(1)})).IsError.Should().BeFalse();
        }
        using(var connection=new SqliteConnection(TestConnectionString))
        {
            await connection.OpenAsync();
            foreach(var table in new[]{"Holon","Avatar","AvatarDetail"})
                foreach(var column in new[]{"CreatedDateKind","ModifiedDateKind","DeletedDateKind"})
                {
                    using var command=connection.CreateCommand();command.CommandText="ALTER TABLE \""+table+"\" DROP COLUMN \""+column+"\"";
                    await command.ExecuteNonQueryAsync();
                }
        }
        using var upgraded=new SQLLiteDBOASIS(TestConnectionString);
        (await upgraded.ActivateProviderAsync()).IsError.Should().BeFalse();
        var loaded=await upgraded.LoadAvatarAsync(id);loaded.IsError.Should().BeFalse(loaded.Message+loaded.Exception);
        loaded.Result.CreatedDate.Ticks.Should().Be(stamp.Ticks);loaded.Result.CreatedDate.Kind.Should().Be(DateTimeKind.Unspecified);
        loaded.Result.ModifiedDate.Ticks.Should().Be(stamp.AddTicks(1).Ticks);loaded.Result.ModifiedDate.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AuditDateKindMigrationRejectsLossyDowngrade(bool hasUtcData)
    {
        using(var initial=new DataContext(TestConnectionString))await initial.Database.MigrateAsync();
        if(hasUtcData)
        {
            using var provider=new SQLLiteDBOASIS(TestConnectionString);
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveAvatarAsync(new Avatar{Id=Guid.NewGuid(),Username="kind-migration",CreatedDate=DateTime.UtcNow})).IsError.Should().BeFalse();
        }
        using var context=new DataContext(TestConnectionString);var migrator=context.GetService<IMigrator>();
        const string previous="20261020070000_AddKarmaHistoryFidelity";
        if(hasUtcData)
        {
            Func<Task> down=()=>migrator.MigrateAsync(previous);await down.Should().ThrowAsync<SqliteException>();
            (await context.Database.GetAppliedMigrationsAsync()).Should().Contain("20261020080000_AddAuditDateKinds");
        }
        else{await migrator.MigrateAsync(previous);await migrator.MigrateAsync();}
    }

    [Theory]
    [InlineData("CreatedDateKind",-1)] [InlineData("CreatedDateKind",3)]
    [InlineData("ModifiedDateKind",-1)] [InlineData("ModifiedDateKind",3)]
    [InlineData("DeletedDateKind",-1)] [InlineData("DeletedDateKind",3)]
    public async Task AuditDateKindCorruptionRejectsRead(string column,int invalid)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id=Guid.NewGuid();(await provider.SaveAvatarAsync(new Avatar{Id=id,Username="kind-invalid"})).IsError.Should().BeFalse();
        using(var connection=new SqliteConnection(TestConnectionString))
        {
            await connection.OpenAsync();using var command=connection.CreateCommand();
            command.CommandText="UPDATE Avatar SET \""+column+"\"=$kind";command.Parameters.AddWithValue("$kind",invalid);
            await command.ExecuteNonQueryAsync();
        }
        var loaded=await provider.LoadAvatarAsync(id);loaded.IsError.Should().BeTrue();loaded.Exception.Should().NotBeNull();
    }

    public static IEnumerable<object[]> AuditDateKindCases =>
        from entity in new[]{"holon","avatar","detail"}
        from asynchronous in new[]{false,true}
        from kind in new[]{DateTimeKind.Unspecified,DateTimeKind.Utc,DateTimeKind.Local}
        select new object[]{entity,asynchronous,kind};

    [Theory]
    [MemberData(nameof(AuditDateKindCases))]
    public async Task AuditTimestampAcknowledgementsEqualPersistedValuesAndKinds(string entity,bool asynchronous,DateTimeKind kind)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id=Guid.NewGuid();
        IHolonBase input=entity switch{
            "avatar"=>new Avatar{Id=id,Username="audit-kinds",Email="audit-kinds@example.test"},
            "detail"=>new AvatarDetail{Id=id,Username="audit-kinds",Email="audit-kinds@example.test"},
            _=>new Holon{Id=id}};
        var stamp=new DateTime(2026,10,20,12,30,0,kind).AddTicks(1234567);
        input.CreatedDate=stamp;input.ModifiedDate=stamp.AddTicks(1);
        input.DeletedDate=DateTime.SpecifyKind(stamp.AddTicks(2),DateTimeKind.Utc);
        input.DeletedByAvatarId=Guid.NewGuid();input.IsActive=false;
        async Task<IHolonBase> Save(IHolonBase value)
        {
            if(value is IAvatar avatar)
            {
                var result=asynchronous?await provider.SaveAvatarAsync(avatar):provider.SaveAvatar(avatar);
                result.IsError.Should().BeFalse(result.Message+result.Exception);return result.Result;
            }
            if(value is IAvatarDetail detail)
            {
                var result=asynchronous?await provider.SaveAvatarDetailAsync(detail):provider.SaveAvatarDetail(detail);
                result.IsError.Should().BeFalse(result.Message+result.Exception);return result.Result;
            }
            var saved=asynchronous?await provider.SaveHolonAsync((IHolon)value,saveChildren:false):provider.SaveHolon((IHolon)value,saveChildren:false);
            saved.IsError.Should().BeFalse(saved.Message+saved.Exception);return saved.Result;
        }
        async Task<IHolonBase> Load()
        {
            if(entity=="avatar")
            {var result=asynchronous?await provider.LoadAvatarAsync(id):provider.LoadAvatar(id);result.IsError.Should().BeFalse(result.Message+result.Exception);return result.Result;}
            if(entity=="detail")
            {var result=asynchronous?await provider.LoadAvatarDetailAsync(id):provider.LoadAvatarDetail(id);result.IsError.Should().BeFalse(result.Message+result.Exception);return result.Result;}
            var loaded=asynchronous?await provider.LoadHolonAsync(id,loadChildren:false):provider.LoadHolon(id,loadChildren:false);
            loaded.IsError.Should().BeFalse(loaded.Message+loaded.Exception);return loaded.Result;
        }
        static void EqualDates(IHolonBase actual,IHolonBase expected)
        {
            actual.CreatedDate.Ticks.Should().Be(expected.CreatedDate.Ticks);actual.CreatedDate.Kind.Should().Be(expected.CreatedDate.Kind);
            actual.ModifiedDate.Ticks.Should().Be(expected.ModifiedDate.Ticks);actual.ModifiedDate.Kind.Should().Be(expected.ModifiedDate.Kind);
            actual.DeletedDate.Ticks.Should().Be(expected.DeletedDate.Ticks);actual.DeletedDate.Kind.Should().Be(expected.DeletedDate.Kind);
        }
        var acknowledged=await Save(input);var reloaded=await Load();EqualDates(reloaded,acknowledged);
        reloaded.ModifiedDate=DateTime.SpecifyKind(stamp.AddTicks(9),kind==DateTimeKind.Utc?DateTimeKind.Local:DateTimeKind.Utc);
        var updated=await Save(reloaded);EqualDates(await Load(),updated);
    }
}
