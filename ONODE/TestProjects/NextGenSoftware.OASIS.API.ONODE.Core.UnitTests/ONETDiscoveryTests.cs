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

        [Fact]
        public async Task LoadPeerCacheAsync_CorruptCache_FailsInsteadOfStartingFromEmptyState()
        {
            string dataDirectory = Path.Combine(Path.GetTempPath(), $"onet-corrupt-cache-{Guid.NewGuid():N}");
            Directory.CreateDirectory(dataDirectory);
            await File.WriteAllTextAsync(Path.Combine(dataDirectory, "onet-peers.json"), "{not-json");
            try
            {
                var dna = new OASISDNA { OASIS = { DataDirectory = dataDirectory } };
                var discovery = new ONETDiscovery(storageProvider: null, dna);

                var loadTask = (Task)InvokePrivate(discovery, "LoadPeerCacheAsync");
                Func<Task> action = async () => await loadTask;

                await action.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*Failed to load persisted OASIS state*");
            }
            finally
            {
                Directory.Delete(dataDirectory, recursive: true);
            }
        }

        [Fact]
        public async Task RegisterNodeAsync_PeerCacheWriteFailure_ReturnsSuccessfulRegistrationWithWarning()
        {
            string parent = Path.Combine(Path.GetTempPath(), $"onet-cache-write-{Guid.NewGuid():N}");
            Directory.CreateDirectory(parent);
            string fileWhereDirectoryIsRequired = Path.Combine(parent, "occupied");
            await File.WriteAllTextAsync(fileWhereDirectoryIsRequired, "occupied");
            try
            {
                var dna = new OASISDNA { OASIS = { DataDirectory = fileWhereDirectoryIsRequired } };
                var discovery = new ONETDiscovery(storageProvider: null, dna);

                var result = await discovery.RegisterNodeAsync("local-node", "127.0.0.1:9001", new());

                result.IsError.Should().BeFalse();
                result.Result.Should().BeTrue();
                result.IsWarning.Should().BeTrue();
                result.WarningCount.Should().Be(1);
                result.Message.Should().Contain("peer cache was not persisted");
                result.InnerMessages.Should().ContainSingle(message => message.Contains("OASIS_PERSISTENCE_SAVE_FAILED"));
            }
            finally
            {
                Directory.Delete(parent, recursive: true);
            }
        }

        [Fact]
        public async Task PerformIterativeDHTLookupAsync_ChainOfRealHttpServers_DiscoversTransitivePeerByHopping()
        {
            // Three real local HTTP servers: bootstrap node A advertises only B. B (when queried) advertises
            // only C. A real iterative lookup starting from A must hop A -> B -> C to find node C, proving
            // this is genuine breadth-first network traversal and not a single-hop fetch.
            var portB = GetFreeTcpPort();
            var portC = GetFreeTcpPort();
            var nodeB = CreateAuthenticatedNode($"127.0.0.1:{portB}");
            var nodeC = CreateAuthenticatedNode($"127.0.0.1:{portC}");

            using var listenerA = StartNodesServer(out var portA, JsonSerializer.Serialize(new[] { nodeB }));
            using var listenerB = StartNodesServer(portB, JsonSerializer.Serialize(new[] { nodeC }));
            using var listenerC = StartNodesServer(portC, "[]");

            try
            {
                var discovery = new ONETDiscovery(storageProvider: null) { BootstrapServers = new() { $"http://127.0.0.1:{portA}" } };
                var query = new DHTQuery { MaxResults = 10, Timeout = TimeSpan.FromSeconds(5) };

                var resultTask = (Task<System.Collections.Generic.List<DHTResult>>)InvokePrivate(discovery, "PerformIterativeDHTLookupAsync", query);
                var results = await resultTask;

                results.Select(r => r.NodeInfo.Id).Should().Contain(new[] { nodeB.Id, nodeC.Id },
                    "the lookup should hop through B to discover C, not just return A's immediate peer list");
            }
            finally
            {
                listenerA.Stop();
                listenerB.Stop();
                listenerC.Stop();
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
