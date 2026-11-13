using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Web3;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class EthereumCqlRpcTests
{
    private sealed class Activated(string url) : EthereumOASIS(url, "0x" + new string('1', 64),
        31337, "0x" + new string('3', 40))
    {
        public void Attach()
        {
            Web3Client = new Web3(HostURI);
            typeof(EthereumOASIS).GetField("_nextGenSoftwareOasisService", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(this, new NextGenSoftwareOASISService(Web3Client, ContractAddress));
            IsProviderActivated = true;
        }
    }

    // Canonical ABI encoding of the contract's single dynamic Holon tuple.
    // The production Nethereum decoder, native identity checks and ciphertext
    // decoder all run against these controlled JSON-RPC responses.
    private static string Tuple(BigInteger key, string id, string payload)
    {
        static string Word(BigInteger value) => value.ToString("x").TrimStart('0').PadLeft(64, '0');
        static string Text(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            return Word(bytes.Length) + Convert.ToHexString(bytes).ToLowerInvariant().PadRight((bytes.Length + 31) / 32 * 64, '0');
        }
        var identity = Text(id);
        return "0x" + Word(32) + Word(key) + Word(96) + Word(96 + identity.Length / 2)
            + identity + Text(payload);
    }

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("absent")]
    [DataRow("corrupt")]
    [DataRow("rpc-error")]
    [DataRow("foreign")]
    public async Task NativeKeyReadsPinOneBlockAndNeverPublishPartialFailure(string shape)
    {
        var previous = Environment.GetEnvironmentVariable("OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY");
        Environment.SetEnvironmentVariable("OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY", Convert.ToBase64String(new byte[32]));
        var portProbe = new TcpListener(IPAddress.Loopback, 0);
        portProbe.Start();
        var port = ((IPEndPoint)portProbe.LocalEndpoint).Port;
        portProbe.Stop();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var actor = Guid.NewGuid();
        var ids = new[] { Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Guid.Parse("20000000-0000-0000-0000-000000000002") };
        var keys = ids.Select(id => HashUtility.GetNumericHash(id.ToString())).ToArray();
        var serialize = typeof(EthereumOASIS).GetMethod("SerializePrivateStorage", BindingFlags.Static | BindingFlags.NonPublic)!;
        var ciphertext = ids.Select(id => (string)serialize.Invoke(null, new object[] { "holon",
            new Holon { Id = id, Name = "coffee", CreatedByAvatarId = actor } })!).ToArray();
        var methods = new List<string>();
        var calls = 0;
        var server = Task.Run(async () =>
        {
            while (!stop.IsCancellationRequested)
            {
                var context = await listener.GetContextAsync().WaitAsync(stop.Token);
                var request = JsonNode.Parse(await new StreamReader(context.Request.InputStream).ReadToEndAsync())!;
                var method = request["method"]!.GetValue<string>();
                methods.Add(method);
                var response = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = request["id"]!.DeepClone() };
                if (method == "eth_blockNumber") response["result"] = "0x2a";
                else
                {
                    Assert.AreEqual("eth_call", method);
                    Assert.AreEqual("0x2a", request["params"]![1]!.GetValue<string>());
                    var data = request["params"]![0]!["data"]!.GetValue<string>();
                    var key = BigInteger.Parse("0" + data[^64..], System.Globalization.NumberStyles.HexNumber);
                    Assert.AreEqual(keys[calls], key, "Only compiler-selected native keys may be read.");
                    var index = calls++;
                    if (index == 1 && shape == "rpc-error")
                        response["error"] = new JsonObject { ["code"] = -32000, ["message"] = "controlled native failure" };
                    else response["result"] = index == 1 ? shape switch
                    {
                        "absent" => Tuple(0, "", ""),
                        "corrupt" => Tuple(keys[index], ids[index].ToString(), "invalid-ciphertext"),
                        "foreign" => Tuple(keys[0], ids[0].ToString(), ciphertext[0]),
                        _ => Tuple(keys[index], ids[index].ToString(), ciphertext[index])
                    } : Tuple(keys[index], ids[index].ToString(), ciphertext[index]);
                }
                var bytes = Encoding.UTF8.GetBytes(response.ToJsonString());
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, stop.Token);
                context.Response.Close();
            }
        });
        try
        {
            var provider = new Activated($"http://127.0.0.1:{port}/");
            provider.Attach();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.EthereumOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.EthereumOASIS, ids).Result).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.EthereumOASIS,
                new[] { new CosmicQueryTarget(ProviderType.EthereumOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: stop.Token);
            Assert.AreEqual(2, calls);
            CollectionAssert.AreEqual(new[] { "eth_blockNumber", "eth_call", "eth_call" }, methods);
            if (shape is "success" or "absent")
            {
                Assert.IsFalse(result.IsError, result.Exception?.ToString());
                Assert.AreEqual(shape == "success" ? 2 : 1, result.Result.Count);
                Assert.IsTrue(result.IsLoaded);
            }
            else
            {
                Assert.IsTrue(result.IsError);
                Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode);
                Assert.IsNull(result.Result, "Earlier valid rows must not escape a later native failure.");
                Assert.IsFalse(result.IsLoaded);
            }
        }
        finally
        {
            stop.Cancel();
            listener.Stop();
            try { await server; }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
            catch (HttpListenerException) when (stop.IsCancellationRequested) { }
            Environment.SetEnvironmentVariable("OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY", previous);
        }
    }
}
