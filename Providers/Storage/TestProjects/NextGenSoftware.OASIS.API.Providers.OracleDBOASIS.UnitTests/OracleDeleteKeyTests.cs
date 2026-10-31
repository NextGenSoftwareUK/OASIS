using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleDeleteKeyTests
{
    [DataTestMethod]
    [DataRow(null,false)][DataRow(null,true)][DataRow("",false)][DataRow("",true)]
    [DataRow("owned-user@example.invalid",false)][DataRow("owned-user@example.invalid",true)]
    public async Task NonGuidStorageKeyCannotBecomeUsernameOrEmailDeletion(string? key,bool synchronous)
    {
        var provider=new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");
        var result=synchronous?provider.DeleteAvatar(key!,false):await provider.DeleteAvatarAsync(key!,false);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);
        Assert.IsInstanceOfType<FormatException>(result.Exception);
    }
}
