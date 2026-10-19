using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class PolkadotNativeBalanceTests
{
    [DataTestMethod]
    [DataRow("1000000000001", 12, "1.000000000001")]
    [DataRow("18446744073709551616", 0, "18446744073709551616")]
    [DataRow("10000000000", 38, "0.0000000000000000000000000001")]
    [DataRow("0", 38, "0")]
    [DataRow("79228162514264337593543950335", 0, "79228162514264337593543950335")]
    public void DecimalBalancePreservesExactRuntimeUnits(string units, int decimals, string expected)
    {
        Assert.AreEqual(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), ParseDecimal(units, decimals));
    }

    [DataTestMethod]
    [DataRow("1", 29)]
    [DataRow("79228162514264337593543950336", 0)]
    [DataRow("01", 12)]
    [DataRow("340282366920938463463374607431768211456", 12)]
    public void DecimalBalanceRejectsRoundingOverflowAndMalformedUnits(string units, int decimals)
    {
        var failure = Assert.ThrowsException<TargetInvocationException>(() => ParseDecimal(units, decimals));
        Assert.IsInstanceOfType(failure.InnerException, typeof(InvalidDataException));
    }

    private static decimal ParseDecimal(string units, int decimals)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(new { units, decimals, blockHash = "0x" + new string('a', 64) }));
        return (decimal)typeof(PolkadotOASIS).GetMethod("ParseNativeDecimalBalance", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { json.RootElement })!;
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task MissingSdkIsAnErrorNotAnInventedZeroBalance(bool synchronous)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        var missing=Path.Combine(AppContext.BaseDirectory,"missing-polkadot-sdk-fixture","bridge.mjs");
        Assert.IsFalse(File.Exists(missing));
        using var provider=new PolkadotOASIS("https://owned.invalid");
        typeof(PolkadotOASIS).GetField("_isActivated",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,true);
        try
        {
            Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",missing);
            var request=new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.GetWeb3WalletBalanceRequest{WalletAddress="owned-account"};
            var result=synchronous?provider.GetBalance(request):await provider.GetBalanceAsync(request);
            Assert.IsTrue(result.IsError);
            Assert.IsInstanceOfType(result.Exception,typeof(FileNotFoundException));
        }
        finally{Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",previous);}
    }

    [DataTestMethod]
    [DataRow("0", 10, 0d)]
    [DataRow("10000000001", 10, 1.0000000001d)]
    [DataRow("1000000000000", 12, 1d)]
    [DataRow("18446744073709551616", 0, 18446744073709551616d)]
    [DataRow("340282366920938463463374607431768211455", 38, 3.4028236692093846d)]
    public void UsesRuntimePrecisionAndUnsigned128Units(string units, int decimals, double expected)
    {
        Assert.AreEqual(expected, Parse(units, decimals, "0x" + new string('a', 64)), Math.Max(1e-15, Math.Abs(expected) * 1e-15));
    }

    [DataTestMethod]
    [DataRow("-1", 10, true)][DataRow("+1", 10, true)]
    [DataRow("01", 10, true)][DataRow("1.0", 10, true)]
    [DataRow(" 1", 10, true)][DataRow("1 ", 10, true)]
    [DataRow("", 10, true)][DataRow("340282366920938463463374607431768211456", 10, true)]
    [DataRow("1", -1, true)][DataRow("1", 39, true)][DataRow("1", 10, false)]
    public void RejectsMalformedAmountsPrecisionAndMissingFinality(string units, int decimals, bool validBlock)
    {
        var failure = Assert.ThrowsException<TargetInvocationException>(() => Parse(units, decimals, validBlock ? "0x" + new string('a', 64) : "not-finalized"));
        Assert.IsInstanceOfType(failure.InnerException, typeof(InvalidDataException));
    }

    private static double Parse(string units, int decimals, string blockHash)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(new { units, decimals, blockHash }));
        var method = typeof(PolkadotOASIS).GetMethod("ParseNativeBalance", BindingFlags.Static | BindingFlags.NonPublic)!;
        return (double)method.Invoke(null, new object[] { json.RootElement })!;
    }
}
