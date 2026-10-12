using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.SuiOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class SuiRealProviderRoutingTests
{
    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Real provider setting {name} is required; no skipped evidence.");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task V2FailsOverToRealStoreAfterSuiFailure(bool stopOwnedNode)
    {
        var rpc = Required("OASIS_SUI_TEST_RPC");
        new Uri(rpc).IsLoopback.Should().BeTrue();
        var root = Path.GetFullPath(Required("OASIS_HYPERDRIVE_TEST_ROOT"));
        Path.GetFileName(root).Should().Be("HyperDriveSui");
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var fixture = JsonDocument.Parse(File.ReadAllText(Required("OASIS_SUI_TEST_DEPLOYMENT")));
        var settings = fixture.RootElement;
        using var sui = new SuiOASIS(rpc, "localnet", settings.GetProperty("chainId").GetString()!,
            settings.GetProperty("packageAddress").GetString()!, settings.GetProperty("privateKey").GetString()!,
            settings.GetProperty("storageObjectId").GetString()!);
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var activated = await sui.ActivateProviderAsync();
        activated.IsError.Should().BeFalse(activated.Message);
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        var manager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        manager.RegisterProvider(sui);
        manager.RegisterProvider(local);
        manager.SetAndActivateCurrentStorageProvider(sui).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoFailOverListForProviders(new[]
            { new EnumValue<ProviderType>(ProviderType.SuiOASIS), new EnumValue<ProviderType>(ProviderType.LocalFileOASIS) })
            .IsError.Should().BeFalse();
        if (stopOwnedNode)
        {
            var pid = Required("OASIS_SUI_TEST_OWNED_PID");
            int.TryParse(pid, out var number).Should().BeTrue();
            number.Should().BeGreaterThan(1);
            var script = Required("OASIS_SUI_TEST_STOP_SCRIPT");
            script.Should().StartWith("/mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/");
            Path.GetFileName(script).Should().Be("stop-owned-sui-test-node.sh");
            var start = new ProcessStartInfo("wsl.exe") { UseShellExecute = false, CreateNoWindow = true };
            foreach (var argument in new[] { "-d", "Ubuntu", "-u", "root", "--", "bash", script, pid })
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start)!;
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            process.ExitCode.Should().Be(0, "only the explicitly owned local Sui node may be stopped");
        }
        else
        {
            sui.Dispose();
            var failure = await sui.ActivateProviderAsync();
            failure.IsError.Should().BeTrue("closed transport must not fake successful activation");
        }
        var holon = new Holon { Id = Guid.NewGuid(), Name = "real-v2-sui-failover" };
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
    public async Task V2ReplicatesToSuiAndSelectsFromMeasuredRealProviderReads()
    {
        var rpc = Required("OASIS_SUI_TEST_RPC");
        new Uri(rpc).IsLoopback.Should().BeTrue("tests must not spend public-chain assets");
        var root = Path.GetFullPath(Required("OASIS_HYPERDRIVE_TEST_ROOT"));
        Path.GetFileName(root).Should().Be("HyperDriveSui");
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var fixture = JsonDocument.Parse(File.ReadAllText(Required("OASIS_SUI_TEST_DEPLOYMENT")));
        var settings = fixture.RootElement;
        using var sui = new SuiOASIS(rpc, "localnet", settings.GetProperty("chainId").GetString()!,
            settings.GetProperty("packageAddress").GetString()!, settings.GetProperty("privateKey").GetString()!,
            settings.GetProperty("storageObjectId").GetString()!);
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var holon = new Holon { Id = Guid.NewGuid(), Name = "real-v2-sui-replication" };
        holon.MetaData = new Dictionary<string, object> { ["owned-routing-fixture"] = holon.Id.ToString() };
        var deletedThroughRouter = false;
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "sui-v2-" + Guid.NewGuid().ToString("N"),
            Email = Guid.NewGuid().ToString("N") + "@example.invalid" };
        var detail = new AvatarDetail { Id = avatar.Id, Username = avatar.Username, Email = avatar.Email };
        var avatarSaved = false;
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        var activated = await sui.ActivateProviderAsync();
        activated.IsError.Should().BeFalse(activated.Message);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        var manager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = true, IsAutoLoadBalanceEnabled = false };
        manager.RegisterProvider(local);
        manager.RegisterProvider(sui);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]
            { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS), new EnumValue<ProviderType>(ProviderType.SuiOASIS) })
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
            (await sui.LoadAvatarAsync(avatar.Id)).Result.Username.Should().Be(avatar.Username);
            (await local.LoadAvatarAsync(avatar.Id)).Result.Username.Should().Be(avatar.Username);
            var savedDetail = await router.RouteRequestAsync<IAvatarDetail>(new StorageOperationRequest
                { Operation = "SaveAvatarDetail", Payload = detail, PreferredProvider = ProviderType.LocalFileOASIS });
            savedDetail.IsError.Should().BeFalse(savedDetail.Message);
            savedDetail.IsWarning.Should().BeFalse(savedDetail.Message);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
            (await sui.LoadAvatarDetailAsync(avatar.Id)).Result.Username.Should().Be(avatar.Username);
            (await local.LoadAvatarDetailAsync(avatar.Id)).Result.Username.Should().Be(avatar.Username);
            async Task AssertAvatarLists(bool present)
            {
                foreach (var providerType in new[] { ProviderType.LocalFileOASIS, ProviderType.SuiOASIS })
                {
                    var avatars = new StorageOperationRequest { Operation = "LoadAllAvatars", PreferredProvider = providerType };
                    foreach (var read in new[] { router.RouteRequest<IEnumerable<IAvatar>>(avatars), await router.RouteRequestAsync<IEnumerable<IAvatar>>(avatars) })
                    { read.IsError.Should().BeFalse(read.Message); read.Result.Any(value => value.Id == avatar.Id).Should().Be(present); }
                    var details = new StorageOperationRequest { Operation = "LoadAllAvatarDetails", PreferredProvider = providerType };
                    foreach (var read in new[] { router.RouteRequest<IEnumerable<IAvatarDetail>>(details), await router.RouteRequestAsync<IEnumerable<IAvatarDetail>>(details) })
                    { read.IsError.Should().BeFalse(read.Message); read.Result.Any(value => value.Id == avatar.Id).Should().Be(present); }
                }
            }
            await AssertAvatarLists(true);
            foreach (var providerType in new[] { ProviderType.LocalFileOASIS, ProviderType.SuiOASIS })
            {
                foreach (var operation in new[] { "LoadAvatar", "LoadAvatarByUsername", "LoadAvatarByEmail" })
                {
                    var request = new StorageOperationRequest { Operation = operation, AvatarId = avatar.Id,
                        Username = avatar.Username, Email = avatar.Email, PreferredProvider = providerType };
                    foreach (var read in new[] { router.RouteRequest<IAvatar>(request), await router.RouteRequestAsync<IAvatar>(request) })
                    { read.IsError.Should().BeFalse(read.Message); read.Result.Id.Should().Be(avatar.Id); }
                }
                foreach (var operation in new[] { "LoadAvatarDetail", "LoadAvatarDetailByUsername", "LoadAvatarDetailByEmail" })
                {
                    var request = new StorageOperationRequest { Operation = operation, AvatarId = avatar.Id,
                        Username = avatar.Username, Email = avatar.Email, PreferredProvider = providerType };
                    foreach (var read in new[] { router.RouteRequest<IAvatarDetail>(request), await router.RouteRequestAsync<IAvatarDetail>(request) })
                    { read.IsError.Should().BeFalse(read.Message); read.Result.Id.Should().Be(avatar.Id); }
                }
            }
            var saved = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.LocalFileOASIS });
            saved.IsError.Should().BeFalse(saved.Message);
            var replicated = await sui.LoadHolonAsync(holon.Id, loadChildren: false);
            replicated.IsError.Should().BeFalse(replicated.Message);
            replicated.Result.Name.Should().Be(holon.Name);
            (await local.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
            foreach (var providerType in new[] { ProviderType.LocalFileOASIS, ProviderType.SuiOASIS })
            {
                foreach (var operation in new[] { "LoadAllHolons", "LoadHolonsByMetaData", "LoadHolonsByMetaDataPairs" })
                {
                    var request = new StorageOperationRequest { Operation = operation, PreferredProvider = providerType, LoadChildren = false,
                        MetaKey = "owned-routing-fixture", MetaValue = holon.Id.ToString(),
                        MetaKeyValuePairs = new Dictionary<string, string> { ["owned-routing-fixture"] = holon.Id.ToString() } };
                    foreach (var read in new[] { router.RouteRequest<IEnumerable<IHolon>>(request), await router.RouteRequestAsync<IEnumerable<IHolon>>(request) })
                    { read.IsError.Should().BeFalse(read.Message); read.Result.Single(value => value.Id == holon.Id).Name.Should().Be(holon.Name); }
                }
                var search = new StorageOperationRequest { Operation = "Search", PreferredProvider = providerType, LoadChildren = false,
                    SearchParams = new SearchParams { SearchGroups = new List<ISearchGroupBase> {
                        new SearchTextGroup { SearchQuery = holon.Name, SearchHolons = true, SearchAvatars = false } } } };
                foreach (var read in new[] { router.RouteRequest<ISearchResults>(search), await router.RouteRequestAsync<ISearchResults>(search) })
                { read.IsError.Should().BeFalse(read.Message); read.Result.SearchResultHolons.Single(value => value.Id == holon.Id).Name.Should().Be(holon.Name); }
            }
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);

            holon.Name = "real-v2-sui-replication-updated";
            var updated = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.LocalFileOASIS });
            updated.IsError.Should().BeFalse(updated.Message);
            updated.IsWarning.Should().BeFalse(updated.Message);
            (await sui.LoadHolonAsync(holon.Id, loadChildren: false)).Result.Name.Should().Be(holon.Name);
            (await local.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);

            manager.IsAutoReplicationEnabled = false;
            manager.IsAutoLoadBalanceEnabled = true;
            manager.SetAndReplaceAutoLoadBalanceListForProviders(new[]
                { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS), new EnumValue<ProviderType>(ProviderType.SuiOASIS) })
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
            var suiMs = await Measure(sui);
            manager.PerformanceMonitor.RecordRequest(ProviderType.LocalFileOASIS, true, localMs);
            manager.PerformanceMonitor.RecordRequest(ProviderType.SuiOASIS, true, suiMs);
            var expected = localMs <= suiMs ? ProviderType.LocalFileOASIS : ProviderType.SuiOASIS;
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
            var absent = await sui.LoadHolonAsync(holon.Id, loadChildren: false);
            absent.Result.Should().BeNull("successful replicated deletion must remove the Sui copy");
            deletedThroughRouter = true;
            var deletedAvatar = await router.RouteRequestAsync<bool>(new StorageOperationRequest
                { Operation = "DeleteAvatar", AvatarId = avatar.Id, SoftDelete = false, PreferredProvider = ProviderType.LocalFileOASIS });
            deletedAvatar.IsError.Should().BeFalse(deletedAvatar.Message);
            deletedAvatar.IsWarning.Should().BeFalse(deletedAvatar.Message);
            deletedAvatar.Result.Should().BeTrue();
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
            (await local.LoadAvatarAsync(avatar.Id)).Result.Should().BeNull();
            (await sui.LoadAvatarAsync(avatar.Id)).Result.Should().BeNull();
            (await local.LoadAvatarDetailAsync(avatar.Id)).Result.Should().BeNull();
            (await sui.LoadAvatarDetailAsync(avatar.Id)).Result.Should().BeNull();
            (await sui.LoadAvatarByUsernameAsync(avatar.Username)).Result.Should().BeNull();
            (await sui.LoadAvatarByEmailAsync(avatar.Email)).Result.Should().BeNull();
            foreach (var providerType in new[] { ProviderType.LocalFileOASIS, ProviderType.SuiOASIS })
            {
                foreach (var operation in new[] { "LoadAvatar", "LoadAvatarByUsername", "LoadAvatarByEmail" })
                {
                    var request = new StorageOperationRequest { Operation = operation, AvatarId = avatar.Id,
                        Username = avatar.Username, Email = avatar.Email, PreferredProvider = providerType };
                    foreach (var read in new[] { router.RouteRequest<IAvatar>(request), await router.RouteRequestAsync<IAvatar>(request) })
                    { read.IsError.Should().BeFalse(read.Message); read.Result.Should().BeNull("replicated deletion must retire identity indexes"); }
                }
                foreach (var operation in new[] { "LoadAvatarDetail", "LoadAvatarDetailByUsername", "LoadAvatarDetailByEmail" })
                {
                    var request = new StorageOperationRequest { Operation = operation, AvatarId = avatar.Id,
                        Username = avatar.Username, Email = avatar.Email, PreferredProvider = providerType };
                    foreach (var read in new[] { router.RouteRequest<IAvatarDetail>(request), await router.RouteRequestAsync<IAvatarDetail>(request) })
                    { read.IsError.Should().BeFalse(read.Message); read.Result.Should().BeNull("replicated deletion must remove detail projections"); }
                }
            }
            avatarSaved = false;
            await AssertAvatarLists(false);
        }
        finally
        {
            if (!deletedThroughRouter)
            {
                var deleted = await sui.DeleteHolonAsync(holon.Id);
                deleted.IsError.Should().BeFalse(deleted.Message);
                (await local.DeleteHolonAsync(holon.Id)).IsError.Should().BeFalse();
            }
            await sui.DeActivateProviderAsync();
            await local.DeActivateProviderAsync();
            if (avatarSaved)
            {
                (await sui.DeleteAvatarAsync(avatar.Id, softDelete: false)).IsError.Should().BeFalse();
                (await local.DeleteAvatarAsync(avatar.Id, softDelete: false)).IsError.Should().BeFalse();
                await sui.DeActivateProviderAsync();
                await local.DeActivateProviderAsync();
                var ownedDetailFile = Path.GetFullPath(Path.Combine(root, "files", "AvatarDetails", avatar.Id + ".json"));
                ownedDetailFile.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
                File.Delete(ownedDetailFile);
            }
        }
    }
}
