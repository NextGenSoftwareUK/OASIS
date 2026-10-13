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

    [Fact]
    public async Task AutoReplication_WritesMutationToBothRealStores()
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

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(
            new StorageOperationRequest { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.LocalFileOASIS });

        result.IsError.Should().BeFalse(result.Message);
        (await local.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
        (await sqlite.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
        manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
        manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
        holon.Name = "replicated-update";
        holon.ModifiedByAvatarId = Guid.NewGuid();
        holon.Version = 3;
        var updated = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(
            new StorageOperationRequest { Operation = "SaveHolon", Payload = holon, SaveChildren = false, PreferredProvider = ProviderType.LocalFileOASIS });
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
    }

    [Fact]
    public async Task AutoFailover_WritesToRealSecondaryAfterPrimaryStorageFails()
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

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(
            new StorageOperationRequest { Operation = "SaveHolon", Payload = holon });

        result.IsError.Should().BeFalse(result.Message);
        (await sqlite.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
        manager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.SQLLiteDBOASIS);
    }

    [Fact]
    public async Task AutoLoadBalancing_UsesLatencyMeasuredFromRealProviderCalls()
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

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(
            new StorageOperationRequest { Operation = "LoadHolon", HolonId = holon.Id }, LoadBalancingStrategy.Performance);

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
