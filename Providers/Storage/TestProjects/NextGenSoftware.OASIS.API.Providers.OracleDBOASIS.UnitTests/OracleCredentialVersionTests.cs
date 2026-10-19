using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleCredentialVersionTests
{
    [DataTestMethod]
    [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    public async Task HistoricalCredentialsAreRejectedBeforeConnection(int selector,bool synchronous)
    {
        var provider=new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");
        var result=selector==0?(synchronous?provider.LoadAvatarByVerificationToken("owned",1):await provider.LoadAvatarByVerificationTokenAsync("owned",1))
            :selector==1?(synchronous?provider.LoadAvatarByResetToken("owned",1):await provider.LoadAvatarByResetTokenAsync("owned",1))
            :(synchronous?provider.LoadAvatarByRefreshToken("owned",1):await provider.LoadAvatarByRefreshTokenAsync("owned",1));
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
    }
}
