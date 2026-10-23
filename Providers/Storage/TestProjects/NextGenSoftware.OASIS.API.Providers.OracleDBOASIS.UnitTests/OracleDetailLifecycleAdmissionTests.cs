using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleDetailLifecycleAdmissionTests
{
    [DataTestMethod]
    [DataRow(0,false,false)][DataRow(0,true,false)][DataRow(1,false,false)][DataRow(1,true,false)][DataRow(2,false,false)][DataRow(2,true,false)]
    [DataRow(3,false,false)][DataRow(3,true,false)][DataRow(4,false,false)][DataRow(4,true,false)][DataRow(5,false,false)][DataRow(5,true,false)]
    [DataRow(0,false,true)][DataRow(0,true,true)][DataRow(1,false,true)][DataRow(1,true,true)][DataRow(2,false,true)][DataRow(2,true,true)]
    [DataRow(3,false,true)][DataRow(3,true,true)][DataRow(4,false,true)][DataRow(4,true,true)][DataRow(5,false,true)][DataRow(5,true,true)]
    public async Task ValidDeleteSelectorDoesNotClassifyProviderConfigurationAsPayload(int kind,bool synchronous,bool softDelete)
    {
        IOASISStorageProvider provider=new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");
        var id=Guid.NewGuid();
        var result=kind switch {
            0=>synchronous?provider.DeleteAvatar(id,softDelete):await provider.DeleteAvatarAsync(id,softDelete),
            1=>synchronous?provider.DeleteAvatarByUsername("owned-user",softDelete):await provider.DeleteAvatarByUsernameAsync("owned-user",softDelete),
            2=>synchronous?provider.DeleteAvatarByEmail("owned-user@example.invalid",softDelete):await provider.DeleteAvatarByEmailAsync("owned-user@example.invalid",softDelete),
            3=>synchronous?provider.DeleteAvatarDetail(id,softDelete):await provider.DeleteAvatarDetailAsync(id,softDelete),
            4=>synchronous?provider.DeleteAvatarDetailByUsername("owned-user",softDelete):await provider.DeleteAvatarDetailByUsernameAsync("owned-user",softDelete),
            _=>synchronous?provider.DeleteAvatarDetailByEmail("owned-user@example.invalid",softDelete):await provider.DeleteAvatarDetailByEmailAsync("owned-user@example.invalid",softDelete)
        };
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);
        Assert.AreEqual(0,result.DeletedCount);
        Assert.IsInstanceOfType<ArgumentException>(result.Exception);
        Assert.AreNotEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode,"A valid selector must not turn a provider connection configuration failure into a caller payload rejection.");
    }

    [DataTestMethod]
    [DataRow(0,false)][DataRow(1,false)][DataRow(2,false)][DataRow(3,false)][DataRow(4,false)][DataRow(5,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)][DataRow(3,true)][DataRow(4,true)][DataRow(5,true)]
    public async Task PairedAvatarInvalidSelectorFailsBeforeOpeningConnection(int kind,bool softDelete)
    {
        IOASISStorageProvider provider=new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");
        var result=kind switch {
            0=>await provider.DeleteAvatarAsync(Guid.Empty,softDelete),
            1=>provider.DeleteAvatar(Guid.Empty,softDelete),
            2=>await provider.DeleteAvatarByEmailAsync(" ",softDelete),
            3=>provider.DeleteAvatarByEmail("",softDelete),
            4=>await provider.DeleteAvatarByUsernameAsync("",softDelete),
            _=>provider.DeleteAvatarByUsername(" ",softDelete)
        };
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);
        Assert.AreEqual(0,result.DeletedCount);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
        Assert.IsInstanceOfType<ArgumentException>(result.Exception);
        StringAssert.Contains(result.Exception.Message,"selector");
    }

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
