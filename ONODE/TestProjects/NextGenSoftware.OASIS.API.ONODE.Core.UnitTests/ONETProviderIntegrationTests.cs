using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    /// <summary>
    /// Regression tests for ONETProviderIntegration: latency/reliability used to be hardcoded per-ProviderType
    /// switch statements with made-up numbers that never changed regardless of real outcomes, and
    /// GetProviderStatsAsync used direct dictionary indexers that would throw KeyNotFoundException if a
    /// category was ever missing.
    /// </summary>
    public class ONETProviderIntegrationTests
    {
        private static object InvokePrivate(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            return method!.Invoke(target, args)!;
        }

        [Fact]
        public async Task CalculateProviderLatencyAsync_NoHistory_ReturnsNeutralFallback()
        {
            var integration = new ONETProviderIntegration(storageProvider: null);

            var latency = await (Task<double>)InvokePrivate(integration, "CalculateProviderLatencyAsync", ProviderType.EthereumOASIS);

            latency.Should().Be(20.0);
        }

        [Fact]
        public async Task RecordProviderCall_ThenCalculateLatencyAndReliability_ReflectsRealRecordedHistory()
        {
            var integration = new ONETProviderIntegration(storageProvider: null);
            var recordMethod = typeof(ONETProviderIntegration).GetMethod("RecordProviderCall", BindingFlags.NonPublic | BindingFlags.Instance);

            // Two successes at 10ms/20ms, one failure at 30ms - average latency 20ms, reliability 67%.
            recordMethod!.Invoke(integration, new object[] { ProviderType.SolanaOASIS, 10.0, true });
            recordMethod.Invoke(integration, new object[] { ProviderType.SolanaOASIS, 20.0, true });
            recordMethod.Invoke(integration, new object[] { ProviderType.SolanaOASIS, 30.0, false });

            var latency = await (Task<double>)InvokePrivate(integration, "CalculateProviderLatencyAsync", ProviderType.SolanaOASIS);
            var reliability = await (Task<int>)InvokePrivate(integration, "CalculateProviderReliabilityAsync", ProviderType.SolanaOASIS);

            latency.Should().BeApproximately(20.0, 0.01, "average of 10, 20, 30 is 20 - not a hardcoded per-provider constant");
            reliability.Should().Be(67, "2 successes out of 3 calls rounds to 67%");
        }

        [Fact]
        public async Task GetProviderStatsAsync_NeverThrowsKeyNotFoundException()
        {
            var integration = new ONETProviderIntegration(storageProvider: null);

            var result = await integration.GetProviderStatsAsync();

            result.IsError.Should().BeFalse();
            result.Result.Should().NotBeNull();
            result.Result.BlockchainProviders.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetNetworkUptimeAsync_ReflectsActiveBridgeFraction_NotHardcoded999()
        {
            var integration = new ONETProviderIntegration(storageProvider: null);
            var method = typeof(ONETProviderIntegration).GetMethod("GetNetworkUptimeAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            var uptime = await (Task<double>)method!.Invoke(integration, null)!;

            // All bridges are seeded as IsActive = true by default, so this should be exactly 100, not 99.9.
            uptime.Should().Be(100.0);
        }
    }
}
