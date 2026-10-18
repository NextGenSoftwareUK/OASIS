using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ExistingAvatarSchemaGainsWalletColumnWithoutChangingProfile(bool synchronous)
    {
        var id=Guid.NewGuid();
        using(var provider=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveAvatarAsync(new Avatar {Id=id,Username="owned-upgrade",Email="upgrade@example.invalid",FirstName="retained",
                IsActive=true,AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),MetaData=new() {{"marker","retained"}}})).IsError.Should().BeFalse();
        }
        using(var control=new DataContext(TestConnectionString))
            await control.Database.ExecuteSqlRawAsync("ALTER TABLE Avatar DROP COLUMN ProviderWalletsJson");
        using(var provider=new SQLLiteDBOASIS(TestConnectionString))
        {
            var active=synchronous ? provider.ActivateProvider():await provider.ActivateProviderAsync();active.IsError.Should().BeFalse(active.Message);
            var loaded=synchronous ? provider.LoadAvatar(id):await provider.LoadAvatarAsync(id);
            loaded.IsError.Should().BeFalse(loaded.Message);loaded.Result.Username.Should().Be("owned-upgrade");
            loaded.Result.Email.Should().Be("upgrade@example.invalid");loaded.Result.FirstName.Should().Be("retained");
            loaded.Result.MetaData["marker"].ToString().Should().Be("retained");loaded.Result.ProviderWallets.Should().BeEmpty();
            loaded.Result.ProviderWallets[ProviderType.SolanaOASIS]=new() {new ProviderWallet {Id=Guid.NewGuid(),PublicKey="owned-upgraded-key"}};
            (synchronous ? provider.SaveAvatar(loaded.Result):await provider.SaveAvatarAsync(loaded.Result)).IsError.Should().BeFalse();
            (synchronous ? provider.ActivateProvider():await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.LoadAvatarByPublicKeyAsync("owned-upgraded-key")).Result.Id.Should().Be(id);
        }
    }

    [Fact]
    public async Task WalletColumnUpgradeRollsBackWhenLaterSchemaValidationFails()
    {
        using(var provider=new SQLLiteDBOASIS(TestConnectionString))
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))
        {
            await control.Database.ExecuteSqlRawAsync("ALTER TABLE Avatar DROP COLUMN ProviderWalletsJson");
            await control.Database.ExecuteSqlRawAsync("DROP TABLE HeartRateEntry");
        }
        using(var provider=new SQLLiteDBOASIS(TestConnectionString))
        {
            var active=await provider.ActivateProviderAsync();active.IsError.Should().BeTrue();provider.IsProviderActivated.Should().BeFalse();
        }
        using(var control=new DataContext(TestConnectionString))
        {
            await control.Database.OpenConnectionAsync();
            using var command=control.Database.GetDbConnection().CreateCommand();
            command.CommandText="SELECT count(*) FROM pragma_table_info('Avatar') WHERE name='ProviderWalletsJson'";
            Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(0);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AvatarWalletsSurviveReopenAndEditAndCorruptionIsNotAbsence(bool synchronous)
    {
        var id=Guid.NewGuid();var walletId=Guid.NewGuid();
        var avatar=new Avatar {Id=id,Username="owned-wallet",IsActive=true,AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),
            ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {new ProviderWallet {Id=walletId,PublicKey="owned-public",PrivateKey="owned-private"}}}}};
        using(var provider=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (synchronous ? provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        }
        using(var provider=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var loaded=synchronous ? provider.LoadAvatar(id):await provider.LoadAvatarAsync(id);
            loaded.IsError.Should().BeFalse(loaded.Message);
            var wallet=loaded.Result.ProviderWallets[ProviderType.SolanaOASIS].Single();
            wallet.Id.Should().Be(walletId);wallet.PublicKey.Should().Be("owned-public");wallet.PrivateKey.Should().Be("owned-private");
            wallet.PublicKey="owned-edited-public";wallet.PrivateKey="owned-edited-private";
            (synchronous ? provider.SaveAvatar(loaded.Result):await provider.SaveAvatarAsync(loaded.Result)).IsError.Should().BeFalse();
        }
        using(var provider=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var old=synchronous ? provider.LoadAvatarByPublicKey("owned-public"):await provider.LoadAvatarByPublicKeyAsync("owned-public");
            old.ErrorCode.Should().Be(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound);
            var edited=synchronous ? provider.LoadAvatarByPrivateKey("owned-edited-private"):await provider.LoadAvatarByPrivateKeyAsync("owned-edited-private");
            edited.IsError.Should().BeFalse(edited.Message);edited.Result.Id.Should().Be(id);
            using(var control=new DataContext(TestConnectionString))
                await control.Database.ExecuteSqlInterpolatedAsync($"UPDATE Avatar SET ProviderWalletsJson={"{invalid"} WHERE Id={id.ToString()}");
            var corrupt=synchronous ? provider.LoadAvatarByPublicKey("owned-edited-public"):await provider.LoadAvatarByPublicKeyAsync("owned-edited-public");
            corrupt.IsError.Should().BeTrue();corrupt.Result.Should().BeNull();corrupt.Exception.Should().BeOfType<System.IO.InvalidDataException>();
            corrupt.Exception.InnerException.Should().BeAssignableTo<Newtonsoft.Json.JsonException>();
        }
    }
}
