using System.Numerics;
using System.Reflection;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public class TronTokenBurnSafetyTests
{
    private static BigInteger Units(decimal amount, int decimals) => (BigInteger)
        typeof(TronNativeTransferClient).GetMethod("TokenUnits", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { amount, decimals })!;

    [Fact]
    public void TokenAmountUsesLedgerPrecisionWithoutDoubleRounding()
    {
        Assert.Equal(new BigInteger(12345678), Units(0.12345678m, 8));
        Assert.Equal(BigInteger.Parse("9007199254740993"), Units(9007199254740993m, 0));
        foreach (var (amount, decimals) in new[] { (0m, 6), (-1m, 6), (1.001m, 2), (1m, 255) })
            Assert.IsAssignableFrom<ArgumentException>(Assert.Throws<TargetInvocationException>(() => Units(amount, decimals)).InnerException);
    }

    [Fact]
    public void ContractDefaultReceiptIsNotSdkSuccess()
    {
        var result = new ResourceReceipt().Result;
        Assert.Equal(0, (int)result);
        Assert.Equal("Success", Enum.GetName(result.GetType(), 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidBurnAmountCannotContactNode(int amount)
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => runtime.NativeTransfers.BurnTokenAsync("contract", amount, ""));
    }

    [Fact]
    public async Task BurnRejectsWrongPublicKeyBeforeContactingNode()
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        var error = await Assert.ThrowsAsync<ArgumentException>(() => runtime.NativeTransfers.BurnTokenAsync(
            "contract", 1m, new string('1', 64), publicKey: new string('0', 130)));
        Assert.Contains("public key", error.Message);
    }
}
