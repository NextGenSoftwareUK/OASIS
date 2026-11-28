using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class CanonicalProviderSwitchNativeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanonicalSwitchAcknowledgesOnlyActivatedNativeSelection(bool synchronous)
    {
        var directory = Directory.CreateTempSubdirectory("oasis-canonical-switch-");
        var sqlite = new SQLLiteDBOASIS($"Data Source={Path.Combine(directory.FullName, "storage.db")};Pooling=False");
        var filePath = Path.Combine(directory.FullName, "files");
        var local = new LocalFileOASIS(storageDirectory: filePath);
        var instanceField = typeof(ProviderManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
        var previousManager = instanceField.GetValue(null);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
        {
            HyperDriveMode = HyperDriveModes.V2,
            StorageProviders = new() { ProviderMethodCallTimeOutSeconds = 3 },
            Security = new() { SecretKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N") },
            SubscriptionConfig = new() { PlanType = "Gold" }
        } };
        var manager = new ProviderManager(null, dna);
        // The public facades intentionally address the process manager. This isolated test host
        // installs a real-provider manager and restores the prior singleton in finally.
        instanceField.SetValue(null, manager);
        try
        {
            var registry = ProviderRegistry.Instance;
            var control = ProviderSwitcher.Instance;
            var selector = ProviderSelector.Instance;
            var sqliteType = sqlite.ProviderType.Value;
            var fileType = local.ProviderType.Value;
            Assert.True(manager.RegisterProvider(sqlite));
            registry.RegisterStorageProvider(fileType, local);
            Assert.Same(local, manager.GetStorageProvider(fileType));
            Assert.Same(sqlite, registry.GetStorageProvider(sqliteType));
            Assert.Throws<ArgumentException>(() => registry.RegisterStorageProvider(sqliteType, local));
            registry.RegisterStorageProvider(fileType, local);
            manager.IsAutoFailOverEnabled = false;
            manager.IsAutoReplicationEnabled = false;
            manager.IsAutoLoadBalanceEnabled = false;
            var initial = synchronous ? manager.SetAndActivateCurrentStorageProvider(sqliteType, true)
                : await manager.SetAndActivateCurrentStorageProviderAsync(sqliteType, true);
            Assert.False(initial.IsError, initial.Message);
            var id = Guid.NewGuid();
            var saved = await sqlite.SaveHolonAsync(new Holon { Id = id, Name = "native-sqlite" }, saveChildren: false);
            Assert.False(saved.IsError, saved.Message);
            await File.WriteAllTextAsync(filePath, "owned activation obstruction");
            var before = control.GetSwitchStatus().Result.LastSwitchTime;
            var rejected = await control.SwitchStorageProviderAsync(fileType);
            Assert.True(rejected.IsError);
            Assert.Same(sqlite, manager.CurrentStorageProvider);
            Assert.Same(sqlite, manager.DefaultGlobalStorageProvider);
            Assert.True(sqlite.IsProviderActivated);
            Assert.False(local.IsProviderActivated);
            Assert.Equal(before, control.GetSwitchStatus().Result.LastSwitchTime);
            Assert.Equal(sqliteType, registry.CurrentStorageProviderType.Value);
            File.Delete(filePath);
            var switched = await control.SwitchStorageProviderAsync(fileType);
            Assert.False(switched.IsError, switched.Message);
            Assert.True(switched.Result);
            Assert.True(local.IsProviderActivated);
            Assert.True(sqlite.IsProviderActivated);
            Assert.Same(local, manager.CurrentStorageProvider);
            Assert.Same(local, manager.DefaultGlobalStorageProvider);
            Assert.Equal(fileType, control.GetSwitchStatus().Result.CurrentStorageProvider);
            var acknowledgedAt = control.GetSwitchStatus().Result.LastSwitchTime;
            Assert.NotEqual(default, acknowledgedAt);
            var noop = await control.SwitchStorageProviderAsync(fileType);
            Assert.False(noop.IsError, noop.Message);
            Assert.Equal(acknowledgedAt, control.GetSwitchStatus().Result.LastSwitchTime);
            var wrongRole = await control.SwitchMapProviderAsync(fileType);
            Assert.True(wrongRole.IsError);
            Assert.Equal(acknowledgedAt, control.GetSwitchStatus().Result.LastSwitchTime);
            var fileSaved = await local.SaveHolonAsync(new Holon { Id = id, Name = "native-files" }, saveChildren: false);
            Assert.False(fileSaved.IsError, fileSaved.Message);
            var router = new OASISHyperDrive(manager);
            var read = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "LoadHolon", HolonId = id, LoadChildren = false });
            Assert.False(read.IsError, read.Message);
            Assert.Equal("native-files", read.Result.Name);
            var back = await control.SwitchStorageProviderAsync(sqliteType);
            Assert.False(back.IsError, back.Message);
            read = await router.RouteRequestAsync<IHolon>(new StorageOperationRequest
                { Operation = "LoadHolon", HolonId = id, LoadChildren = false });
            Assert.False(read.IsError, read.Message);
            Assert.Equal("native-sqlite", read.Result.Name);
            var deactivated = synchronous ? sqlite.DeActivateProvider() : await sqlite.DeActivateProviderAsync();
            Assert.False(deactivated.IsError, deactivated.Message);
            Assert.False(sqlite.IsProviderActivated);
            var reactivated = synchronous ? manager.SetAndActivateCurrentStorageProvider(sqliteType, true)
                : await manager.SetAndActivateCurrentStorageProviderAsync(sqliteType, true);
            Assert.False(reactivated.IsError, reactivated.Message);
            Assert.True(sqlite.IsProviderActivated);
            Assert.True(local.IsProviderActivated);
            var types = new[] { new EnumValue<ProviderType>(sqliteType), new EnumValue<ProviderType>(fileType) };
            Assert.False(manager.SetAndReplaceAutoLoadBalanceListForProviders(types).IsError);
            Assert.False(manager.SetAndReplaceAutoFailOverListForProviders(types).IsError);
            Assert.False(manager.SetAndReplaceAutoReplicationListForProviders(types).IsError);
            Assert.Equal(2, registry.ProviderAutoLoadBalanceList.Count);
            var view = registry.ProviderAutoLoadBalanceList;
            view[0].Value = ProviderType.None;
            Assert.Equal(sqliteType, registry.ProviderAutoLoadBalanceList[0].Value);
            Assert.Equal(sqliteType, selector.SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy.RoundRobin).Value);
            manager.IsAutoLoadBalanceEnabled = true;
            var first = selector.SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy.RoundRobin).Value;
            var second = manager.SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy.RoundRobin).Value;
            Assert.NotEqual(first, second);
            Assert.Equal(first, selector.SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy.RoundRobin).Value);
            Assert.Equal(fileType, selector.SelectFailoverProvider(new(sqliteType)).Value);
            Assert.Equal(fileType, selector.SelectReplicationProvider(new(sqliteType)).Value);
            Assert.False(manager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(sqliteType) }).IsError);
            Assert.Throws<InvalidOperationException>(() => selector.SelectFailoverProvider(new(sqliteType)));
        }
        finally
        {
            try { await local.DeActivateProviderAsync(); await sqlite.DeActivateProviderAsync(); }
            finally
            {
                sqlite.Dispose();
                instanceField.SetValue(null, previousManager);
                var full = Path.GetFullPath(directory.FullName);
                var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                Assert.True(full.StartsWith(temp, StringComparison.OrdinalIgnoreCase));
                Assert.StartsWith("oasis-canonical-switch-", directory.Name);
                directory.Delete(recursive: true);
                Assert.False(Directory.Exists(full));
            }
        }
    }
}
