using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.UnitTests;

[TestClass]
public class PostgreSqlHolonDeleteAdmissionTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task InvalidHolonDeleteKeyIsRejectedBeforeNativeConnection(bool key,bool synchronous)
    {
        var provider=new PostgreSQLOASIS("UnsupportedConnectionKeyword=true");
        var result=key?(synchronous?provider.DeleteHolon("not-a-guid"):await provider.DeleteHolonAsync("not-a-guid")):(synchronous?provider.DeleteHolon(Guid.Empty):await provider.DeleteHolonAsync(Guid.Empty));
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.IsInstanceOfType<ArgumentException>(result.Exception);
        Assert.AreEqual(key?"providerKey":"id",((ArgumentException)result.Exception).ParamName);
    }
}
