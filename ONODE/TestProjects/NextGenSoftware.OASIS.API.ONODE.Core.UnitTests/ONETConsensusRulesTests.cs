using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    public class ONETConsensusRulesTests
    {
        private static async Task<ONETConsensus> WithMembers(params string[] ids)
        {
            var consensus = new ONETConsensus(storageProvider: null);
            foreach (var id in ids)
                await consensus.AddConsensusNodeAsync(id, stake: 10, capabilities: new List<string>());
            return consensus;
        }

        [Fact]
        public async Task Vote_FromNonMember_IsRejected()
        {
            var consensus = await WithMembers("a", "b");
            var proposalId = (await consensus.ProposeAsync("a", "t", new { })).Result;

            (await consensus.VoteAsync(proposalId, "outsider", approve: true)).IsError.Should().BeTrue();
        }

        [Fact]
        public async Task Vote_OnUnknownProposal_IsRejected()
        {
            var consensus = await WithMembers("a");

            (await consensus.VoteAsync("missing", "a", approve: true)).IsError.Should().BeTrue();
        }

        [Fact]
        public async Task Quorum_IgnoresInactiveMembers()
        {
            var consensus = await WithMembers("a", "b", "c");
            consensus.HeartbeatTimeout = TimeSpan.FromMilliseconds(200);
            var reached = new List<string>();
            consensus.ConsensusReached += (_, e) => reached.Add(e.ConsensusId);
            var proposalId = (await consensus.ProposeAsync("a", "t", new { })).Result;

            await Task.Delay(400);
            await consensus.RecordHeartbeatAsync("a");
            await consensus.RecordHeartbeatAsync("b");
            await consensus.VoteAsync(proposalId, "a", approve: true);
            await consensus.VoteAsync(proposalId, "b", approve: true);

            reached.Should().Contain(proposalId, "2 of 2 active members approved; the timed-out member is not counted");
        }

        [Fact]
        public async Task PendingProposal_PastTimeout_ExpiresAndFiresFailed()
        {
            var consensus = await WithMembers("a", "b", "c");
            consensus.ProposalTimeout = TimeSpan.FromMilliseconds(100);
            var failed = new List<string>();
            consensus.ConsensusFailed += (_, e) => failed.Add(e.Reason);
            var proposalId = (await consensus.ProposeAsync("a", "t", new { })).Result;

            await Task.Delay(250);
            await consensus.EvaluateAsync();

            failed.Should().ContainSingle(r => r.Contains(proposalId) && r.Contains("expired"));
        }

        [Fact]
        public async Task DecidedProposals_ArePrunedAfterRetention()
        {
            var consensus = await WithMembers("a");
            consensus.DecidedProposalRetention = TimeSpan.FromMilliseconds(100);
            var proposalId = (await consensus.ProposeAsync("a", "t", new { })).Result;
            await consensus.VoteAsync(proposalId, "a", approve: true);

            await Task.Delay(250);
            await consensus.EvaluateAsync();

            var stats = (await consensus.GetConsensusStatsAsync()).Result;
            stats.PendingProposals.Should().Be(0);
            stats.TotalVotes.Should().Be(0);
        }
    }
}
