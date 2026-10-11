using System.Diagnostics;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class CosmosRealProviderRoutingTests
{
    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Real provider setting {name} is required; no skipped evidence.");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task V2FailsOverToRealStoreAfterCosmosFailure(bool stopOwnedNode)
    {
        var rpc = Required("OASIS_COSMOS_TEST_RPC");
        new Uri(rpc).IsLoopback.Should().BeTrue();
        var root = Path.GetFullPath(Required("OASIS_HYPERDRIVE_TEST_ROOT"));
        Path.GetFileName(root).Should().Be("HyperDriveCosmos");
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var cosmos = new CosmosBlockChainOASIS(rpc, Required("OASIS_COSMOS_TEST_CHAIN"),
            Required("OASIS_COSMOS_TEST_KEY"), Required("OASIS_COSMOS_TEST_CONTRACT"),
            "wasm", "0.025stake", "stake");
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var activated = await cosmos.ActivateProviderAsync();
        activated.IsError.Should().BeFalse(activated.Message);
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        var manager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        manager.RegisterProvider(cosmos);
        manager.RegisterProvider(local);
        manager.SetAndActivateCurrentStorageProvider(cosmos).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoFailOverListForProviders(new[]
            { new EnumValue<ProviderType>(ProviderType.CosmosBlockChainOASIS), new EnumValue<ProviderType>(ProviderType.LocalFileOASIS) })
            .IsError.Should().BeFalse();
        if (stopOwnedNode)
        {
            var script = Required("OASIS_COSMOS_TEST_STOP_SCRIPT");
            script.Should().Be("/mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/stop-owned-cosmos-test-node.sh");
            var start = new ProcessStartInfo("wsl.exe") { UseShellExecute = false, CreateNoWindow = true };
            foreach (var argument in new[] { "-d", "Ubuntu", "-u", "root", "--", "bash", script })
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start)!;
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            process.ExitCode.Should().Be(0, "only the pinned reusable Cosmos test node may be stopped");
        }
        else
        {
            cosmos.Dispose();
            var failure = await cosmos.ActivateProviderAsync();
            failure.IsError.Should().BeTrue("closed transport must not fake successful activation");
        }
        var holon = new Holon { Id = Guid.NewGuid(), Name = "real-v2-cosmos-failover" };
        try
        {
            var result = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "SaveHolon", Payload = holon });
            result.IsError.Should().BeFalse(result.Message);
            (await local.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
            manager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.LocalFileOASIS);
        }
        finally
        {
            (await local.DeleteHolonAsync(holon.Id)).IsError.Should().BeFalse();
            await local.DeActivateProviderAsync();
        }
    }

    [Fact]
    public async Task V2ReplicatesToCosmosAndSelectsFromMeasuredRealProviderReads()
    {
        var rpc = Required("OASIS_COSMOS_TEST_RPC");
        new Uri(rpc).IsLoopback.Should().BeTrue("tests must not spend public-chain assets");
        var root = Path.GetFullPath(Required("OASIS_HYPERDRIVE_TEST_ROOT"));
        Path.GetFileName(root).Should().Be("HyperDriveCosmos");
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var cosmos = new CosmosBlockChainOASIS(rpc, Required("OASIS_COSMOS_TEST_CHAIN"),
            Required("OASIS_COSMOS_TEST_KEY"), Required("OASIS_COSMOS_TEST_CONTRACT"),
            "wasm", "0.025stake", "stake");
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var holon = new Holon { Id = Guid.NewGuid(), Name = "real-v2-cosmos-replication" };
        var deletedThroughRouter = false;
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "cosmos-v2-" + Guid.NewGuid().ToString("N"),
            Email = Guid.NewGuid().ToString("N") + "@example.invalid" };
        var detail = new AvatarDetail { Id = avatar.Id, Username = avatar.Username, Email = avatar.Email };
        var avatarSaved = false;
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        var activated = await cosmos.ActivateProviderAsync();
        activated.IsError.Should().BeFalse(activated.Message);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        var manager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = true, IsAutoLoadBalanceEnabled = false };
        manager.RegisterProvider(local);
        manager.RegisterProvider(cosmos);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]
            { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS), new EnumValue<ProviderType>(ProviderType.CosmosBlockChainOASIS) })
            .IsError.Should().BeFalse();
        try
        {
            var router = new OASISHyperDrive(manager);
            var savedAvatar = await router.RouteRequestAsync<IAvatar>(new StorageOperationRequest
                { Operation = "SaveAvatar", Payload = avatar, PreferredProvider = ProviderType.LocalFileOASIS });
            avatarSaved = !savedAvatar.IsError;
            savedAvatar.IsError.Should().BeFalse(savedAvatar.Message);
            savedAvatar.IsWarning.Should().BeFalse(savedAvatar.Message);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            (await cosmos.LoadAvatarAsync(avatar.Id)).Result.Username.Should().Be(avatar.Username);
            (await local.LoadAvatarAsync(avatar.Id)).Result.Username.Should().Be(avatar.Username);
            var savedDetail = await router.RouteRequestAsync<IAvatarDetail>(new StorageOperationRequest
                { Operation = "SaveAvatarDetail", Payload = detail, PreferredProvider = ProviderType.LocalFileOASIS });
            savedDetail.IsError.Should().BeFalse(savedDetail.Message);
            savedDetail.IsWarning.Should().BeFalse(savedDetail.Message);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
            (await cosmos.LoadAvatarDetailAsync(avatar.Id)).Result.Username.Should().Be(avatar.Username);
            (await local.LoadAvatarDetailAsync(avatar.Id)).Result.Username.Should().Be(avatar.Username);
            var saved = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.LocalFileOASIS });
            saved.IsError.Should().BeFalse(saved.Message);
            var replicated = await cosmos.LoadHolonAsync(holon.Id, loadChildren: false);
            replicated.IsError.Should().BeFalse(replicated.Message);
            replicated.Result.Name.Should().Be(holon.Name);
            (await local.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);

            holon.Name = "real-v2-cosmos-replication-updated";
            var updated = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.LocalFileOASIS });
            updated.IsError.Should().BeFalse(updated.Message);
            updated.IsWarning.Should().BeFalse(updated.Message);
            (await cosmos.LoadHolonAsync(holon.Id, loadChildren: false)).Result.Name.Should().Be(holon.Name);
            (await local.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);

            manager.IsAutoReplicationEnabled = false;
            manager.IsAutoLoadBalanceEnabled = true;
            manager.SetAndReplaceAutoLoadBalanceListForProviders(new[]
                { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS), new EnumValue<ProviderType>(ProviderType.CosmosBlockChainOASIS) })
                .IsError.Should().BeFalse();
            async Task<double> Measure(IOASISStorageProvider provider)
            {
                var timer = Stopwatch.StartNew();
                var loaded = await provider.LoadHolonAsync(holon.Id, loadChildren: false);
                timer.Stop();
                loaded.IsError.Should().BeFalse(loaded.Message);
                loaded.Result.Name.Should().Be(holon.Name);
                return Math.Max(timer.Elapsed.TotalMilliseconds, 0.001);
            }
            var localMs = await Measure(local);
            var cosmosMs = await Measure(cosmos);
            manager.PerformanceMonitor.RecordRequest(ProviderType.LocalFileOASIS, true, localMs);
            manager.PerformanceMonitor.RecordRequest(ProviderType.CosmosBlockChainOASIS, true, cosmosMs);
            var expected = localMs <= cosmosMs ? ProviderType.LocalFileOASIS : ProviderType.CosmosBlockChainOASIS;
            var before = manager.PerformanceMonitor.GetMetrics(expected).TotalRequests;
            var loaded = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "LoadHolon", HolonId = holon.Id }, LoadBalancingStrategy.Performance);
            loaded.IsError.Should().BeFalse(loaded.Message);
            loaded.Result.Name.Should().Be(holon.Name);
            manager.PerformanceMonitor.GetMetrics(expected).TotalRequests.Should().Be(before + 1);
            manager.IsAutoLoadBalanceEnabled = false;
            manager.IsAutoReplicationEnabled = true;
            var removed = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "DeleteHolon", HolonId = holon.Id, SoftDelete = false, PreferredProvider = ProviderType.LocalFileOASIS });
            removed.IsError.Should().BeFalse(removed.Message);
            removed.IsWarning.Should().BeFalse(removed.Message);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
            (await local.LoadHolonAsync(holon.Id)).Result.Should().BeNull();
            (await cosmos.LoadHolonAsync(holon.Id, loadChildren: false)).Result.Should().BeNull();
            deletedThroughRouter = true;
            var deletedAvatar = await router.RouteRequestAsync<bool>(new StorageOperationRequest
                { Operation = "DeleteAvatar", AvatarId = avatar.Id, SoftDelete = false, PreferredProvider = ProviderType.LocalFileOASIS });
            deletedAvatar.IsError.Should().BeFalse(deletedAvatar.Message);
            deletedAvatar.IsWarning.Should().BeFalse(deletedAvatar.Message);
            deletedAvatar.Result.Should().BeTrue();
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
            (await local.LoadAvatarAsync(avatar.Id)).Result.Should().BeNull();
            (await cosmos.LoadAvatarAsync(avatar.Id)).Result.Should().BeNull();
            (await local.LoadAvatarDetailAsync(avatar.Id)).Result.Should().BeNull();
            (await cosmos.LoadAvatarDetailAsync(avatar.Id)).Result.Should().BeNull();
            (await cosmos.LoadAvatarByUsernameAsync(avatar.Username)).Result.Should().BeNull();
            (await cosmos.LoadAvatarByEmailAsync(avatar.Email)).Result.Should().BeNull();
            avatarSaved = false;
        }
        finally
        {
            if (!deletedThroughRouter)
            {
                var deleted = await cosmos.DeleteHolonAsync(holon.Id);
                deleted.IsError.Should().BeFalse(deleted.Message);
                (await local.DeleteHolonAsync(holon.Id)).IsError.Should().BeFalse();
            }
            await cosmos.DeActivateProviderAsync();
            await local.DeActivateProviderAsync();
            if (avatarSaved)
            {
                (await cosmos.DeleteAvatarAsync(avatar.Id, softDelete: false)).IsError.Should().BeFalse();
                (await local.DeleteAvatarAsync(avatar.Id, softDelete: false)).IsError.Should().BeFalse();
                await cosmos.DeActivateProviderAsync();
                await local.DeActivateProviderAsync();
            }
        }
    }
}
