using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    public class ONETRoutingTests
    {
        private static ONETNode Node(string id, double latency = 20, int reliability = 95) => new ONETNode
        {
            Id = id,
            Address = "127.0.0.1:1",
            Latency = latency,
            Reliability = reliability,
            Capabilities = new List<string>()
        };

        [Fact]
        public async Task FindOptimalRoute_ActiveTarget_ReturnsDirectHopToTarget()
        {
            var routing = new ONETRouting(storageProvider: null);
            await routing.AddNodeAsync(Node("a", latency: 5));
            await routing.AddNodeAsync(Node("b", latency: 300));
            await routing.AddNodeAsync(Node("c", latency: 40));

            var result = await routing.FindOptimalRouteAsync("b");

            result.IsError.Should().BeFalse(result.Message);
            result.Result.Should().Equal("b");
        }

        [Fact]
        public async Task FindOptimalRoute_UnknownTarget_ReturnsError()
        {
            var routing = new ONETRouting(storageProvider: null);
            await routing.AddNodeAsync(Node("a"));

            var result = await routing.FindOptimalRouteAsync("missing");

            result.IsError.Should().BeTrue();
            result.Message.Should().Contain("missing");
        }

        [Fact]
        public async Task FindOptimalRoute_AfterTargetRemoved_ReturnsErrorInsteadOfCachedRoute()
        {
            var routing = new ONETRouting(storageProvider: null);
            await routing.AddNodeAsync(Node("a"));
            (await routing.FindOptimalRouteAsync("a")).IsError.Should().BeFalse();

            (await routing.RemoveNodeAsync("a")).IsError.Should().BeFalse();

            (await routing.FindOptimalRouteAsync("a")).IsError.Should().BeTrue();
        }

        [Fact]
        public async Task RemoveNode_Unknown_ReturnsError()
        {
            var routing = new ONETRouting(storageProvider: null);

            (await routing.RemoveNodeAsync("nope")).IsError.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateNodeMetrics_UpdatesStatsAverages()
        {
            var routing = new ONETRouting(storageProvider: null);
            await routing.AddNodeAsync(Node("a", latency: 10, reliability: 100));
            await routing.AddNodeAsync(Node("b", latency: 30, reliability: 80));

            (await routing.UpdateNodeMetricsAsync("b", latency: 50, reliability: 60, throughput: 100)).IsError.Should().BeFalse();

            var stats = (await routing.GetRoutingStatsAsync()).Result;
            stats.TotalNodes.Should().Be(2);
            stats.ActiveNodes.Should().Be(2);
            stats.AverageLatency.Should().Be(30);
            stats.AverageReliability.Should().Be(80);
        }

        [Fact]
        public async Task UpdateNodeMetrics_UnknownNode_ReturnsError()
        {
            var routing = new ONETRouting(storageProvider: null);

            (await routing.UpdateNodeMetricsAsync("x", 1, 1, 1)).IsError.Should().BeTrue();
        }

        [Fact]
        public async Task GetRoutingStats_EmptyTable_SucceedsWithZeroAverages()
        {
            var routing = new ONETRouting(storageProvider: null);

            var result = await routing.GetRoutingStatsAsync();

            result.IsError.Should().BeFalse(result.Message);
            result.Result.TotalNodes.Should().Be(0);
            result.Result.AverageLatency.Should().Be(0);
            result.Result.AverageReliability.Should().Be(0);
        }
    }
}
