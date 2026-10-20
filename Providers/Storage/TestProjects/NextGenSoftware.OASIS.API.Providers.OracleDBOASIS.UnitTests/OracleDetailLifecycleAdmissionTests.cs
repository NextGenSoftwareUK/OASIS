using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleDetailLifecycleAdmissionTests
{
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)][DataRow(5)]
    public async Task StandaloneDetailInvalidSelectorFailsBeforeOpeningConnection(int kind)
    {
        IOASISStorageProvider provider=new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");
        var result=kind switch {
            0=>await provider.DeleteAvatarDetailAsync(Guid.Empty,false),
            1=>provider.DeleteAvatarDetail(Guid.Empty),
            2=>await provider.DeleteAvatarDetailByEmailAsync(" ",false),
            3=>provider.DeleteAvatarDetailByEmail(""),
            4=>await provider.DeleteAvatarDetailByUsernameAsync("",false),
            _=>provider.DeleteAvatarDetailByUsername(" ")
        };
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
        Assert.IsInstanceOfType<ArgumentException>(result.Exception);
        StringAssert.Contains(result.Exception.Message,"selector");
        var map=typeof(OracleDBOASIS).GetInterfaceMap(typeof(IOASISStorageProvider));
        foreach(var target in map.TargetMethods.Where(method=>method.Name.StartsWith("DeleteAvatarDetail",StringComparison.Ordinal)))
            Assert.AreEqual(typeof(OracleDBOASIS),target.DeclaringType);
    }
}
