using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>
    /// In-memory radio environment for tests and demos: build any topology (line, mesh, partitions) with a
    /// chosen MTU and optional frame loss, with no hardware.
    /// </summary>
    public sealed class LoopbackNearbyNetwork
    {
        private readonly object _gate = new object();
        private readonly Random _random = new Random(12345);

        public double FrameLossRate { get; set; }

        public LoopbackNearbyLink CreateLink(string name, int maximumFrameSize = 185) => new LoopbackNearbyLink(this, name, maximumFrameSize);

        /// <summary>Puts two links in radio range of each other.</summary>
        public void Connect(LoopbackNearbyLink a, LoopbackNearbyLink b)
        {
            a.AddPeer(b);
            b.AddPeer(a);
        }

        /// <summary>Takes two links out of range (a device walking away).</summary>
        public void Disconnect(LoopbackNearbyLink a, LoopbackNearbyLink b)
        {
            a.RemovePeer(b);
            b.RemovePeer(a);
        }

        internal bool ShouldDrop()
        {
            lock (_gate) return FrameLossRate > 0 && _random.NextDouble() < FrameLossRate;
        }
    }

    public sealed class LoopbackNearbyLink : INearbyLink
    {
        private readonly LoopbackNearbyNetwork _network;
        private readonly Dictionary<string, LoopbackNearbyLink> _peers = new Dictionary<string, LoopbackNearbyLink>();
        private readonly object _gate = new object();

        internal LoopbackNearbyLink(LoopbackNearbyNetwork network, string name, int maximumFrameSize)
        {
            _network = network;
            Name = name;
            MaximumFrameSize = maximumFrameSize;
        }

        public string Name { get; }
        public NearbyLinkKind Kind => NearbyLinkKind.Loopback;
        public int MaximumFrameSize { get; }
        public int FramesSent { get; private set; }

        public IReadOnlyCollection<string> ConnectedPeerIds { get { lock (_gate) return _peers.Keys.ToList(); } }

        public event Action<string, byte[]> FrameReceived;

        internal void AddPeer(LoopbackNearbyLink peer) { lock (_gate) _peers[peer.Name] = peer; }
        internal void RemovePeer(LoopbackNearbyLink peer) { lock (_gate) _peers.Remove(peer.Name); }

        public Task SendAsync(string peerId, byte[] frame, CancellationToken cancellationToken)
        {
            if (frame.Length > MaximumFrameSize)
                throw new ArgumentException($"Frame of {frame.Length} bytes exceeds the {MaximumFrameSize}-byte link limit.", nameof(frame));
            LoopbackNearbyLink peer;
            lock (_gate)
            {
                if (!_peers.TryGetValue(peerId, out peer))
                    throw new InvalidOperationException($"Peer '{peerId}' is out of range.");
                FramesSent++;
            }
            if (!_network.ShouldDrop())
            {
                var copy = (byte[])frame.Clone();
                var from = Name;
                _ = Task.Run(() => peer.FrameReceived?.Invoke(from, copy));
            }
            return Task.CompletedTask;
        }

        public void Dispose() { }
    }
}
