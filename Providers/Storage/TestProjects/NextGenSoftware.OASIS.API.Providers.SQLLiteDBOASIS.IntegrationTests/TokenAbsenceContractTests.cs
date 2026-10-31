using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MissingAndDeletedTokenSelectorsReturnExplicitNeutralAbsence(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        IOASISStorageProvider storage=provider;
        var token="owned-missing-"+Guid.NewGuid().ToString("N");
        var avatar=new Avatar {Id=Guid.NewGuid(),Username=token,IsActive=true,VerificationToken=token,ResetToken=token,
            AvatarType=new NextGenSoftware.Utilities.EnumValue<NextGenSoftware.OASIS.API.Core.Enums.AvatarType>(NextGenSoftware.OASIS.API.Core.Enums.AvatarType.User),
            RefreshTokens=new() {new RefreshToken {Token=token}}};
        var saved=await provider.SaveAvatarAsync(avatar);saved.IsError.Should().BeFalse(saved.Message);
        (await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=avatar.Id,IsActive=true})).IsError.Should().BeFalse();
        var deleted=synchronous ? provider.DeleteAvatar(avatar.Id,true):await provider.DeleteAvatarAsync(avatar.Id,true);
        deleted.IsError.Should().BeFalse(deleted.Message);deleted.Result.Should().BeTrue();
        foreach(var selector in new[] {token,token+"-missing"})
        {
        var results=new[] {
            synchronous ? storage.LoadAvatarByVerificationToken(selector):await storage.LoadAvatarByVerificationTokenAsync(selector),
            synchronous ? storage.LoadAvatarByResetToken(selector):await storage.LoadAvatarByResetTokenAsync(selector),
            synchronous ? storage.LoadAvatarByRefreshToken(selector):await storage.LoadAvatarByRefreshTokenAsync(selector)};
        foreach(var result in results)
        {
            result.ErrorCode.Should().Be(OASISResultErrorCodes.RecordNotFound);
            result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();
            result.IsError.Should().BeFalse();result.Exception.Should().BeNull();
        }
        }
        foreach(var invalid in new[] {(Value:" ",Version:0),(Value:token,Version:-1)})
        {
            var results=new[] {
                synchronous ? storage.LoadAvatarByVerificationToken(invalid.Value,invalid.Version):await storage.LoadAvatarByVerificationTokenAsync(invalid.Value,invalid.Version),
                synchronous ? storage.LoadAvatarByResetToken(invalid.Value,invalid.Version):await storage.LoadAvatarByResetTokenAsync(invalid.Value,invalid.Version),
                synchronous ? storage.LoadAvatarByRefreshToken(invalid.Value,invalid.Version):await storage.LoadAvatarByRefreshTokenAsync(invalid.Value,invalid.Version)};
            foreach(var result in results)
            {
                result.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");result.IsError.Should().BeTrue();
                result.Exception.Should().BeAssignableTo<ArgumentException>();result.Result.Should().BeNull();
            }
        }
    }
}
