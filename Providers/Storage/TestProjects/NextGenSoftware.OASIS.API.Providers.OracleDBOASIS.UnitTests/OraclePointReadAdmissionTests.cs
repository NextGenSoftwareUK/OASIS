using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OraclePointReadAdmissionTests
{
    public static IEnumerable<object[]> IdentityCases()
    {
        foreach(var selector in Enumerable.Range(0,7))foreach(var sync in new[]{false,true})
        foreach(var version in new[]{0,1})foreach(var invalid in new[]{false,true})yield return new object[]{selector,sync,version,invalid};
    }

    [DataTestMethod][DynamicData(nameof(IdentityCases),DynamicDataSourceType.Method)]
    public async Task EmptyPointSelectorRejectsBeforeOracleConfigurationAccess(int selector,bool sync,int version,bool invalid)
    {
        var provider=new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");
        var id=invalid?Guid.Empty:Guid.NewGuid();var username=invalid?" ":"owned";var email=invalid?"":"owned@example.invalid";
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result)
        {
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
            Assert.IsInstanceOfType<ArgumentException>(result.Exception);
            var cause=(ArgumentException)result.Exception;
            if(invalid)Assert.AreEqual("selector",cause.ParamName,"Invalid identity must be rejected by the shared Oracle read owner, not SDK configuration parsing.");
            else Assert.AreNotEqual("selector",cause.ParamName,"Valid selectors must not be blanket-rejected by admission.");
        }
        switch(selector){
            case 0:Check(sync?provider.LoadAvatar(id,version):await provider.LoadAvatarAsync(id,version));break;
            case 1:Check(sync?provider.LoadAvatarByUsername(username,version):await provider.LoadAvatarByUsernameAsync(username,version));break;
            case 2:Check(sync?provider.LoadAvatarByEmail(email,version):await provider.LoadAvatarByEmailAsync(email,version));break;
            case 3:Check(sync?provider.LoadAvatarDetail(id,version):await provider.LoadAvatarDetailAsync(id,version));break;
            case 4:Check(sync?provider.LoadAvatarDetailByUsername(username,version):await provider.LoadAvatarDetailByUsernameAsync(username,version));break;
            case 5:Check(sync?provider.LoadAvatarDetailByEmail(email,version):await provider.LoadAvatarDetailByEmailAsync(email,version));break;
            default:Check(sync?provider.LoadHolon(id,loadChildren:false,version:version):await provider.LoadHolonAsync(id,loadChildren:false,version:version));break;
        }
    }

    [DataTestMethod]
    [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    [DataRow(3,false)][DataRow(3,true)][DataRow(4,false)][DataRow(4,true)][DataRow(5,false)][DataRow(5,true)]
    [DataRow(6,false)][DataRow(6,true)]
    public async Task NegativeVersionCannotReachNativeConnection(int selector,bool synchronous)
    {
        var provider=new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");var id=Guid.NewGuid();
        switch(selector){
            case 0:Check(synchronous?provider.LoadAvatar(id,-1):await provider.LoadAvatarAsync(id,-1));break;
            case 1:Check(synchronous?provider.LoadAvatarByUsername("owned",-1):await provider.LoadAvatarByUsernameAsync("owned",-1));break;
            case 2:Check(synchronous?provider.LoadAvatarByEmail("owned@example.invalid",-1):await provider.LoadAvatarByEmailAsync("owned@example.invalid",-1));break;
            case 3:Check(synchronous?provider.LoadAvatarDetail(id,-1):await provider.LoadAvatarDetailAsync(id,-1));break;
            case 4:Check(synchronous?provider.LoadAvatarDetailByUsername("owned",-1):await provider.LoadAvatarDetailByUsernameAsync("owned",-1));break;
            case 5:Check(synchronous?provider.LoadAvatarDetailByEmail("owned@example.invalid",-1):await provider.LoadAvatarDetailByEmailAsync("owned@example.invalid",-1));break;
            default:Check(synchronous?provider.LoadHolon(id,version:-1):await provider.LoadHolonAsync(id,version:-1));break;
        }
        static void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
        }
    }
}
