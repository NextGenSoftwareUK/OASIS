using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.TestSupport;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.IntegrationTests;

public partial class EOSIOOASISIntegrationTests
{
    [TestMethod]
    public async Task HyperDriveRoutingOnOfficialAntelope()
    {
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.AreEqual("1", RequiredRoutingSetting("OASIS_ANTELOPE_ROUTING"));
        Assert.AreEqual("http://127.0.0.1:18888", RequiredRoutingSetting("OASIS_ANTELOPE_ENDPOINT"));
        var account = RequiredRoutingSetting("OASIS_ANTELOPE_ACCOUNT");
        Assert.AreEqual("oasisfixture", account);
        var mode = RequiredRoutingSetting("OASIS_ANTELOPE_STORAGE_MODE");
        Assert.IsTrue(mode is "sync" or "async");
        var root = Path.GetFullPath(RequiredRoutingSetting("OASIS_ANTELOPE_RESULTS"));
        var fixture = Directory.GetParent(root)!;
        Assert.AreEqual(Path.GetFullPath(RequiredRoutingSetting("RUNNER_TEMP")), fixture.Parent!.FullName);
        StringAssert.StartsWith(fixture.Name, "oasis-antelope.");
        Directory.CreateDirectory(root);

        var database = Path.Combine(root, "routing.db");
        Assert.IsFalse(File.Exists(database));
        using var sqlite = new SqliteProvider($"Data Source={database};Pooling=False");
        var missing = Path.Combine(root, "missing.db");
        using var unavailableSqlite = new SqliteProvider($"Data Source={missing};Mode=ReadWrite;Pooling=False;Default Timeout=1");
        using var closed = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        closed.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var unavailable = new EOSIOOASIS($"http://127.0.0.1:{((IPEndPoint)closed.LocalEndPoint!).Port}",
            account, RequiredRoutingSetting("OASIS_ANTELOPE_CHAIN_ID"), RequiredRoutingSetting("OASIS_ANTELOPE_PRIVATE_KEY"));

        try
        {
            var active = await sqlite.ActivateProviderAsync();
            Assert.IsTrue(!active.IsError && active.Result, active.Message);
            await NativePriorityRouting.VerifyAsync(_provider, sqlite, unavailable, unavailableSqlite, mode == "sync");
            Assert.IsFalse(File.Exists(missing));
            var records = await _provider.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
            Assert.IsFalse(records.IsError, records.Message);
            var fixtures = records.Result.ToArray();
            CollectionAssert.AreEquivalent(
                new[] { $"priority routing {_provider.ProviderType.Value}", $"priority routing {sqlite.ProviderType.Value}" },
                fixtures.Select(record => record.Name).ToArray());
            foreach (var record in fixtures)
            {
                var removed = await _provider.DeleteHolonAsync(record.Id);
                Assert.IsTrue(!removed.IsError && removed.IsDeleted && removed.DeletedCount == 1, removed.Message);
            }
        }
        finally
        {
            await sqlite.DeActivateProviderAsync();
            await unavailable.DeActivateProviderAsync();
        }
    }

    private static string RequiredRoutingSetting(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        Assert.IsFalse(string.IsNullOrWhiteSpace(value), $"Missing fixture setting {name}.");
        return value!;
    }
}
