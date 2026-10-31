using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public sealed class NearSelectorFailureTests
{
    private sealed class FailedSelector(string code) : NEAROASIS("http://127.0.0.1:1", "localnet", "localnet", "contract.local")
    {
        public int Reads;
        public readonly IOException Cause=new("owned selector failure");
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email,int version=0)=>Failure();
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username,int version=0)=>Failure();
        private Task<OASISResult<IAvatar>> Failure()
        {
            Reads++;
            return Task.FromResult(new OASISResult<IAvatar>{IsError=true,ErrorCode=code,Exception=Cause,
                Message="owned selector message",DetailedMessage="owned selector detail",ErrorCount=1});
        }
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach(var operation in Enumerable.Range(0,4))
        foreach(var synchronous in new[]{false,true})
        foreach(var code in new[]{OASISResultErrorCodes.RecordNotFound,OASISResultErrorCodes.AmbiguousIdentity})
            yield return new object[]{operation,synchronous,code};
    }

    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task ExportAndDeletePreserveFailedIdentitySelection(int operation,bool synchronous,string code)
    {
        var provider=new FailedSelector(code);
        void Check<T>(OASISResult<T> result)
        {
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);
            Assert.AreEqual(code,result.ErrorCode);Assert.AreSame(provider.Cause,result.Exception);
            Assert.AreEqual("owned selector message",result.Message);Assert.AreEqual("owned selector detail",result.DetailedMessage);
            Assert.AreEqual(1,result.ErrorCount);Assert.AreEqual(1,provider.Reads);
        }
        switch(operation){
            case 0:Check(synchronous?provider.ExportAllDataForAvatarByEmail("owned"):await provider.ExportAllDataForAvatarByEmailAsync("owned"));break;
            case 1:Check(synchronous?provider.ExportAllDataForAvatarByUsername("owned"):await provider.ExportAllDataForAvatarByUsernameAsync("owned"));break;
            case 2:Check(synchronous?provider.DeleteAvatarByEmail("owned"):await provider.DeleteAvatarByEmailAsync("owned"));break;
            default:Check(synchronous?provider.DeleteAvatarByUsername("owned"):await provider.DeleteAvatarByUsernameAsync("owned"));break;
        }
    }
}
