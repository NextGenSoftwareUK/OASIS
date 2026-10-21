using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AvatarEnumPresenceMigrationRefusesLossyDowngrade(bool absent)
    {
        using(var initial=new DataContext(TestConnectionString))await initial.Database.MigrateAsync();
        using(var provider=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var avatar=new Avatar{Id=Guid.NewGuid(),Username="presence-migration",Email="presence-migration@example.test",
                AvatarType=absent?null!:new EnumValue<AvatarType>(AvatarType.Wizard),
                CreatedProviderType=new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS),
                CreatedOASISType=new EnumValue<OASISType>(System.Enum.GetValues<OASISType>()[0])};
            (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        }
        using var context=new DataContext(TestConnectionString);var migrator=context.GetService<IMigrator>();
        const string previous="20261020050000_AddCosmicWritePreparation";
        const string current="20261020060000_AddAvatarEnumPresence";
        if(absent)
        {
            Func<Task> down=()=>migrator.MigrateAsync(previous);
            await down.Should().ThrowAsync<SqliteException>();
            (await context.Database.GetAppliedMigrationsAsync()).Should().Contain(current);
            context.Avatars.AsNoTracking().Single().HasAvatarType.Should().BeFalse();
        }
        else
        {
            await migrator.MigrateAsync(previous);
            (await context.Database.GetAppliedMigrationsAsync()).Should().NotContain(current);
            await migrator.MigrateAsync();
            context.Avatars.AsNoTracking().Single().HasAvatarType.Should().BeTrue();
        }
    }

    [Fact]
    public void AvatarEntityProjectionPreservesAllOptionalEnumWrappers()
    {
        var avatar=new Avatar{Id=Guid.NewGuid(),AvatarType=null!,CreatedProviderType=null!,CreatedOASISType=null!};
        var roundTrip=new Entities.AvatarModel(avatar).GetAvatar();
        roundTrip.AvatarType.Should().BeNull();roundTrip.CreatedProviderType.Should().BeNull();roundTrip.CreatedOASISType.Should().BeNull();
    }

    [Fact]
    public async Task AvatarEnumPresenceUpgradePreservesExistingDefinedZeroAndNonzeroValues()
    {
        var id=Guid.NewGuid();
        using(var initial=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await initial.ActivateProviderAsync()).IsError.Should().BeFalse();
            var saved=await initial.SaveAvatarAsync(new Avatar{Id=id,Username="legacy-enums",Email="legacy-enums@example.test",
                AvatarType=new EnumValue<AvatarType>(AvatarType.Wizard),
                CreatedProviderType=new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS),
                CreatedOASISType=new EnumValue<OASISType>(System.Enum.GetValues<OASISType>()[0])});
            saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        }
        using(var native=new SqliteConnection(TestConnectionString))
        {
            await native.OpenAsync();
            foreach(var column in new[]{"HasAvatarType","HasCreatedProviderType","HasCreatedOASISType"})
            {
                using var command=native.CreateCommand();command.CommandText="ALTER TABLE Avatar DROP COLUMN \""+column+"\"";
                await command.ExecuteNonQueryAsync();
            }
        }
        using var upgraded=new SQLLiteDBOASIS(TestConnectionString);
        (await upgraded.ActivateProviderAsync()).IsError.Should().BeFalse();
        var loaded=await upgraded.LoadAvatarAsync(id);loaded.IsError.Should().BeFalse(loaded.Message+loaded.Exception);
        loaded.Result.AvatarType.Value.Should().Be(AvatarType.Wizard);
        loaded.Result.CreatedProviderType.Value.Should().Be(ProviderType.SQLLiteDBOASIS);
        loaded.Result.CreatedOASISType.Value.Should().Be(System.Enum.GetValues<OASISType>()[0]);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AvatarOptionalEnumPresenceSurvivesSaveReloadAndUpdates(bool asynchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatar=new Avatar{Id=Guid.NewGuid(),Username="optional-avatar",Email="optional-avatar@example.test",
            AvatarType=null!,CreatedProviderType=null!,CreatedOASISType=null!};
        var saved=asynchronous?await provider.SaveAvatarAsync(avatar):provider.SaveAvatar(avatar);
        saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        var loaded=await provider.LoadAvatarAsync(avatar.Id);
        loaded.IsError.Should().BeFalse(loaded.Message+loaded.Exception);
        loaded.Result.AvatarType.Should().BeNull();loaded.Result.CreatedProviderType.Should().BeNull();loaded.Result.CreatedOASISType.Should().BeNull();
        var value=System.Enum.GetValues<AvatarType>().Last();
        loaded.Result.AvatarType=new EnumValue<AvatarType>(value);
        var updated=asynchronous?await provider.SaveAvatarAsync(loaded.Result):provider.SaveAvatar(loaded.Result);
        updated.IsError.Should().BeFalse(updated.Message+updated.Exception);
        var defined=await provider.LoadAvatarAsync(avatar.Id);defined.Result.AvatarType.Value.Should().Be(value);
        defined.Result.AvatarType=null!;
        var cleared=asynchronous?await provider.SaveAvatarAsync(defined.Result):provider.SaveAvatar(defined.Result);
        cleared.IsError.Should().BeFalse(cleared.Message+cleared.Exception);
        var final=await provider.LoadAvatarAsync(avatar.Id);
        final.Result.AvatarType.Should().BeNull();final.Result.CreatedProviderType.Should().BeNull();final.Result.CreatedOASISType.Should().BeNull();
    }
}
