using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.UnitTests;

public class SolidAvatarSoftPairTests
{
    [Theory][InlineData(false,0)][InlineData(true,0)][InlineData(false,1)][InlineData(false,2)]
    public async Task SoftDeleteConditionallyDeactivatesPairOrPreservesPartialOutcome(bool sync,int rejectedWrite)
    {
        var root=new Uri("https://pod.invalid/private/");var key=Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();var id=Guid.NewGuid();
        var transport=new SolidRecordHttpTests.Transport{RequireConditionalWrites=true,RejectWrite=rejectedWrite};
        using var codec=new SolidPrivateRecordCodec(root,key);
        var avatarUri=codec.Resource<IAvatar>(id).AbsoluteUri;var detailUri=codec.Resource<IAvatarDetail>(id).AbsoluteUri;
        transport.Records[avatarUri]=codec.Encode<IAvatar>(new Avatar{Id=id,IsActive=true,Password="retain"});
        transport.Records[detailUri]=codec.Encode<IAvatarDetail>(new AvatarDetail{Id=id,IsActive=true,Description="retain"});
        using var provider=new SOLIDOASIS(root.AbsoluteUri,"",key,transport);Assert.False((await provider.ActivateProviderAsync()).IsError);
        var result=sync?provider.DeleteAvatar(id):await provider.DeleteAvatarAsync(id);
        var avatar=codec.Decode<IAvatar>(transport.Records[avatarUri],id);var detail=codec.Decode<IAvatarDetail>(transport.Records[detailUri],id);
        Assert.Equal("retain",avatar.Password);Assert.Equal("retain",detail.Description);
        Assert.Equal(rejectedWrite!=0,result.IsError);Assert.Equal(rejectedWrite==0,result.IsDeleted);
        if(rejectedWrite==0){
            Assert.True(result.Result);Assert.False(avatar.IsActive);Assert.False(detail.IsActive);
            Assert.NotEqual(DateTime.MinValue,avatar.DeletedDate);Assert.Equal(avatar.DeletedDate,detail.DeletedDate);Assert.Equal(2,transport.Writes);
        }else if(rejectedWrite==1){
            Assert.True(avatar.IsActive);Assert.True(detail.IsActive);Assert.Equal(1,transport.Writes);
            Assert.IsType<HttpRequestException>(result.Exception);
        }else{
            Assert.False(avatar.IsActive);Assert.True(detail.IsActive);Assert.Equal(2,transport.Writes);
            var partial=Assert.IsType<KeyValuePartialMutationException>(result.Exception);
            Assert.Equal(avatarUri,Assert.Single(partial.AcknowledgedKeys));Assert.IsType<HttpRequestException>(partial.InnerException);
        }
    }
}
