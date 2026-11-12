using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData("owner", true)]
    [InlineData("foreign", false)]
    [InlineData("wizard", true)]
    [InlineData("missing", false)]
    [InlineData("empty-wizard", false)]
    public void V2RefreshRevocationChecksAuthoritativeOwnerDespiteRedactedRequest(string actor, bool permitted)
    {
        var provider = CreateActiveProvider();
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.Security = new SecuritySettings { HideRefreshTokens = true };
        var providers = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false, IsAutoReplicationEnabled = false };
        providers.RegisterProvider(provider.Object);
        providers.SetAndActivateCurrentStorageProvider(provider.Object);
        var manager = new AvatarManager(provider.Object, dna, providers);
        var token = new RefreshToken { Token = "controlled-refresh", Expires = DateTime.UtcNow.AddHours(1) };
        var stored = new Avatar { Id = Guid.NewGuid(), RefreshTokens = new() { token },
            Password = "$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy" };
        provider.Setup(value => value.LoadAvatarByRefreshToken("controlled-refresh", 0))
            .Returns(new OASISResult<IAvatar>(stored));
        provider.Setup(value => value.SaveAvatar(It.IsAny<IAvatar>()))
            .Returns((IAvatar value) => new OASISResult<IAvatar>(value) { IsSaved = true });
        IAvatar? request = actor == "missing" ? null : new Avatar {
            Id = actor == "owner" ? stored.Id : actor == "empty-wizard" ? Guid.Empty : Guid.NewGuid(),
            RefreshTokens = null,
            AvatarType = new EnumValue<AvatarType>(actor.Contains("wizard") ? AvatarType.Wizard : AvatarType.User) };
        var result = manager.RevokeToken("controlled-refresh", "127.0.0.1", request!);
        Assert.Equal(!permitted, result.IsError);
        if (permitted)
        {
            Assert.NotNull(token.Revoked);
            provider.Verify(value => value.SaveAvatar(It.Is<IAvatar>(saved => saved.Id == stored.Id)), Times.Once);
        }
        else
        {
            Assert.Equal("REFRESH_TOKEN_FORBIDDEN", result.ErrorCode);
            Assert.Null(token.Revoked);
            Assert.DoesNotContain(provider.Invocations, call => call.Method.Name is "SaveAvatar" or "SaveAvatarAsync");
        }
        if (request != null) Assert.Null(request.RefreshTokens);
    }
}
