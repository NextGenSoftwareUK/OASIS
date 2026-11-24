using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>Bounded reassembly of chunked messages: caps partial count and size and expires stalled ones.</summary>
    public sealed class NearbyReassembler
    {
        private sealed class Partial
        {
            public byte[][] Chunks;
            public int Received;
            public long Bytes;
            public DateTime Started;
        }

        private readonly Dictionary<string, Partial> _partials = new Dictionary<string, Partial>();
        private readonly object _gate = new object();
        private readonly int _maximumPartials;
        private readonly long _maximumMessageBytes;
        private readonly TimeSpan _timeout;
        private readonly Func<DateTime> _clock;

        public NearbyReassembler(int maximumPartials = 64, long maximumMessageBytes = NearbyEnvelope.MaximumPayloadBytes + 4096,
            TimeSpan? timeout = null, Func<DateTime> clock = null)
        {
            _maximumPartials = maximumPartials;
            _maximumMessageBytes = maximumMessageBytes;
            _timeout = timeout ?? TimeSpan.FromSeconds(60);
            _clock = clock ?? (() => DateTime.UtcNow);
        }

        public int PendingCount { get { lock (_gate) return _partials.Count; } }

        /// <summary>Adds a frame; returns the complete message when this frame finishes it, otherwise null.</summary>
        public byte[] Add(string peerId, byte[] frame)
        {
            if (frame == null || frame.Length < NearbyFrameChunker.HeaderSize)
                return null;
            var messageId = BitConverter.ToInt64(frame, 0);
            int index = BitConverter.ToUInt16(frame, 8);
            int count = BitConverter.ToUInt16(frame, 10);
            if (count == 0 || index >= count)
                return null;
            var payloadLength = frame.Length - NearbyFrameChunker.HeaderSize;
            var key = peerId + ":" + messageId;

            lock (_gate)
            {
                Expire();
                if (!_partials.TryGetValue(key, out var partial))
                {
                    if (_partials.Count >= _maximumPartials)
                        return null;
                    partial = new Partial { Chunks = new byte[count][], Started = _clock() };
                    _partials[key] = partial;
                }
                if (partial.Chunks.Length != count || partial.Chunks[index] != null)
                    return null;
                partial.Bytes += payloadLength;
                if (partial.Bytes > _maximumMessageBytes)
                {
                    _partials.Remove(key);
                    return null;
                }
                var chunk = new byte[payloadLength];
                Buffer.BlockCopy(frame, NearbyFrameChunker.HeaderSize, chunk, 0, payloadLength);
                partial.Chunks[index] = chunk;
                if (++partial.Received < count)
                    return null;
                _partials.Remove(key);
                var message = new byte[partial.Bytes];
                var offset = 0;
                foreach (var c in partial.Chunks)
                {
                    Buffer.BlockCopy(c, 0, message, offset, c.Length);
                    offset += c.Length;
                }
                return message;
            }
        }

        private void Expire()
        {
            var now = _clock();
            foreach (var key in _partials.Where(p => now - p.Value.Started > _timeout).Select(p => p.Key).ToList())
                _partials.Remove(key);
        }
    }
}
