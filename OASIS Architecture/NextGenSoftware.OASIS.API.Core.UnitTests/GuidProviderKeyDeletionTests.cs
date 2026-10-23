using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class GuidProviderKeyDeletionTests
{
    public static IEnumerable<object[]> InvalidKeys()
    {
        foreach(var key in new string?[]{null,""," ","owned-user","owned@example.invalid",Guid.Empty.ToString(),"native:opaque"})
            foreach(var soft in new[]{false,true})yield return new object[]{key!,soft};
    }

    [Theory][MemberData(nameof(InvalidKeys))]
    public async Task InvalidKeysNeverInvokeDeletion(string? key,bool soft)
    {
        var calls=0;
        var result=await GuidProviderKeyDeletion.DeleteAvatarAsync(key!,soft,(id,flag)=>{calls++;throw new InvalidOperationException("No invalid key may reach mutation.");});
        Assert.Equal(0,calls);Assert.True(result.IsError);Assert.IsType<ArgumentException>(result.Exception);
        Assert.False(result.Result);Assert.False(result.IsDeleted);Assert.False(result.IsSaved);Assert.Equal(0,result.DeletedCount);
    }

    public static IEnumerable<object[]> ValidKeys()
    {
        var id=Guid.Parse("89092bbe-8ce6-4f6f-aaca-4ef8a8af2fb9");
        foreach(var format in new[]{"N","D","B","P"})foreach(var soft in new[]{false,true})foreach(var error in new[]{false,true})
            yield return new object[]{id.ToString(format),soft,error};
    }

    [Theory][MemberData(nameof(ValidKeys))]
    public async Task ValidKeysPreserveOneOriginalTaskAndResult(string key,bool soft,bool error)
    {
        var expected=new OASISResult<bool>{Result=!error,IsError=error,Message="original acknowledgement",ErrorCode="owned-native-code",Exception=error?new IOException("original outcome"):null};
        var original=Task.FromResult(expected);var calls=0;
        var actual=GuidProviderKeyDeletion.DeleteAvatarAsync(key,soft,(id,flag)=>{calls++;Assert.Equal(Guid.Parse(key),id);Assert.Equal(soft,flag);return original;});
        Assert.Equal(1,calls);Assert.Same(original,actual);Assert.Same(expected,await actual);
    }

    [Fact]
    public async Task NativeFailureIsNotRetriedOrRewrapped()
    {
        var failure=new IOException("original uncertain native outcome");var original=Task.FromException<OASISResult<bool>>(failure);var calls=0;
        var actual=GuidProviderKeyDeletion.DeleteAvatarAsync(Guid.NewGuid().ToString(),false,(id,soft)=>{calls++;return original;});
        Assert.Same(original,actual);Assert.Same(failure,await Assert.ThrowsAsync<IOException>(()=>actual));Assert.Equal(1,calls);
    }

    [Fact]
    public void MissingMutationCallbackIsAProgrammingError()
        =>Assert.Throws<ArgumentNullException>(()=>{_ = GuidProviderKeyDeletion.DeleteAvatarAsync(Guid.NewGuid().ToString(),true,null!);});
}
