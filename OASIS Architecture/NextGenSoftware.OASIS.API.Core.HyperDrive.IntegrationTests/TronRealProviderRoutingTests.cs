using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
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
    public async Task InspectOriginalInterruptedFixtureWithoutMutation()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"Scripts/TestHosts/Use-ReusableTronRuntime.ps1")))directory=directory.Parent;
        directory.Should().NotBeNull();var root=Path.Combine(directory!.FullName,"TestResults/HyperDriveTron");
        using var lease=new FileStream(Path.Combine(root,"store.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        File.Exists(Path.Combine(root,"pending-native-outcome.json")).Should().BeTrue("this is read-only reconciliation of the retained original fence");
        var contract=(await File.ReadAllTextAsync(Path.Combine(directory.FullName,"TestResults/TronRuntime/local-store-address.txt"))).Trim();
        var key=new string('0',63)+"1";
        await using var runtime=new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),new Uri("http://127.0.0.1:50052"),
            "0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f",contractFeeLimitSun:1000000000);
        var owner=new TronECKey(key,TronNetwork.MainNet).GetPublicAddress();
        using var authorityDeadline=new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var authority=await runtime.NativeTransfers.LoadPermissionAuthorityAsync(owner,authorityDeadline.Token);
        TronPermissionVerifier.Verify(authority,authority.Address,0,(int)TronNet.Protocol.Transaction.Types.Contract.Types.ContractType.TransferContract,new[]{authority.Address});
        Console.WriteLine("Retained real TRON account owner permission accepts its original local fixture signer.");
        using var storage=new TronPrivateStorageBackend(runtime.NativeTransfers,new TronECKey(key,TronNetwork.MainNet).GetPublicAddress(),contract,()=>key,
            ()=>SHA256.HashData(Encoding.UTF8.GetBytes("public reusable local TRON encryption fixture")));
        var tron=new TRONOASIS(rpcEndpoint:"http://127.0.0.1:9090",nativeTransfers:runtime.NativeTransfers,storageBackend:storage);
        var activated=await tron.ActivateProviderAsync();activated.IsError.Should().BeFalse(activated.Message);
        try{
            var id=Guid.Parse("b531a581-5627-451c-9de5-95c198646c20");
            var avatar=await tron.LoadAvatarAsync(id);var detail=await tron.LoadAvatarDetailAsync(id);
            avatar.Exception.Should().BeNull();detail.Exception.Should().BeNull();
            (!avatar.IsError||avatar.ErrorCode=="OASIS_RECORD_NOT_FOUND").Should().BeTrue(avatar.Message);
            (!detail.IsError||detail.ErrorCode=="OASIS_RECORD_NOT_FOUND").Should().BeTrue(detail.Message);
            if(avatar.Result!=null){avatar.IsError.Should().BeFalse(avatar.Message);avatar.Result.Id.Should().Be(id);avatar.Result.Username.Should().Be("tron-real-v2-avatar");}
            if(detail.Result!=null){detail.IsError.Should().BeFalse(detail.Message);detail.Result.Id.Should().Be(id);detail.Result.Username.Should().Be("tron-real-v2-avatar");}
            Console.WriteLine($"Original owned TRON fixture readback: avatarPresent={avatar.Result!=null}; detailPresent={detail.Result!=null}; original transaction fence retained.");
            var local=new LocalFileOASIS(storageDirectory:Path.Combine(root,"files"));
            (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
            try{
                var localAvatar=await local.LoadAvatarAsync(id);localAvatar.Exception.Should().BeNull();
                var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
                    HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings(),SubscriptionConfig=new SubscriptionConfig{PlanType="Gold"}}};
                var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=true};
                manager.RegisterProvider(local);manager.RegisterProvider(tron);
                manager.SetAndReplaceAutoLoadBalanceListForProviders(new[]{new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),new EnumValue<ProviderType>(ProviderType.TRONOASIS)}).IsError.Should().BeFalse();
                var router=new OASISHyperDrive(manager);
                var timings=new Dictionary<ProviderType,double>();
                foreach(var provider in new IOASISStorageProvider[]{local,tron}){
                    var timer=Stopwatch.StartNew();var measured=await provider.LoadAvatarAsync(id);timer.Stop();
                    measured.Exception.Should().BeNull();
                    manager.PerformanceMonitor.RecordRequest(provider==tron?ProviderType.TRONOASIS:ProviderType.LocalFileOASIS,!measured.IsError,timer.Elapsed.TotalMilliseconds);
                    timings[provider==tron?ProviderType.TRONOASIS:ProviderType.LocalFileOASIS]=timer.Elapsed.TotalMilliseconds;
                }
                var expectedFastest=timings[ProviderType.LocalFileOASIS]<=timings[ProviderType.TRONOASIS]?ProviderType.LocalFileOASIS:ProviderType.TRONOASIS;
                var fastestBefore=manager.PerformanceMonitor.GetMetrics(expectedFastest).TotalRequests;
                var performance=await router.RouteRequestAsync<IAvatar>(new StorageOperationRequest{Operation="LoadAvatar",AvatarId=id},LoadBalancingStrategy.Performance);
                performance.IsError.Should().BeFalse(performance.Message);
                manager.PerformanceMonitor.GetMetrics(expectedFastest).TotalRequests.Should().Be(fastestBefore+1);
                (performance.Result?.Id).Should().Be((expectedFastest==ProviderType.TRONOASIS?avatar.Result:localAvatar.Result)?.Id);
                var beforeTron=manager.PerformanceMonitor.GetMetrics(ProviderType.TRONOASIS).TotalRequests;
                var beforeLocal=manager.PerformanceMonitor.GetMetrics(ProviderType.LocalFileOASIS).TotalRequests;
                for(var index=0;index<6;index++){
                    var prior=manager.PerformanceMonitor.GetMetrics(ProviderType.TRONOASIS).TotalRequests;
                    var request=new StorageOperationRequest{Operation="LoadAvatar",AvatarId=id};
                    var read=index%2==0?router.RouteRequest<IAvatar>(request,LoadBalancingStrategy.RoundRobin)
                        :await router.RouteRequestAsync<IAvatar>(request,LoadBalancingStrategy.RoundRobin);
                    read.IsError.Should().BeFalse(read.Message);
                    var usedTron=manager.PerformanceMonitor.GetMetrics(ProviderType.TRONOASIS).TotalRequests>prior;
                    var expected=usedTron?avatar.Result:localAvatar.Result;
                    (read.Result?.Id).Should().Be(expected?.Id,"each real provider's authoritative read must be retained, not replaced by another copy");
                }
                manager.PerformanceMonitor.GetMetrics(ProviderType.TRONOASIS).TotalRequests.Should().Be(beforeTron+3);
                manager.PerformanceMonitor.GetMetrics(ProviderType.LocalFileOASIS).TotalRequests.Should().Be(beforeLocal+3);
                dna.OASIS.SubscriptionConfig.PlanType="Free";
                var excludedBefore=manager.PerformanceMonitor.GetMetrics(ProviderType.TRONOASIS).TotalRequests;
                foreach(var strategy in new[]{LoadBalancingStrategy.RoundRobin,LoadBalancingStrategy.WeightedRoundRobin,
                    LoadBalancingStrategy.LeastConnections,LoadBalancingStrategy.Geographic,LoadBalancingStrategy.Performance}){
                    var request=new StorageOperationRequest{Operation="LoadAvatar",AvatarId=id};
                    foreach(var read in new[]{router.RouteRequest<IAvatar>(request,strategy),await router.RouteRequestAsync<IAvatar>(request,strategy)}){
                        read.IsError.Should().BeFalse(read.Message);(read.Result?.Id).Should().Be(localAvatar.Result?.Id);
                        manager.LastProviderSelectionDiagnostic.Candidates.Select(candidate=>candidate.Provider).Should().Equal(ProviderType.LocalFileOASIS);
                    }
                }
                manager.PerformanceMonitor.GetMetrics(ProviderType.TRONOASIS).TotalRequests.Should().Be(excludedBefore);
                dna.OASIS.SubscriptionConfig.PlanType="Gold";
                manager.IsAutoLoadBalanceEnabled=false;manager.IsAutoFailOverEnabled=true;
                manager.SetAndReplaceAutoFailOverListForProviders(new[]{new EnumValue<ProviderType>(ProviderType.TRONOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS)}).IsError.Should().BeFalse();
                manager.SetAndActivateCurrentStorageProvider(tron).IsError.Should().BeFalse();
                await runtime.DisposeAsync();
                foreach(var asynchronous in new[]{false,true}){
                    var request=new StorageOperationRequest{Operation="LoadAvatar",AvatarId=id,PreferredProvider=ProviderType.TRONOASIS};
                    var read=asynchronous?await router.RouteRequestAsync<IAvatar>(request):router.RouteRequest<IAvatar>(request);
                    read.IsError.Should().BeFalse(read.Message);(read.Result?.Id).Should().Be(localAvatar.Result?.Id);
                    manager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.LocalFileOASIS);
                }
            }finally{await local.DeActivateProviderAsync();}
            File.Exists(Path.Combine(root,"pending-native-outcome.json")).Should().BeTrue();
        }finally{await tron.DeActivateProviderAsync();}
    }
    [Fact]
    public async Task V2ReplicatesBalancesAndFailsOverDuringOwnedNodePause()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/Use-ReusableTronRuntime.ps1"))) directory = directory.Parent;
        directory.Should().NotBeNull();
        var root = Path.Combine(directory!.FullName, "TestResults/HyperDriveTron");
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (File.Exists(Path.Combine(root, "pending-native-outcome.json")))
            throw new InvalidOperationException("Retained TRON transaction outcome requires reconciliation before another mutation test. See pending-native-outcome.json.");
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
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings(),
                SubscriptionConfig = new SubscriptionConfig { PlanType = "Gold" } } };
        var manager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = true, IsAutoLoadBalanceEnabled = false };
        manager.RegisterProvider(local); manager.RegisterProvider(tron);
        EnumValue<ProviderType>[] Providers() => new[] { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS), new EnumValue<ProviderType>(ProviderType.TRONOASIS) };
        manager.SetAndReplaceAutoReplicationListForProviders(Providers()).IsError.Should().BeFalse();
        var holon = new Holon { Id = Guid.Parse("deba869c-8078-4a98-a7e4-cb7c11d59249"), Name = "Real V2 TRON replication",
            ParentHolonId = Guid.Parse("9e5be296-e435-44a5-838b-cbfcd8918c22"),
            ProviderUniqueStorageKey = new() { [ProviderType.LocalFileOASIS] = "deba869c-8078-4a98-a7e4-cb7c11d59249" },
            MetaData = new Dictionary<string, object> { ["routing-fixture"] = "tron-real-v2" } };
        var failedOver = new Holon { Id = Guid.Parse("c8752504-dff2-44c6-99d9-32a601ae9670"), Name = "Real V2 TRON outage" };
        var partial = new Holon { Id = Guid.Parse("f8882565-96ba-40f5-b2ac-a47e78ab8ea9"), Name = "Real V2 partial replication" };
        var paused = false;
        var publicKey = Convert.ToHexString(new Nethereum.Signer.EthECKey(key).GetPubKey());
        var encryptedKey = Rijndael256.Rijndael.Encrypt(key, new string('k', 32), Rijndael256.KeySize.Aes256);
        var avatar = new Avatar { Id = Guid.Parse("b531a581-5627-451c-9de5-95c198646c20"),
            Username = "tron-real-v2-avatar", Email = "tron-real-v2-avatar@example.invalid",
            ProviderUniqueStorageKey = new() { [ProviderType.LocalFileOASIS] = "b531a581-5627-451c-9de5-95c198646c20" },
            VerificationToken = "PublicLocalV2Verification", ResetToken = "PublicLocalV2Reset",
            RefreshTokens = new() { new RefreshToken { Token = "PublicLocalV2Refresh" } } };
        avatar.ProviderWallets = new() { [ProviderType.TRONOASIS] = new List<IProviderWallet> {
            new ProviderWallet { Id = Guid.Parse("20e85856-3c4c-4acd-bca4-b07c2af07c34"), AvatarId = avatar.Id,
                ProviderType = ProviderType.TRONOASIS, PublicKey = publicKey, PrivateKey = encryptedKey,
                WalletAddress = new TronECKey(key, TronNetwork.MainNet).GetPublicAddress(), IsDefaultWallet = true } } };
        var detail = new AvatarDetail { Id = avatar.Id, Username = avatar.Username, Email = avatar.Email };
        var batch = new IHolon[] {
            new Holon { Id = Guid.Parse("9e5be296-e435-44a5-838b-cbfcd8918c22"), Name = "V2 TRON batch first", CreatedByAvatarId = avatar.Id,
                ProviderUniqueStorageKey = new() { [ProviderType.LocalFileOASIS] = "9e5be296-e435-44a5-838b-cbfcd8918c22" } },
            new Holon { Id = Guid.Parse("ac96a3b6-6d9f-475a-8422-b756dc7af654"), Name = "V2 TRON batch second", CreatedByAvatarId = avatar.Id,
                ProviderUniqueStorageKey = new() { [ProviderType.LocalFileOASIS] = "ac96a3b6-6d9f-475a-8422-b756dc7af654" } } };
        var avatarSaved = false;
        try
        {
            var router = new OASISHyperDrive(manager);
            async Task AssertIndexedRoutes<T>(string operation, bool absent, Func<T, Guid> identity) where T : class
            {
                foreach (var providerType in new[] { ProviderType.LocalFileOASIS, ProviderType.TRONOASIS })
                {
                    var request = new StorageOperationRequest { Operation = operation, AvatarId = avatar.Id, Username = avatar.Username,
                        Email = avatar.Email, VerificationToken = avatar.VerificationToken, ResetToken = avatar.ResetToken,
                        RefreshToken = avatar.RefreshTokens[0].Token, ProviderKey = avatar.Id.ToString(),
                        PublicKey = publicKey, PrivateKey = encryptedKey, PreferredProvider = providerType };
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
                await AssertIndexedRoutes<IAvatar>("LoadAvatarByProviderKey", absent, value => value.Id);
                await AssertIndexedRoutes<IAvatar>("LoadAvatarByPublicKey", absent, value => value.Id);
                await AssertIndexedRoutes<IAvatar>("LoadAvatarByPrivateKey", absent, value => value.Id);
                await AssertIndexedRoutes<IAvatar>("LoadAvatarByVerificationToken", absent, value => value.Id);
                await AssertIndexedRoutes<IAvatar>("LoadAvatarByResetToken", absent, value => value.Id);
                await AssertIndexedRoutes<IAvatar>("LoadAvatarByRefreshToken", absent, value => value.Id);
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
            var imported = router.RouteRequest<bool>(new StorageOperationRequest { Operation = "Import", Payload = batch,
                PreferredProvider = ProviderType.LocalFileOASIS });
            imported.IsError.Should().BeFalse(imported.Message); imported.IsWarning.Should().BeFalse(imported.Message);
            imported.Result.Should().BeTrue();
            batch[0].Name = "V2 TRON batch updated";
            var batchSaved = await router.RouteRequestAsync<IEnumerable<IHolon>>(new StorageOperationRequest {
                Operation = "SaveHolons", Payload = batch, PreferredProvider = ProviderType.LocalFileOASIS });
            batchSaved.IsError.Should().BeFalse(batchSaved.Message); batchSaved.IsWarning.Should().BeFalse(batchSaved.Message);
            foreach (var providerType in new[] { ProviderType.LocalFileOASIS, ProviderType.TRONOASIS })
            foreach (var operation in new[] { "ExportAll", "ExportAllDataForAvatarById", "ExportAllDataForAvatarByUsername", "ExportAllDataForAvatarByEmail" })
            {
                var request = new StorageOperationRequest { Operation = operation, AvatarId = avatar.Id, Username = avatar.Username,
                    Email = avatar.Email, PreferredProvider = providerType };
                foreach (var exported in new[] { router.RouteRequest<IEnumerable<IHolon>>(request),
                    await router.RouteRequestAsync<IEnumerable<IHolon>>(request) })
                {
                    exported.IsError.Should().BeFalse(exported.Message);
                    foreach (var item in batch) exported.Result.Single(value => value.Id == item.Id).Name.Should().Be(item.Name);
                }
            }
            foreach (var operation in new[] { "DeleteAvatarDetail", "DeleteAvatarDetailByUsername", "DeleteAvatarDetailByEmail" })
            foreach (var synchronous in new[] { false, true })
            foreach (var softDelete in new[] { true, false })
            {
                detail.IsActive = true; detail.DeletedDate = DateTime.MinValue;
                var restored = await router.RouteRequestAsync<IAvatarDetail>(new StorageOperationRequest {
                    Operation = "SaveAvatarDetail", Payload = detail, PreferredProvider = ProviderType.LocalFileOASIS });
                restored.IsError.Should().BeFalse(restored.Message); restored.IsWarning.Should().BeFalse(restored.Message);
                var request = new StorageOperationRequest { Operation = operation, AvatarId = avatar.Id,
                    Username = avatar.Username, Email = avatar.Email, SoftDelete = softDelete, PreferredProvider = ProviderType.TRONOASIS };
                var removed = synchronous ? router.RouteRequest<bool>(request) : await router.RouteRequestAsync<bool>(request);
                removed.IsError.Should().BeFalse(removed.Message); removed.IsWarning.Should().BeFalse(removed.Message);
                removed.Result.Should().BeTrue(); manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
                foreach (var provider in new IOASISStorageProvider[] { local, tron })
                {
                    var retained = await provider.LoadAvatarDetailAsync(avatar.Id);
                    if (softDelete)
                    {
                        retained.IsError.Should().BeFalse(retained.Message); retained.Result.IsActive.Should().BeFalse();
                        retained.Result.DeletedDate.Should().NotBe(DateTime.MinValue);
                    }
                    else retained.Result.Should().BeNull();
                    (await provider.LoadAvatarByUsernameAsync(avatar.Username)).Result.Id.Should().Be(avatar.Id);
                }
            }
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
            foreach (var operation in new[] { "DeleteAvatarByUsername", "DeleteAvatarByEmail", "DeleteAvatarByProviderKey" })
            foreach (var synchronous in new[] { false, true })
            {
                var restored = await router.RouteRequestAsync<IAvatar>(new StorageOperationRequest { Operation = "SaveAvatar",
                    Payload = avatar, PreferredProvider = ProviderType.LocalFileOASIS });
                avatarSaved = !restored.IsError;
                restored.IsError.Should().BeFalse(restored.Message); restored.IsWarning.Should().BeFalse(restored.Message);
                var detailRestored = await router.RouteRequestAsync<IAvatarDetail>(new StorageOperationRequest { Operation = "SaveAvatarDetail",
                    Payload = detail, PreferredProvider = ProviderType.LocalFileOASIS });
                detailRestored.IsError.Should().BeFalse(detailRestored.Message); detailRestored.IsWarning.Should().BeFalse(detailRestored.Message);
                var request = new StorageOperationRequest { Operation = operation, Username = avatar.Username, Email = avatar.Email,
                    ProviderKey = avatar.Id.ToString(), SoftDelete = false, PreferredProvider = ProviderType.TRONOASIS };
                var removed = synchronous ? router.RouteRequest<bool>(request) : await router.RouteRequestAsync<bool>(request);
                removed.IsError.Should().BeFalse(removed.Message); removed.IsWarning.Should().BeFalse(removed.Message); removed.Result.Should().BeTrue();
                manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
                foreach (var provider in new IOASISStorageProvider[] { local, tron })
                {
                    (await provider.LoadAvatarAsync(avatar.Id)).Result.Should().BeNull();
                    (await provider.LoadAvatarByUsernameAsync(avatar.Username)).Result.Should().BeNull();
                    (await provider.LoadAvatarByEmailAsync(avatar.Email)).Result.Should().BeNull();
                    (await provider.LoadAvatarDetailAsync(avatar.Id)).Result.Should().BeNull();
                }
                avatarSaved = false;
            }
            var saved = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.LocalFileOASIS });
            saved.IsError.Should().BeFalse(saved.Message);
            (await tron.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
            (await local.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
            foreach (var providerType in new[] { ProviderType.LocalFileOASIS, ProviderType.TRONOASIS })
            {
                var search = new StorageOperationRequest { Operation = "Search", PreferredProvider = providerType, LoadChildren = false,
                    SearchParams = new SearchParams { SearchGroups = new List<ISearchGroupBase> {
                        new SearchTextGroup { SearchQuery = holon.Name, SearchHolons = true, SearchAvatars = false } } } };
                foreach (var result in new[] { router.RouteRequest<ISearchResults>(search), await router.RouteRequestAsync<ISearchResults>(search) })
                { result.IsError.Should().BeFalse(result.Message); result.Result.SearchResultHolons.Single(value => value.Id == holon.Id).Name.Should().Be(holon.Name); }
                var byKey = new StorageOperationRequest { Operation = "LoadHolonByProviderKey", ProviderKey = holon.Id.ToString(),
                    PreferredProvider = providerType, LoadChildren = false };
                foreach (var result in new[] { router.RouteRequest<IHolon>(byKey), await router.RouteRequestAsync<IHolon>(byKey) })
                { result.IsError.Should().BeFalse(result.Message); result.Result.Id.Should().Be(holon.Id); }
                foreach (var operation in new[] { "LoadHolonsForParent", "LoadHolonsForParentByProviderKey" })
                {
                    var request = new StorageOperationRequest { Operation = operation, HolonId = holon.ParentHolonId,
                        ProviderKey = holon.ParentHolonId.ToString(), PreferredProvider = providerType, LoadChildren = false };
                    foreach (var result in new[] { router.RouteRequest<IEnumerable<IHolon>>(request), await router.RouteRequestAsync<IEnumerable<IHolon>>(request) })
                    { result.IsError.Should().BeFalse(result.Message); result.Result.Single(value => value.Id == holon.Id).Name.Should().Be(holon.Name); }
                }
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
            foreach (var synchronous in new[] { false, true })
            {
                var restored = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest { Operation = "SaveHolon",
                    Payload = batch[1], PreferredProvider = ProviderType.LocalFileOASIS });
                restored.IsError.Should().BeFalse(restored.Message); restored.IsWarning.Should().BeFalse(restored.Message);
                var request = new StorageOperationRequest { Operation = "DeleteHolonByProviderKey", ProviderKey = batch[1].Id.ToString(),
                    SoftDelete = false, PreferredProvider = ProviderType.TRONOASIS };
                var removed = synchronous ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
                removed.IsError.Should().BeFalse(removed.Message); removed.IsWarning.Should().BeFalse(removed.Message);
                manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
                (await tron.LoadHolonAsync(batch[1].Id)).Result.Should().BeNull();
                (await local.LoadHolonAsync(batch[1].Id)).Result.Should().BeNull();
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
            foreach (var id in new[] { holon.Id, failedOver.Id, partial.Id }.Concat(batch.Select(item => item.Id)))
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
