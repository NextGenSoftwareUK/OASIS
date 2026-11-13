using System.Net;
using System.Net.Sockets;
using System.Reflection;
using MongoDB.Driver;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.TestSupport;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class PolkadotSqliteHyperDriveTests
{
    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task RoutesAcrossNativePolkadotAndSqlite(bool sync)
    {
        Assert.AreEqual("true", Required("GITHUB_ACTIONS"));
        Assert.AreEqual("1", Required("OASIS_POLKADOT_FRESH_STORAGE"));
        const string endpoint = "http://127.0.0.1:19944";
        const string genesis = "0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208";
        const string signer = "0x0202020202020202020202020202020202020202020202020202020202020202";
        var root = Path.GetFullPath(Required("OASIS_POLKADOT_CI_FIXTURE"));
        Assert.AreEqual(Path.GetFullPath(Required("RUNNER_TEMP")), Path.GetDirectoryName(root));
        StringAssert.StartsWith(Path.GetFileName(root), "oasis-polkadot-chain.");
        using var setup = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(Required("OASIS_POLKADOT_RECORD_EVIDENCE"), "fresh-record-store.json")));
        var recordStore = setup.RootElement;
        Assert.AreEqual(genesis, recordStore.GetProperty("genesisHash").GetString());
        Assert.IsTrue(recordStore.GetProperty("deploymentVerified").GetBoolean());
        var configuration = new PolkadotStorageConfiguration(
            recordStore.GetProperty("contractAccountId").GetString()!,
            recordStore.GetProperty("contractCodeHash").GetString()!,
            recordStore.GetProperty("ownerAccountId").GetString()!,
            () => Convert.FromBase64String(Required("OASIS_POLKADOT_STORAGE_ENCRYPTION_KEY")));
        var sqlitePath = Path.Combine(root, sync ? "routing-sync.db" : "routing-async.db");
        Assert.IsFalse(File.Exists(sqlitePath));
        using var sqlite = new SqliteProvider($"Data Source={sqlitePath};Pooling=False");
        var missingPath = Path.Combine(root, "missing.db");
        using var unavailableSqlite = new SqliteProvider($"Data Source={missingPath};Mode=ReadWrite;Pooling=False;Default Timeout=1");
        using var polkadot = new PolkadotOASIS(endpoint, genesis, signer, null, configuration);
        using var closed = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        closed.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)closed.LocalEndPoint!).Port;
        using var unavailablePolkadot = new PolkadotOASIS($"http://127.0.0.1:{port}", genesis, signer, null, configuration);

        const string mongoEndpoint = "mongodb://127.0.0.1:27029/?serverSelectionTimeoutMS=5000";
        Assert.AreEqual(mongoEndpoint, Required("OASIS_POLKADOT_ROUTING_MONGO"));
        var database = $"oasis_polkadot_routing_{Guid.NewGuid():N}";
        var outbox = new MongoUsageOutboxStore(mongoEndpoint, database);
        var operation = new UsageOutboxOperation
        {
            Id = Guid.NewGuid().ToString("D"),
            UserId = Guid.NewGuid().ToString("D"),
            Service = "WEB4",
            ExecutionOwner = "polkadot-native-routing",
            Policy = new UsageEndpointPolicy { MeterCategory = "api.request" }
        };
        await outbox.InitializeAsync(default);
        Assert.IsTrue(await outbox.TryBeginAsync(operation, default));
        var constructor = typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.GetParameters().Length == 5);
        var execution = (UsageExecutionHandle)constructor.Invoke(
            new object?[] { operation, outbox, new Web4UsageLedgerOptions(), null, null });
        var prepared = new PreparedStorageExecution(execution, "polkadot-routing",
            new Dictionary<ProviderType, string> { [polkadot.ProviderType.Value] = configuration.OwnerAccountId },
            new[] { sqlite.ProviderType.Value });
        try
        {
            var polkadotActivation = await polkadot.ActivateProviderAsync();
            Assert.IsTrue(!polkadotActivation.IsError && polkadotActivation.Result, polkadotActivation.Message);
            var sqliteActivation = await sqlite.ActivateProviderAsync();
            Assert.IsTrue(!sqliteActivation.IsError && sqliteActivation.Result, sqliteActivation.Message);

            var native = polkadot.CreateRecordBackend(configuration.ContractAccountId, configuration.CodeHash,
                configuration.OwnerAccountId);
            Assert.AreEqual(0, (await native.ListKeysAsync("")).Count);
            await NativePriorityRouting.VerifyAsync(polkadot, sqlite, unavailablePolkadot, unavailableSqlite, sync, prepared);
            Assert.IsFalse(File.Exists(missingPath));
            var records = await polkadot.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
            Assert.IsFalse(records.IsError, records.Message);
            var fixtures = records.Result.ToArray();
            CollectionAssert.AreEquivalent(
                new[] { $"priority routing {polkadot.ProviderType.Value}", $"priority routing {sqlite.ProviderType.Value}" },
                fixtures.Select(record => record.Name).ToArray());
            foreach (var record in fixtures)
            {
                var removed = await polkadot.ExecutePreparedStorageAsync(() => polkadot.DeleteHolonAsync(record.Id),
                    execution, $"cleanup-{record.Id:N}", configuration.OwnerAccountId);
                Assert.IsTrue(!removed.IsError && removed.IsDeleted && removed.Result?.Id == record.Id,
                    removed.Message);
            }
            Assert.AreEqual(0, (await native.ListKeysAsync("")).Count);
            var persisted = await new MongoUsageOutboxStore(mongoEndpoint, database).GetAsync(operation.Id, default);
            Assert.IsNotNull(persisted);
            var originals = persisted.ProviderAttempts.Where(attempt => attempt.NativePreparation != null).ToArray();
            Assert.IsTrue(originals.Length >= 4, "Both replicated saves and exact cleanup must persist native originals.");
            Assert.AreEqual(originals.Length, originals.Select(attempt => attempt.ProviderRequestId).Distinct().Count());
        }
        finally
        {
            await polkadot.DeActivateProviderAsync();
            await unavailablePolkadot.DeActivateProviderAsync();
            await sqlite.DeActivateProviderAsync();
            await new MongoClient(mongoEndpoint).DropDatabaseAsync(database);
        }
    }

    private static string Required(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        Assert.IsFalse(string.IsNullOrWhiteSpace(value), $"Missing fixture setting {name}.");
        return value!;
    }
}
