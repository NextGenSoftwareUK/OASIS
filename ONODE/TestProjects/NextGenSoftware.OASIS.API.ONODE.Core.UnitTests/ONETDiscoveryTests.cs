using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    /// <summary>
    /// Regression tests for ONETDiscovery's bootstrap-server discovery. Previously QueryBootstrapServersAsync,
    /// GetBootstrapNodesAsync and SendDHTQueryToNodeAsync were stubs that unconditionally returned empty/failed
    /// results no matter what was passed in - so discovery could never find a single real peer. These tests
    /// spin up a real local HTTP listener (not a mock) to confirm the discovery code now performs genuine
    /// HTTP calls and parses real responses.
    /// </summary>
    public class ONETDiscoveryTests
    {
        private static object InvokePrivate(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            return method!.Invoke(target, args)!;
        }

        [Fact]
        public async Task QueryBootstrapServersAsync_NoServersConfigured_FailsHonestlyInsteadOfFabricatingNodes()
        {
            var discovery = new ONETDiscovery(storageProvider: null) { BootstrapServers = new() };

            var query = new BootstrapQuery { BootstrapServers = discovery.BootstrapServers, Timeout = 2000 };
            var resultTask = (Task<BootstrapResponse>)InvokePrivate(discovery, "QueryBootstrapServersAsync", query);
            var result = await resultTask;

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("No bootstrap servers configured");
        }

        [Fact]
        public async Task QueryBootstrapServersAsync_RealLocalHttpServer_ReturnsRealParsedNodes()
        {
            using var listener = new HttpListener();
            var port = GetFreeTcpPort();
            var prefix = $"http://127.0.0.1:{port}/";
            listener.Prefixes.Add(prefix);
            listener.Start();
            var node = CreateAuthenticatedNode("127.0.0.1:9001");
            node.Capabilities.Add("relay");

            var serverTask = Task.Run(async () =>
            {
                var context = await listener.GetContextAsync();
                var json = JsonSerializer.Serialize(new[] { node });
                var bytes = Encoding.UTF8.GetBytes(json);
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                context.Response.Close();
            });

            try
            {
                var discovery = new ONETDiscovery(storageProvider: null) { BootstrapServers = new() { prefix.TrimEnd('/') } };
                var query = new BootstrapQuery { BootstrapServers = discovery.BootstrapServers, Timeout = 5000 };

                var resultTask = (Task<BootstrapResponse>)InvokePrivate(discovery, "QueryBootstrapServersAsync", query);
                var result = await resultTask;
                await serverTask;

                result.Success.Should().BeTrue();
                result.Nodes.Should().ContainSingle(n => n.Id == node.Id && n.Address == "127.0.0.1:9001");
                result.ServerUsed.Should().Be(prefix.TrimEnd('/'));
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task GetBootstrapNodesAsync_NoServersConfigured_ReturnsEmptyNotFabricatedSeeds()
        {
            var discovery = new ONETDiscovery(storageProvider: null) { BootstrapServers = new() };

            var resultTask = (Task<System.Collections.Generic.List<DHTNode>>)InvokePrivate(discovery, "GetBootstrapNodesAsync");
            var result = await resultTask;

            result.Should().BeEmpty();
        }

        private static KademliaRoutingTable GetKademliaTable(ONETDiscovery discovery)
            => (KademliaRoutingTable)typeof(ONETDiscovery)
                .GetField("_kademliaTable", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(discovery)!;

        private static ONETNode[] KnownPeers() => new[]
        {
            new ONETNode { Id = "peer-a", Address = "10.0.0.1:9000" },
            new ONETNode { Id = "peer-b", Address = "10.0.0.2:9000" },
            new ONETNode { Id = "local-node", Address = "10.0.0.9:9000" },
            new ONETNode { Id = "no-address", Address = "" },
        };

        [Fact]
        public void AddKnownPeers_AfterLocalIdentityFromDna_AddsPeersToKademliaTable()
        {
            var dna = new OASISDNA();
            dna.OASIS.ONET = new ONETConfig { NodeId = "local-node", BootstrapServers = new() };
            var discovery = new ONETDiscovery(storageProvider: null, dna);

            discovery.AddKnownPeers(KnownPeers());

            var table = GetKademliaTable(discovery);
            table.Count.Should().Be(2, "the local node and address-less peers are never Kademlia contacts");
            table.GetClosestNodes("peer-a", 1).Single().Address.Should().Be("10.0.0.1:9000");
        }

        [Fact]
        public void SetLocalNodeId_AfterKnownPeersAdded_SeedsKademliaTable()
        {
            var discovery = new ONETDiscovery(storageProvider: null);
            discovery.AddKnownPeers(KnownPeers());

            discovery.SetLocalNodeId("local-node");

            GetKademliaTable(discovery).Count.Should().Be(2);
        }

        [Fact]
        public async Task RegisterNodeAsync_OtherNode_DoesNotReplaceLocalIdentity()
        {
            var dna = new OASISDNA();
            dna.OASIS.ONET = new ONETConfig { NodeId = "local-node", BootstrapServers = new() };
            var discovery = new ONETDiscovery(storageProvider: null, dna);

            var result = await discovery.RegisterNodeAsync("some-other-node", "10.0.0.5:9000", new());

            result.IsError.Should().BeFalse();
            typeof(ONETDiscovery).GetField("_localNodeId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(discovery).Should().Be("local-node");
        }

        [Fact]
        public async Task PerformIterativeDHTLookupAsync_BootstrapThenOnetPeerExchange_DiscoversTransitivePeerByHopping()
        {
            // Bootstrap server A (real HTTP) advertises only B. B, queried over ONET peer exchange, advertises only
            // C. The iterative lookup must hop A -> B -> C to find C: genuine traversal, not a single-hop fetch.
            var nodeB = CreateAuthenticatedNode($"127.0.0.1:{GetFreeTcpPort()}");
            var nodeC = CreateAuthenticatedNode($"127.0.0.1:{GetFreeTcpPort()}");

            using var listenerA = StartNodesServer(out var portA, JsonSerializer.Serialize(new[] { nodeB }));

            try
            {
                var discovery = new ONETDiscovery(storageProvider: null)
                {
                    BootstrapServers = new() { $"http://127.0.0.1:{portA}" },
                    QueryPeersOverOnet = (nodeId, _, _) => Task.FromResult<System.Collections.Generic.List<NodeInfo>?>(
                        nodeId == nodeB.Id ? new() { nodeC } : new())
                };
                var query = new DHTQuery { MaxResults = 10, Timeout = TimeSpan.FromSeconds(5) };

                var resultTask = (Task<System.Collections.Generic.List<DHTResult>>)InvokePrivate(discovery, "PerformIterativeDHTLookupAsync", query);
                var results = await resultTask;

                results.Select(r => r.NodeInfo.Id).Should().Contain(new[] { nodeB.Id, nodeC.Id },
                    "the lookup should hop through B to discover C, not just return A's immediate peer list");
            }
            finally
            {
                listenerA.Stop();
            }
        }

        private static HttpListener StartNodesServer(out int port, string responseJson)
        {
            port = GetFreeTcpPort();
            return StartNodesServer(port, responseJson);
        }

        private static HttpListener StartNodesServer(int port, string responseJson)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();

            _ = Task.Run(async () =>
            {
                try
                {
                    while (listener.IsListening)
                    {
                        var context = await listener.GetContextAsync();
                        var bytes = Encoding.UTF8.GetBytes(responseJson);
                        context.Response.ContentType = "application/json";
                        context.Response.ContentLength64 = bytes.Length;
                        await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                        context.Response.Close();
                    }
                }
                catch (HttpListenerException) { /* listener stopped */ }
                catch (ObjectDisposedException) { /* listener stopped */ }
            });

            return listener;
        }

        private static int GetFreeTcpPort()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static NodeInfo CreateAuthenticatedNode(string address)
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var publicKey = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());
            return new NodeInfo
            {
                Id = ONETSecurity.DeriveNodeId(publicKey)!,
                PublicKey = publicKey,
                Address = address
            };
        }
    }
}
