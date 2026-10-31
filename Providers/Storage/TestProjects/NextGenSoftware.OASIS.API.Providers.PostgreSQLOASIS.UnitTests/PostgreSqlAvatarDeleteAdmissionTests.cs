using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.UnitTests;

[TestClass]
public class PostgreSqlAvatarDeleteAdmissionTests
{
    [DataTestMethod]
    [DataRow(0,false,false)][DataRow(1,false,false)][DataRow(2,false,false)][DataRow(3,false,false)]
    [DataRow(0,true,false)][DataRow(1,true,false)][DataRow(2,true,false)][DataRow(3,true,false)]
    [DataRow(0,false,true)][DataRow(1,false,true)][DataRow(2,false,true)][DataRow(3,false,true)]
    [DataRow(0,true,true)][DataRow(1,true,true)][DataRow(2,true,true)][DataRow(3,true,true)]
    public async Task InvalidDeleteSelectorCannotReachNativeStorage(int kind,bool synchronous,bool soft)
    {
        var provider=new PostgreSQLOASIS("UnsupportedConnectionKeyword=true");
        OASISResult<bool> result;
        if(kind==0)result=synchronous?provider.DeleteAvatar(Guid.Empty,soft):await provider.DeleteAvatarAsync(Guid.Empty,soft);
        else if(kind==1)result=synchronous?provider.DeleteAvatar("not-a-guid",soft):await provider.DeleteAvatarAsync("not-a-guid",soft);
        else if(kind==2)result=synchronous?provider.DeleteAvatarByUsername(" ",soft):await provider.DeleteAvatarByUsernameAsync(" ",soft);
        else result=synchronous?provider.DeleteAvatarByEmail(" ",soft):await provider.DeleteAvatarByEmailAsync(" ",soft);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.DeletedCount);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
        Assert.IsInstanceOfType<ArgumentException>(result.Exception);
        Assert.AreEqual(kind==0?"id":kind==1?"providerKey":"selector",((ArgumentException)result.Exception).ParamName);
    }
}
