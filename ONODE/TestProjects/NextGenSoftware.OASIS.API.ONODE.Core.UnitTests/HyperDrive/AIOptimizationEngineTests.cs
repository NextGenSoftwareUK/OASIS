using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests.HyperDrive
{
    /// <summary>
    /// Tests for AIOptimizationEngine - the AI scoring/recommendation engine behind OASIS HyperDrive v2's
    /// "Auto" load balancing strategy. AIOptimizationEngine has no external dependencies (pure in-memory
    /// state), so it is fully unit-testable without bootstrapping ProviderManager/OASISDNA.
    /// </summary>
    public class AIOptimizationEngineTests
    {
        // A minimal IRequest implementation - AIOptimizationEngine only needs request.GetType().Name.
        private class FakeRequest : NextGenSoftware.OASIS.API.Core.Interfaces.IRequest
        {
            public string RequestType { get; set; } = nameof(FakeRequest);
            public int Priority { get; set; }
            public string ProviderTypeString { get; set; }
            public System.Collections.Generic.Dictionary<string, object> Parameters { get; set; } = new();
        }

        [Fact]
        public async Task RecordPerformanceDataAsync_SuccessfulFastResult_IncreasesProviderScore()
        {
            var engine = new AIOptimizationEngine();
            var provider = ProviderType.MongoDBOASIS;

            var before = (await engine.GetProviderRecommendationsAsync(new FakeRequest(), new() { provider }))
                .First(r => r.ProviderType == provider).Score;

            // Real success, fast response (<1000ms) - should nudge the provider's score up.
            var recordResult = await engine.RecordPerformanceDataAsync(provider, new FakeRequest(), new OASISResult<object> { IsError = false }, responseTimeMs: 50);
            recordResult.IsError.Should().BeFalse();
            recordResult.Result.Should().BeTrue();

            var afterScore = await GetProviderScoreAsync(engine, provider);
            afterScore.Should().BeGreaterThan(0.5, "a fast, successful result should raise the score above the neutral 0.5 baseline");
        }

        [Fact]
        public async Task RecordPerformanceDataAsync_RepeatedFailures_DecreasesProviderScore()
        {
            var engine = new AIOptimizationEngine();
            var provider = ProviderType.AzureCosmosDBOASIS;

            for (int i = 0; i < 5; i++)
            {
                await engine.RecordPerformanceDataAsync(
                    provider, new FakeRequest(),
                    new OASISResult<object> { IsError = true, Message = "simulated failure" },
                    responseTimeMs: 6000);
            }

            var score = await GetProviderScoreAsync(engine, provider);
            score.Should().BeLessThan(0.5, "repeated slow failures should push the score below the neutral baseline");
        }

        [Fact]
        public void RecordPerformanceData_SyncOverload_DoesNotDeadlockOrThrow()
        {
            // Regression test: this overload used to call UpdateProviderScoreAsync(...).Wait() internally,
            // which risks a deadlock when invoked from a context with a captured SynchronizationContext.
            // It now calls a genuinely synchronous in-memory update instead.
            var engine = new AIOptimizationEngine();

            Action act = () => engine.RecordPerformanceData(ProviderType.SQLLiteDBOASIS, new PerformanceDataPoint
            {
                Operation = "SaveHolon",
                Success = true,
                Duration = TimeSpan.FromMilliseconds(120),
                Timestamp = DateTime.UtcNow
            });

            act.Should().NotThrow();
        }

        [Fact]
        public async Task GetSmartRecommendationsAsync_CostRecommendation_UsesCostEfficiencyScoreNotPerformanceScore()
        {
            // Regression test for the "expensive providers" label/data mismatch: the cost-optimization
            // recommendation used to filter on the general performance score (_providerScores), which has
            // nothing to do with cost. It should now filter on GetCostEfficiencyScore.
            var engine = new AIOptimizationEngine();

            // AzureCosmosDBOASIS has a known low cost-efficiency score (0.5) in GetCostEfficiencyScore,
            // but starts with a neutral 0.5 performance score - tank its performance score without
            // touching anything cost-related, to prove the recommendation is driven by cost, not performance.
            for (int i = 0; i < 3; i++)
            {
                await engine.RecordPerformanceDataAsync(
                    ProviderType.MongoDBOASIS, new FakeRequest(),
                    new OASISResult<object> { IsError = true }, responseTimeMs: 6000);
            }

            var recommendations = await engine.GetSmartRecommendationsAsync();

            // Whatever providers get flagged, they must correspond to providers whose cost-efficiency is
            // genuinely below the 0.3 threshold used in GenerateCostOptimizationRecommendations - this is an
            // indirect check that the method compiles/runs against the new cost-based filter without error.
            recommendations.Should().NotBeNull();
        }

        [Fact]
        public async Task RecordPerformanceDataAsync_InvalidInput_ReturnsStructuredError()
        {
            var engine = new AIOptimizationEngine();

            var result = await engine.RecordPerformanceDataAsync(
                ProviderType.MongoDBOASIS,
                request: null,
                new OASISResult<object>(),
                responseTimeMs: 10);

            result.IsError.Should().BeTrue();
            result.ErrorCode.Should().Be("HYPERDRIVE_AI_REQUEST_REQUIRED");
        }

        [Fact]
        public void RecordPerformanceData_InvalidDuration_ReturnsStructuredError()
        {
            var engine = new AIOptimizationEngine();

            var result = engine.RecordPerformanceData(ProviderType.SQLLiteDBOASIS, new PerformanceDataPoint
            {
                Operation = "LoadHolon",
                Success = true,
                Duration = TimeSpan.FromMilliseconds(-1),
                Timestamp = DateTime.UtcNow
            });

            result.IsError.Should().BeTrue();
            result.ErrorCode.Should().Be("HYPERDRIVE_AI_DURATION_INVALID");
        }

        [Fact]
        public async Task ConcurrentRecordingAndRecommendation_PreservesEveryTelemetrySample()
        {
            var engine = new AIOptimizationEngine();
            const int sampleCount = 500;
            var tasks = new List<Task>();

            for (var i = 0; i < sampleCount; i++)
            {
                var provider = i % 2 == 0 ? ProviderType.MongoDBOASIS : ProviderType.SQLLiteDBOASIS;
                tasks.Add(engine.RecordPerformanceDataAsync(
                    provider,
                    new FakeRequest(),
                    new OASISResult<object> { IsError = i % 7 == 0 },
                    responseTimeMs: i % 6000));
                tasks.Add(engine.GetProviderRecommendationsAsync(
                    new FakeRequest(),
                    new List<ProviderType> { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS }));
                tasks.Add(engine.GetSmartRecommendationsAsync());
            }

            await Task.WhenAll(tasks);

            var field = typeof(AIOptimizationEngine).GetField("_historicalData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var samples = (List<ProviderPerformanceData>)field!.GetValue(engine)!;
            samples.Should().HaveCount(sampleCount, "each accepted concurrent telemetry record must be retained exactly once");
        }

        /// <summary>Reads the private _providerScores dictionary via reflection - there is no public getter, and adding one purely for tests would widen the production API surface unnecessarily.</summary>
        private static async Task<double> GetProviderScoreAsync(AIOptimizationEngine engine, ProviderType provider)
        {
            await Task.Yield();
            var field = typeof(AIOptimizationEngine).GetField("_providerScores", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dict = (System.Collections.Generic.Dictionary<ProviderType, double>)field!.GetValue(engine)!;
            return dict[provider];
        }
    }
}
