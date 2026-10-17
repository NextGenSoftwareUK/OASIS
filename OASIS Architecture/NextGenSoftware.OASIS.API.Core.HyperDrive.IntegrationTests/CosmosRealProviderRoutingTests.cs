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
    private static async Task FinishOwnedCleanupAsync(Exception? originalFailure,params Func<Task>[] steps)
    {
        var failures=new List<Exception>();
        foreach(var step in steps){try{await step();}catch(Exception failure){failures.Add(failure);}}
        if(failures.Count==0)return;
        if(originalFailure!=null)failures.Insert(0,originalFailure);
        if(failures.Count==1)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
        throw new AggregateException("Cosmos operation/owned cleanup failed; recovery intent retained.",failures);
    }

    [Fact]
    public async Task CleanupPreservesOriginalAndEveryDeactivationFailure()
    {
        var original=new InvalidOperationException("original fixture operation");
        var cleanup=new IOException("owned cleanup");
        var first=new InvalidOperationException("first deactivation");
        var second=new InvalidOperationException("second deactivation");
        var attempted=0;
        var failure=await Assert.ThrowsAsync<AggregateException>(()=>FinishOwnedCleanupAsync(original,
            ()=>{attempted++;return Task.FromException(cleanup);},
            ()=>{attempted++;return Task.FromException(first);},
            ()=>{attempted++;return Task.FromException(second);}));
        Assert.Equal(3,attempted);Assert.Equal(new Exception[]{original,cleanup,first,second},failure.InnerExceptions);
        await FinishOwnedCleanupAsync(original,()=>Task.CompletedTask);
    }
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
        using var indexed=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"cosmos-pending-transactions-current.json")));
        indexed.RootElement.GetProperty("inspectionId").GetString().Should().Be(Required("OASIS_COSMOS_INSPECTION_ID"));
        indexed.RootElement.GetProperty("chain").GetString().Should().Be(Required("OASIS_COSMOS_TEST_CHAIN"));
        indexed.RootElement.GetProperty("contract").GetString().Should().Be(Required("OASIS_COSMOS_TEST_CONTRACT"));
        using var cosmos = new CosmosBlockChainOASIS(Required("OASIS_COSMOS_TEST_RPC"),Required("OASIS_COSMOS_TEST_CHAIN"),
            Required("OASIS_COSMOS_TEST_KEY"),Required("OASIS_COSMOS_TEST_CONTRACT"),"wasm","0.025stake","stake",storageEncryptionKeyResolver:LocalStorageKey);
        (await cosmos.ActivateProviderAsync()).IsError.Should().BeFalse();
        try
        {
            var avatar = await cosmos.LoadAvatarAsync(avatarId);
            var detail = await cosmos.LoadAvatarDetailAsync(avatarId);
            var holon = await cosmos.LoadHolonAsync(holonId,loadChildren:false);
            var samples=indexed.RootElement.GetProperty("feeSamples");samples.GetArrayLength().Should().BeInRange(1,3);
            foreach(var sample in samples.EnumerateArray()){
                var hash=sample.GetProperty("transactionHash").GetString()!;
                var receipt=await cosmos.GetNativeFeeReceiptAsync(hash);
                receipt.IsError.Should().BeFalse(receipt.Message);
                receipt.Result.GetProperty("transactionHash").GetString().Should().Be(hash);
                receipt.Result.GetProperty("chainId").GetString().Should().Be(Required("OASIS_COSMOS_TEST_CHAIN"));
                foreach(var name in new[]{"payer","basis","gasUsed","gasWanted"})
                    receipt.Result.GetProperty(name).GetString().Should().Be(sample.GetProperty(name).GetString());
                receipt.Result.GetProperty("success").GetBoolean().Should().Be(sample.GetProperty("success").GetBoolean());
                receipt.Result.GetProperty("fees").EnumerateArray().Select(coin=>(coin.GetProperty("denom").GetString(),coin.GetProperty("amount").GetString()))
                    .Should().Equal(sample.GetProperty("fees").EnumerateArray().Select(coin=>(coin.GetProperty("denom").GetString(),coin.GetProperty("amount").GetString())));
                receipt.Result.GetProperty("basis").GetString().Should().Be("observed-ante-fee-deduction");
            }
            ((NextGenSoftware.OASIS.API.Core.Interfaces.IPreparedNativeFeeEvidenceReader)cosmos).DeclaredFeeCoverage
                .Should().Be(NextGenSoftware.OASIS.API.Core.Services.Subscriptions.NativeFeeCoverage.AnteDeductionOnly);
            var local=new LocalFileOASIS(storageDirectory:Path.Combine(root,"files"));
            (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
            try{
                var localAvatar=await local.LoadAvatarAsync(avatarId);localAvatar.Exception.Should().BeNull();
                var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,
                    StorageProviders=new StorageProviderSettings(),SubscriptionConfig=new SubscriptionConfig {PlanType="Gold"}}};
                var manager=new ProviderManager(null,dna) {IsAutoLoadBalanceEnabled=true,IsAutoReplicationEnabled=false,IsAutoFailOverEnabled=false};
                manager.RegisterProvider(local);manager.RegisterProvider(cosmos);
                var providers=new[]{ProviderType.LocalFileOASIS,ProviderType.CosmosBlockChainOASIS};
                manager.SetAndReplaceAutoLoadBalanceListForProviders(providers.Select(type=>new EnumValue<ProviderType>(type))).IsError.Should().BeFalse();
                var router=new OASISHyperDrive(manager);
                manager.IsAutoLoadBalanceEnabled=false;
                foreach(var type in providers){
                    var baseline=await router.RouteRequestAsync<IAvatar>(new StorageOperationRequest {Operation="LoadAvatar",AvatarId=avatarId,PreferredProvider=type});
                    baseline.IsError.Should().BeFalse(baseline.Message);
                }
                manager.IsAutoLoadBalanceEnabled=true;
                foreach(var strategy in Enum.GetValues<LoadBalancingStrategy>())foreach(var synchronous in new[]{true,false}){
                    var counts=providers.ToDictionary(type=>type,type=>manager.PerformanceMonitor.GetMetrics(type).TotalRequests);
                    var request=new StorageOperationRequest {Operation="LoadAvatar",AvatarId=avatarId};
                    var read=synchronous?router.RouteRequest<IAvatar>(request,strategy):await router.RouteRequestAsync<IAvatar>(request,strategy);
                    read.IsError.Should().BeFalse(read.Message);var diagnostic=manager.LastProviderSelectionDiagnostic;
                    diagnostic.RequestedStrategy.Should().Be(strategy);diagnostic.Candidates.Select(value=>value.Provider).Should().BeEquivalentTo(providers);
                    providers.Should().Contain(diagnostic.SelectedProvider);
                    (read.Result?.Id).Should().Be((diagnostic.SelectedProvider==ProviderType.CosmosBlockChainOASIS?avatar.Result:localAvatar.Result)?.Id);
                    foreach(var type in providers)manager.PerformanceMonitor.GetMetrics(type).TotalRequests.Should().Be(counts[type]+(type==diagnostic.SelectedProvider?1:0));
                }
                dna.OASIS.SubscriptionConfig.PlanType="Free";
                var excludedBefore=manager.PerformanceMonitor.GetMetrics(ProviderType.CosmosBlockChainOASIS).TotalRequests;
                foreach(var strategy in Enum.GetValues<LoadBalancingStrategy>())foreach(var synchronous in new[]{true,false}){
                    var request=new StorageOperationRequest {Operation="LoadAvatar",AvatarId=avatarId};
                    var read=synchronous?router.RouteRequest<IAvatar>(request,strategy):await router.RouteRequestAsync<IAvatar>(request,strategy);
                    read.IsError.Should().BeFalse(read.Message);(read.Result?.Id).Should().Be(localAvatar.Result?.Id);
                    manager.LastProviderSelectionDiagnostic.Candidates.Select(value=>value.Provider).Should().Equal(ProviderType.LocalFileOASIS);
                }
                manager.PerformanceMonitor.GetMetrics(ProviderType.CosmosBlockChainOASIS).TotalRequests.Should().Be(excludedBefore);
                dna.OASIS.SubscriptionConfig.PlanType="Gold";manager.IsAutoLoadBalanceEnabled=false;manager.IsAutoFailOverEnabled=true;
                manager.SetAndReplaceAutoFailOverListForProviders(new[]{new EnumValue<ProviderType>(ProviderType.CosmosBlockChainOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS)}).IsError.Should().BeFalse();
                var stop=new ProcessStartInfo("wsl.exe") {UseShellExecute=false,CreateNoWindow=true};
                foreach(var argument in new[]{"-d","Ubuntu","-u","root","--","bash",Required("OASIS_COSMOS_TEST_STOP_SCRIPT")})stop.ArgumentList.Add(argument);
                using(var child=Process.Start(stop)??throw new InvalidOperationException("Owned Cosmos stop did not start.")){
                    await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));child.ExitCode.Should().Be(0);
                }
                foreach(var synchronous in new[]{true,false}){
                    var before=manager.PerformanceMonitor.GetMetrics(ProviderType.CosmosBlockChainOASIS).TotalRequests;
                    var request=new StorageOperationRequest {Operation="LoadAvatar",AvatarId=avatarId,PreferredProvider=ProviderType.CosmosBlockChainOASIS};
                    var read=synchronous?router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request);
                    read.IsError.Should().BeFalse(read.Message);(read.Result?.Id).Should().Be(localAvatar.Result?.Id);
                    manager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.LocalFileOASIS);
                    manager.PerformanceMonitor.GetMetrics(ProviderType.CosmosBlockChainOASIS).TotalRequests.Should().Be(before+1);
                }
            }finally{await local.DeActivateProviderAsync();}
            foreach (var state in new[] { (avatar.IsError,avatar.ErrorCode,avatar.Message), (detail.IsError,detail.ErrorCode,detail.Message), (holon.IsError,holon.ErrorCode,holon.Message) })
                if (state.IsError) state.ErrorCode.Should().Be(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,state.Message);
            await File.WriteAllTextAsync(Path.Combine(root,"cosmos-inspection-current.json"),System.Text.Json.JsonSerializer.Serialize(new {
                chain = Required("OASIS_COSMOS_TEST_CHAIN"), contract = Required("OASIS_COSMOS_TEST_CONTRACT"), avatarId, holonId,
                avatarPresent = avatar.Result != null, detailPresent = detail.Result != null, holonPresent = holon.Result != null,
                avatarErrorCode = avatar.ErrorCode, detailErrorCode = detail.ErrorCode, holonErrorCode = holon.ErrorCode,
                inspectionId=Required("OASIS_COSMOS_INSPECTION_ID"),nativeFeeSamplesVerified=samples.GetArrayLength(),nativeFeeCoverage="AnteDeductionOnly",
                strategyPathsVerified=Enum.GetValues<LoadBalancingStrategy>().Length,stoppedNodeFailoverVerified=true,
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
        Exception? originalFailure=null;
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
        catch(Exception failure){originalFailure=failure;throw;}
        finally
        {
            await FinishOwnedCleanupAsync(originalFailure,async()=>{
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
            },async()=>{var deactivated=await cosmos.DeActivateProviderAsync();deactivated.IsError.Should().BeFalse(deactivated.Message);},
              async()=>{var deactivated=await local.DeActivateProviderAsync();deactivated.IsError.Should().BeFalse(deactivated.Message);});
            if(verifiedComplete){journal.Dispose();File.Delete(journalPath);}
            else Console.WriteLine("Retained owned Cosmos V2 recovery intent: "+journalPath);
        }
    }
}
