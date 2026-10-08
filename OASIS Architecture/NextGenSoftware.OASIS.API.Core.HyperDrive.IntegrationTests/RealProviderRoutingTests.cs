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
    private readonly string _root = Path.Combine(Path.GetTempPath(), "oasis-hyperdrive-real", Guid.NewGuid().ToString("N"));
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
        var holon = new Holon { Id = Guid.NewGuid(), Name = $"replicated-{Guid.NewGuid():N}" };

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(
            new StorageOperationRequest { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.LocalFileOASIS });

        result.IsError.Should().BeFalse(result.Message);
        (await local.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
        (await sqlite.LoadHolonAsync(holon.Id)).Result.Name.Should().Be(holon.Name);
        manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
        manager.LastReplicationDiagnostic.FailedCount.Should().Be(0);
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
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
