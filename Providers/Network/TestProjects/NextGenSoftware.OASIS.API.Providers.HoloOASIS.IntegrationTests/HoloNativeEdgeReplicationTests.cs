using System.Diagnostics;
using System.Text.Json;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.IntegrationTests;

public sealed class HoloNativeEdgeReplicationTests
{
    [Fact]
    public async Task EdgeReplayKeepsTheOriginalNativeMutation()
    {
        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var endpoint = new Uri(Required("OASIS_HOLOCHAIN_ADMIN_URI"));
        Assert.True(endpoint.IsLoopback);
        var receiptPath = Environment.GetEnvironmentVariable("OASIS_HOLO_EDGE_RESTART_RECEIPT");
        if (receiptPath != null && File.Exists(receiptPath))
        {
            var receipt = JsonSerializer.Deserialize<RestartReceipt>(await File.ReadAllTextAsync(receiptPath));
            Assert.NotNull(receipt);
            await VerifyAfterRestartAsync(endpoint, receipt);
            return;
        }

        var priorBundle = Environment.GetEnvironmentVariable("OASIS_HOLO_EDGE_PRIOR_HAPP");
        var coordinatorWasm = Environment.GetEnvironmentVariable("OASIS_HOLO_EDGE_COORDINATOR_WASM");
        Assert.Equal(string.IsNullOrEmpty(priorBundle), string.IsNullOrEmpty(coordinatorWasm));
        var admin = OpenAdmin(endpoint);
        var connected = await admin.ConnectAsync();
        Assert.False(connected.IsError, connected.Message);
        try
        {
            var installed = await admin.InstallAppAsync(HoloOASIS.OASIS_HAPP_ID,
                priorBundle ?? Required("HOLONET_LIVE_HAPP_PATH"), network_seed: Guid.NewGuid().ToString("N"));
            Assert.False(installed.IsError, installed.Message);
        }
        finally
        {
            await admin.DisconnectAsync();
        }

        var mutation = new SyncOperation
        {
            OperationId = Guid.NewGuid(), AvatarId = Guid.NewGuid(), EntityId = Guid.NewGuid(),
            EntityType = "holon", VersionId = Guid.NewGuid(), Kind = SyncOperationKind.Upsert,
            PayloadJson = "{\"name\":\"edge native fixture\"}"
        };
        var provider = OpenProvider(endpoint);
        string originalHash;
        string dnaHash;
        string agentKey;
        try
        {
            var activated = await provider.ActivateProviderAsync();
            Assert.True(!activated.IsError && activated.Result, activated.Message);
            var dna = provider.HoloNETClientAppAgent.HoloNETDNA;
            dnaHash = dna.DnaHash;
            agentKey = dna.AgentPubKey;
            Assert.False(string.IsNullOrWhiteSpace(dnaHash));
            Assert.False(string.IsNullOrWhiteSpace(agentKey));
            var repository = new HoloEdgeMutationRepository(new HoloNetEdgeAppClient(provider.HoloNETClientAppAgent));
            var applied = await repository.ApplyLocalMutationAsync(mutation, CancellationToken.None);
            Assert.True(!applied.IsError && applied.IsSaved, applied.Message);
            originalHash = await ReadMutationHashAsync(provider, mutation);
            if (coordinatorWasm != null)
            {
                if (Environment.GetEnvironmentVariable("OASIS_HOLO_BATCH_BENCHMARK") == "1")
                    await BenchmarkIndexedReadsAsync(provider, endpoint, dnaHash, coordinatorWasm);
                else
                    await UpdateCoordinatorAsync(endpoint, dnaHash, coordinatorWasm);
                Assert.Equal(originalHash, await ReadMutationHashAsync(provider, mutation));
            }

            var replay = await repository.ApplyLocalMutationAsync(mutation, CancellationToken.None);
            Assert.True(!replay.IsError && replay.IsSaved, replay.Message);
            Assert.Equal(originalHash, await ReadMutationHashAsync(provider, mutation));

            mutation.VersionId = Guid.NewGuid();
            mutation.PayloadJson = "{\"name\":\"conflicting replay\"}";
            var conflict = await repository.ApplyLocalMutationAsync(mutation, CancellationToken.None);
            Assert.True(conflict.IsError, "A conflicting operation ID was acknowledged.");
            Assert.False(conflict.IsSaved);
            Assert.Contains("already bound to a different mutation", conflict.Message);
            Assert.Equal(originalHash, await ReadMutationHashAsync(provider, mutation));
        }
        finally
        {
            if (provider.IsProviderActivated) await provider.DeActivateProviderAsync();
        }

        var reopened = OpenProvider(endpoint);
        try
        {
            var activated = await reopened.ActivateProviderAsync();
            Assert.True(!activated.IsError && activated.Result, activated.Message);
            Assert.Equal(dnaHash, reopened.HoloNETClientAppAgent.HoloNETDNA.DnaHash);
            Assert.Equal(agentKey, reopened.HoloNETClientAppAgent.HoloNETDNA.AgentPubKey);
            Assert.Equal(originalHash, await ReadMutationHashAsync(reopened, mutation));
            var latest = await reopened.HoloNETClientAppAgent.CallZomeFunctionAsync("oasis",
                "get_latest_hyperdrive_entity", new
                {
                    avatar_id = mutation.AvatarId.ToString("D"),
                    entity_type = mutation.EntityType,
                    entity_id = mutation.EntityId.ToString("D")
                });
            Assert.False(latest.IsError, latest.Message);
            Assert.Equal(originalHash, Assert.Single(latest.Records).ActionHash);
        }
        finally
        {
            if (reopened.IsProviderActivated) await reopened.DeActivateProviderAsync();
        }

        if (receiptPath != null)
        {
            Assert.False(string.IsNullOrEmpty(coordinatorWasm));
            var receipt = new RestartReceipt(mutation, dnaHash, agentKey, originalHash);
            await File.WriteAllTextAsync(receiptPath, JsonSerializer.Serialize(receipt));
        }
    }

    private static async Task VerifyAfterRestartAsync(Uri endpoint, RestartReceipt receipt)
    {
        var provider = OpenProvider(endpoint);
        try
        {
            var activated = await provider.ActivateProviderAsync();
            Assert.True(!activated.IsError && activated.Result, activated.Message);
            Assert.Equal(receipt.DnaHash, provider.HoloNETClientAppAgent.HoloNETDNA.DnaHash);
            Assert.Equal(receipt.AgentKey, provider.HoloNETClientAppAgent.HoloNETDNA.AgentPubKey);
            Assert.Equal(receipt.ActionHash, await ReadMutationHashAsync(provider, receipt.ConflictingMutation));
            var repository = new HoloEdgeMutationRepository(new HoloNetEdgeAppClient(provider.HoloNETClientAppAgent));
            var conflict = await repository.ApplyLocalMutationAsync(receipt.ConflictingMutation, CancellationToken.None);
            Assert.True(conflict.IsError);
            Assert.False(conflict.IsSaved);
            Assert.Contains("already bound to a different mutation", conflict.Message);
            Assert.Equal(receipt.ActionHash, await ReadMutationHashAsync(provider, receipt.ConflictingMutation));
        }
        finally
        {
            if (provider.IsProviderActivated) await provider.DeActivateProviderAsync();
        }
    }

    private sealed record RestartReceipt(SyncOperation ConflictingMutation, string DnaHash, string AgentKey, string ActionHash);

    private static async Task BenchmarkIndexedReadsAsync(HoloOASIS provider, Uri endpoint, string dnaHash,
        string coordinatorWasm)
    {
        var app = provider.HoloNETClientAppAgent;
        var fixture = Enumerable.Range(0, 64).Select(i => new { id = Guid.NewGuid().ToString("D"), name = $"record {i}" }).ToArray();
        var seeded = await app.CallZomeFunctionAsync("oasis", "save_all_holons", fixture);
        Assert.False(seeded.IsError, seeded.Message);
        var expected = seeded.Records.Select(record => record.ActionHash).ToArray();
        Assert.Equal(fixture.Length, expected.Length);

        var previous = await MeasureAsync();
        await UpdateCoordinatorAsync(endpoint, dnaHash, coordinatorWasm);
        var batched = await MeasureAsync();
        var report = new { records = fixture.Length, previous, batched };
        var path = Path.Combine(Required("OASIS_HOLOCHAIN_REPORT_DIR"), "holo-indexed-read-timing.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report));

        async Task<double[]> MeasureAsync()
        {
            const int warmup = 3;
            const int samples = 20;
            var timings = new double[samples];
            for (var sample = -warmup; sample < samples; sample++)
            {
                var timer = Stopwatch.StartNew();
                var result = await app.CallZomeFunctionAsync("oasis", "get_all_holons", (object?)null);
                timer.Stop();
                Assert.False(result.IsError, result.Message);
                Assert.Equal(expected, result.Records.Select(record => record.ActionHash).ToArray());
                if (sample >= 0) timings[sample] = timer.Elapsed.TotalMilliseconds;
            }
            return timings;
        }
    }

    private static async Task UpdateCoordinatorAsync(Uri endpoint, string dnaHash, string wasmPath)
    {
        const string resource = "oasis.wasm";
        var bundle = new CoordinatorBundle
        {
            manifest = new CoordinatorManifest
            {
                zomes = new[]
                {
                    new ZomeManifest
                    {
                        name = "oasis", path = resource,
                        dependencies = new[] { new ZomeDependency { name = "oasis_integrity" } }
                    }
                }
            },
            resources = new Dictionary<string, byte[]> { [resource] = await File.ReadAllBytesAsync(wasmPath) }
        };
        var admin = OpenAdmin(endpoint);
        var connected = await admin.ConnectAsync();
        Assert.False(connected.IsError, connected.Message);
        try
        {
            var result = await admin.UpdateCoordinatorsAsync(dnaHash, bundle);
            Assert.False(result.IsError, result.Message);
        }
        finally
        {
            await admin.DisconnectAsync();
        }
    }

    private static async Task<string> ReadMutationHashAsync(HoloOASIS provider, SyncOperation mutation)
    {
        var result = await provider.HoloNETClientAppAgent.CallZomeFunctionAsync("oasis",
            "get_hyperdrive_mutation", mutation.OperationId.ToString("D"));
        Assert.False(result.IsError, result.Message);
        var record = Assert.Single(result.Records);
        Assert.False(string.IsNullOrWhiteSpace(record.ActionHash));
        return record.ActionHash;
    }

    private static HoloNETClientAdmin OpenAdmin(Uri endpoint) => new(new HoloNETDNA
    {
        HolochainConductorAdminURI = endpoint.AbsoluteUri,
        AutoStartHolochainConductor = false,
        AutoShutdownHolochainConductor = false
    });

    private static HoloOASIS OpenProvider(Uri endpoint) => new(OpenAdmin(endpoint), null!, useHoloNetwork: false);

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value : throw new InvalidOperationException($"Missing fixture setting {name}.");
}
