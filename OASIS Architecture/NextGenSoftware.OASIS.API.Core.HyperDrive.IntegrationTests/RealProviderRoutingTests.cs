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
