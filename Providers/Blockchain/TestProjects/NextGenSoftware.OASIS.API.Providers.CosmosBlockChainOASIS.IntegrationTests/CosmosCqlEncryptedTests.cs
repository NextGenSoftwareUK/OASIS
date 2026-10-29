using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests;

[TestClass]
public class CosmosCqlEncryptedTests
{
    private sealed class Probe(string shape) : IKeyValueBackend
    {
        public Dictionary<string, string> Values = new();
        public List<string> Reads = new();
        public int Writes;
        public bool QueryStarted;
        public Task<string> GetAsync(string key, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Reads.Add(key);
            if (QueryStarted && shape == "transport" && Reads.Count == 2) throw new IOException("controlled source outage");
            return Task.FromResult(Values.GetValueOrDefault(key))!;
        }
        public Task PutAsync(string key, string value, CancellationToken token = default)
        { Writes++;Values[key] = value;return Task.CompletedTask; }
        public Task DeleteAsync(string key, CancellationToken token = default)
            => throw new AssertFailedException("CQL must not delete.");
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken token = default)
            => QueryStarted ? throw new AssertFailedException("CQL must not enumerate the source.")
                : Task.FromResult<IReadOnlyList<string>>(Values.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray());
        public Task VerifyAsync(CancellationToken token = default) => Task.CompletedTask;
    }

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("absent")]
    [DataRow("deleted")]
    [DataRow("corrupt")]
    [DataRow("foreign")]
    [DataRow("transport")]
    [DataRow("budget")]
    public async Task ShippingPrivateCodecAndQueryRejectPartialOrUnauthorizedResults(string shape)
    {
        var actor = Guid.NewGuid();
        var ids = new[] { Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Guid.Parse("20000000-0000-0000-0000-000000000002") };
        var probe = new Probe(shape);
        using var storage = new PersistedPrivateKeyValueBackend(probe, () => new byte[32]);
        for (var index = 0; index < ids.Length; index++)
        {
            if (shape == "absent" && index == 1) continue;
            var row = new Holon { Id = shape == "foreign" && index == 1 ? Guid.NewGuid() : ids[index],
                Name = "coffee", CreatedByAvatarId = actor,
                DeletedDate = shape == "deleted" && index == 1 ? new DateTime(2026,10,29,0,0,0,DateTimeKind.Utc) : DateTime.MinValue };
            await storage.PutAsync("oasis/holon/" + ids[index].ToString("D"), OasisJson.Serialize(row));
        }
        var physicalKeys = probe.Values.Keys.ToArray();
        Assert.IsFalse(probe.Values.Values.Any(value => value.Contains("coffee", StringComparison.Ordinal)),
            "The controlled source must exercise actual encrypted records, not plaintext fixtures.");

        // Inject only the transport beneath the existing encrypted storage wrapper;
        // production query, key layout, codec, access and error handling all run.
        using var template = new CosmosBlockChainOASIS(rpcEndpoint: "http://127.0.0.1:1",
            chainId: "controlled", contractAddress: "owned-unused");
        var sdk = typeof(CosmosBlockChainOASIS).GetField("_sdkBackend", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(template);
        var constructor = typeof(CosmosBlockChainOASIS).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(c => c.GetParameters().Length == 6 && c.GetParameters()[1].ParameterType == typeof(PersistedPrivateKeyValueBackend));
        using var provider = (CosmosBlockChainOASIS)constructor.Invoke(new[] { sdk, storage, "", "owned-unused", "utest", (object)6 });
        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        // Activation checks existing encrypted key material. Only subsequent
        // query I/O is subject to this test's no-enumeration/fault boundary.
        probe.Reads.Clear();
        probe.QueryStarted = true;
        if (shape == "corrupt") probe.Values[physicalKeys[1]] = "invalid-ciphertext";
        var writesBefore = probe.Writes;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CosmosBlockChainOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.CosmosBlockChainOASIS, ids).Result).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.CosmosBlockChainOASIS,
            new[] { new CosmicQueryTarget(ProviderType.CosmosBlockChainOASIS, provider.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var result = await provider.ExecuteCosmicQueryAsync(plan.Result, maximumResults: shape == "budget" ? 1 : 100);
        Assert.AreEqual(writesBefore, probe.Writes);
        Assert.AreEqual(2, probe.Reads.Count);
        if (shape != "absent") CollectionAssert.AreEquivalent(physicalKeys, probe.Reads.ToArray());
        if (shape is "success" or "absent" or "deleted")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
            Assert.AreEqual(shape == "success" ? 2 : 1, result.Result.Count);
        }
        else
        {
            Assert.IsTrue(result.IsError);
            Assert.IsNull(result.Result, "Earlier valid rows must not escape a later source failure.");
            Assert.IsFalse(result.IsLoaded);
            Assert.AreEqual(shape == "budget" ? "CQL_QUERY_LIMIT" : "CQL_QUERY_FAILED", result.ErrorCode);
        }
    }
}
