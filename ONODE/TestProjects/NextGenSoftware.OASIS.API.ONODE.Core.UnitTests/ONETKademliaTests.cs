using System;
using System.Linq;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    public class ONETKademliaTests
    {
        // ── XOR distance correctness ──────────────────────────────────────────

        [Fact]
        public void HashNodeId_SameInput_ProducesSameHash()
        {
            var a = KademliaRoutingTable.HashNodeId("node-1");
            var b = KademliaRoutingTable.HashNodeId("node-1");
            a.Should().Equal(b);
        }

        [Fact]
        public void HashNodeId_DifferentInputs_ProduceDifferentHashes()
        {
            var a = KademliaRoutingTable.HashNodeId("node-1");
            var b = KademliaRoutingTable.HashNodeId("node-2");
            a.Should().NotEqual(b);
        }

        [Fact]
        public void HashNodeId_AlwaysProduces20Bytes()
        {
            KademliaRoutingTable.HashNodeId("anything").Should().HaveCount(20);
        }

        // ── AddNode / Count ───────────────────────────────────────────────────

        [Fact]
        public void AddNode_SinglePeer_CountIsOne()
        {
            var table = new KademliaRoutingTable("local");
            table.AddNode("peer-1", "192.168.1.1:9001");
            table.Count.Should().Be(1);
        }

        [Fact]
        public void AddNode_SamePeerTwice_CountRemainsOne()
        {
            var table = new KademliaRoutingTable("local");
            table.AddNode("peer-1", "192.168.1.1:9001");
            table.AddNode("peer-1", "192.168.1.1:9001");
            table.Count.Should().Be(1);
        }

        [Fact]
        public void AddNode_TwentyDistinctPeers_CountIsTwenty()
        {
            var table = new KademliaRoutingTable("local-node");
            for (int i = 0; i < 20; i++)
                table.AddNode($"peer-{i}", $"10.0.0.{i}:9000");
            table.Count.Should().Be(20);
        }

        // ── k-bucket eviction at K=20 ─────────────────────────────────────────
        // The local node's SHA-1 determines which bucket each peer lands in.
        // To guarantee eviction we need K+1 peers that all map to the same bucket.
        // We brute-force that by finding peers whose SHA-1 XOR with the local ID
        // shares the same highest-differing bit index.

        [Fact]
        public void AddNode_MoreThanKPeersInOneBucket_EvictsLeastRecentlySeen()
        {
            // K=20 per bucket; use a fixed local node ID so the bucket layout is deterministic.
            const string localId = "eviction-test-local";
            var localHash = KademliaRoutingTable.HashNodeId(localId);

            // Find 21 peer IDs that all land in the same Kademlia bucket (same XOR bit-prefix length).
            // Bucket index = index of highest differing bit = leading zeros in XOR(local, peer).
            // We target bucket 0 (first differing bit at position 0 means XOR byte[0] & 0x80 != 0).
            var sameKBucketPeers = Enumerable
                .Range(0, 10_000)
                .Select(i => $"bucket0-peer-{i}")
                .Where(id =>
                {
                    var h = KademliaRoutingTable.HashNodeId(id);
                    // Bucket 0 means byte[0] XOR is non-zero and has its MSB set.
                    int xor0 = localHash[0] ^ h[0];
                    return (xor0 & 0x80) != 0;
                })
                .Take(21)
                .ToList();

            sameKBucketPeers.Should().HaveCount(21,
                "we need K+1=21 peers in the same bucket to trigger eviction");

            var table = new KademliaRoutingTable(localId);
            foreach (var id in sameKBucketPeers)
                table.AddNode(id, $"addr-for-{id}:9000");

            // Table should hold exactly K=20 peers (one was evicted).
            table.Count.Should().Be(20);
        }

        // ── GetClosestNodes ───────────────────────────────────────────────────

        [Fact]
        public void GetClosestNodes_EmptyTable_ReturnsEmpty()
        {
            var table = new KademliaRoutingTable("local");
            table.GetClosestNodes("target", 5).Should().BeEmpty();
        }

        [Fact]
        public void GetClosestNodes_FewerPeersThanRequested_ReturnsAll()
        {
            var table = new KademliaRoutingTable("local");
            table.AddNode("a", "10.0.0.1:9000");
            table.AddNode("b", "10.0.0.2:9000");

            var result = table.GetClosestNodes("target", 10);
            result.Should().HaveCount(2);
        }

        [Fact]
        public void GetClosestNodes_ReturnsExactCount()
        {
            var table = new KademliaRoutingTable("local");
            for (int i = 0; i < 10; i++)
                table.AddNode($"peer-{i}", $"10.0.0.{i}:9000");

            table.GetClosestNodes("target", 3).Should().HaveCount(3);
        }

        [Fact]
        public void GetClosestNodes_ClosestPeerIsIdenticalToTarget()
        {
            var table = new KademliaRoutingTable("local");
            table.AddNode("far-peer",  "10.0.0.1:9000");
            table.AddNode("exact-match", "10.0.0.2:9000");

            // Ask for the node closest to "exact-match"; it should come back first.
            var result = table.GetClosestNodes("exact-match", 2);
            result.Should().HaveCount(2);
            // The first result's address should be the exact-match peer's address.
            result[0].Address.Should().Be("10.0.0.2:9000");
        }

        [Fact]
        public void GetClosestNodes_OrderedByXorDistanceAscending()
        {
            // Verify the returned list is strictly XOR-ordered relative to the target.
            var localId = "ordering-local";
            var table = new KademliaRoutingTable(localId);
            var peers = new[] { "alpha", "beta", "gamma", "delta" };
            foreach (var p in peers)
                table.AddNode(p, $"addr-{p}:9000");

            var target = "gamma";
            var targetHash = KademliaRoutingTable.HashNodeId(target);
            var result = table.GetClosestNodes(target, peers.Length);

            // Compute XOR distances from the returned order and verify ascending.
            var distances = result.Select(r => Xor(r.NodeId, targetHash)).ToList();
            for (int i = 0; i < distances.Count - 1; i++)
                Compare(distances[i], distances[i + 1]).Should().BeLessThanOrEqualTo(0,
                    $"entry {i} should be no further than entry {i + 1}");
        }

        private static byte[] Xor(byte[] a, byte[] b)
        {
            var r = new byte[Math.Min(a.Length, b.Length)];
            for (int i = 0; i < r.Length; i++) r[i] = (byte)(a[i] ^ b[i]);
            return r;
        }

        private static int Compare(byte[] x, byte[] y)
        {
            int len = Math.Min(x.Length, y.Length);
            for (int i = 0; i < len; i++)
                if (x[i] != y[i]) return x[i].CompareTo(y[i]);
            return x.Length.CompareTo(y.Length);
        }
    }
}
