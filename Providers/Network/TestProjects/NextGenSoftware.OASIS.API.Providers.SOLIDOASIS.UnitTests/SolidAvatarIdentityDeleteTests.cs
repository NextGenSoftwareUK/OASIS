using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidAvatarIdentityDeleteTests
{
    [Theory][InlineData(0,false)][InlineData(0,true)][InlineData(1,false)][InlineData(1,true)][InlineData(2,false)][InlineData(2,true)]
    public async Task IdentitySoftDeleteResolvesCanonicalRecordAndRetainsPayload(int route,bool sync)
    {
        var root=new Uri("https://pod.invalid/private/");var key=Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
        var transport=new SolidRecordHttpTests.Transport();var id=Guid.NewGuid();
        using var codec=new SolidPrivateRecordCodec(root,key);
        var uri=codec.Resource<IAvatar>(id).AbsoluteUri;
        transport.Records[uri]=codec.Encode<IAvatar>(new Avatar{Id=id,Username="private-user",Email="user@example.invalid",Password="preserve"});
        using var provider=new SOLIDOASIS(root.AbsoluteUri,"",key,transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=route switch{
            0=>sync?provider.DeleteAvatar(id.ToString("N")):await provider.DeleteAvatarAsync(id.ToString("N")),
            1=>sync?provider.DeleteAvatarByUsername(" PRIVATE-USER "):await provider.DeleteAvatarByUsernameAsync(" PRIVATE-USER "),
            _=>sync?provider.DeleteAvatarByEmail(" USER@example.invalid "):await provider.DeleteAvatarByEmailAsync(" USER@example.invalid ")};
        Assert.False(result.IsError,result.Exception?.ToString());Assert.True(result.Result);Assert.Equal(1,transport.Writes);
        var stored=codec.Decode<IAvatar>(transport.Records[uri],id);
        Assert.NotEqual(DateTime.MinValue,stored.DeletedDate);Assert.Equal("preserve",stored.Password);Assert.Equal(id,stored.Id);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task MissingIdentityDoesNotMutate(bool email)
    {
        var transport=new SolidRecordHttpTests.Transport();var key=Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
        using var provider=new SOLIDOASIS("https://pod.invalid/private/","",key,transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=email?await provider.DeleteAvatarByEmailAsync("absent@example.invalid"):await provider.DeleteAvatarByUsernameAsync("absent");
        Assert.True(result.IsError);Assert.False(result.Result);Assert.Equal(0,transport.Writes);
        Assert.Equal(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
    }
}
