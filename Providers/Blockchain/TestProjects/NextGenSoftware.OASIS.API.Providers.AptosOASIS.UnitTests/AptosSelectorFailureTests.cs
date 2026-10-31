using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS.UnitTests;

[TestClass]
public sealed class AptosSelectorFailureTests
{
    private sealed class FailedSelector(string code):AptosOASIS("http://127.0.0.1:1/v1","local")
    {
        public int Reads,Deletes;
        public readonly IOException Cause=new("owned selector failure");
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email,int version=0)=>Failure();
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username,int version=0)=>Failure();
        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id,bool softDelete=true)
        {Deletes++;throw new InvalidOperationException("Failed selection must not dispatch deletion.");}
        private Task<OASISResult<IAvatar>> Failure()
        {
            Reads++;return Task.FromResult(new OASISResult<IAvatar>{IsError=true,ErrorCode=code,Exception=Cause,
                Message="owned selector message",DetailedMessage="owned selector detail",ErrorCount=1});
        }
    }
    public static IEnumerable<object[]> Cases()
    {
        foreach(var email in new[]{false,true})foreach(var sync in new[]{false,true})
        foreach(var code in new[]{OASISResultErrorCodes.RecordNotFound,OASISResultErrorCodes.AmbiguousIdentity})
            yield return new object[]{email,sync,code};
    }
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task DeletePreservesSelectorFailureWithoutDispatch(bool email,bool sync,string code)
    {
        using var provider=new FailedSelector(code);
        var result=email?(sync?provider.DeleteAvatarByEmail("owned"):await provider.DeleteAvatarByEmailAsync("owned"))
            :(sync?provider.DeleteAvatarByUsername("owned"):await provider.DeleteAvatarByUsernameAsync("owned"));
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(code,result.ErrorCode);Assert.AreSame(provider.Cause,result.Exception);
        Assert.AreEqual("owned selector message",result.Message);Assert.AreEqual("owned selector detail",result.DetailedMessage);
        Assert.AreEqual(1,result.ErrorCount);Assert.AreEqual(1,provider.Reads);Assert.AreEqual(0,provider.Deletes);
    }
}
