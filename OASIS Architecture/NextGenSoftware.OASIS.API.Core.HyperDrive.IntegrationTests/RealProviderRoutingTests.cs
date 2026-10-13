using System.Diagnostics;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class RealProviderRoutingTests : IDisposable
{
    private readonly string _root;
    private readonly FileStream _lease;
    public RealProviderRoutingTests()
    {
        var checkout = new DirectoryInfo(AppContext.BaseDirectory);
        while (checkout != null && !File.Exists(Path.Combine(checkout.FullName, "Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1"))) checkout = checkout.Parent;
        if (checkout == null) throw new InvalidOperationException("Existing task checkout required.");
        var environment = Path.Combine(checkout.FullName, "TestResults/HyperDriveSqliteCurrent");
        Directory.CreateDirectory(environment);
        _lease = new FileStream(Path.Combine(environment, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        _root = Path.Combine(environment, "owned-data");
        if (Directory.Exists(_root) || File.Exists(_root))
        {
            _lease.Dispose();
            throw new InvalidOperationException("Existing routing fixture requires inspection; refusing overwrite or cleanup.");
        }
    }
    private string SqlitePath => Path.Combine(_root, "oasis.db");
    private string FilesPath => Path.Combine(_root, "files");

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AvatarDetailReplicationPreservesKarmaAndXpAcrossUpdates(bool synchronous)
    {
        Directory.CreateDirectory(_root);
        var local = new LocalFileOASIS(storageDirectory: FilesPath);
        using var sqlite = new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CreateManager(autoReplication:true);
        manager.RegisterProvider(local);
        manager.RegisterProvider(sqlite);
        manager.SetAndReplaceAutoReplicationListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
        var detail = new AvatarDetail { Id = Guid.NewGuid(), Username = "owned-detail", Email = "detail@test.local", Karma = 345, XP = 123 };
        detail.Stats.HP.Current = 37;
        detail.Stats.Mana.Current = 42;
        detail.Stats.Stamina.Current = 53;
        var item = new InventoryItem { Id = Guid.NewGuid(), Name = "owned-seed", Quantity = 3 };
        item.Properties = new Dictionary<string,object> { ["species"] = "oak" };
        item.MetaData["quest-source"] = "owned-tree";
        detail.Inventory.Add(item);
        var request = new StorageOperationRequest { Operation = "SaveAvatarDetail", Payload = detail, PreferredProvider = ProviderType.LocalFileOASIS };
        var router = new OASISHyperDrive(manager);
        foreach (var updating in new[] { false, true })
        {
            if (updating) { detail.Karma = 456; detail.XP = 234; detail.Stats.HP.Current = 38; detail.Stats.Mana.Current = 43; detail.Stats.Stamina.Current = 54; item.Quantity = 4; }
            var result = synchronous ? router.RouteRequest<IAvatarDetail>(request) : await router.RouteRequestAsync<IAvatarDetail>(request);
            result.IsError.Should().BeFalse(result.Message);
            result.Result.Should().NotBeNull();
            result.Result.Id.Should().Be(detail.Id);
            result.IsWarning.Should().BeFalse(string.Join("; ", manager.LastReplicationDiagnostic.Attempts.Select(attempt => attempt.Message)));
            foreach (var loaded in new[] { await local.LoadAvatarDetailAsync(detail.Id), await sqlite.LoadAvatarDetailAsync(detail.Id) })
            {
                loaded.IsError.Should().BeFalse(loaded.Message);
                loaded.Result.Should().NotBeNull();
                loaded.Result.Karma.Should().Be(detail.Karma);
                loaded.Result.XP.Should().Be(detail.XP);
                loaded.Result.Stats.HP.Current.Should().Be(detail.Stats.HP.Current);
                loaded.Result.Stats.Mana.Current.Should().Be(detail.Stats.Mana.Current);
                loaded.Result.Stats.Stamina.Current.Should().Be(detail.Stats.Stamina.Current);
                loaded.Result.Inventory.Single().Id.Should().Be(item.Id);
                loaded.Result.Inventory.Single().Quantity.Should().Be(item.Quantity);
                loaded.Result.Inventory.Single().Properties["species"].ToString().Should().Be("oak");
                loaded.Result.Inventory.Single().MetaData["quest-source"].ToString().Should().Be("owned-tree");
            }
        }
        using var freshSqlite = new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await freshSqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var persisted = await freshSqlite.LoadAvatarDetailAsync(detail.Id);
        persisted.Result.Stats.HP.Current.Should().Be(38);
        persisted.Result.Stats.Mana.Current.Should().Be(43);
        persisted.Result.Stats.Stamina.Current.Should().Be(54);
        persisted.Result.Inventory.Single().Id.Should().Be(item.Id);
        persisted.Result.Inventory.Single().Quantity.Should().Be(4);
        persisted.Result.Inventory.Single().Properties["species"].ToString().Should().Be("oak");
        persisted.Result.Inventory.Single().MetaData["quest-source"].ToString().Should().Be("owned-tree");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AvatarReplicationPreservesIdentityInBothRealStores(bool synchronous)
    {
        Directory.CreateDirectory(_root);
        var local = new LocalFileOASIS(storageDirectory: FilesPath);
        using var sqlite = new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CreateManager(autoReplication:true);
        manager.RegisterProvider(local);
        manager.RegisterProvider(sqlite);
        manager.SetAndReplaceAutoReplicationListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "owned-v2-avatar", Email = "owned-v2@test.local", FirstName = "Owned", LastName = "Fixture", AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
        var request = new StorageOperationRequest { Operation = "SaveAvatar", Payload = avatar, PreferredProvider = ProviderType.LocalFileOASIS };
        var router = new OASISHyperDrive(manager);
        var result = synchronous ? router.RouteRequest<IAvatar>(request) : await router.RouteRequestAsync<IAvatar>(request);
        result.IsError.Should().BeFalse(result.Message);
        manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
        manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
        foreach (var loaded in new[] { await local.LoadAvatarAsync(avatar.Id), await sqlite.LoadAvatarAsync(avatar.Id) })
        {
            loaded.IsError.Should().BeFalse(loaded.Message);
            loaded.Result.Id.Should().Be(avatar.Id);
            loaded.Result.Username.Should().Be(avatar.Username);
            loaded.Result.Email.Should().Be(avatar.Email);
        }
        avatar.FirstName = "Updated";
        avatar.Email = "updated-owned-v2@test.local";
        var updated = synchronous ? router.RouteRequest<IAvatar>(request) : await router.RouteRequestAsync<IAvatar>(request);
        updated.IsError.Should().BeFalse(updated.Message);
        manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
        foreach (var loaded in new[] { await local.LoadAvatarByUsernameAsync(avatar.Username), await sqlite.LoadAvatarByUsernameAsync(avatar.Username),
            await local.LoadAvatarByEmailAsync(avatar.Email), await sqlite.LoadAvatarByEmailAsync(avatar.Email) })
        {
            loaded.IsError.Should().BeFalse(loaded.Message);
            loaded.Result.Should().NotBeNull();
            loaded.Result.Id.Should().Be(avatar.Id);
            loaded.Result.Email.Should().Be(avatar.Email);
            loaded.Result.FirstName.Should().Be("Updated");
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task ReadPoliciesPreserveActiveProviderAndAuthoritativeAbsence(bool synchronous, bool primaryHasRecord)
    {
        Directory.CreateDirectory(_root);
        var local = new LocalFileOASIS(storageDirectory: FilesPath);
        using var sqlite = new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id = Guid.NewGuid();
        (await sqlite.SaveHolonAsync(new Holon { Id = id, Name = "stale-secondary" }, saveChildren:false)).IsError.Should().BeFalse();
        if (primaryHasRecord)
            (await local.SaveHolonAsync(new Holon { Id = id, Name = "authoritative-primary" }, saveChildren:false)).IsError.Should().BeFalse();
        var manager = CreateManager(autoFailover:true);
        manager.RegisterProvider(local);
        manager.RegisterProvider(sqlite);
        var providers = new[] { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
        manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoLoadBalanceListForProviders(providers).IsError.Should().BeFalse();
        manager.IsAutoLoadBalanceEnabled = false;
        manager.SetAndActivateCurrentStorageProvider(local).IsError.Should().BeFalse();
        manager.PerformanceMonitor.RecordRequest(ProviderType.LocalFileOASIS, true, 100);
        manager.PerformanceMonitor.RecordRequest(ProviderType.SQLLiteDBOASIS, true, 1);
        var router = new OASISHyperDrive(manager);
        var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = id, LoadChildren = false };
        var result = synchronous ? router.RouteRequest<IHolon>(request, LoadBalancingStrategy.Performance) : await router.RouteRequestAsync<IHolon>(request, LoadBalancingStrategy.Performance);
        result.IsError.Should().BeFalse(result.Message);
        if (primaryHasRecord) result.Result.Name.Should().Be("authoritative-primary");
        else result.Result.Should().BeNull("absence is not a provider failure and must not resurrect a stale replica");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DisabledPoliciesDoNotReplicateOrFailOverDespiteConfiguredLists(bool synchronous)
    {
        Directory.CreateDirectory(_root);
        var local = new LocalFileOASIS(storageDirectory: FilesPath);
        using var sqlite = new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CreateManager();
        manager.RegisterProvider(local);
        manager.RegisterProvider(sqlite);
        var providers = new[] { new EnumValue<ProviderType>(ProviderType.LocalFileOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
        manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError.Should().BeFalse();
        manager.IsAutoReplicationEnabled = false;
        manager.IsAutoFailOverEnabled = false;
        manager.SetAndActivateCurrentStorageProvider(local).IsError.Should().BeFalse();
        var router = new OASISHyperDrive(manager);
        var initial = new Holon { Id = Guid.NewGuid(), Name = "only-primary" };
        var request = new StorageOperationRequest { Operation = "SaveHolon", Payload = initial, SaveChildren = false };
        var saved = synchronous ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
        saved.IsError.Should().BeFalse(saved.Message);
        (await local.LoadHolonAsync(initial.Id)).Result.Id.Should().Be(initial.Id);
        (await sqlite.LoadHolonAsync(initial.Id, loadChildren:false)).Result.Should().BeNull();
        Directory.Delete(FilesPath, recursive:true);
        File.WriteAllText(FilesPath, "owned primary failure with failover disabled");
        var failed = new Holon { Id = Guid.NewGuid(), Name = "must-not-failover" };
        request.Payload = failed;
        var result = synchronous ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
        result.IsError.Should().BeTrue();
        (await sqlite.LoadHolonAsync(failed.Id, loadChildren:false)).Result.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReplicaIoFailurePreservesPrimaryAndReportsPartialReplication(bool synchronous)
    {
        Directory.CreateDirectory(_root);
        var local = new LocalFileOASIS(storageDirectory: FilesPath);
        using var sqlite = new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CreateManager(autoReplication:true);
        manager.RegisterProvider(local);
        manager.RegisterProvider(sqlite);
        manager.SetAndReplaceAutoReplicationListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS), new EnumValue<ProviderType>(ProviderType.LocalFileOASIS) }).IsError.Should().BeFalse();
        Directory.Delete(FilesPath, recursive:true);
        File.WriteAllText(FilesPath, "owned replica I/O failure");
        var holon = new Holon { Id = Guid.NewGuid(), Name = "primary-survives" };
        var request = new StorageOperationRequest { Operation = "SaveHolon", Payload = holon, SaveChildren = false, PreferredProvider = ProviderType.SQLLiteDBOASIS };
        var router = new OASISHyperDrive(manager);
        var result = synchronous ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
        result.IsError.Should().BeFalse(result.Message);
        result.IsWarning.Should().BeTrue();
        (await sqlite.LoadHolonAsync(holon.Id, loadChildren:false)).Result.Name.Should().Be(holon.Name);
        manager.LastReplicationDiagnostic.SucceededCount.Should().Be(0);
        manager.LastReplicationDiagnostic.FailedCount.Should().Be(1);
        manager.LastReplicationDiagnostic.Attempts.Single().Provider.Should().Be(ProviderType.LocalFileOASIS);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LocalFileDeletionPreservesUnreadableRecordAndReportsSuccessfulDeletion(bool synchronous)
    {
        Directory.CreateDirectory(_root);
        var local = new LocalFileOASIS(storageDirectory: FilesPath);
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id = Guid.NewGuid();
        var path = Path.Combine(FilesPath, "Holons", $"{id}.json");
        const string corrupt = "{invalid-owned-fixture";
        await File.WriteAllTextAsync(path, corrupt);
        var rejected = synchronous ? local.DeleteHolon(id) : await local.DeleteHolonAsync(id);
        rejected.IsError.Should().BeTrue();
        rejected.IsDeleted.Should().BeFalse();
        rejected.DeletedCount.Should().Be(0);
        (await File.ReadAllTextAsync(path)).Should().Be(corrupt);
        var valid = new Holon { Id = Guid.NewGuid(), Name = "valid-delete" };
        (await local.SaveHolonAsync(valid, saveChildren:false)).IsError.Should().BeFalse();
        var deleted = synchronous ? local.DeleteHolon(valid.Id) : await local.DeleteHolonAsync(valid.Id);
        deleted.IsError.Should().BeFalse(deleted.Message);
        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletedCount.Should().Be(1);
        deleted.IsSaved.Should().BeFalse();
        deleted.Result.Id.Should().Be(valid.Id);
        (await local.LoadHolonAsync(valid.Id)).Result.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AutoReplication_WritesMutationToBothRealStores(bool synchronous)
    {
        Directory.CreateDirectory(_root);
        var local = new LocalFileOASIS(storageDirectory: FilesPath);
        using var sqlite = new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CreateManager(autoReplication: true);
        manager.RegisterProvider(local);
        manager.RegisterProvider(sqlite);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),
            new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)
        }).IsError.Should().BeFalse();
        var creator = Guid.NewGuid();
        var holon = new Holon { Id = Guid.NewGuid(), Name = $"replicated-{Guid.NewGuid():N}", CreatedByAvatarId = creator,
            CreatedDate = new DateTime(2025,1,2,3,4,5,DateTimeKind.Utc), MetaData = new Dictionary<string,object> { ["Email"] = "private-query@test.local" } };

        var router = new OASISHyperDrive(manager);
        var save = new StorageOperationRequest { Operation = "SaveHolon", Payload = holon, SaveChildren = false, PreferredProvider = ProviderType.LocalFileOASIS };
        var result = synchronous ? router.RouteRequest<IHolon>(save) : await router.RouteRequestAsync<IHolon>(save);

        result.IsError.Should().BeFalse(result.Message);
        (await local.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
        (await sqlite.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
        manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
        manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
        holon.Name = "replicated-update";
        holon.ModifiedByAvatarId = Guid.NewGuid();
        holon.Version = 3;
        var updated = synchronous ? router.RouteRequest<IHolon>(save) : await router.RouteRequestAsync<IHolon>(save);
        updated.IsError.Should().BeFalse(updated.Message);
        foreach (var stored in new[] { (await local.LoadHolonAsync(holon.Id)).Result, (await sqlite.LoadHolonAsync(holon.Id)).Result })
        {
            stored.Name.Should().Be("replicated-update");
            stored.Version.Should().Be(3);
            stored.CreatedByAvatarId.Should().Be(creator);
            stored.ModifiedByAvatarId.Should().Be(holon.ModifiedByAvatarId);
            stored.MetaData["Email"].ToString().Should().Be("private-query@test.local");
        }
        manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
        var delete = new StorageOperationRequest { Operation = "DeleteHolon", HolonId = holon.Id, SoftDelete = false, PreferredProvider = ProviderType.LocalFileOASIS };
        var deleted = synchronous ? router.RouteRequest<IHolon>(delete) : await router.RouteRequestAsync<IHolon>(delete);
        deleted.IsError.Should().BeFalse(deleted.Message);
        deleted.IsDeleted.Should().BeTrue();
        (await local.LoadHolonAsync(holon.Id)).Result.Should().BeNull();
        (await sqlite.LoadHolonAsync(holon.Id)).Result.Should().BeNull();
        manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
        manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AutoFailover_WritesToRealSecondaryAfterPrimaryStorageFails(bool synchronous)
    {
        Directory.CreateDirectory(_root);
        var local = new LocalFileOASIS(storageDirectory: FilesPath);
        using var sqlite = new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager = CreateManager(autoFailover: true);
        manager.RegisterProvider(local);
        manager.RegisterProvider(sqlite);
        manager.SetAndActivateCurrentStorageProvider(local).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoFailOverListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),
            new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)
        }).IsError.Should().BeFalse();
        Directory.Delete(FilesPath, recursive: true);
        File.WriteAllText(FilesPath, "force LocalFile I/O failure");
        var holon = new Holon { Id = Guid.NewGuid(), Name = $"failover-{Guid.NewGuid():N}" };

        var router = new OASISHyperDrive(manager);
        var request = new StorageOperationRequest { Operation = "SaveHolon", Payload = holon, SaveChildren = false };
        var result = synchronous ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);

        result.IsError.Should().BeFalse(result.Message);
        (await sqlite.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
        manager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.SQLLiteDBOASIS);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AutoLoadBalancing_UsesLatencyMeasuredFromRealProviderCalls(bool synchronous)
    {
        Directory.CreateDirectory(_root);
        var local = new LocalFileOASIS(storageDirectory: FilesPath);
        using var sqlite = new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var holon = new Holon { Id = Guid.NewGuid(), Name = "latency-selection" };
        (await local.SaveHolonAsync(holon)).IsError.Should().BeFalse();
        (await sqlite.SaveHolonAsync(holon)).IsError.Should().BeFalse();
        var manager = CreateManager(autoLoadBalance: true);
        manager.RegisterProvider(local);
        manager.RegisterProvider(sqlite);
        manager.SetAndReplaceAutoLoadBalanceListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),
            new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)
        }).IsError.Should().BeFalse();

        double localMs = await MeasureAsync(() => local.LoadHolonAsync(holon.Id));
        double sqliteMs = await MeasureAsync(() => sqlite.LoadHolonAsync(holon.Id));
        manager.PerformanceMonitor.RecordRequest(ProviderType.LocalFileOASIS, true, localMs);
        manager.PerformanceMonitor.RecordRequest(ProviderType.SQLLiteDBOASIS, true, sqliteMs);
        ProviderType expected = localMs <= sqliteMs ? ProviderType.LocalFileOASIS : ProviderType.SQLLiteDBOASIS;

        var router = new OASISHyperDrive(manager);
        var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = holon.Id, LoadChildren = false };
        var result = synchronous ? router.RouteRequest<IHolon>(request, LoadBalancingStrategy.Performance) : await router.RouteRequestAsync<IHolon>(request, LoadBalancingStrategy.Performance);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Id.Should().Be(holon.Id);
        manager.PerformanceMonitor.GetMetrics(expected).TotalRequests.Should().BeGreaterThan(1);
    }

    private static ProviderManager CreateManager(bool autoFailover = false, bool autoReplication = false, bool autoLoadBalance = false)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        return new ProviderManager(null, dna)
        {
            IsAutoFailOverEnabled = autoFailover,
            IsAutoReplicationEnabled = autoReplication,
            IsAutoLoadBalanceEnabled = autoLoadBalance
        };
    }

    private static async Task<double> MeasureAsync(Func<Task<OASISResult<IHolon>>> action)
    {
        var timer = Stopwatch.StartNew();
        var result = await action();
        timer.Stop();
        result.IsError.Should().BeFalse(result.Message);
        return Math.Max(timer.Elapsed.TotalMilliseconds, 0.001);
    }

    public void Dispose()
    {
        try
        {
            // Exact fixed task-owned fixture, checked absent before acquiring ownership.
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }
        finally { _lease.Dispose(); }
    }
}
