using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;
using NextGenSoftware.Utilities;
using TronNet;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class TronRealProviderRoutingTests
{
    [Fact]
    public async Task V2ReplicatesBalancesAndFailsOverDuringOwnedNodePause()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/Use-ReusableTronRuntime.ps1"))) directory = directory.Parent;
        directory.Should().NotBeNull();
        var root = Path.Combine(directory!.FullName, "TestResults/HyperDriveTron");
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var contract = (await File.ReadAllTextAsync(Path.Combine(directory.FullName, "TestResults/TronRuntime/local-store-address.txt"))).Trim();
        var key = new string('0', 63) + "1"; // Public local fixture, never a public-chain key.
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"),
            "0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f", contractFeeLimitSun: 1000000000);
        using var storage = new TronPrivateStorageBackend(runtime.NativeTransfers, new TronECKey(key, TronNetwork.MainNet).GetPublicAddress(),
            contract, () => key, () => SHA256.HashData(Encoding.UTF8.GetBytes("public reusable local TRON encryption fixture")));
        var tron = new TRONOASIS(rpcEndpoint: "http://127.0.0.1:9090", nativeTransfers: runtime.NativeTransfers, storageBackend: storage);
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var activation = await tron.ActivateProviderAsync();
        activation.IsError.Should().BeFalse(activation.Message);
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        var manager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = true, IsAutoLoadBalanceEnabled = false };
        manager.RegisterProvider(local); manager.RegisterProvider(tron);
        EnumValue<ProviderType>[] Providers() => new[] { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS), new EnumValue<ProviderType>(ProviderType.TRONOASIS) };
        manager.SetAndReplaceAutoReplicationListForProviders(Providers()).IsError.Should().BeFalse();
        var holon = new Holon { Id = Guid.Parse("deba869c-8078-4a98-a7e4-cb7c11d59249"), Name = "Real V2 TRON replication",
            MetaData = new Dictionary<string, object> { ["routing-fixture"] = "tron-real-v2" } };
        var failedOver = new Holon { Id = Guid.Parse("c8752504-dff2-44c6-99d9-32a601ae9670"), Name = "Real V2 TRON outage" };
        var partial = new Holon { Id = Guid.Parse("f8882565-96ba-40f5-b2ac-a47e78ab8ea9"), Name = "Real V2 partial replication" };
        var paused = false;
        var avatar = new Avatar { Id = Guid.Parse("b531a581-5627-451c-9de5-95c198646c20"),
            Username = "tron-real-v2-avatar", Email = "tron-real-v2-avatar@example.invalid" };
        var detail = new AvatarDetail { Id = avatar.Id, Username = avatar.Username, Email = avatar.Email };
        var avatarSaved = false;
        try
        {
            var router = new OASISHyperDrive(manager);
            async Task AssertIndexedRoutes<T>(string operation, bool absent, Func<T, Guid> identity) where T : class
            {
                foreach (var providerType in new[] { ProviderType.LocalFileOASIS, ProviderType.TRONOASIS })
                {
                    var request = new StorageOperationRequest { Operation = operation, AvatarId = avatar.Id, Username = avatar.Username,
                        Email = avatar.Email, PreferredProvider = providerType };
                    var sync = router.RouteRequest<T>(request);
                    var asyncResult = await router.RouteRequestAsync<T>(request);
                    foreach (var result in new[] { sync, asyncResult })
                    {
                        result.IsError.Should().BeFalse(result.Message);
                        if (absent) result.Result.Should().BeNull("deleted indexes must resolve to authoritative absence");
                        else identity(result.Result).Should().Be(avatar.Id);
                    }
                }
            }
            async Task AssertAllIndexedRoutes(bool absent)
            {
                await AssertIndexedRoutes<IAvatar>("LoadAvatar", absent, value => value.Id);
                await AssertIndexedRoutes<IAvatarDetail>("LoadAvatarDetail", absent, value => value.Id);
                await AssertIndexedRoutes<IAvatar>("LoadAvatarByUsername", absent, value => value.Id);
                await AssertIndexedRoutes<IAvatar>("LoadAvatarByEmail", absent, value => value.Id);
                await AssertIndexedRoutes<IAvatarDetail>("LoadAvatarDetailByUsername", absent, value => value.Id);
                await AssertIndexedRoutes<IAvatarDetail>("LoadAvatarDetailByEmail", absent, value => value.Id);
            }
            async Task AssertListRoutes<T>(string operation, bool absent, Func<T, Guid> identity)
            {
                foreach (var providerType in new[] { ProviderType.LocalFileOASIS, ProviderType.TRONOASIS })
                {
                    var request = new StorageOperationRequest { Operation = operation, PreferredProvider = providerType };
                    var sync = router.RouteRequest<IEnumerable<T>>(request);
                    var asyncResult = await router.RouteRequestAsync<IEnumerable<T>>(request);
                    foreach (var result in new[] { sync, asyncResult })
                    {
                        result.IsError.Should().BeFalse(result.Message);
                        result.Result.Should().NotBeNull();
                        result.Result.Count(value => identity(value) == avatar.Id).Should().Be(absent ? 0 : 1);
                    }
                }
            }
            async Task AssertAvatarLists(bool absent)
            {
                await AssertListRoutes<IAvatar>("LoadAllAvatars", absent, value => value.Id);
                await AssertListRoutes<IAvatarDetail>("LoadAllAvatarDetails", absent, value => value.Id);
            }
            var savedAvatar = await router.RouteRequestAsync<IAvatar>(new StorageOperationRequest
                { Operation = "SaveAvatar", Payload = avatar, PreferredProvider = ProviderType.LocalFileOASIS });
            avatarSaved = !savedAvatar.IsError;
            savedAvatar.IsError.Should().BeFalse(savedAvatar.Message);
            savedAvatar.IsWarning.Should().BeFalse(savedAvatar.Message);
            (await tron.LoadAvatarAsync(avatar.Id)).Result.Username.Should().Be(avatar.Username);
            (await local.LoadAvatarAsync(avatar.Id)).Result.Username.Should().Be(avatar.Username);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            var savedDetail = await router.RouteRequestAsync<IAvatarDetail>(new StorageOperationRequest
                { Operation = "SaveAvatarDetail", Payload = detail, PreferredProvider = ProviderType.LocalFileOASIS });
            savedDetail.IsError.Should().BeFalse(savedDetail.Message);
            savedDetail.IsWarning.Should().BeFalse(savedDetail.Message + " " + System.Text.Json.JsonSerializer.Serialize(manager.LastReplicationDiagnostic));
            (await tron.LoadAvatarDetailAsync(avatar.Id)).Result.Username.Should().Be(avatar.Username);
            (await local.LoadAvatarDetailAsync(avatar.Id)).Result.Username.Should().Be(avatar.Username);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
            // Exercise both username/email indexes and sync/async projections against real replicas.
            foreach (var provider in new IOASISStorageProvider[] { local, tron })
            {
                (await provider.LoadAvatarByUsernameAsync(avatar.Username)).Result.Id.Should().Be(avatar.Id);
                (await provider.LoadAvatarByEmailAsync(avatar.Email)).Result.Id.Should().Be(avatar.Id);
                provider.LoadAvatarByUsername(avatar.Username).Result.Id.Should().Be(avatar.Id);
                provider.LoadAvatarByEmail(avatar.Email).Result.Id.Should().Be(avatar.Id);
                (await provider.LoadAvatarDetailByUsernameAsync(avatar.Username)).Result.Id.Should().Be(avatar.Id);
                (await provider.LoadAvatarDetailByEmailAsync(avatar.Email)).Result.Id.Should().Be(avatar.Id);
                provider.LoadAvatarDetailByUsername(avatar.Username).Result.Id.Should().Be(avatar.Id);
                provider.LoadAvatarDetailByEmail(avatar.Email).Result.Id.Should().Be(avatar.Id);
            }
            await AssertAllIndexedRoutes(absent: false);
            await AssertAvatarLists(absent: false);
            var deletedAvatar = await router.RouteRequestAsync<bool>(new StorageOperationRequest
                { Operation = "DeleteAvatar", AvatarId = avatar.Id, SoftDelete = false, PreferredProvider = ProviderType.LocalFileOASIS });
            deletedAvatar.IsError.Should().BeFalse(deletedAvatar.Message);
            deletedAvatar.IsWarning.Should().BeFalse(deletedAvatar.Message);
            deletedAvatar.Result.Should().BeTrue();
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
            (await local.LoadAvatarAsync(avatar.Id)).Result.Should().BeNull();
            (await tron.LoadAvatarAsync(avatar.Id)).Result.Should().BeNull();
            (await local.LoadAvatarDetailAsync(avatar.Id)).Result.Should().BeNull();
            (await tron.LoadAvatarDetailAsync(avatar.Id)).Result.Should().BeNull();
            (await tron.LoadAvatarByUsernameAsync(avatar.Username)).Result.Should().BeNull();
            (await tron.LoadAvatarByEmailAsync(avatar.Email)).Result.Should().BeNull();
            await AssertAllIndexedRoutes(absent: true);
            await AssertAvatarLists(absent: true);
            avatarSaved = false;
            var saved = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.LocalFileOASIS });
            saved.IsError.Should().BeFalse(saved.Message);
            (await tron.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
            (await local.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
            foreach (var providerType in new[] { ProviderType.LocalFileOASIS, ProviderType.TRONOASIS })
            {
                foreach (var operation in new[] { "LoadAllHolons", "LoadHolonsByMetaData", "LoadHolonsByMetaDataPairs" })
                {
                    var request = new StorageOperationRequest { Operation = operation, PreferredProvider = providerType,
                        LoadChildren = false, MetaKey = "routing-fixture", MetaValue = "tron-real-v2",
                        MetaKeyValuePairs = new Dictionary<string, string> { ["routing-fixture"] = "tron-real-v2" } };
                    var sync = router.RouteRequest<IEnumerable<IHolon>>(request);
                    var asyncResult = await router.RouteRequestAsync<IEnumerable<IHolon>>(request);
                    foreach (var result in new[] { sync, asyncResult })
                    {
                        result.IsError.Should().BeFalse(result.Message);
                        result.Result.Should().NotBeNull();
                        result.Result.Count(value => value.Id == holon.Id).Should().Be(1);
                    }
                }
            }
            manager.IsAutoReplicationEnabled = false; manager.IsAutoLoadBalanceEnabled = true;
            manager.SetAndReplaceAutoLoadBalanceListForProviders(Providers()).IsError.Should().BeFalse();
            async Task<double> Measure(IOASISStorageProvider provider)
            {
                var timer = Stopwatch.StartNew(); var read = await provider.LoadHolonAsync(holon.Id, loadChildren: false); timer.Stop();
                read.IsError.Should().BeFalse(read.Message); read.Result.Name.Should().Be(holon.Name);
                return Math.Max(timer.Elapsed.TotalMilliseconds, 0.001);
            }
            var localMs = await Measure(local); var tronMs = await Measure(tron);
            manager.PerformanceMonitor.RecordRequest(ProviderType.LocalFileOASIS, true, localMs);
            manager.PerformanceMonitor.RecordRequest(ProviderType.TRONOASIS, true, tronMs);
            var expected = localMs <= tronMs ? ProviderType.LocalFileOASIS : ProviderType.TRONOASIS;
            var before = manager.PerformanceMonitor.GetMetrics(expected).TotalRequests;
            var loaded = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest { Operation = "LoadHolon", HolonId = holon.Id }, LoadBalancingStrategy.Performance);
            loaded.IsError.Should().BeFalse(loaded.Message);
            manager.PerformanceMonitor.GetMetrics(expected).TotalRequests.Should().Be(before + 1);
            manager.IsAutoLoadBalanceEnabled = false; manager.IsAutoFailOverEnabled = true;
            manager.SetAndReplaceAutoFailOverListForProviders(Providers().Reverse()).IsError.Should().BeFalse();
            await ChangeNode("pause"); paused = true;
            var routed = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "SaveHolon", Payload = failedOver, PreferredProvider = ProviderType.TRONOASIS }).WaitAsync(TimeSpan.FromSeconds(60));
            routed.IsError.Should().BeFalse(routed.Message);
            (await local.LoadHolonAsync(failedOver.Id)).Result.Name.Should().Be(failedOver.Name);
            manager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.LocalFileOASIS);
            manager.IsAutoFailOverEnabled = false;
            manager.IsAutoReplicationEnabled = true;
            var partiallySaved = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "SaveHolon", Payload = partial, PreferredProvider = ProviderType.LocalFileOASIS }).WaitAsync(TimeSpan.FromSeconds(60));
            partiallySaved.IsError.Should().BeFalse(partiallySaved.Message);
            partiallySaved.IsWarning.Should().BeTrue("primary success must not conceal the unavailable replica");
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(0);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(1);
            manager.LastReplicationDiagnostic.Attempts.Single().Provider.Should().Be(ProviderType.TRONOASIS);
            (await local.LoadHolonAsync(partial.Id)).Result.Name.Should().Be(partial.Name);
            await ChangeNode("unpause"); paused = false;
            (await tron.LoadHolonAsync(partial.Id)).IsError.Should().BeTrue("the failed replica must not be reported as persisted");
            // A new explicit mutation after recovery repairs this record; no automatic replay is claimed.
            var repaired = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "SaveHolon", Payload = partial, PreferredProvider = ProviderType.LocalFileOASIS });
            repaired.IsError.Should().BeFalse(repaired.Message);
            repaired.IsWarning.Should().BeFalse(repaired.Message);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
            (await tron.LoadHolonAsync(partial.Id)).Result.Name.Should().Be(partial.Name);
            var deleted = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "DeleteHolon", HolonId = partial.Id, SoftDelete = false, PreferredProvider = ProviderType.LocalFileOASIS });
            deleted.IsError.Should().BeFalse(deleted.Message);
            deleted.IsWarning.Should().BeFalse(deleted.Message);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
            (await local.LoadHolonAsync(partial.Id)).Result.Should().BeNull();
            (await tron.LoadHolonAsync(partial.Id)).IsError.Should().BeTrue();
            await ChangeNode("pause"); paused = true;
            var partialDelete = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "DeleteHolon", HolonId = holon.Id, SoftDelete = false, PreferredProvider = ProviderType.LocalFileOASIS }).WaitAsync(TimeSpan.FromSeconds(60));
            partialDelete.IsError.Should().BeFalse(partialDelete.Message);
            partialDelete.IsWarning.Should().BeTrue("deletion at the unavailable replica has not occurred");
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(1);
            (await local.LoadHolonAsync(holon.Id)).Result.Should().BeNull();
            await ChangeNode("unpause"); paused = false;
            (await tron.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
        }
        finally
        {
            if (paused) await ChangeNode("unpause");
            if (avatarSaved)
            {
                if ((await tron.LoadAvatarAsync(avatar.Id)).Result != null)
                    (await tron.DeleteAvatarAsync(avatar.Id, softDelete: false)).IsError.Should().BeFalse();
                (await local.DeleteAvatarAsync(avatar.Id, softDelete: false)).IsError.Should().BeFalse();
            }
            foreach (var id in new[] { holon.Id, failedOver.Id, partial.Id })
            {
                if (!(await tron.LoadHolonAsync(id)).IsError) (await tron.DeleteHolonAsync(id)).IsError.Should().BeFalse();
                if (!(await local.LoadHolonAsync(id)).IsError) (await local.DeleteHolonAsync(id)).IsError.Should().BeFalse();
            }
            await tron.DeActivateProviderAsync(); await local.DeActivateProviderAsync();
        }
    }

    private static async Task ChangeNode(string action)
    {
        if (action != "pause" && action != "unpause") throw new ArgumentException("Only reusable node pause/unpause is allowed.");
        var start = new ProcessStartInfo("wsl.exe") { UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "-u", "root", "-e", "podman", action, "oasis-tron-tre" }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20));
        process.ExitCode.Should().Be(0);
    }
}
