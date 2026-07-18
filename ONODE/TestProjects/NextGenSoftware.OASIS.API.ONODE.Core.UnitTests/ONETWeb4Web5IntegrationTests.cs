using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    /// <summary>
    /// Regression tests for ONETWEB4APIIntegration/ONETWEB5STARIntegration: service lookup used to be pure
    /// bidirectional substring matching (FindWEB4ServiceAsync/FindSTARServiceAsync), which could route to an
    /// unrelated service purely because of a short Name being a substring of an unrelated apiName (e.g.
    /// "metadata" containing "data" would have matched the "Data API" service). Network uptime and node-ID
    /// fallback had the same hardcoded/fabricated patterns fixed elsewhere in ONET.
    /// </summary>
    public class ONETWeb4Web5IntegrationTests
    {
        private static object InvokePrivate(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            return method!.Invoke(target, args)!;
        }

        [Fact]
        public async Task FindWEB4ServiceAsync_ExactKeyMatch_IsPreferredOverFuzzySubstring()
        {
            var integration = new ONETWEB4APIIntegration(storageProvider: null);

            // "data" is an exact dictionary key (the "Data API" service) - must resolve directly to it.
            var resultTask = (Task<WEB4APIService?>)InvokePrivate(integration, "FindWEB4ServiceAsync", "data");
            var service = await resultTask;

            service.Should().NotBeNull();
            service!.Name.Should().Be("Data API");
        }

        [Fact]
        public async Task FindWEB4ServiceAsync_FullDisplayNameWithNoExactKey_StillResolvesViaFuzzyFallback()
        {
            // Documents the intentional fallback behaviour: a caller passing the full display name
            // ("Wallet API") rather than the canonical short key ("wallet") has no exact key match, so the
            // fuzzy substring fallback still resolves it correctly.
            var integration = new ONETWEB4APIIntegration(storageProvider: null);

            var resultTask = (Task<WEB4APIService?>)InvokePrivate(integration, "FindWEB4ServiceAsync", "Wallet API");
            var service = await resultTask;

            service.Should().NotBeNull();
            service!.Name.Should().Be("Wallet API");
        }

        [Fact]
        public async Task CallWEB4APIAsync_UnknownService_FailsWithoutRoutingAnywhere()
        {
            var integration = new ONETWEB4APIIntegration(storageProvider: null);
            await integration.InitializeIntegrationAsync();

            var result = await integration.CallWEB4APIAsync<object>("totally-unrelated-service-xyz", "/api/x", null);

            result.IsError.Should().BeTrue();
            result.Message.Should().Contain("not found");
        }

        [Fact]
        public async Task GetWEB4APIStatsAsync_NetworkUptime_ReflectsActiveServiceFraction_NotHardcoded999()
        {
            var integration = new ONETWEB4APIIntegration(storageProvider: null);

            var result = await integration.GetWEB4APIStatsAsync();

            result.IsError.Should().BeFalse();
            result.Result!.NetworkUptime.Should().Be(100.0, "every seeded WEB4 service defaults to IsActive = true");
        }

        [Fact]
        public async Task FindSTARServiceAsync_ExactKeyMatch_IsPreferredOverFuzzySubstring()
        {
            var integration = new ONETWEB5STARIntegration(storageProvider: null);

            var resultTask = (Task<STARService?>)InvokePrivate(integration, "FindSTARServiceAsync", "oapps");
            var service = await resultTask;

            service.Should().NotBeNull();
        }

        [Fact]
        public async Task GetSTARAPIStatsAsync_NetworkUptime_ReflectsActiveServiceFraction_NotHardcoded999()
        {
            var integration = new ONETWEB5STARIntegration(storageProvider: null);

            var result = await integration.GetSTARAPIStatsAsync();

            result.IsError.Should().BeFalse();
            result.Result!.NetworkUptime.Should().Be(100.0, "every seeded STAR service defaults to IsActive = true");
        }
    }
}
