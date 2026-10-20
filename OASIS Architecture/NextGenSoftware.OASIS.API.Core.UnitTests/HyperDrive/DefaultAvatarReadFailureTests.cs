using Moq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public class DefaultAvatarReadFailureTests
{
    public static IEnumerable<object[]> Cases()=>from inherited in new[]{false,true}
        from kind in Enumerable.Range(0,inherited?3:5) from synchronous in new[]{false,true}
        from timeout in new[]{false,true} select new object[]{inherited,kind,synchronous,timeout};

    [Theory][MemberData(nameof(Cases))]
    public async Task CollectionExceptionIsReturnedWithoutRetryOrCauseReplacement(bool inherited,int kind,bool synchronous,bool timeout)
    {
        Exception cause=timeout?new TimeoutException("owned collection deadline"):new IOException("owned collection read failure");
        var direct=new Mock<IOASISStorageProvider>{CallBase=true};var based=new Mock<OASISStorageProviderBase>{CallBase=true};
        direct.Setup(p=>p.LoadAllAvatars(0)).Throws(cause);direct.Setup(p=>p.LoadAllAvatarsAsync(0)).Returns(Task.FromException<OASISResult<IEnumerable<IAvatar>>>(cause));
        based.Setup(p=>p.LoadAllAvatars(0)).Throws(cause);based.Setup(p=>p.LoadAllAvatarsAsync(0)).Returns(Task.FromException<OASISResult<IEnumerable<IAvatar>>>(cause));
        IOASISStorageProvider provider=inherited?based.Object:direct.Object;
        var result=kind switch
        {
            0=>synchronous?provider.LoadAvatarByVerificationToken("owned-selector",0):await provider.LoadAvatarByVerificationTokenAsync("owned-selector",0),
            1=>synchronous?provider.LoadAvatarByResetToken("owned-selector",0):await provider.LoadAvatarByResetTokenAsync("owned-selector",0),
            2=>synchronous?provider.LoadAvatarByRefreshToken("owned-selector",0):await provider.LoadAvatarByRefreshTokenAsync("owned-selector",0),
            3=>synchronous?provider.LoadAvatarByPublicKey("owned-selector",0):await provider.LoadAvatarByPublicKeyAsync("owned-selector",0),
            _=>synchronous?provider.LoadAvatarByPrivateKey("owned-selector",0):await provider.LoadAvatarByPrivateKeyAsync("owned-selector",0)
        };
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.False(result.IsLoaded);Assert.Equal(0,result.LoadedCount);Assert.Same(cause,result.Exception);
        Assert.DoesNotContain("owned-selector",result.Message);
        if(inherited)
        {based.Verify(p=>p.LoadAllAvatars(0),synchronous?Times.Once():Times.Never());based.Verify(p=>p.LoadAllAvatarsAsync(0),synchronous?Times.Never():Times.Once());}
        else
        {direct.Verify(p=>p.LoadAllAvatars(0),synchronous?Times.Once():Times.Never());direct.Verify(p=>p.LoadAllAvatarsAsync(0),synchronous?Times.Never():Times.Once());}
    }
}
