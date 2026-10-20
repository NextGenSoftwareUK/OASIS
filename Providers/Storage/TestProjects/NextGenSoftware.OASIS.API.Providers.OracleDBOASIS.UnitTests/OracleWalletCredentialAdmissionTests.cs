using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleWalletCredentialAdmissionTests
{
    public static IEnumerable<object[]> Cases(){foreach(var method in Enumerable.Range(0,4))foreach(var version in new[]{-1,1,int.MaxValue})yield return new object[]{method,version};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task WalletCredentialSelectorsCannotReadHistoricalAuthority(int method,int version)
    {
        IOASISStorageProvider provider=new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");
        var result=method switch {
            0=>await provider.LoadAvatarByPublicKeyAsync("owned-key",version),
            1=>provider.LoadAvatarByPublicKey("owned-key",version),
            2=>await provider.LoadAvatarByPrivateKeyAsync("owned-key",version),
            _=>provider.LoadAvatarByPrivateKey("owned-key",version)
        };
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
        StringAssert.Contains(result.Exception.Message,"latest version zero");
    }
}
