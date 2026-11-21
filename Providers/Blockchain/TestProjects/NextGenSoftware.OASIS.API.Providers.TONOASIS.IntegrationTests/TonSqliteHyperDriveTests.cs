using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.TestSupport;
using NextGenSoftware.Utilities;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class TonSqliteHyperDriveTests
{
    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task RoutesAcrossNativeTonAndSqlite(bool sync)
    {
        Assert.AreEqual("true", Required("GITHUB_ACTIONS"));
        var root = Path.GetFullPath(Required("OASIS_TON_ROUTING_DIRECTORY"));
        Assert.AreEqual(Path.GetFullPath(Required("RUNNER_TEMP")), Path.GetDirectoryName(root));
        StringAssert.StartsWith(Path.GetFileName(root), "oasis-ton-routing.");
        var fixture = Path.Combine(root, sync ? "sync" : "async");
        Assert.IsFalse(Directory.Exists(fixture));
        var source = Path.Combine(fixture, "source");
        Directory.CreateDirectory(source);

        var options = new TonSandboxStorageOptions(Required("OASIS_TON_NODE"), Required("OASIS_TON_WORKER"),
            Path.Combine(root, "compiled", "TonHolonStore_TonHolonStore.cjs"), source, Guid.NewGuid(),
            Required("OASIS_TON_CODE_HASH"), ReadOnly: false);
        var native = new TonSandboxStorageBackend(options);
        await native.CreateSourceAsync();
        var key = RandomNumberGenerator.GetBytes(32);
        using var ton = new TONOASIS(options, () => (byte[])key.Clone());
        var missingSource = Path.Combine(fixture, "unavailable-source");
        Directory.CreateDirectory(missingSource);
        using var unavailableTon = new TONOASIS(options with
        {
            SourceDirectory = missingSource,
            SourceId = Guid.NewGuid(),
            ReadOnly = true
        }, () => (byte[])key.Clone());
        using var sqlite = new SqliteProvider($"Data Source={Path.Combine(fixture, "routing.db")};Pooling=False");
        var missing = Path.Combine(fixture, "missing.db");
        using var unavailableSqlite = new SqliteProvider($"Data Source={missing};Mode=ReadWrite;Pooling=False;Default Timeout=1");

        try
        {
            var tonActivation = await ton.ActivateProviderAsync();
            Assert.IsTrue(!tonActivation.IsError && tonActivation.Result, tonActivation.Message);
            var sqliteActivation = await sqlite.ActivateProviderAsync();
            Assert.IsTrue(!sqliteActivation.IsError && sqliteActivation.Result, sqliteActivation.Message);
            Assert.AreEqual(0, (await native.ListKeysAsync(string.Empty)).Count);
            await NativePriorityRouting.VerifyAsync(ton, sqlite, unavailableTon, unavailableSqlite, sync);
            Assert.IsFalse(File.Exists(missing));
            var records = await ton.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
            Assert.IsFalse(records.IsError, records.Message);
            var fixtures = records.Result.ToArray();
            CollectionAssert.AreEquivalent(
                new[] { $"priority routing {ton.ProviderType.Value}", $"priority routing {sqlite.ProviderType.Value}" },
                fixtures.Select(record => record.Name).ToArray());
            foreach (var record in fixtures)
            {
                var removed = await ton.DeleteHolonAsync(record.Id);
                Assert.IsTrue(!removed.IsError && removed.IsDeleted && removed.Result?.Id == record.Id, removed.Message);
            }
            Assert.AreEqual(0, (await native.ListKeysAsync(string.Empty)).Count);

            var staleReplica = fixtures[0];
            Assert.AreEqual(staleReplica.Id, (await sqlite.LoadHolonAsync(staleReplica.Id, loadChildren: false)).Result?.Id);
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var manager = new ProviderManager(null, dna) { IsAutoFailOverEnabled = true };
            manager.RegisterProvider(ton);
            manager.RegisterProvider(sqlite);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[]
            {
                new EnumValue<ProviderType>(ton.ProviderType.Value),
                new EnumValue<ProviderType>(sqlite.ProviderType.Value)
            }).IsError);
            var router = new OASISHyperDrive(manager);
            var request = new StorageOperationRequest
            {
                Operation = "LoadHolon", HolonId = staleReplica.Id,
                PreferredProvider = ton.ProviderType.Value, LoadChildren = false
            };
            var absent = sync ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, absent.ErrorCode, absent.Message);
            Assert.IsNull(absent.Result);
            Assert.AreEqual(0L, manager.PerformanceMonitor.GetMetrics(sqlite.ProviderType.Value)?.TotalRequests ?? 0);
        }
        finally
        {
            await ton.DeActivateProviderAsync();
            await unavailableTon.DeActivateProviderAsync();
            await sqlite.DeActivateProviderAsync();
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static string Required(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        Assert.IsFalse(string.IsNullOrWhiteSpace(value), $"Missing fixture setting {name}.");
        return value!;
    }
}
