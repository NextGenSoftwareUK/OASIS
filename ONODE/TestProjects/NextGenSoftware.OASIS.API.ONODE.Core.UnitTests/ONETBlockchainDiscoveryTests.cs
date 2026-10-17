using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests
{
    /// <summary>
    /// Regression tests for real blockchain registry discovery. Previously CallSmartContractFunctionAsync
    /// was a stub that unconditionally returned a failed/empty ContractResult regardless of configuration.
    /// These tests run a real local HTTP server that mimics an Ethereum JSON-RPC node's eth_call response to
    /// confirm the real RPC call and ABI decoding logic works end-to-end.
    /// </summary>
    public class ONETBlockchainDiscoveryTests
    {
        private static object InvokePrivate(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            return method!.Invoke(target, args)!;
        }

        [Fact]
        public async Task CallSmartContractFunctionAsync_NoContractAddress_FailsHonestly()
        {
            var discovery = new ONETDiscovery(storageProvider: null);
            var query = new BlockchainQuery { ContractAddress = "", Timeout = System.TimeSpan.FromSeconds(2) };

            var resultTask = (Task<ContractResult>)InvokePrivate(discovery, "CallSmartContractFunctionAsync", query);
            var result = await resultTask;

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("No ONET registry contract address");
        }

        [Fact]
        public async Task CallSmartContractFunctionAsync_NoRpcEndpointConfigured_FailsHonestly()
        {
            var discovery = new ONETDiscovery(storageProvider: null);
            var query = new BlockchainQuery { ContractAddress = "0xabc", Timeout = System.TimeSpan.FromSeconds(2) };

            var resultTask = (Task<ContractResult>)InvokePrivate(discovery, "CallSmartContractFunctionAsync", query);
            var result = await resultTask;

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("No blockchain RPC endpoint configured");
        }

        [Fact]
        public async Task CallSmartContractFunctionAsync_NoExplicitSelector_ComputesItFromFunctionNameViaKeccak()
        {
            // No Parameters["selector"] supplied - the real selector is computed from FunctionName via
            // Keccak256 and sent as the eth_call "data" field. The mock RPC server here just echoes back
            // whatever "data" it received, so a correct selector proves the auto-computation path is real.
            using var listener = new HttpListener();
            var port = GetFreeTcpPort();
            var prefix = $"http://127.0.0.1:{port}/";
            listener.Prefixes.Add(prefix);
            listener.Start();

            string? receivedData = null;
            var serverTask = Task.Run(async () =>
            {
                var context = await listener.GetContextAsync();
                using var reader = new System.IO.StreamReader(context.Request.InputStream);
                var requestBody = await reader.ReadToEndAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(requestBody);
                receivedData = doc.RootElement.GetProperty("params")[0].GetProperty("data").GetString();

                var json = "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":\"0x\"}";
                var bytes = Encoding.UTF8.GetBytes(json);
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                context.Response.Close();
            });

            try
            {
                var discovery = new ONETDiscovery(storageProvider: null) { BlockchainRpcEndpointUrl = prefix.TrimEnd('/') };
                var query = new BlockchainQuery
                {
                    ContractAddress = "0xRegistryContract",
                    FunctionName = "getRegisteredNodes",
                    Timeout = System.TimeSpan.FromSeconds(5)
                };

                var resultTask = (Task<ContractResult>)InvokePrivate(discovery, "CallSmartContractFunctionAsync", query);
                await resultTask;
                await serverTask;

                receivedData.Should().Be(NextGenSoftware.OASIS.API.ONODE.Core.Network.Keccak256.ComputeFunctionSelectorHex("getRegisteredNodes()"));
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public async Task CallSmartContractFunctionAsync_RealJsonRpcServer_DecodesAddressArrayFromAbiEncodedResult()
        {
            // ABI-encode a return value of address[] { 0x1111...1111, 0x2222...2222 } exactly as a real
            // Ethereum node would: offset word (0x20), length word (2), then two right-aligned address words.
            var abiHex =
                "0x" +
                "0000000000000000000000000000000000000000000000000000000000000020" + // offset to array data
                "0000000000000000000000000000000000000000000000000000000000000002" + // array length = 2
                "0000000000000000000000001111111111111111111111111111111111111111" + // address 1
                "0000000000000000000000002222222222222222222222222222222222222222";  // address 2

            using var listener = new HttpListener();
            var port = GetFreeTcpPort();
            var prefix = $"http://127.0.0.1:{port}/";
            listener.Prefixes.Add(prefix);
            listener.Start();

            var serverTask = Task.Run(async () =>
            {
                var context = await listener.GetContextAsync();
                var json = "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":\"" + abiHex + "\"}";
                var bytes = Encoding.UTF8.GetBytes(json);
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                context.Response.Close();
            });

            try
            {
                var discovery = new ONETDiscovery(storageProvider: null) { BlockchainRpcEndpointUrl = prefix.TrimEnd('/') };
                var query = new BlockchainQuery
                {
                    ContractAddress = "0xRegistryContract",
                    Timeout = System.TimeSpan.FromSeconds(5),
                    Parameters = new Dictionary<string, object> { ["selector"] = "0x5a3b7e42" }
                };

                var resultTask = (Task<ContractResult>)InvokePrivate(discovery, "CallSmartContractFunctionAsync", query);
                var result = await resultTask;
                await serverTask;

                result.Success.Should().BeTrue();
                var addresses = result.Data.Should().BeAssignableTo<List<string>>().Subject;
                addresses.Should().BeEquivalentTo(new[]
                {
                    "0x1111111111111111111111111111111111111111",
                    "0x2222222222222222222222222222222222222222"
                });
            }
            finally
            {
                listener.Stop();
            }
        }

        [Fact]
        public void ParseNodeInfoFromBlockchainData_AddressList_ConvertsToNodeInfo()
        {
            var discovery = new ONETDiscovery(storageProvider: null);
            var method = typeof(ONETDiscovery).GetMethod("ParseNodeInfoFromBlockchainData", BindingFlags.NonPublic | BindingFlags.Instance);

            var addresses = new List<string> { "0xabc123" };
            var nodes = (List<NodeInfo>)method!.Invoke(discovery, new object[] { addresses })!;

            nodes.Should().ContainSingle(n => n.Id == "0xabc123" && n.Address == "0xabc123");
        }

        private static int GetFreeTcpPort()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
