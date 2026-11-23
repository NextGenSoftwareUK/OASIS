using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    public class ONETHoloNETP2PProviderTests
    {
        private static HoloNETClientAppAgent ClientWithoutConductorUri()
        {
            var client = new HoloNETClientAppAgent("oasis", "agent", new HoloNETDNA());
            client.HoloNETDNA.HolochainConductorAppAgentURI = "";
            return client;
        }

        [Fact]
        public async Task StartNetworkAsync_NoConductorUri_FailsWithoutStartingOnetTransport()
        {
            var transport = new Mock<IP2PNetworkProvider>();
            var provider = new HoloNETP2PProvider(ClientWithoutConductorUri(), transport.Object);

            var result = await provider.StartNetworkAsync();

            result.IsError.Should().BeTrue();
            result.Message.Should().Contain("HolochainConductorAppAgentURI");
            transport.Verify(t => t.StartNetworkAsync(), Times.Never);
        }

        [Fact]
        public void TransportEvents_AreForwardedOnceFromTheProvider()
        {
            var transport = new Mock<IP2PNetworkProvider>();
            var provider = new HoloNETP2PProvider(ClientWithoutConductorUri(), transport.Object);
            object? sender = null;
            var raised = 0;
            provider.NodeConnected += (s, _) => { sender = s; raised++; };

            transport.Raise(t => t.NodeConnected += null, new NodeConnectedEventArgs { NodeId = "peer-1" });

            raised.Should().Be(1);
            sender.Should().BeSameAs(provider);
        }

        [Fact]
        public async Task StopNetworkAsync_StopsOnetTransportAndLeavesConductorToHoloOASIS()
        {
            var transport = new Mock<IP2PNetworkProvider>();
            transport.Setup(t => t.StopNetworkAsync()).ReturnsAsync(new OASISResult<bool> { Result = true });
            var provider = new HoloNETP2PProvider(ClientWithoutConductorUri(), transport.Object);

            (await provider.StopNetworkAsync()).Result.Should().BeTrue();

            transport.Verify(t => t.StopNetworkAsync(), Times.Once);
        }
    }
}
