using System;
using System.IO;
using System.Text;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>
    /// A signed, routable message. Everything except <see cref="HopsRemaining"/> is signed, so relays can
    /// decrement the hop count without invalidating the sender's signature, but cannot alter content.
    /// </summary>
    public sealed class NearbyEnvelope
    {
        public const byte WireVersion = 1;
        public const int MaximumPayloadBytes = 1024 * 1024;

        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public NearbyEnvelopeKind Kind { get; set; }
        public string SenderNodeId { get; set; }
        public string RecipientNodeId { get; set; }
        public byte HopsRemaining { get; set; }
        public byte[] Payload { get; set; } = Array.Empty<byte>();
        public byte[] Signature { get; set; } = Array.Empty<byte>();

        public byte[] GetSignedBytes()
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms, Encoding.UTF8))
            {
                w.Write(WireVersion);
                w.Write((byte)Kind);
                w.Write(Id.ToByteArray());
                w.Write(CorrelationId.ToByteArray());
                WriteString(w, SenderNodeId);
                WriteString(w, RecipientNodeId);
                w.Write(Payload.Length);
                w.Write(Payload);
                w.Flush();
                return ms.ToArray();
            }
        }

        public byte[] ToBytes()
        {
            var signed = GetSignedBytes();
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                w.Write(HopsRemaining);
                w.Write(signed.Length);
                w.Write(signed);
                w.Write((ushort)Signature.Length);
                w.Write(Signature);
                w.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>Parses wire bytes; returns false for anything malformed or oversized (never throws on hostile input).</summary>
        public static bool TryParse(byte[] data, out NearbyEnvelope envelope)
        {
            envelope = null;
            try
            {
                using (var ms = new MemoryStream(data))
                using (var r = new BinaryReader(ms, Encoding.UTF8))
                {
                    var hops = r.ReadByte();
                    var signedLength = r.ReadInt32();
                    if (signedLength < 0 || signedLength > MaximumPayloadBytes + 1024 || signedLength > ms.Length - ms.Position)
                        return false;
                    var signed = r.ReadBytes(signedLength);
                    var sigLength = r.ReadUInt16();
                    if (sigLength > ms.Length - ms.Position)
                        return false;
                    var signature = r.ReadBytes(sigLength);

                    using (var sm = new MemoryStream(signed))
                    using (var sr = new BinaryReader(sm, Encoding.UTF8))
                    {
                        if (sr.ReadByte() != WireVersion)
                            return false;
                        var kind = (NearbyEnvelopeKind)sr.ReadByte();
                        if (kind != NearbyEnvelopeKind.Request && kind != NearbyEnvelopeKind.Response)
                            return false;
                        var e = new NearbyEnvelope
                        {
                            Kind = kind,
                            HopsRemaining = hops,
                            Id = new Guid(sr.ReadBytes(16)),
                            CorrelationId = new Guid(sr.ReadBytes(16)),
                            SenderNodeId = ReadString(sr),
                            RecipientNodeId = ReadString(sr)
                        };
                        var payloadLength = sr.ReadInt32();
                        if (payloadLength < 0 || payloadLength > MaximumPayloadBytes || payloadLength != sm.Length - sm.Position)
                            return false;
                        e.Payload = sr.ReadBytes(payloadLength);
                        e.Signature = signature;
                        envelope = e;
                        return true;
                    }
                }
            }
            catch (Exception ex) when (ex is EndOfStreamException || ex is ArgumentException || ex is IOException || ex is FormatException)
            {
                return false;
            }
        }

        private static void WriteString(BinaryWriter w, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            w.Write((ushort)bytes.Length);
            w.Write(bytes);
        }

        private static string ReadString(BinaryReader r)
        {
            var length = r.ReadUInt16();
            return Encoding.UTF8.GetString(r.ReadBytes(length));
        }
    }
}
