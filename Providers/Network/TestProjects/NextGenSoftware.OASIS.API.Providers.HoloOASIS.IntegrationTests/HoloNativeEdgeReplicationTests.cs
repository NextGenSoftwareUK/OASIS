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
