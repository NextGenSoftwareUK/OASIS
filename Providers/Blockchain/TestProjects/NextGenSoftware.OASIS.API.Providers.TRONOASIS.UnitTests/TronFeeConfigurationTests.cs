using System.Reflection;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronFeeConfigurationTests
{
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void InvalidBudgetFailsBeforeNetworkAllocation(long budget)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TRONOASIS(contractFeeLimitSun: budget));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64), contractFeeLimitSun: budget));
    }

    [Theory]
    [InlineData(100000000L)]
    [InlineData(1000000000L)]
    public async Task ExplicitBudgetPropagatesWithoutNetworkRequests(long budget)
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"),
            new string('0', 64), contractFeeLimitSun: budget);
        var field = typeof(TronNativeTransferClient).GetField("_contractFeeLimitSun", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Assert.Equal(budget, field.GetValue(runtime.NativeTransfers));
    }
}
