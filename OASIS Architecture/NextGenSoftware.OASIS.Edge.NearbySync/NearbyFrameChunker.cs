using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>
    /// Splits a message into link-sized frames and reassembles them. BLE frames are tiny (often under 200
    /// bytes), so every message is chunked. Loss is not retransmitted here: a lost chunk expires the partial
    /// message and the idempotent sync exchange is simply retried by the caller.
    /// </summary>
    public static class NearbyFrameChunker
    {
        public const int HeaderSize = 12;
        public const int MinimumFrameSize = 16;

        public static IReadOnlyList<byte[]> Split(byte[] message, int maximumFrameSize, long messageId)
        {
            if (maximumFrameSize < MinimumFrameSize)
                throw new ArgumentOutOfRangeException(nameof(maximumFrameSize), $"Frames must be at least {MinimumFrameSize} bytes.");
            var chunkSize = maximumFrameSize - HeaderSize;
            var count = Math.Max(1, (message.Length + chunkSize - 1) / chunkSize);
            if (count > ushort.MaxValue)
                throw new ArgumentException("The message is too large for this link.", nameof(message));
            var frames = new List<byte[]>(count);
            for (var i = 0; i < count; i++)
            {
                var length = Math.Min(chunkSize, message.Length - i * chunkSize);
                var frame = new byte[HeaderSize + length];
                BitConverter.GetBytes(messageId).CopyTo(frame, 0);
                BitConverter.GetBytes((ushort)i).CopyTo(frame, 8);
                BitConverter.GetBytes((ushort)count).CopyTo(frame, 10);
                Buffer.BlockCopy(message, i * chunkSize, frame, HeaderSize, length);
                frames.Add(frame);
            }
            return frames;
        }
    }
}
