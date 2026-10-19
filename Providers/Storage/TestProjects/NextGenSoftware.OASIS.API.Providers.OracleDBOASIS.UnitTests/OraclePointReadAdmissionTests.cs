using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OraclePointReadAdmissionTests
{
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
