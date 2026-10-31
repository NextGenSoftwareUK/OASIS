using System.Globalization;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public class NearNativeAmountTests
{
    private static object Convert(string method, object value) => typeof(NEAROASIS).Assembly
        .GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearNativeAmount")!
        .GetMethod(method)!.Invoke(null, new[] { value })!;

    [TestMethod]
    public void ScalesLargeNativeBalancesBeforeDecimalConversion()
    {
        Assert.AreEqual(1_000_000_000m, Convert("FromYocto", "1000000000000000000000000000000000"));
        Assert.AreEqual(0.000000000000000000000001m, Convert("FromYocto", "1"));
        Assert.AreEqual(0m, Convert("FromYocto", "0"));
        var maximum = ((BigInteger.One << 128) - 1).ToString(CultureInfo.InvariantCulture);
        Assert.IsTrue((decimal)Convert("FromYocto", maximum) > 340_000_000_000_000m);
    }

    [TestMethod]
    public void EncodesLargeAndSmallAmountsExactlyWithoutDecimalMultiplication()
    {
        Assert.AreEqual("1000000000000000000000000000000000", Convert("ToYocto", 1_000_000_000m));
        Assert.AreEqual("1", Convert("ToYocto", 0.000000000000000000000001m));
        Assert.AreEqual("1250000000000000000000000", Convert("ToYocto", 1.25m));
    }

    [TestMethod]
    public void RejectsRoundingNegativeAndOutOfProtocolAmounts()
    {
        foreach(var amount in new[]{0m,-1m,0.0000000000000000000000001m,decimal.MaxValue})
        {
            var error=Assert.ThrowsException<TargetInvocationException>(()=>Convert("ToYocto",amount));
            Assert.IsInstanceOfType<ArgumentException>(error.InnerException);
        }
        var overflow=((BigInteger.One<<128)).ToString(CultureInfo.InvariantCulture);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(
            Assert.ThrowsException<TargetInvocationException>(()=>Convert("FromYocto",overflow)).InnerException);
    }
}
