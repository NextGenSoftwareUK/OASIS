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
