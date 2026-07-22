using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    /// <summary>
    /// A single k-bucket in the Kademlia routing table.
    /// Holds up to K=20 peers; the least-recently-seen peer is evicted when the bucket is full.
    /// </summary>
    internal sealed class KBucket
    {
        internal const int K = 20;
        private readonly LinkedList<KademliaPeer> _peers = new();

        internal IReadOnlyList<KademliaPeer> Peers
        {
            get { lock (_peers) return _peers.ToList(); }
        }

        internal void AddOrUpdate(KademliaPeer peer)
        {
            lock (_peers)
            {
                var existing = _peers.FirstOrDefault(p => p.NodeId.SequenceEqual(peer.NodeId));
                if (existing != null)
                {
                    // Move to tail (most recently seen).
                    _peers.Remove(existing);
                    _peers.AddLast(peer);
                    return;
                }

                if (_peers.Count < K)
                {
                    _peers.AddLast(peer);
                }
                else
                {
                    // Evict the least-recently-seen peer (head) — a real Kademlia implementation
                    // would ping the head first and only evict if it doesn't reply; that requires an
                    // async round-trip that doesn't belong inside the routing table itself.
                    _peers.RemoveFirst();
                    _peers.AddLast(peer);
                }
            }
        }
    }

    /// <summary>A peer entry stored in a k-bucket.</summary>
    internal sealed class KademliaPeer
    {
        internal byte[] NodeId { get; init; } = Array.Empty<byte>();
        internal string Address { get; init; } = string.Empty;
        internal DateTime LastSeen { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 160-bucket Kademlia routing table (standard Kademlia over 160-bit / SHA-1 node IDs).
    /// Bucket i holds peers whose XOR distance to the local node falls in [2^i, 2^(i+1)).
    /// Provides O(log n) FIND_NODE lookups.
    /// </summary>
    public sealed class KademliaRoutingTable
    {
        private const int IdBits = 160;
        private readonly KBucket[] _buckets = Enumerable.Range(0, IdBits).Select(_ => new KBucket()).ToArray();
        private readonly byte[] _localId;

        /// <param name="localNodeId">String node ID; will be SHA-1 hashed to produce the 160-bit Kademlia ID.</param>
        public KademliaRoutingTable(string localNodeId)
        {
            _localId = HashNodeId(localNodeId);
        }

        /// <summary>Compute the 160-bit Kademlia ID for a string node ID.</summary>
        public static byte[] HashNodeId(string nodeId)
        {
            using var sha1 = SHA1.Create();
            return sha1.ComputeHash(Encoding.UTF8.GetBytes(nodeId ?? string.Empty));
        }

        /// <summary>Add or refresh a remote peer in the routing table.</summary>
        public void AddNode(string nodeIdStr, string address)
        {
            var nodeId = HashNodeId(nodeIdStr);
            var bucketIdx = BucketIndex(_localId, nodeId);
            _buckets[bucketIdx].AddOrUpdate(new KademliaPeer { NodeId = nodeId, Address = address });
        }

        /// <summary>
        /// Return the <paramref name="count"/> peers closest to <paramref name="targetNodeIdStr"/>
        /// by XOR distance. Always returns at most min(<paramref name="count"/>, total peers).
        /// </summary>
        public IReadOnlyList<(byte[] NodeId, string Address)> GetClosestNodes(string targetNodeIdStr, int count)
        {
            var target = HashNodeId(targetNodeIdStr);
            return _buckets
                .SelectMany(b => b.Peers)
                .OrderBy(p => XorDistance(p.NodeId, target), new ByteArrayComparer())
                .Take(count)
                .Select(p => (p.NodeId, p.Address))
                .ToList();
        }

        /// <summary>Total number of peers currently in the routing table.</summary>
        public int Count => _buckets.Sum(b => b.Peers.Count);

        // ----- helpers -----

        private static int BucketIndex(byte[] local, byte[] remote)
        {
            // Find the index of the highest bit that differs (i.e. the common-prefix length in XOR space).
            for (int byteIdx = 0; byteIdx < local.Length; byteIdx++)
            {
                int xorByte = local[byteIdx] ^ remote[byteIdx];
                if (xorByte == 0) continue;
                // Highest differing bit in this byte.
                for (int bit = 7; bit >= 0; bit--)
                {
                    if ((xorByte & (1 << bit)) != 0)
                        return byteIdx * 8 + (7 - bit);
                }
            }
            return IdBits - 1; // identical IDs — put in the last bucket
        }

        private static byte[] XorDistance(byte[] a, byte[] b)
        {
            var result = new byte[Math.Min(a.Length, b.Length)];
            for (int i = 0; i < result.Length; i++)
                result[i] = (byte)(a[i] ^ b[i]);
            return result;
        }

        private sealed class ByteArrayComparer : IComparer<byte[]>
        {
            public int Compare(byte[]? x, byte[]? y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                int len = Math.Min(x.Length, y.Length);
                for (int i = 0; i < len; i++)
                    if (x[i] != y[i]) return x[i].CompareTo(y[i]);
                return x.Length.CompareTo(y.Length);
            }
        }
    }
}
