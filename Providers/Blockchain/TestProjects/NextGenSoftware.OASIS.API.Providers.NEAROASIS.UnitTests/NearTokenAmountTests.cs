using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public class NearTokenAmountTests
{
    private static string Units(decimal amount) => (string)typeof(NEAROASIS)
        .GetMethod("ToTokenUnits", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { amount })!;

    [TestMethod]
    public void ExactUnitsNeverRoundOrOverflowDecimalMultiplication()
    {
        Assert.AreEqual("12345678", Units(0.12345678m));
        Assert.AreEqual((BigInteger.Parse(decimal.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)) * 100000000).ToString(), Units(decimal.MaxValue));
        foreach (var amount in new[] { 0m, -1m, 0.000000001m })
        {
            var error = Assert.ThrowsException<TargetInvocationException>(() => Units(amount));
            Assert.IsInstanceOfType<ArgumentException>(error.InnerException);
        }
    }
}
