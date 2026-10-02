using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    public class ONETConsensusLeaderTests
    {
        [Fact]
        public async Task Leader_IsHighestStakeActiveNode()
        {
            var consensus = new ONETConsensus(storageProvider: null);
            await consensus.AddConsensusNodeAsync("low", stake: 1, capabilities: new List<string>());
            await consensus.AddConsensusNodeAsync("high", stake: 50, capabilities: new List<string>());

            await consensus.RecordHeartbeatAsync("low");

            consensus.CurrentLeader.Should().Be("high");
        }

        [Fact]
        public async Task Leader_MissesHeartbeatWindow_IsDemotedAndReplaced()
        {
            var consensus = new ONETConsensus(storageProvider: null) { HeartbeatTimeout = TimeSpan.FromMilliseconds(200) };
            await consensus.AddConsensusNodeAsync("leader", stake: 100, capabilities: new List<string>());
            await consensus.AddConsensusNodeAsync("follower", stake: 10, capabilities: new List<string>());
            await consensus.RecordHeartbeatAsync("follower");
            consensus.CurrentLeader.Should().Be("leader");

            await Task.Delay(400);
            await consensus.RecordHeartbeatAsync("follower");

            consensus.CurrentLeader.Should().Be("follower");
            (await consensus.GetConsensusStatsAsync()).Result.ActiveNodes.Should().Be(1);
        }

        [Fact]
        public async Task DemotedLeader_HeartbeatResumes_RegainsLeadership()
        {
            var consensus = new ONETConsensus(storageProvider: null) { HeartbeatTimeout = TimeSpan.FromMilliseconds(200) };
            await consensus.AddConsensusNodeAsync("leader", stake: 100, capabilities: new List<string>());
            await consensus.AddConsensusNodeAsync("follower", stake: 10, capabilities: new List<string>());

            await Task.Delay(400);
            await consensus.RecordHeartbeatAsync("follower");
            consensus.CurrentLeader.Should().Be("follower");

            await consensus.RecordHeartbeatAsync("leader");

            consensus.CurrentLeader.Should().Be("leader");
        }

        [Fact]
        public async Task AllNodesTimedOut_NoLeader()
        {
            var consensus = new ONETConsensus(storageProvider: null) { HeartbeatTimeout = TimeSpan.FromMilliseconds(100) };
            await consensus.AddConsensusNodeAsync("only", stake: 5, capabilities: new List<string>());
            await consensus.RecordHeartbeatAsync("only");
            consensus.CurrentLeader.Should().Be("only");

            await Task.Delay(300);
            // Any vote triggers a consensus evaluation pass.
            await consensus.VoteAsync("no-such-proposal", "only", approve: true);

            consensus.CurrentLeader.Should().BeEmpty();
        }

        [Fact]
        public async Task RecordHeartbeat_UnknownNode_ReturnsError()
        {
            var consensus = new ONETConsensus(storageProvider: null);

            (await consensus.RecordHeartbeatAsync("ghost")).IsError.Should().BeTrue();
        }
    }
}
