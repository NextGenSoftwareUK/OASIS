using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    /// <summary>
    /// Regression tests for real mDNS discovery. Previously SendMDNSQueryAsync was a stub that always
    /// returned an empty MDNSResponse, and there was no responder at all - so mDNS could never find a node
    /// even on the same machine. These tests run a real responder (joining the actual 224.0.0.251:5353
    /// multicast group) and a real querier in the same process to confirm the genuine UDP/DNS exchange works.
    /// </summary>
    public class ONETMdnsTests
    {
        private static object InvokePrivate(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            return method!.Invoke(target, args)!;
        }

        [Fact]
        public async Task SendMDNSQueryAsync_RealResponderRunning_DiscoversItOverRealMulticastUdp()
        {
            var responderNode = new ONETDiscovery(storageProvider: null) { MdnsAdvertisedAddress = "127.0.0.1:9999" };
            responderNode.StartMdnsResponder();

            try
            {
                await Task.Delay(300); // let the responder join the multicast group

                var querier = new ONETDiscovery(storageProvider: null);
                var query = new MDNSQuery { ServiceType = "_onet._tcp.local", Timeout = 2000 };

                var resultTask = (Task<MDNSResponse>)InvokePrivate(querier, "SendMDNSQueryAsync", query);
                var response = await resultTask;

                response.Services.Should().Contain(s => s.Address == "127.0.0.1" && s.Port == 9999);
            }
            finally
            {
                responderNode.StopMdnsResponder();
            }
        }

        [Fact]
        public void DnsPacketEncoding_QueryThenResponse_RoundTripsWithoutNetworking()
        {
            // Isolates the pure DNS packet encode/parse logic from any real networking, so a failure here
            // means a parsing bug, while a pass + a failing live-multicast test means the test environment's
            // network stack doesn't support multicast (common in sandboxed/virtualized CI).
            var discoveryType = typeof(ONETDiscovery);

            var buildQuery = discoveryType.GetMethod("BuildPtrQuery", BindingFlags.NonPublic | BindingFlags.Static);
            var buildResponse = discoveryType.GetMethod("BuildPtrResponse", BindingFlags.NonPublic | BindingFlags.Static);
            var parseQuestion = discoveryType.GetMethod("TryParseFirstQuestionName", BindingFlags.NonPublic | BindingFlags.Static);
            var parseAnswer = discoveryType.GetMethod("TryParseFirstPtrAnswer", BindingFlags.NonPublic | BindingFlags.Static);
            var parseHostPort = discoveryType.GetMethod("TryParseHostPort", BindingFlags.NonPublic | BindingFlags.Static);

            var queryBytes = (byte[])buildQuery!.Invoke(null, new object[] { "_onet._tcp.local" })!;
            var parsedQuestionName = (string?)parseQuestion!.Invoke(null, new object[] { queryBytes });
            parsedQuestionName.Should().Be("_onet._tcp.local");

            var responseBytes = (byte[])buildResponse!.Invoke(null, new object[] { "127.0.0.1:9999" })!;
            var parsedInstance = (string?)parseAnswer!.Invoke(null, new object[] { responseBytes });
            parsedInstance.Should().Be("127.0.0.1:9999._onet._tcp.local");

            var hostPortArgs = new object[] { parsedInstance!, null!, 0 };
            var parsed = (bool)parseHostPort!.Invoke(null, hostPortArgs)!;
            parsed.Should().BeTrue();
            hostPortArgs[1].Should().Be("127.0.0.1");
            hostPortArgs[2].Should().Be(9999);
        }

        [Fact]
        public async Task SendMDNSQueryAsync_NoResponderRunning_ReturnsEmptyHonestly()
        {
            var querier = new ONETDiscovery(storageProvider: null);
            var query = new MDNSQuery { ServiceType = "_onet._tcp.local", Timeout = 500 };

            var resultTask = (Task<MDNSResponse>)InvokePrivate(querier, "SendMDNSQueryAsync", query);
            var response = await resultTask;

            response.Services.Should().BeEmpty();
        }
    }
}
