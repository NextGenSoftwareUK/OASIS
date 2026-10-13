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
    private static byte[] LocalStorageKey()=>System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes("oasis-cosmos-local-storage-fixture-v1"));
    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Real provider setting {name} is required; no skipped evidence.");

    [Fact]
    public async Task InspectPendingCosmosV2IntentWithoutMutations()
    {
        var root = Path.GetFullPath(Required("OASIS_HYPERDRIVE_TEST_ROOT"));
        Path.GetFileName(root).Should().Be("HyperDriveCosmos");
        using var lease = new FileStream(Path.Combine(root,"store.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        using var intent = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"replication-pending.json")));
        intent.RootElement.GetProperty("chain").GetString().Should().Be(Required("OASIS_COSMOS_TEST_CHAIN"));
        intent.RootElement.GetProperty("contract").GetString().Should().Be(Required("OASIS_COSMOS_TEST_CONTRACT"));
        intent.RootElement.GetProperty("encryptionKeyLabel").GetString().Should().Be("oasis-cosmos-local-storage-fixture-v1");
        var avatarId = intent.RootElement.GetProperty("avatarId").GetGuid();
        var holonId = intent.RootElement.GetProperty("holonId").GetGuid();
        using var cosmos = new CosmosBlockChainOASIS(Required("OASIS_COSMOS_TEST_RPC"),Required("OASIS_COSMOS_TEST_CHAIN"),
            Required("OASIS_COSMOS_TEST_KEY"),Required("OASIS_COSMOS_TEST_CONTRACT"),"wasm","0.025stake","stake",storageEncryptionKeyResolver:LocalStorageKey);
        (await cosmos.ActivateProviderAsync()).IsError.Should().BeFalse();
        try
        {
            var avatar = await cosmos.LoadAvatarAsync(avatarId);
            var detail = await cosmos.LoadAvatarDetailAsync(avatarId);
            var holon = await cosmos.LoadHolonAsync(holonId,loadChildren:false);
            foreach (var state in new[] { (avatar.IsError,avatar.ErrorCode,avatar.Message), (detail.IsError,detail.ErrorCode,detail.Message), (holon.IsError,holon.ErrorCode,holon.Message) })
                if (state.IsError) state.ErrorCode.Should().Be(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,state.Message);
            await File.WriteAllTextAsync(Path.Combine(root,"cosmos-inspection-current.json"),System.Text.Json.JsonSerializer.Serialize(new {
                chain = Required("OASIS_COSMOS_TEST_CHAIN"), contract = Required("OASIS_COSMOS_TEST_CONTRACT"), avatarId, holonId,
                avatarPresent = avatar.Result != null, detailPresent = detail.Result != null, holonPresent = holon.Result != null,
                avatarErrorCode = avatar.ErrorCode, detailErrorCode = detail.ErrorCode, holonErrorCode = holon.ErrorCode,
                reconciliationComplete = false, explanation = "Read-only state evidence; absence is not original submission outcome proof." }));
            Console.WriteLine($"Read-only pending Cosmos intent: avatarPresent={avatar.Result != null}; detailPresent={detail.Result != null}; holonPresent={holon.Result != null}. Intent retained; absence is not non-submission proof.");
        }
        finally { (await cosmos.DeActivateProviderAsync()).IsError.Should().BeFalse(); }
    }

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
        File.Exists(Path.Combine(root,"replication-pending.json")).Should().BeFalse("reconcile prior owned replication intent before outage tests");
        using var cosmos = new CosmosBlockChainOASIS(rpc, Required("OASIS_COSMOS_TEST_CHAIN"),
            Required("OASIS_COSMOS_TEST_KEY"), Required("OASIS_COSMOS_TEST_CONTRACT"),
            "wasm", "0.025stake", "stake",storageEncryptionKeyResolver:LocalStorageKey);
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var activated = await cosmos.ActivateProviderAsync();
        activated.IsError.Should().BeFalse(activated.Message);
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        var manager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        dna.OASIS.SubscriptionConfig.PlanType="Gold";
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
            "wasm", "0.025stake", "stake",storageEncryptionKeyResolver:LocalStorageKey);
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var holon = new Holon { Id = Guid.NewGuid(), Name = "real-v2-cosmos-replication" };
        var deletedThroughRouter = false;
        var holonSaved = false;
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
        dna.OASIS.SubscriptionConfig.PlanType="Gold";
        manager.RegisterProvider(local);
        manager.RegisterProvider(cosmos);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]
            { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS), new EnumValue<ProviderType>(ProviderType.CosmosBlockChainOASIS) })
            .IsError.Should().BeFalse();
        var journalPath=Path.Combine(root,"replication-pending.json");
        using var journal=new FileStream(journalPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
        await System.Text.Json.JsonSerializer.SerializeAsync(journal,new{chain=Required("OASIS_COSMOS_TEST_CHAIN"),contract=Required("OASIS_COSMOS_TEST_CONTRACT"),
            holonId=holon.Id,avatarId=avatar.Id,avatar.Username,avatar.Email,encryptionKeyLabel="oasis-cosmos-local-storage-fixture-v1",recovery="Inspect exactly owned records; do not repeat an uncertain replication run."});
        journal.Flush(true);var verifiedComplete=false;
        try
        {
            var router = new OASISHyperDrive(manager);
            var savedAvatar = await router.RouteRequestAsync<IAvatar>(new StorageOperationRequest
                { Operation = "SaveAvatar", Payload = avatar, PreferredProvider = ProviderType.LocalFileOASIS });
            avatarSaved = !savedAvatar.IsError && !savedAvatar.IsWarning && manager.LastReplicationDiagnostic.SucceededCount == 1;
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
            holonSaved = true;
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
            dna.OASIS.SubscriptionConfig.PlanType="Free";
            var excludedBefore=manager.PerformanceMonitor.GetMetrics(ProviderType.CosmosBlockChainOASIS).TotalRequests;
            foreach(var strategy in new[]{LoadBalancingStrategy.RoundRobin,LoadBalancingStrategy.WeightedRoundRobin,LoadBalancingStrategy.LeastConnections,LoadBalancingStrategy.Geographic,LoadBalancingStrategy.Performance}){
                var request=new StorageOperationRequest{Operation="LoadHolon",HolonId=holon.Id,LoadChildren=false};
                foreach(var permitted in new[]{router.RouteRequest<IHolon>(request,strategy),await router.RouteRequestAsync<IHolon>(request,strategy)}){
                    permitted.IsError.Should().BeFalse(permitted.Message);permitted.Result.Id.Should().Be(holon.Id);
                    manager.LastProviderSelectionDiagnostic.Candidates.Select(value=>value.Provider).Should().Equal(ProviderType.LocalFileOASIS);
                }
            }
            manager.PerformanceMonitor.GetMetrics(ProviderType.CosmosBlockChainOASIS).TotalRequests.Should().Be(excludedBefore);
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var roundRobinBefore=new[]{ProviderType.LocalFileOASIS,ProviderType.CosmosBlockChainOASIS}.ToDictionary(type=>type,type=>manager.PerformanceMonitor.GetMetrics(type).TotalRequests);
            for(var index=0;index<6;index++){
                var request=new StorageOperationRequest{Operation="LoadHolon",HolonId=holon.Id,LoadChildren=false};
                var balanced=index%2==0?router.RouteRequest<IHolon>(request,LoadBalancingStrategy.RoundRobin):await router.RouteRequestAsync<IHolon>(request,LoadBalancingStrategy.RoundRobin);
                balanced.IsError.Should().BeFalse(balanced.Message);balanced.Result.Id.Should().Be(holon.Id);
            }
            foreach(var type in roundRobinBefore.Keys)manager.PerformanceMonitor.GetMetrics(type).TotalRequests.Should().Be(roundRobinBefore[type]+3);
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
            verifiedComplete=true;
        }
        finally
        {
            try
            {
                if (!deletedThroughRouter && holonSaved)
                {
                    var deleted = await cosmos.DeleteHolonAsync(holon.Id);
                    deleted.IsError.Should().BeFalse(deleted.Message);
                    (await local.DeleteHolonAsync(holon.Id)).IsError.Should().BeFalse();
                }
                if (avatarSaved)
                {
                    (await cosmos.DeleteAvatarAsync(avatar.Id, softDelete: false)).IsError.Should().BeFalse();
                    (await local.DeleteAvatarAsync(avatar.Id, softDelete: false)).IsError.Should().BeFalse();
                }
            }
            finally
            {
                try { await cosmos.DeActivateProviderAsync(); }
                finally { await local.DeActivateProviderAsync(); }
            }
            if(verifiedComplete){journal.Dispose();File.Delete(journalPath);}
            else Console.WriteLine("Retained owned Cosmos V2 recovery intent: "+journalPath);
        }
    }
}
