using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
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

        private static HoloNETP2PProvider Provider(Mock<IP2PNetworkProvider> transport)
            => new HoloNETP2PProvider(ClientWithoutConductorUri(), transport.Object, new ONETProtocol(storageProvider: null));

        private static ONETHolochainNodeRecord SignedRecord(string agent = "uhCAkAgent", string address = "node.example:9000")
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var publicKey = key.ExportSubjectPublicKeyInfo();
            var nodeId = Convert.ToHexString(SHA256.HashData(publicKey)).ToLowerInvariant();
            var signature = key.SignData(Encoding.UTF8.GetBytes(HoloNETP2PProvider.NodeRecordSigningText(nodeId, agent, address)),
                HashAlgorithmName.SHA256);
            return new ONETHolochainNodeRecord
            {
                Agent = agent,
                NodeId = nodeId,
                PublicKey = Convert.ToBase64String(publicKey),
                AdvertisedAddress = address,
                Signature = Convert.ToBase64String(signature)
            };
        }

        [Fact]
        public async Task StartNetworkAsync_NoConductorUri_FailsWithoutStartingOnetTransport()
        {
            var transport = new Mock<IP2PNetworkProvider>();
            var provider = Provider(transport);

            var result = await provider.StartNetworkAsync();

            result.IsError.Should().BeTrue();
            result.Message.Should().Contain("HolochainConductorAppAgentURI");
            transport.Verify(t => t.StartNetworkAsync(), Times.Never);
        }

        [Fact]
        public void TransportEvents_AreForwardedOnceFromTheProvider()
        {
            var transport = new Mock<IP2PNetworkProvider>();
            var provider = Provider(transport);
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
            var provider = Provider(transport);

            (await provider.StopNetworkAsync()).Result.Should().BeTrue();

            transport.Verify(t => t.StopNetworkAsync(), Times.Once);
        }

        [Fact]
        public void VerifyNodeRecord_SignedByItsOwnIdentity_IsAccepted()
            => HoloNETP2PProvider.VerifyNodeRecord(SignedRecord()).Should().BeTrue();

        [Fact]
        public void VerifyNodeRecord_ClaimedForAnotherAgentOrAddress_IsRejected()
        {
            var movedAgent = SignedRecord();
            movedAgent.Agent = "uhCAkSomeoneElse";
            var movedAddress = SignedRecord();
            movedAddress.AdvertisedAddress = "attacker.example:9000";

            HoloNETP2PProvider.VerifyNodeRecord(movedAgent).Should().BeFalse();
            HoloNETP2PProvider.VerifyNodeRecord(movedAddress).Should().BeFalse();
        }

        [Fact]
        public void VerifyNodeRecord_NodeIdNotDerivedFromKey_IsRejected()
        {
            var record = SignedRecord();
            record.NodeId = new string('a', 64);

            HoloNETP2PProvider.VerifyNodeRecord(record).Should().BeFalse();
        }

        [Fact]
        public void VerifyNodeRecord_MalformedKeyOrSignature_IsRejectedNotThrown()
        {
            var record = SignedRecord();
            record.Signature = "not base64!";

            HoloNETP2PProvider.VerifyNodeRecord(record).Should().BeFalse();
        }

        [Fact]
        public void ParseNodes_ReadsTheOnetNodesZomeShape_AndSkipsMalformedEntries()
        {
            var client = ClientWithoutConductorUri();
            var provider = new HoloNETP2PProvider(client, new Mock<IP2PNetworkProvider>().Object, new ONETProtocol(storageProvider: null));
            var agentBytes = new byte[39];
            agentBytes[0] = 0x84; agentBytes[1] = 0x20; agentBytes[2] = 0x24;
            var zomeReturn = new List<object>
            {
                new Dictionary<object, object>
                {
                    ["agent"] = agentBytes,
                    ["record"] = new Dictionary<object, object>
                    {
                        ["node_id"] = "id-1", ["public_key"] = "pk", ["advertised_address"] = "a:1", ["signature"] = "sig"
                    }
                },
                new Dictionary<object, object> { ["agent"] = "not bytes", ["record"] = new Dictionary<object, object>() },
                "not a node"
            };

            var nodes = provider.ParseNodes(zomeReturn);

            nodes.Should().ContainSingle();
            nodes[0].NodeId.Should().Be("id-1");
            nodes[0].AdvertisedAddress.Should().Be("a:1");
            nodes[0].Agent.Should().Be(client.ConvertHoloHashToString(agentBytes));
        }
    }
}
