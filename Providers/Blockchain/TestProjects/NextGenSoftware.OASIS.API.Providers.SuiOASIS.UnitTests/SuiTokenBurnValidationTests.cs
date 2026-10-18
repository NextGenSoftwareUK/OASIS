using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass]
public class SuiTokenBurnValidationTests
{
    [TestMethod]
    public void TokenBalancePreservesExactU64AndRejectsMalformedNativeUnits()
    {
        var method=typeof(SuiOASIS).GetMethod("TokenBalanceFromUnits",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!;
        Assert.AreEqual(18446744073.709551615m,method.Invoke(null,new object[]{"18446744073709551615"}));
        Assert.AreEqual(0m,method.Invoke(null,new object[]{"0"}));
        Assert.AreEqual(0.000000001m,method.Invoke(null,new object[]{"1"}));
        foreach(var invalid in new string?[]{null,"","-1","1.5","1e3","18446744073709551616"," 1","01","+1"}){
            var error=Assert.ThrowsException<System.Reflection.TargetInvocationException>(()=>method.Invoke(null,new object?[]{invalid}));
            Assert.IsInstanceOfType<InvalidOperationException>(error.InnerException);
        }
    }
    [TestMethod]
    public async Task InvalidExplicitAmountsFailBeforeSigningOrNetwork()
    {
        using var provider = new SuiOASIS("http://127.0.0.1:1", "localnet", contractAddress: "0x1", currencyObjectId: "0x1");
        foreach (var amount in new[] { 0m, -1m, 0.0000000001m, decimal.MaxValue })
        {
            var result = await provider.BurnTokenAsync(new BurnWeb3TokenRequest
            { Amount = amount, TokenAddress = "0x1::token::TOKEN", OwnerPrivateKey = "", OwnerPublicKey = "", OwnerSeedPhrase = "" });
            Assert.IsTrue(result.IsError);
            Assert.IsNull(result.Result);
            if (amount == decimal.MaxValue)
                Assert.IsInstanceOfType<OverflowException>(result.Exception);
            else {
                Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
                Assert.AreEqual("amount", ((ArgumentOutOfRangeException)result.Exception).ParamName);
            }
        }
    }
}
