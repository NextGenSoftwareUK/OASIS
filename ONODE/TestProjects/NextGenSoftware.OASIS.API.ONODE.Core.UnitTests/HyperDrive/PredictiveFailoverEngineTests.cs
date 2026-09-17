using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests.HyperDrive
{
    /// <summary>
    /// Regression tests for PredictiveFailoverEngine's per-request override mechanism.
    ///
    /// Before the race-condition fix, the background prediction loop called
    /// SetAndActivateCurrentStorageProviderAsync globally, which mutated shared state and raced
    /// with concurrent in-flight requests.  The fix stores a per-provider override in a
    /// ConcurrentDictionary that RouteToProviderAsync reads on the hot path, then clears on
    /// success.  These tests verify that lifecycle.
    /// </summary>
    public class PredictiveFailoverEngineTests
    {
        private static readonly PredictiveFailoverEngine _engine = PredictiveFailoverEngine.Instance;

        // Helper: call the private ExecutePreventiveFailoverAsync via reflection.
        private static async Task ExecutePreventiveFailoverAsync(ProviderType from, ProviderType to)
        {
            var method = typeof(PredictiveFailoverEngine)
                .GetMethod("ExecutePreventiveFailoverAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Should().NotBeNull("ExecutePreventiveFailoverAsync must exist");
            await (Task)method!.Invoke(_engine, new object[] { from, to })!;
        }

        // ── no override ───────────────────────────────────────────────────────

        [Fact]
        public void GetFailoverOverride_NoOverrideSet_ReturnsDefault()
        {
            // Use a provider type that we never set in these tests to avoid cross-test pollution.
            _engine.GetFailoverOverride(ProviderType.MongoDBOASIS)
                .Should().Be(ProviderType.Default);
        }

        // ── set override via background loop ──────────────────────────────────

        [Fact]
        public async Task ExecutePreventiveFailover_SetsOverride_GetFailoverOverrideReturnsTarget()
        {
            const ProviderType from = ProviderType.SolanaOASIS;
            const ProviderType to   = ProviderType.LocalFileOASIS;

            try
            {
                await ExecutePreventiveFailoverAsync(from, to);
                _engine.GetFailoverOverride(from).Should().Be(to);
            }
            finally
            {
                _engine.ClearFailoverOverride(from);
            }
        }

        // ── clear on success ──────────────────────────────────────────────────

        [Fact]
        public async Task ClearFailoverOverride_AfterOverrideSet_GetReturnsDefault()
        {
            const ProviderType from = ProviderType.SolanaOASIS;
            const ProviderType to   = ProviderType.LocalFileOASIS;

            await ExecutePreventiveFailoverAsync(from, to);
            _engine.GetFailoverOverride(from).Should().Be(to, "override should be active before clear");

            _engine.ClearFailoverOverride(from);

            _engine.GetFailoverOverride(from).Should().Be(ProviderType.Default,
                "override must be gone after a successful operation clears it");
        }

        // ── overwrite with a newer prediction ─────────────────────────────────

        [Fact]
        public async Task ExecutePreventiveFailover_CalledTwice_LatestOverrideWins()
        {
            const ProviderType from    = ProviderType.SolanaOASIS;
            const ProviderType firstTo = ProviderType.LocalFileOASIS;
            const ProviderType secondTo = ProviderType.MongoDBOASIS;

            try
            {
                await ExecutePreventiveFailoverAsync(from, firstTo);
                await ExecutePreventiveFailoverAsync(from, secondTo);

                _engine.GetFailoverOverride(from).Should().Be(secondTo,
                    "the most recent prediction should overwrite the earlier one");
            }
            finally
            {
                _engine.ClearFailoverOverride(from);
            }
        }

        // ── independent providers don't interfere ─────────────────────────────

        [Fact]
        public async Task ExecutePreventiveFailover_TwoProviders_OverridesAreIndependent()
        {
            const ProviderType fromA = ProviderType.SolanaOASIS;
            const ProviderType fromB = ProviderType.MongoDBOASIS;
            const ProviderType toA   = ProviderType.LocalFileOASIS;
            const ProviderType toB   = ProviderType.SolanaOASIS;

            try
            {
                await ExecutePreventiveFailoverAsync(fromA, toA);
                await ExecutePreventiveFailoverAsync(fromB, toB);

                _engine.GetFailoverOverride(fromA).Should().Be(toA);
                _engine.GetFailoverOverride(fromB).Should().Be(toB);

                _engine.ClearFailoverOverride(fromA);

                _engine.GetFailoverOverride(fromA).Should().Be(ProviderType.Default,
                    "clearing A must not affect B");
                _engine.GetFailoverOverride(fromB).Should().Be(toB);
            }
            finally
            {
                _engine.ClearFailoverOverride(fromA);
                _engine.ClearFailoverOverride(fromB);
            }
        }
    }
}
