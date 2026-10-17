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
    private static byte[] LocalStorageKey() => System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes("oasis-sui-local-storage-fixture-v1"));
    [Fact]
    public async Task ArchiveExactSearchValidationFixtureAfterReadback()
    {
        var rpc=Required("OASIS_SUI_TEST_RPC");new Uri(rpc).IsLoopback.Should().BeTrue();
        var root=Path.GetFullPath(Required("OASIS_HYPERDRIVE_TEST_ROOT"));Path.GetFileName(root).Should().Be("HyperDriveSui");
        using var lease=new FileStream(Path.Combine(root,"store.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var path=Path.Combine(root,"encrypted-replication-pending.json");var original=File.ReadAllBytes(path);
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(original)).Should().Be("69DFE679CD911A28C5C7A10F9E23CDD06837C5B2250694AD9535DDDD3859C292");
        using var intent=JsonDocument.Parse(original);using var deployment=JsonDocument.Parse(File.ReadAllText(Required("OASIS_SUI_TEST_DEPLOYMENT")));
        var data=intent.RootElement;var settings=deployment.RootElement;
        data.GetProperty("chainId").GetString().Should().Be(settings.GetProperty("chainId").GetString());
        data.GetProperty("storageObjectId").GetString().Should().Be(settings.GetProperty("storageObjectId").GetString());
        data.GetProperty("encryptionKeyLabel").GetString().Should().Be("oasis-sui-local-storage-fixture-v1");
        var reportPath=Path.Combine(root,"sui-retained-v2-healthy-current.trx");var reportBytes=File.ReadAllBytes(reportPath);
        var report=new System.Xml.XmlDocument();using(var xml=new MemoryStream(reportBytes))report.Load(xml);
        var failed=report.SelectNodes("//*[local-name()='UnitTestResult'][@outcome='Failed']")!;failed.Count.Should().Be(1);
        failed[0]!.SelectSingleNode(".//*[local-name()='Message']")!.InnerText.Should().Contain("Invalid search request: Current-avatar search requires AvatarId.");
        using var sui=new SuiOASIS(rpc,"localnet",settings.GetProperty("chainId").GetString()!,settings.GetProperty("packageAddress").GetString()!,
            settings.GetProperty("privateKey").GetString()!,settings.GetProperty("storageObjectId").GetString()!,storageEncryptionKeyResolver:LocalStorageKey);
        var local=new LocalFileOASIS(storageDirectory:Path.Combine(root,"files"));var observed=new List<object>();
        try {
            foreach(var provider in new IOASISStorageProvider[]{sui,local}){
                (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
                foreach(var pair in new[]{("holonId","real-v2-sui-replication"),("parentId","Owned Sui routing parent")}){
                    var read=await provider.LoadHolonAsync(data.GetProperty(pair.Item1).GetGuid(),loadChildren:false);
                    read.Exception.Should().BeNull();
                    (read.IsError==false||read.ErrorCode==NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound).Should().BeTrue(read.Message);
                    if(read.Result!=null){read.Result.Id.Should().Be(data.GetProperty(pair.Item1).GetGuid());read.Result.Name.Should().Be(pair.Item2);
                        read.Result.DeletedDate.Should().NotBe(DateTime.MinValue,"finally acknowledged soft deletion must be visible");}
                    observed.Add(new {provider=provider.ProviderType.Value.ToString(),record=pair.Item1,absent=read.Result==null,deletedDate=read.Result?.DeletedDate});
                }
                var avatarId=data.GetProperty("avatarId").GetGuid();var username=data.GetProperty("Username").GetString()!;var email=data.GetProperty("Email").GetString()!;
                foreach(var read in new[]{await provider.LoadAvatarAsync(avatarId),await provider.LoadAvatarByUsernameAsync(username),await provider.LoadAvatarByEmailAsync(email)}){
                    read.Exception.Should().BeNull();read.Result.Should().BeNull();(read.IsError==false||read.ErrorCode==NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound).Should().BeTrue(read.Message);
                }
                var detail=await provider.LoadAvatarDetailAsync(avatarId);detail.Exception.Should().BeNull();detail.Result.Should().BeNull();
                (detail.IsError==false||detail.ErrorCode==NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound).Should().BeTrue(detail.Message);
            }
            var archivePath=Path.Combine(root,"search-validation-fixture-archived.json");
            using(var archive=new FileStream(archivePath,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
                await JsonSerializer.SerializeAsync(archive,new {intent=data,observed,failedReportSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(reportBytes)),
                    fixtureCleanupConfirmed=true,nativeMutationSubmitted=false,productionBillingReconciled=false});archive.Flush(true);
            }
            using var archived=JsonDocument.Parse(File.ReadAllBytes(archivePath));archived.RootElement.GetProperty("fixtureCleanupConfirmed").GetBoolean().Should().BeTrue();
            File.ReadAllBytes(path).Should().Equal(original);File.Delete(path);
            Console.WriteLine("Exact reviewed search-validation fixture archived after native/local deletion readback; no native mutation or replay.");
        } finally {await sui.DeActivateProviderAsync();await local.DeActivateProviderAsync();}
    }
    [Fact]
    public async Task ReconcileExactOwnedInterruptedAvatarFixture()
    {
        var rpc = Required("OASIS_SUI_TEST_RPC"); new Uri(rpc).IsLoopback.Should().BeTrue();
        var root = Path.GetFullPath(Required("OASIS_HYPERDRIVE_TEST_ROOT")); Path.GetFileName(root).Should().Be("HyperDriveSui");
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var fixture = JsonDocument.Parse(File.ReadAllText(Required("OASIS_SUI_TEST_DEPLOYMENT")));
        var settings = fixture.RootElement;
        using var sui = new SuiOASIS(rpc, "localnet", settings.GetProperty("chainId").GetString()!, settings.GetProperty("packageAddress").GetString()!,
            settings.GetProperty("privateKey").GetString()!, settings.GetProperty("storageObjectId").GetString()!,storageEncryptionKeyResolver:LocalStorageKey);
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var id = Guid.Parse("c63bdd86-cf89-49ca-8e7f-8a4099b335f8");
        const string username = "sui-v2-1b84cc7a726345119c8425bbe7c0a43a";
        try
        {
            (await local.ActivateProviderAsync()).IsError.Should().BeFalse(); (await sui.ActivateProviderAsync()).IsError.Should().BeFalse();
            var present = new List<IOASISStorageProvider>();
            foreach (var provider in new IOASISStorageProvider[] { local, sui })
            {
                var read = await provider.LoadAvatarAsync(id);
                if (read.Result == null)
                {
                    read.Exception.Should().BeNull();
                    (!read.IsError || read.ErrorCode == "OASIS_RECORD_NOT_FOUND").Should().BeTrue(read.Message);
                    continue;
                }
                read.IsError.Should().BeFalse(read.Message); read.Result.Username.Should().Be(username);
                read.Result.Id.Should().Be(id); read.Result.Email.Should().EndWith("@example.invalid");
                present.Add(provider);
            }
            var detailPath = Path.GetFullPath(Path.Combine(root, "files", "AvatarDetails", id + ".json"));
            detailPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
            if (File.Exists(detailPath))
            {
                var detail = await local.LoadAvatarDetailAsync(id); detail.IsError.Should().BeFalse(detail.Message);
                detail.Result.Id.Should().Be(id); detail.Result.Username.Should().Be(username);
            }
            foreach (var provider in present) (await provider.DeleteAvatarAsync(id, softDelete: false)).IsError.Should().BeFalse();
            File.Delete(detailPath);
            (await sui.LoadAvatarAsync(id)).Result.Should().BeNull(); (await local.LoadAvatarAsync(id)).Result.Should().BeNull();
        }
        finally { await sui.DeActivateProviderAsync(); await local.DeActivateProviderAsync(); }
    }

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
            settings.GetProperty("storageObjectId").GetString()!,storageEncryptionKeyResolver:LocalStorageKey);
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var activated = await sui.ActivateProviderAsync();
        activated.IsError.Should().BeFalse(activated.Message);
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings(),
                SubscriptionConfig = new SubscriptionConfig { PlanType = "Gold" } } };
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
            settings.GetProperty("storageObjectId").GetString()!,storageEncryptionKeyResolver:LocalStorageKey);
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var holon = new Holon { Id = Guid.NewGuid(), Name = "real-v2-sui-replication" };
        holon.ParentHolonId = Guid.NewGuid();
        holon.ProviderUniqueStorageKey = new() { [ProviderType.LocalFileOASIS] = holon.Id.ToString() };
        var parent = new Holon { Id = holon.ParentHolonId, Name = "Owned Sui routing parent",
            ProviderUniqueStorageKey = new() { [ProviderType.LocalFileOASIS] = holon.ParentHolonId.ToString() } };
        var parentSaved = false;
        holon.MetaData = new Dictionary<string, object> { ["owned-routing-fixture"] = holon.Id.ToString() };
        var deletedThroughRouter = false;
        var holonSaved = false;
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "sui-v2-" + Guid.NewGuid().ToString("N"),
            Email = Guid.NewGuid().ToString("N") + "@example.invalid" };
        avatar.VerificationToken = "owned-sui-verification-" + avatar.Id;
        avatar.ProviderUniqueStorageKey = new() { [ProviderType.LocalFileOASIS] = avatar.Id.ToString() };
        avatar.ResetToken = "owned-sui-reset-" + avatar.Id;
        avatar.RefreshTokens = new() { new RefreshToken { Token = "owned-sui-refresh-" + avatar.Id } };
        var detail = new AvatarDetail { Id = avatar.Id, Username = avatar.Username, Email = avatar.Email };
        var avatarSaved = false;
        var allAssertionsPassed=false;
        var pendingPath=Path.Combine(root,"encrypted-replication-pending.json");
        using(var pending=new FileStream(pendingPath,FileMode.CreateNew,FileAccess.Write,FileShare.None))
        {
            JsonSerializer.Serialize(pending,new { holonId=holon.Id,parentId=parent.Id,avatarId=avatar.Id,
                avatar.Username,avatar.Email,chainId=settings.GetProperty("chainId").GetString(),
                storageObjectId=settings.GetProperty("storageObjectId").GetString(),
                encryptionKeyLabel="oasis-sui-local-storage-fixture-v1" });
            pending.Flush(flushToDisk:true);
        }
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        var activated = await sui.ActivateProviderAsync();
        activated.IsError.Should().BeFalse(activated.Message);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings(),
                SubscriptionConfig = new SubscriptionConfig { PlanType = "Gold" } } };
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
            async Task AssertTokenLookups(bool present)
            {
                foreach (var providerType in new[] { ProviderType.LocalFileOASIS, ProviderType.SuiOASIS })
                    foreach (var operation in new[] { "LoadAvatarByVerificationToken", "LoadAvatarByResetToken", "LoadAvatarByRefreshToken" })
                    {
                        var request = new StorageOperationRequest { Operation = operation, PreferredProvider = providerType,
                            VerificationToken = avatar.VerificationToken, ResetToken = avatar.ResetToken, RefreshToken = avatar.RefreshTokens[0].Token };
                        foreach (var read in new[] { router.RouteRequest<IAvatar>(request), await router.RouteRequestAsync<IAvatar>(request) })
                        {
                            read.IsError.Should().BeFalse(read.Message);
                            if (present) read.Result.Id.Should().Be(avatar.Id);
                            else read.Result.Should().BeNull("deleted token indexes must resolve to authoritative absence");
                        }
                    }
            }
            await AssertTokenLookups(true);
            foreach (var providerType in new[] { ProviderType.LocalFileOASIS, ProviderType.SuiOASIS })
            {
                foreach (var operation in new[] { "LoadAvatar", "LoadAvatarByUsername", "LoadAvatarByEmail", "LoadAvatarByProviderKey" })
                {
                    var request = new StorageOperationRequest { Operation = operation, AvatarId = avatar.Id,
                        Username = avatar.Username, Email = avatar.Email, ProviderKey = avatar.Id.ToString(), PreferredProvider = providerType };
                    foreach (var read in new[] { router.RouteRequest<IAvatar>(request), await router.RouteRequestAsync<IAvatar>(request) })
                    { read.IsError.Should().BeFalse(read.Message); read.Result.Id.Should().Be(avatar.Id); }
                }
                foreach (var operation in new[] { "LoadAvatarDetail", "LoadAvatarDetailByUsername", "LoadAvatarDetailByEmail" })
                {
                    var request = new StorageOperationRequest { Operation = operation, AvatarId = avatar.Id,
                        Username = avatar.Username, Email = avatar.Email, ProviderKey = avatar.Id.ToString(), PreferredProvider = providerType };
                    foreach (var read in new[] { router.RouteRequest<IAvatarDetail>(request), await router.RouteRequestAsync<IAvatarDetail>(request) })
                    { read.IsError.Should().BeFalse(read.Message); read.Result.Id.Should().Be(avatar.Id); }
                }
            }
            var saved = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.LocalFileOASIS });
            holonSaved = !saved.IsError;
            var savedParent = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "SaveHolon", Payload = parent, PreferredProvider = ProviderType.LocalFileOASIS });
            parentSaved = !savedParent.IsError;
            savedParent.IsError.Should().BeFalse(savedParent.Message);
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
                    SearchParams = new SearchParams { SearchOnlyForCurrentAvatar=false, SearchGroups = new List<ISearchGroupBase> {
                        new SearchTextGroup { SearchQuery = holon.Name, SearchHolons = true, SearchAvatars = false,HolonType=HolonType.All } } } };
                foreach (var read in new[] { router.RouteRequest<ISearchResults>(search), await router.RouteRequestAsync<ISearchResults>(search) })
                { read.IsError.Should().BeFalse(read.Message); read.Result.SearchResultHolons.Single(value => value.Id == holon.Id).Name.Should().Be(holon.Name); }
                var byKey = new StorageOperationRequest { Operation = "LoadHolonByProviderKey", ProviderKey = holon.Id.ToString(),
                    PreferredProvider = providerType, LoadChildren = false };
                foreach (var read in new[] { router.RouteRequest<IHolon>(byKey), await router.RouteRequestAsync<IHolon>(byKey) })
                { read.IsError.Should().BeFalse(read.Message); read.Result.Id.Should().Be(holon.Id); }
                foreach (var operation in new[] { "LoadHolonsForParent", "LoadHolonsForParentByProviderKey" })
                {
                    var request = new StorageOperationRequest { Operation = operation, HolonId = holon.ParentHolonId,
                        ProviderKey = holon.ParentHolonId.ToString(), PreferredProvider = providerType, LoadChildren = false };
                    foreach (var read in new[] { router.RouteRequest<IEnumerable<IHolon>>(request), await router.RouteRequestAsync<IEnumerable<IHolon>>(request) })
                    { read.IsError.Should().BeFalse(read.Message); read.Result.Single(value => value.Id == holon.Id).ParentHolonId.Should().Be(holon.ParentHolonId); }
                }
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

            holon.Name = "real-v2-sui-native-primary-updated";
            var reverseUpdate = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.SuiOASIS });
            reverseUpdate.IsError.Should().BeFalse(reverseUpdate.Message);
            reverseUpdate.IsWarning.Should().BeFalse(reverseUpdate.Message);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
            (await sui.LoadHolonAsync(holon.Id, loadChildren: false)).Result.Name.Should().Be(holon.Name);
            (await local.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);

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

            var roundRobinBefore = new Dictionary<ProviderType, int>();
            dna.OASIS.SubscriptionConfig.PlanType = "Free";
            var excludedSuiBefore = manager.PerformanceMonitor.GetMetrics(ProviderType.SuiOASIS).TotalRequests;
            foreach (var strategy in new[] { LoadBalancingStrategy.RoundRobin, LoadBalancingStrategy.WeightedRoundRobin,
                LoadBalancingStrategy.LeastConnections, LoadBalancingStrategy.Geographic, LoadBalancingStrategy.Performance })
            {
                var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = holon.Id, LoadChildren = false };
                foreach (var permitted in new[] { router.RouteRequest<IHolon>(request, strategy),
                    await router.RouteRequestAsync<IHolon>(request, strategy) })
                {
                    permitted.IsError.Should().BeFalse(permitted.Message);
                    permitted.Result.Id.Should().Be(holon.Id);
                    permitted.Result.Name.Should().Be(holon.Name);
                    manager.LastProviderSelectionDiagnostic.Candidates.Select(value => value.Provider)
                        .Should().Equal(ProviderType.LocalFileOASIS);
                }
            }
            manager.PerformanceMonitor.GetMetrics(ProviderType.SuiOASIS).TotalRequests.Should().Be(excludedSuiBefore,
                "the configured real Sui provider must not execute a Free-policy automatic read");
            manager.GetProviderAutoLoadBalanceList().Select(value => value.Value).Should()
                .BeEquivalentTo(new[] { ProviderType.LocalFileOASIS, ProviderType.SuiOASIS });
            // The next phase permits both providers and verifies actual distribution.
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            foreach (var providerType in new[] { ProviderType.LocalFileOASIS, ProviderType.SuiOASIS })
                roundRobinBefore[providerType] = (int)manager.PerformanceMonitor.GetMetrics(providerType).TotalRequests;
            for (var index = 0; index < 6; index++)
            {
                var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = holon.Id, LoadChildren = false };
                var balanced = index % 2 == 0
                    ? router.RouteRequest<IHolon>(request, LoadBalancingStrategy.RoundRobin)
                    : await router.RouteRequestAsync<IHolon>(request, LoadBalancingStrategy.RoundRobin);
                balanced.IsError.Should().BeFalse(balanced.Message);
                balanced.Result.Id.Should().Be(holon.Id);
                balanced.Result.Name.Should().Be(holon.Name);
            }
            foreach (var providerType in roundRobinBefore.Keys)
                manager.PerformanceMonitor.GetMetrics(providerType).TotalRequests.Should().Be(roundRobinBefore[providerType] + 3,
                    "six real V2 round-robin reads must use both registered providers equally across sync and async calls");

            foreach(var strategy in Enum.GetValues<LoadBalancingStrategy>())
                foreach(var synchronous in new[]{true,false})
                {
                    var providers=new[]{ProviderType.LocalFileOASIS,ProviderType.SuiOASIS};
                    var counts=providers.ToDictionary(type=>type,type=>manager.PerformanceMonitor.GetMetrics(type).TotalRequests);
                    var request=new StorageOperationRequest {Operation="LoadHolon",HolonId=holon.Id,LoadChildren=false};
                    var balanced=synchronous?router.RouteRequest<IHolon>(request,strategy):await router.RouteRequestAsync<IHolon>(request,strategy);
                    balanced.IsError.Should().BeFalse(balanced.Message);
                    balanced.Result.Id.Should().Be(holon.Id);balanced.Result.Name.Should().Be(holon.Name);
                    var diagnostic=manager.LastProviderSelectionDiagnostic;
                    diagnostic.RequestedStrategy.Should().Be(strategy);
                    diagnostic.Candidates.Select(value=>value.Provider).Should().BeEquivalentTo(providers);
                    providers.Should().Contain(diagnostic.SelectedProvider);
                    foreach(var type in providers)
                        manager.PerformanceMonitor.GetMetrics(type).TotalRequests.Should().Be(counts[type]+(type==diagnostic.SelectedProvider?1:0),
                            "the diagnosed selection must match exactly one physical provider read");
                }

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
                foreach (var operation in new[] { "LoadAvatar", "LoadAvatarByUsername", "LoadAvatarByEmail", "LoadAvatarByProviderKey" })
                {
                    var request = new StorageOperationRequest { Operation = operation, AvatarId = avatar.Id,
                        Username = avatar.Username, Email = avatar.Email, ProviderKey = avatar.Id.ToString(), PreferredProvider = providerType };
                    foreach (var read in new[] { router.RouteRequest<IAvatar>(request), await router.RouteRequestAsync<IAvatar>(request) })
                    { read.IsError.Should().BeFalse(read.Message); read.Result.Should().BeNull("replicated deletion must retire identity indexes"); }
                }
                foreach (var operation in new[] { "LoadAvatarDetail", "LoadAvatarDetailByUsername", "LoadAvatarDetailByEmail" })
                {
                    var request = new StorageOperationRequest { Operation = operation, AvatarId = avatar.Id,
                        Username = avatar.Username, Email = avatar.Email, ProviderKey = avatar.Id.ToString(), PreferredProvider = providerType };
                    foreach (var read in new[] { router.RouteRequest<IAvatarDetail>(request), await router.RouteRequestAsync<IAvatarDetail>(request) })
                    { read.IsError.Should().BeFalse(read.Message); read.Result.Should().BeNull("replicated deletion must remove detail projections"); }
                }
            }
            avatarSaved = false;
            await AssertAvatarLists(false);
            await AssertTokenLookups(false);
            allAssertionsPassed=true;
        }
        finally
        {
            try
            {
            if (!deletedThroughRouter && holonSaved)
            {
                var deleted = await sui.DeleteHolonAsync(holon.Id);
                deleted.IsError.Should().BeFalse(deleted.Message);
                (await local.DeleteHolonAsync(holon.Id)).IsError.Should().BeFalse();
            }
            if (parentSaved)
            {
                (await sui.DeleteHolonAsync(parent.Id)).IsError.Should().BeFalse();
                (await local.DeleteHolonAsync(parent.Id)).IsError.Should().BeFalse();
            }
            if (avatarSaved)
            {
                (await sui.DeleteAvatarAsync(avatar.Id, softDelete: false)).IsError.Should().BeFalse();
                (await local.DeleteAvatarAsync(avatar.Id, softDelete: false)).IsError.Should().BeFalse();
                var ownedDetailFile = Path.GetFullPath(Path.Combine(root, "files", "AvatarDetails", avatar.Id + ".json"));
                ownedDetailFile.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
                File.Delete(ownedDetailFile);
            }
            }
            finally
            {
                await sui.DeActivateProviderAsync();
                await local.DeActivateProviderAsync();
            }
            if(allAssertionsPassed)File.Delete(pendingPath); // Failed/interrupted runs retain their exact ownership fence.
        }
    }
}
