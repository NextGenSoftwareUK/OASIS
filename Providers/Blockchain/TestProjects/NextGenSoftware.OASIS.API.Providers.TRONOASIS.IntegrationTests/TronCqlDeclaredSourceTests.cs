using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using TronNet;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.IntegrationTests;

/// <summary>A separately declared local source; never recovery/replay of the old fixture.</summary>
public sealed class TronCqlDeclaredSourceTests
{
    private const string Genesis = "0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f";
    private sealed record Seed(Guid Actor, Guid[] Ids, string Contract, string Deployment);

    [Fact]
    public async Task DeclaredLocalSourceUsesJournaledWritesAndNativeCqlReads()
    {
        Assert.Equal("create-or-read-explicit-separate-source", Environment.GetEnvironmentVariable("OASIS_TRON_CQL_DECLARED"));
        var state = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_TRON_CQL_STATE")
            ?? throw new InvalidOperationException("Explicit persistent state required."));
        Assert.True(Directory.Exists(state));
        using var ownership = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "source.pending.json");
        Assert.False(File.Exists(pending), "Preserve unknown original setup; never replay/reseed.");
        Assert.True(File.Exists(Path.Combine(state, "declared-source-admission.json")), "Runner must bind retained node and explicitly separate source before writes.");
        using var admission = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "declared-source-admission.json")));
        Assert.Equal("Independent declared TRON CQL contract", admission.RootElement.GetProperty("scope").GetString());
        Assert.Equal(Genesis, admission.RootElement.GetProperty("genesis").GetString());
        Assert.Equal("ef2843babd111bde2709159e6eaacf2350977cccee738d62d1bcbd8833c46a12", admission.RootElement.GetProperty("containerId").GetString());
        Assert.False(admission.RootElement.GetProperty("originalRecovery").GetBoolean());

        // The existing local TRE fixture signer is public test material. It is
        // never selected for a hosted endpoint, nor printed or stored in evidence.
        var signingKey = new string('0', 63) + "1";
        var owner = new TronECKey(signingKey, TronNetwork.MainNet).GetPublicAddress();
        var encodedKey = Environment.GetEnvironmentVariable("OASIS_TRON_CQL_STORAGE_KEY")
            ?? throw new InvalidOperationException("Separately retained storage key required.");
        var storageKey = Convert.FromBase64String(encodedKey);
        Assert.Equal(32, storageKey.Length);
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        var token = deadline.Token;
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), Genesis, contractFeeLimitSun: 100_000_000);
        var preparations = 0;
        var client = runtime.CreatePreparedNativeTransfers(async (transactionId, network, signed, cancellation) =>
        {
            Assert.Equal(Genesis, network);
            // CreateNew + durable flush precede the SDK's single broadcast.
            // A lost response leaves this identity intact and setup pending.
            await WriteNew(Path.Combine(state, $"write-{preparations++:00}.prepared.json"),
                new { transactionId, network, signedTransactionBase64 = signed }, cancellation);
        });
        Seed seed;
        try
        {
            if (!File.Exists(manifest))
            {
                Assert.Empty(Directory.EnumerateFiles(state, "write-*.prepared.json"));
                var actor = Guid.NewGuid();
                var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
                await WriteNew(pending, new { actor, ids, genesis = Genesis, owner,
                    originalRecovery = false, scope = "Independent declared TRON CQL contract" }, token);
                var bytecodeFile = Environment.GetEnvironmentVariable("OASIS_TRON_CQL_BYTECODE")
                    ?? throw new InvalidOperationException("Compiled shipping contract required.");
                var bytecode = Convert.FromHexString((await File.ReadAllTextAsync(bytecodeFile, token)).Trim());
                var deployment = await client.DeployBytecodeAsync(owner, bytecode, signingKey, token);
                await WriteNew(Path.Combine(state, "deployment.acknowledged.json"),
                    new { deployment.TransactionId, deployment.ContractAddress }, token);
                seed = new Seed(actor, ids, deployment.ContractAddress, deployment.TransactionId);
                using var storage = new TronPrivateStorageBackend(client, owner, seed.Contract,
                    () => signingKey, () => (byte[])storageKey.Clone());
                var provider = new TRONOASIS(rpcEndpoint: "http://127.0.0.1:9090", nativeTransfers: client, storageBackend: storage)
                    { IsVersionControlEnabled = false };
                Assert.False((await provider.ActivateProviderAsync()).IsError);
                try
                {
                    var names = new[] { "Espresso", "Latte", "Tea" };
                    for (var index = 0; index < ids.Length; index++)
                    {
                        var saved = await provider.SaveHolonAsync(new Holon { Id = ids[index], Name = names[index], CreatedByAvatarId = actor }, saveChildren: false);
                        Assert.False(saved.IsError, saved.Message);
                        await WriteNew(Path.Combine(state, $"record-{index:00}.acknowledged.json"),
                            new { id = ids[index], name = names[index], version = saved.Result.Version }, token);
                    }
                }
                finally { Assert.False((await provider.DeActivateProviderAsync()).IsError); }
                Assert.True(preparations >= 4, "Deployment and every physical storage write must be retained before broadcast.");
                await WriteNew(manifest, seed, token);
                File.Delete(pending); // Only after all exact acknowledged outcomes and durable manifest.
            }
            else seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest, token))!;

            var before = Directory.GetFiles(state, "write-*.prepared.json")
                .ToDictionary(path => path, path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
            using var reader = new TronPrivateStorageBackend(client, owner, seed.Contract,
                () => throw new InvalidOperationException("Read phase must never request signing material."), () => (byte[])storageKey.Clone());
            var readProvider = new TRONOASIS(rpcEndpoint: "http://127.0.0.1:9090", nativeTransfers: client, storageBackend: reader);
            Assert.False((await readProvider.ActivateProviderAsync()).IsError);
            try
            {
                foreach (var selected in new[] { seed.Ids, seed.Ids.Take(2).ToArray() })
                {
                    var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TRONOASIS).Result)
                        .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.TRONOASIS, seed.Ids).Result).Result
                        .Where(row => selected.Contains(row.Id)).Result;
                    var plan = CosmicQueryPlanner.Plan(query, ProviderType.TRONOASIS,
                        new[] { new CosmicQueryTarget(ProviderType.TRONOASIS, readProvider.QueryCapabilities) });
                    Assert.False(plan.IsError, plan.Message);
                    var rows = await readProvider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: token);
                    Assert.False(rows.IsError, rows.Message);
                    Assert.Equal(selected.Order(), rows.Result.Select(row => row.Id).Order());
                    Assert.All(rows.Result, row => Assert.Contains(row.Name, new[] { "Espresso", "Latte", "Tea" }));
                }
            }
            finally { Assert.False((await readProvider.DeActivateProviderAsync()).IsError); }
            Assert.Equal(before.Count, Directory.GetFiles(state, "write-*.prepared.json").Length);
            foreach (var entry in before) Assert.Equal(entry.Value, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(entry.Key))));
        }
        finally { CryptographicOperations.ZeroMemory(storageKey); }
    }

    private static async Task WriteNew<T>(string path, T value, CancellationToken token)
    {
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(file, value, cancellationToken: token);
        file.Flush(true);
    }
}
