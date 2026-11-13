using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.TestSupport;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class HederaSqliteHyperDriveTests
{
    [TestMethod]
    public async Task RoutesAcrossNativeHederaAndSqlite()
    {
        Assert.AreEqual("true", Required("GITHUB_ACTIONS"));
        Assert.AreEqual("1", Required("OASIS_HEDERA_ROUTING"));
        var mode = Required("OASIS_HEDERA_LOCAL_MODE");
        Assert.IsTrue(mode is "async" or "sync");
        var fixture = Path.GetFullPath(Required("OASIS_HEDERA_LOCAL_FIXTURE"));
        Assert.AreEqual(Path.GetFullPath(Required("RUNNER_TEMP")), Path.GetDirectoryName(fixture));
        StringAssert.StartsWith(Path.GetFileName(fixture), "oasis-hedera.");
        var contract = Required("OASIS_HEDERA_LOCAL_CONTRACT_ID");
        var nodes = new Dictionary<string, string> { ["127.0.0.1:50211"] = "0.0.3" };
        var configuration = new HederaStorageConfiguration
        {
            ContractId = contract,
            OperatorAccountId = "0.0.2",
            SigningKeyType = "der",
            SigningKeyResolver = () => Required("OASIS_HEDERA_LOCAL_SIGNING_KEY"),
            EncryptionKeyResolver = () => Convert.FromBase64String(Required("OASIS_HEDERA_LOCAL_ENCRYPTION_KEY")),
            GasLimit = 4_000_000,
            MaxFeeTinybars = 10_000_000_000
        };
        var hedera = new HashgraphOASIS("http://127.0.0.1:5551", "local", "298",
            consensusNodes: nodes, storageConfiguration: configuration);
        var raw = new HederaStorageBackend("local", contract, configuration.OperatorAccountId,
            configuration.SigningKeyType, configuration.SigningKeyResolver, configuration.GasLimit,
            configuration.MaxFeeTinybars, nodes);
        using var closed = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        closed.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var unavailableEndpoint = $"127.0.0.1:{((IPEndPoint)closed.LocalEndPoint!).Port}";
        var unavailableHedera = new HashgraphOASIS($"http://{unavailableEndpoint}", "local", "298",
            consensusNodes: new Dictionary<string, string> { [unavailableEndpoint] = "0.0.3" },
            storageConfiguration: configuration);
        var path = Path.Combine(fixture, "routing.db");
        Assert.IsFalse(File.Exists(path));
        using var sqlite = new SqliteProvider($"Data Source={path};Pooling=False");
        var missing = Path.Combine(fixture, "missing.db");
        using var unavailableSqlite = new SqliteProvider($"Data Source={missing};Mode=ReadWrite;Pooling=False;Default Timeout=1");

        try
        {
            Assert.AreEqual(0, (await raw.ListKeysAsync(string.Empty)).Count);
            var active = await hedera.ActivateProviderAsync();
            Assert.IsTrue(!active.IsError && active.Result, active.Message);
            active = await sqlite.ActivateProviderAsync();
            Assert.IsTrue(!active.IsError && active.Result, active.Message);
            await NativePriorityRouting.VerifyAsync(hedera, sqlite, unavailableHedera, unavailableSqlite, mode == "sync");
            Assert.IsFalse(File.Exists(missing));

            var records = await hedera.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
            Assert.IsFalse(records.IsError, records.Message);
            var fixtures = records.Result.ToArray();
            CollectionAssert.AreEquivalent(
                new[] { $"priority routing {hedera.ProviderType.Value}", $"priority routing {sqlite.ProviderType.Value}" },
                fixtures.Select(record => record.Name).ToArray());
            foreach (var record in fixtures)
            {
                var removed = await hedera.DeleteHolonAsync(record.Id);
                Assert.IsTrue(!removed.IsError && removed.IsDeleted && removed.DeletedCount == 1, removed.Message);
            }
            Assert.AreEqual(0, (await raw.ListKeysAsync(string.Empty)).Count);
        }
        finally
        {
            await hedera.DeActivateProviderAsync();
            await unavailableHedera.DeActivateProviderAsync();
            await sqlite.DeActivateProviderAsync();
        }
    }

    private static string Required(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        Assert.IsFalse(string.IsNullOrWhiteSpace(value), $"Missing fixture setting {name}.");
        return value!;
    }
}
