using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public class DefaultAvatarValidationTests
{
    public static IEnumerable<object[]> Cases()=>from inherited in new[]{false,true}
        from kind in Enumerable.Range(0,inherited?3:5) from synchronous in new[]{false,true}
        from scenario in new[]{"null","empty","blank","version","historical","maximum"} select new object[]{inherited,kind,synchronous,scenario};

    [Theory][MemberData(nameof(Cases))]
    public async Task InvalidCredentialRequestNeverReadsProvider(bool inherited,int kind,bool synchronous,string scenario)
    {
        string? value=scenario=="null"?null:scenario=="empty"?"":scenario=="blank"?" \t":"owned-selector";
        var version=scenario=="version"?-1:scenario=="historical"?1:scenario=="maximum"?int.MaxValue:0;
        var source=new OASISResult<IEnumerable<IAvatar>>(new[]{new Avatar{Id=Guid.NewGuid(),VerificationToken="owned-selector",ResetToken="owned-selector"}});
        var direct=new Mock<IOASISStorageProvider>{CallBase=true};var based=new Mock<OASISStorageProviderBase>{CallBase=true};
        direct.Setup(p=>p.LoadAllAvatars(It.IsAny<int>())).Returns(source);direct.Setup(p=>p.LoadAllAvatarsAsync(It.IsAny<int>())).ReturnsAsync(source);
        based.Setup(p=>p.LoadAllAvatars(It.IsAny<int>())).Returns(source);based.Setup(p=>p.LoadAllAvatarsAsync(It.IsAny<int>())).ReturnsAsync(source);
        IOASISStorageProvider provider=inherited?based.Object:direct.Object;
        var result=kind switch
        {
            0=>synchronous?provider.LoadAvatarByVerificationToken(value!,version):await provider.LoadAvatarByVerificationTokenAsync(value!,version),
            1=>synchronous?provider.LoadAvatarByResetToken(value!,version):await provider.LoadAvatarByResetTokenAsync(value!,version),
            2=>synchronous?provider.LoadAvatarByRefreshToken(value!,version):await provider.LoadAvatarByRefreshTokenAsync(value!,version),
            3=>synchronous?provider.LoadAvatarByPublicKey(value!,version):await provider.LoadAvatarByPublicKeyAsync(value!,version),
            _=>synchronous?provider.LoadAvatarByPrivateKey(value!,version):await provider.LoadAvatarByPrivateKeyAsync(value!,version)
        };
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.False(result.IsLoaded);Assert.Equal(0,result.LoadedCount);
        Assert.Equal("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.IsAssignableFrom<ArgumentException>(result.Exception);
        direct.Verify(p=>p.LoadAllAvatars(It.IsAny<int>()),Times.Never());direct.Verify(p=>p.LoadAllAvatarsAsync(It.IsAny<int>()),Times.Never());
        based.Verify(p=>p.LoadAllAvatars(It.IsAny<int>()),Times.Never());based.Verify(p=>p.LoadAllAvatarsAsync(It.IsAny<int>()),Times.Never());
    }
}
