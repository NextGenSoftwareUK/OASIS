using System.Net;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidCredentialVersionTests
{
    private sealed class NoAccess:HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {Calls++;throw new InvalidOperationException("Credential history must be refused before HTTP.");}
    }
    public static IEnumerable<object[]> Cases(){foreach(var kind in Enumerable.Range(0,5))foreach(var version in new[]{-1,1,int.MaxValue})foreach(var synchronous in new[]{false,true})yield return new object[]{kind,version,synchronous};}

    [Theory][MemberData(nameof(Cases))]
    public async Task ArchivedCredentialSelectorsFailBeforeAnyHttp(int kind,int version,bool synchronous)
    {
        var handler=new NoAccess();using var provider=new SOLIDOASIS("https://pod.invalid/private/","",new byte[32],handler);
        IOASISStorageProvider contract=provider;
        var result=kind switch{
            0=>synchronous?contract.LoadAvatarByVerificationToken("owned",version):await contract.LoadAvatarByVerificationTokenAsync("owned",version),
            1=>synchronous?contract.LoadAvatarByResetToken("owned",version):await contract.LoadAvatarByResetTokenAsync("owned",version),
            2=>synchronous?contract.LoadAvatarByRefreshToken("owned",version):await contract.LoadAvatarByRefreshTokenAsync("owned",version),
            3=>synchronous?contract.LoadAvatarByPublicKey("owned",version):await contract.LoadAvatarByPublicKeyAsync("owned",version),
            _=>synchronous?contract.LoadAvatarByPrivateKey("owned",version):await contract.LoadAvatarByPrivateKeyAsync("owned",version)
        };
        Assert.True(result.IsError);Assert.False(result.IsLoaded);Assert.Null(result.Result);Assert.IsType<ArgumentOutOfRangeException>(result.Exception);Assert.Equal(0,handler.Calls);
    }
}
