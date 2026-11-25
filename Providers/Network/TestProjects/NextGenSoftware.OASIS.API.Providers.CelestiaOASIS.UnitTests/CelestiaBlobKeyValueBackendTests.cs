using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.CelestiaOASIS.UnitTests;

/// <summary>
/// Backend semantics against an in-memory celestia-node JSON-RPC substitute that keeps blobs per height and records the
/// share-version-1 signer like consensus does. The live devnet run (IntegrationTests) covers the real node.
/// </summary>
[TestClass]
public sealed class CelestiaBlobKeyValueBackendTests
{
    private const string Namespace = "6f617369732d756e6974";
    private const string Writer = "celestia14fgu87y8dfau4xeph25rhs7qc466rfatq29hv7";
    private const string WriterSigner = "qlHD+Idqe8qbIbqoO8PAxXWhp6s=";

    private sealed class FakeNode : HttpMessageHandler
    {
        public readonly List<List<JsonObject>> Heights = new() { new() }; // height 1 is empty
        public readonly List<string> Methods = new();

        public void Append(JsonObject blob) => Heights.Add(new List<JsonObject> { blob });

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var call = JsonNode.Parse(await request.Content!.ReadAsStringAsync(token))!;
            var method = call["method"]!.GetValue<string>();
            var args = call["params"]!.AsArray();
            Methods.Add(method);
            JsonNode? result = method switch
            {
                "header.LocalHead" => new JsonObject { ["header"] = new JsonObject { ["height"] = Heights.Count.ToString() } },
                "header.WaitForHeight" => new JsonObject(),
                "state.AccountAddress" => Writer,
                "blob.Submit" => Submit(args[0]!.AsArray()),
                "blob.GetAll" => GetAll(args[0]!.GetValue<ulong>()),
                "blob.Get" => Get(args[0]!.GetValue<ulong>(), args[2]!.GetValue<string>()),
                _ => throw new InvalidOperationException("unexpected " + method)
            };
            var body = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = call["id"]!.DeepClone(), ["result"] = result };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
        }

        private JsonNode Submit(JsonArray blobs)
        {
            var block = new List<JsonObject>();
            foreach (var blob in blobs)
            {
                var copy = blob!.DeepClone().AsObject();
                Assert.AreEqual(1, copy["share_version"]!.GetValue<int>());
                Assert.AreEqual(WriterSigner, copy["signer"]!.GetValue<string>());
                block.Add(copy);
            }
            Heights.Add(block);
            return Heights.Count;
        }

        private JsonNode? GetAll(ulong height)
        {
            var blobs = Heights[(int)height - 1];
            if (blobs.Count == 0) return null;
            var result = new JsonArray();
            for (var i = 0; i < blobs.Count; i++)
            {
                var b = blobs[i].DeepClone().AsObject();
                b["index"] = i * 3 + 1;
                b["commitment"] = Commitment(height, i);
                result.Add(b);
            }
            return result;
        }

        private JsonNode? Get(ulong height, string commitment)
            => GetAll(height)!.AsArray().Single(b => b!["commitment"]!.GetValue<string>() == commitment)!.DeepClone();

        private static string Commitment(ulong height, int i) => Convert.ToBase64String(Encoding.ASCII.GetBytes($"c{height}/{i}"));
    }

    private static JsonObject Blob(string? signer, object record) => new()
    {
        ["namespace"] = Convert.ToBase64String(CelestiaBlobKeyValueBackend.NamespaceBytes(Namespace)),
        ["data"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(record))),
        ["share_version"] = signer == null ? 0 : 1, ["signer"] = signer
    };

    private static CelestiaBlobKeyValueBackend Backend(FakeNode node, string? writer = null)
        => new(new HttpClient(node, false) { BaseAddress = new Uri("http://node.invalid/") }, Namespace, 1, writer, ownsClient: true);

    [TestMethod]
    public async Task PutGetOverwriteDeleteAreAppendOnlyAndRecoveredByANewInstance()
    {
        var node = new FakeNode();
        using (var backend = Backend(node))
        {
            await backend.PutAsync("a/1", "one");
            await backend.PutAsync("a/2", "two");
            await backend.PutAsync("a/1", "uno");
            await backend.DeleteAsync("a/2");
            await backend.DeleteAsync("a/missing"); // absent: no tombstone is written
            Assert.AreEqual("uno", await backend.GetAsync("a/1"));
            Assert.IsNull(await backend.GetAsync("a/2"));
        }
        Assert.AreEqual(1 + 4, node.Heights.Count, "three records and one tombstone; deleting a missing key submits nothing");

        using var restarted = Backend(node, Writer);
        await restarted.VerifyAsync();
        CollectionAssert.AreEqual(new[] { "a/1" }, (await restarted.ListKeysAsync("a/")).ToArray());
        Assert.AreEqual("uno", await restarted.GetAsync("a/1"));
        Assert.IsNull(await restarted.GetAsync("a/2"));
    }

    [TestMethod]
    public async Task BlobsFromOtherAccountsAreNotRecords()
    {
        var node = new FakeNode();
        using var backend = Backend(node, Writer);
        await backend.PutAsync("k", "mine");
        node.Append(Blob("AAAAAAAAAAAAAAAAAAAAAAAAAAA=", new { k = "k", v = (string?)null, n = "forged" })); // other signer tombstone
        node.Append(Blob(null, new { k = "k", v = "unsigned", n = "v0" }));                         // share version 0, no signer
        Assert.AreEqual("mine", await backend.GetAsync("k"));
    }

    [TestMethod]
    public async Task MalformedRecordFromTheWriterFailsInsteadOfBeingSkipped()
    {
        var node = new FakeNode();
        node.Append(Blob(WriterSigner, new { unexpected = true }));
        using var backend = Backend(node, Writer);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => backend.VerifyAsync());
    }

    [TestMethod]
    public async Task OversizedRecordIsRejectedBeforeSubmission()
    {
        var node = new FakeNode();
        using var backend = Backend(node, Writer);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() =>
            backend.PutAsync("big", new string('x', CelestiaBlobKeyValueBackend.MaxBlobDataBytes)));
        CollectionAssert.DoesNotContain(node.Methods, "blob.Submit");
    }

    [TestMethod]
    public void WriterAddressChecksumIsVerified()
        => Assert.ThrowsException<FormatException>(() =>
        {
            using var backend = Backend(new FakeNode(), "celestia14fgu87y8dfau4xeph25rhs7qc466rfatq29hv8");
            backend.PutAsync("k", "v").GetAwaiter().GetResult();
        });
}
