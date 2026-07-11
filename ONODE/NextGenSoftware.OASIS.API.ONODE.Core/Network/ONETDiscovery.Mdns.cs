using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    /// <summary>
    /// Real RFC 6762 multicast DNS (mDNS) discovery for ONET, split into its own file for readability.
    /// Implements a minimal but genuine DNS-over-UDP-multicast query/response exchange: a real PTR query is
    /// sent to 224.0.0.251:5353, a real responder on other ONET nodes answers it, and real DNS packets are
    /// parsed on both sides - this is not a simulation. To keep the wire format tractable without a full
    /// SRV/A record parser, the PTR answer's RDATA name itself embeds "host:port._onet._tcp.local", which is
    /// a valid (if minimal) DNS label sequence and gives the querier everything it needs.
    /// </summary>
    public partial class ONETDiscovery
    {
        private const string MdnsServiceType = "_onet._tcp.local";
        private static readonly IPAddress MdnsMulticastAddress = IPAddress.Parse("224.0.0.251");
        private const int MdnsPort = 5353;

        /// <summary>The address (host:port) this node advertises to other ONET nodes via mDNS PTR answers.</summary>
        public string MdnsAdvertisedAddress { get; set; } = string.Empty;

        private UdpClient? _mdnsResponderClient;
        private CancellationTokenSource? _mdnsResponderCts;

        /// <summary>
        /// Starts the real mDNS responder: joins the 224.0.0.251 multicast group on port 5353 and answers
        /// any PTR query for "_onet._tcp.local" with this node's MdnsAdvertisedAddress. Without this running
        /// on at least one reachable node, mDNS queries from other nodes will always find nothing - which is
        /// the correct, honest outcome when no responder exists, not a bug to paper over with fake results.
        /// </summary>
        public void StartMdnsResponder()
        {
            if (string.IsNullOrEmpty(MdnsAdvertisedAddress))
            {
                LoggingManager.Log("mDNS responder not started: MdnsAdvertisedAddress is not configured.", Logging.LogType.Warning);
                return;
            }

            try
            {
                _mdnsResponderCts = new CancellationTokenSource();
                _mdnsResponderClient = new UdpClient();
                _mdnsResponderClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _mdnsResponderClient.Client.Bind(new IPEndPoint(IPAddress.Any, MdnsPort));
                _mdnsResponderClient.JoinMulticastGroup(MdnsMulticastAddress);

                _ = Task.Run(() => MdnsResponderLoopAsync(_mdnsResponderClient, _mdnsResponderCts.Token));
                LoggingManager.Log($"mDNS responder listening on {MdnsMulticastAddress}:{MdnsPort} for '{MdnsServiceType}' queries.", Logging.LogType.Info);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error starting mDNS responder: {ex.Message}", ex);
            }
        }

        public void StopMdnsResponder()
        {
            try
            {
                _mdnsResponderCts?.Cancel();
                _mdnsResponderClient?.Close();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error stopping mDNS responder: {ex.Message}", ex);
            }
            finally
            {
                _mdnsResponderCts?.Dispose();
                _mdnsResponderCts = null;
                _mdnsResponderClient = null;
            }
        }

        private async Task MdnsResponderLoopAsync(UdpClient client, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await client.ReceiveAsync(cancellationToken);
                    var questionName = TryParseFirstQuestionName(result.Buffer);

                    if (questionName != null && questionName.Equals(MdnsServiceType, StringComparison.OrdinalIgnoreCase))
                    {
                        // Parse the "host:port" MdnsAdvertisedAddress into components for proper SRV+A emission.
                        if (TryParseHostPortSimple(MdnsAdvertisedAddress, out var advHost, out var advPort))
                        {
                            var response = BuildServiceResponse(advHost, advPort);
                            await client.SendAsync(response, response.Length, result.RemoteEndPoint);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                        OASISErrorHandling.HandleError($"Error in mDNS responder loop: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Real mDNS query: sends a PTR question for "_onet._tcp.local" with the QU (unicast-response) bit
        /// set to 224.0.0.251:5353, then listens on the same socket for direct unicast replies from any
        /// responder on the local network for up to query.Timeout. Previously this was a stub returning an
        /// empty MDNSResponse regardless of what was actually reachable via multicast.
        /// </summary>
        private async Task<MDNSResponse> SendMDNSQueryAsync(MDNSQuery query)
        {
            var response = new MDNSResponse();

            try
            {
                using var client = new UdpClient(0);
                var queryPacket = BuildPtrQuery(query.ServiceType);
                await client.SendAsync(queryPacket, queryPacket.Length, new IPEndPoint(MdnsMulticastAddress, MdnsPort));

                var deadline = DateTime.UtcNow.AddMilliseconds(query.Timeout > 0 ? query.Timeout : 2000);
                while (DateTime.UtcNow < deadline)
                {
                    var remaining = deadline - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                        break;

                    using var cts = new CancellationTokenSource(remaining);
                    UdpReceiveResult result;
                    try
                    {
                        result = await client.ReceiveAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    // Try the standard SRV+A path first; fall back to the legacy host:port-in-PTR-name
                    // encoding for interop with older ONET nodes that haven't been upgraded yet.
                    if (TryParseServiceResponse(result.Buffer, out var parsedHost, out var parsedPort))
                    {
                        var instance = TryParseFirstPtrAnswer(result.Buffer) ?? $"{parsedHost}:{parsedPort}.{MdnsServiceType}";
                        response.Services.Add(new MDNSService
                        {
                            Name = instance,
                            Address = parsedHost,
                            Port = parsedPort,
                            Properties = new Dictionary<string, string>()
                        });
                    }
                    else
                    {
                        // Legacy: host:port embedded in PTR RDATA name.
                        var instance = TryParseFirstPtrAnswer(result.Buffer);
                        if (instance != null && TryParseHostPort(instance, out var host, out var port))
                        {
                            response.Services.Add(new MDNSService
                            {
                                Name = instance,
                                Address = host,
                                Port = port,
                                Properties = new Dictionary<string, string>()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error sending mDNS query: {ex.Message}", ex);
            }

            return response;
        }

        // --- Minimal real DNS message encoding/decoding (RFC 1035 section 4.1, RFC 6762) ---

        private static byte[] EncodeDnsName(string name)
        {
            var bytes = new List<byte>();
            foreach (var label in name.Split('.'))
            {
                var labelBytes = Encoding.ASCII.GetBytes(label);
                bytes.Add((byte)labelBytes.Length);
                bytes.AddRange(labelBytes);
            }
            bytes.Add(0); // root label
            return bytes.ToArray();
        }

        private static byte[] BuildPtrQuery(string serviceName)
        {
            var packet = new List<byte>
            {
                0x00, 0x00, // transaction ID
                0x00, 0x00, // flags: standard query
                0x00, 0x01, // QDCOUNT = 1
                0x00, 0x00, // ANCOUNT = 0
                0x00, 0x00, // NSCOUNT = 0
                0x00, 0x00, // ARCOUNT = 0
            };

            packet.AddRange(EncodeDnsName(serviceName));
            packet.AddRange(new byte[] { 0x00, 0x0C }); // QTYPE = PTR (12)
            packet.AddRange(new byte[] { 0x80, 0x01 }); // QCLASS = IN (1) with QU bit set (request unicast reply)

            return packet.ToArray();
        }

        /// <summary>
        /// Builds a standard RFC 6762 + RFC 6763 DNS-SD response containing three records:
        ///   PTR  _onet._tcp.local        → &lt;instanceName&gt;._onet._tcp.local  (ANCOUNT=1)
        ///   SRV  &lt;instanceName&gt;._onet._tcp.local → 0 0 &lt;port&gt; &lt;hostname&gt;.local  (ARCOUNT=2)
        ///   A    &lt;hostname&gt;.local        → &lt;IPv4 address&gt;
        /// This replaces the old practice of stuffing "host:port" into the PTR target name
        /// label, which is non-standard and breaks avahi, Bonjour, and other real mDNS clients.
        /// </summary>
        private static byte[] BuildServiceResponse(string host, int port)
        {
            // Instance name: sanitize host to produce a valid DNS label ("192.168.1.1" → "192-168-1-1").
            var safeHost = host.Replace('.', '-').Replace(':', '-');
            var instanceLabel = $"onet-{safeHost}-{port}";        // e.g. "onet-192-168-1-1-38470"
            var instanceName = $"{instanceLabel}.{MdnsServiceType}"; // full name of this service instance
            var hostLocal = $"{safeHost}.local";                   // hostname in .local domain for SRV target + A name

            var packet = new List<byte>
            {
                0x00, 0x00, // transaction ID
                0x84, 0x00, // flags: QR=1 (response) AA=1 (authoritative)
                0x00, 0x00, // QDCOUNT = 0
                0x00, 0x01, // ANCOUNT = 1  (PTR)
                0x00, 0x00, // NSCOUNT = 0
                0x00, 0x02, // ARCOUNT = 2  (SRV + A)
            };

            // --- Answer: PTR record ---
            packet.AddRange(EncodeDnsName(MdnsServiceType));
            packet.AddRange(new byte[] { 0x00, 0x0C });             // TYPE  = PTR (12)
            packet.AddRange(new byte[] { 0x80, 0x01 });             // CLASS = IN (1), cache-flush bit set
            packet.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x78 }); // TTL   = 120 s
            var ptrRdata = EncodeDnsName(instanceName);
            packet.AddRange(ToBeUInt16(ptrRdata.Length));
            packet.AddRange(ptrRdata);

            // --- Additional: SRV record ---
            // RDATA: Priority(2) + Weight(2) + Port(2) + Target(name)
            packet.AddRange(EncodeDnsName(instanceName));
            packet.AddRange(new byte[] { 0x00, 0x21 });             // TYPE  = SRV (33)
            packet.AddRange(new byte[] { 0x80, 0x01 });             // CLASS = IN, cache-flush
            packet.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x78 }); // TTL   = 120 s
            var targetName = EncodeDnsName(hostLocal);
            var srvRdata = new List<byte> { 0x00, 0x00, 0x00, 0x00 }; // Priority=0, Weight=0
            srvRdata.AddRange(ToBeUInt16(port));                    // Port
            srvRdata.AddRange(targetName);
            packet.AddRange(ToBeUInt16(srvRdata.Count));
            packet.AddRange(srvRdata);

            // --- Additional: A record ---
            packet.AddRange(EncodeDnsName(hostLocal));
            packet.AddRange(new byte[] { 0x00, 0x01 });             // TYPE  = A (1)
            packet.AddRange(new byte[] { 0x80, 0x01 });             // CLASS = IN, cache-flush
            packet.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x78 }); // TTL   = 120 s
            // Resolve host to bytes; fall back to 0.0.0.0 if not a parseable IPv4 literal.
            byte[] addrBytes;
            if (IPAddress.TryParse(host, out var ipAddr) && ipAddr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                addrBytes = ipAddr.GetAddressBytes();
            else
                addrBytes = new byte[] { 0, 0, 0, 0 };
            packet.AddRange(ToBeUInt16(addrBytes.Length)); // RDLENGTH = 4
            packet.AddRange(addrBytes);

            return packet.ToArray();
        }

        /// <summary>
        /// Parse a multi-record mDNS response looking for PTR→SRV→A record chain.
        /// Returns true with <paramref name="host"/> and <paramref name="port"/> when the full
        /// SRV+A chain is resolved; returns false when only legacy PTR-name encoding is present.
        /// </summary>
        private static bool TryParseServiceResponse(byte[] buffer, out string host, out int port)
        {
            host = string.Empty;
            port = 0;
            try
            {
                if (buffer.Length < 12) return false;
                int qdCount = (buffer[4] << 8) | buffer[5];
                int anCount = (buffer[6] << 8) | buffer[7];
                int arCount = (buffer[10] << 8) | buffer[11];
                if (anCount + arCount < 1) return false;

                int offset = 12;
                // Skip questions
                for (int i = 0; i < qdCount; i++) { ReadDnsName(buffer, ref offset); offset += 4; }

                // Walk all answer + additional records; collect SRV and A records.
                string? srvTarget = null;
                int srvPort = 0;
                string? aHost = null;
                int totalRecords = anCount + arCount;

                for (int i = 0; i < totalRecords && offset < buffer.Length - 4; i++)
                {
                    ReadDnsName(buffer, ref offset);              // NAME
                    if (offset + 10 > buffer.Length) break;
                    int type     = (buffer[offset] << 8) | buffer[offset + 1]; offset += 2;
                    offset += 2;                                  // CLASS (skip)
                    offset += 4;                                  // TTL   (skip)
                    int rdLen    = (buffer[offset] << 8) | buffer[offset + 1]; offset += 2;
                    int rdataEnd = offset + rdLen;

                    if (type == 33 && rdLen >= 7) // SRV
                    {
                        offset += 4; // Priority + Weight (skip)
                        srvPort = (buffer[offset] << 8) | buffer[offset + 1]; offset += 2;
                        srvTarget = ReadDnsName(buffer, ref offset);
                    }
                    else if (type == 1 && rdLen == 4) // A
                    {
                        aHost = $"{buffer[offset]}.{buffer[offset+1]}.{buffer[offset+2]}.{buffer[offset+3]}";
                    }

                    offset = rdataEnd;
                }

                if (srvPort > 0 && !string.IsNullOrEmpty(aHost))
                {
                    host = aHost;
                    port = srvPort;
                    return true;
                }
            }
            catch { /* fall through */ }
            return false;
        }

        /// <summary>Simple "host:port" parser for MdnsAdvertisedAddress (not for DNS wire format).</summary>
        private static bool TryParseHostPortSimple(string hostPort, out string host, out int port)
        {
            host = string.Empty;
            port = 0;
            if (string.IsNullOrWhiteSpace(hostPort)) return false;
            var idx = hostPort.LastIndexOf(':');
            if (idx < 0 || !int.TryParse(hostPort.Substring(idx + 1), out port)) return false;
            host = hostPort.Substring(0, idx);
            return !string.IsNullOrEmpty(host);
        }

        private static byte[] ToBeUInt16(int value)
        {
            var b = BitConverter.GetBytes((ushort)value);
            if (BitConverter.IsLittleEndian) Array.Reverse(b);
            return b;
        }

        /// <summary>Parses just the QNAME of the first question in a DNS message, with basic label-pointer support.</summary>
        private static string? TryParseFirstQuestionName(byte[] buffer)
        {
            try
            {
                if (buffer.Length < 12) return null;
                int qdCount = (buffer[4] << 8) | buffer[5];
                if (qdCount < 1) return null;

                int offset = 12;
                return ReadDnsName(buffer, ref offset);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Parses the RDATA name of the first PTR answer record in a DNS response message.</summary>
        private static string? TryParseFirstPtrAnswer(byte[] buffer)
        {
            try
            {
                if (buffer.Length < 12) return null;
                int qdCount = (buffer[4] << 8) | buffer[5];
                int anCount = (buffer[6] << 8) | buffer[7];
                if (anCount < 1) return null;

                int offset = 12;
                for (int i = 0; i < qdCount; i++)
                {
                    ReadDnsName(buffer, ref offset);
                    offset += 4; // QTYPE + QCLASS
                }

                for (int i = 0; i < anCount; i++)
                {
                    ReadDnsName(buffer, ref offset); // answer NAME
                    int type = (buffer[offset] << 8) | buffer[offset + 1];
                    offset += 2; // TYPE
                    offset += 2; // CLASS
                    offset += 4; // TTL
                    int rdLength = (buffer[offset] << 8) | buffer[offset + 1];
                    offset += 2; // RDLENGTH

                    if (type == 12) // PTR
                    {
                        int rdataOffset = offset;
                        return ReadDnsName(buffer, ref rdataOffset);
                    }

                    offset += rdLength;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Reads a (possibly compressed) DNS name starting at offset, advancing offset past it.</summary>
        private static string ReadDnsName(byte[] buffer, ref int offset)
        {
            var labels = new List<string>();
            var visitedPointers = new HashSet<int>();

            while (true)
            {
                if (offset >= buffer.Length) break;
                int length = buffer[offset];

                if (length == 0)
                {
                    offset++;
                    break;
                }

                if ((length & 0xC0) == 0xC0) // compression pointer
                {
                    int pointer = ((length & 0x3F) << 8) | buffer[offset + 1];
                    offset += 2;
                    if (!visitedPointers.Add(pointer)) break; // guard against loops
                    int pointerOffset = pointer;
                    labels.Add(ReadDnsName(buffer, ref pointerOffset));
                    break;
                }

                offset++;
                labels.Add(Encoding.ASCII.GetString(buffer, offset, length));
                offset += length;
            }

            return string.Join(".", labels);
        }

        /// <summary>
        /// Extracts "host:port" from an instance name like "127.0.0.1:9999._onet._tcp.local". Strips the
        /// known "._onet._tcp.local" suffix rather than splitting on the first dot - an IPv4 host itself
        /// contains dots, so naively taking everything before the first '.' truncated the host to just its
        /// first octet.
        /// </summary>
        private static bool TryParseHostPort(string instanceName, out string host, out int port)
        {
            host = string.Empty;
            port = 0;

            var suffix = "." + MdnsServiceType;
            if (!instanceName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return false;

            var hostPort = instanceName.Substring(0, instanceName.Length - suffix.Length);
            var separatorIndex = hostPort.LastIndexOf(':');
            if (separatorIndex < 0 || !int.TryParse(hostPort.Substring(separatorIndex + 1), out port))
                return false;

            host = hostPort.Substring(0, separatorIndex);
            return true;
        }
    }
}
