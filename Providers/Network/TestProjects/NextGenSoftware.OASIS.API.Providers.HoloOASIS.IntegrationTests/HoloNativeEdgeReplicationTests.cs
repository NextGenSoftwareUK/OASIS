using NextGenSoftware.Holochain.HoloNET.Client;
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
        var admin = OpenAdmin(endpoint);
        var connected = await admin.ConnectAsync();
        Assert.False(connected.IsError, connected.Message);
        try
        {
            var installed = await admin.InstallAppAsync(HoloOASIS.OASIS_HAPP_ID,
                Required("HOLONET_LIVE_HAPP_PATH"), network_seed: Guid.NewGuid().ToString("N"));
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
        try
        {
            var activated = await provider.ActivateProviderAsync();
            Assert.True(!activated.IsError && activated.Result, activated.Message);
            var repository = new HoloEdgeMutationRepository(new HoloNetEdgeAppClient(provider.HoloNETClientAppAgent));
            var applied = await repository.ApplyLocalMutationAsync(mutation, CancellationToken.None);
            Assert.True(!applied.IsError && applied.IsSaved, applied.Message);
            originalHash = await ReadMutationHashAsync(provider, mutation);

            var replay = await repository.ApplyLocalMutationAsync(mutation, CancellationToken.None);
            Assert.True(!replay.IsError && replay.IsSaved, replay.Message);
            Assert.Equal(originalHash, await ReadMutationHashAsync(provider, mutation));

            mutation.VersionId = Guid.NewGuid();
            mutation.PayloadJson = "{\"name\":\"conflicting replay\"}";
            var conflict = await repository.ApplyLocalMutationAsync(mutation, CancellationToken.None);
            Assert.True(conflict.IsError, "A conflicting operation ID was acknowledged.");
            Assert.False(conflict.IsSaved);
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
            Assert.Equal(originalHash, await ReadMutationHashAsync(reopened, mutation));
            var latest = await reopened.HoloNETClientAppAgent.CallZomeFunctionAsync("oasis",
                "get_latest_hyperdrive_entity", new
                {
                    avatar_id = mutation.AvatarId.ToString("D"),
                    entity_type = mutation.EntityType,
                    entity_id = mutation.EntityId.ToString("D")
                });
            Assert.False(latest.IsError, latest.Message);
            Assert.Equal(originalHash, latest.ZomeReturnHash);
        }
        finally
        {
            if (reopened.IsProviderActivated) await reopened.DeActivateProviderAsync();
        }
    }

    private static async Task<string> ReadMutationHashAsync(HoloOASIS provider, SyncOperation mutation)
    {
        var result = await provider.HoloNETClientAppAgent.CallZomeFunctionAsync("oasis",
            "get_hyperdrive_mutation", mutation.OperationId.ToString("D"));
        Assert.False(result.IsError, result.Message);
        Assert.False(string.IsNullOrWhiteSpace(result.ZomeReturnHash));
        return result.ZomeReturnHash;
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
