using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.TestSupport;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

public partial class SolanaCoreStorageConformanceTests
{
    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task RoutesAcrossNativeSolanaAndSqlite(bool sync)
    {
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var fixture = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_SOLANA_ROUTING_DIRECTORY")!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(fixture));
        StringAssert.StartsWith(Path.GetFileName(fixture), "oasis-agave.");

        var database = Path.Combine(fixture, sync ? "sync.db" : "async.db");
        Assert.IsFalse(File.Exists(database));
        using var sqlite = new SqliteProvider($"Data Source={database};Pooling=False");
        var missing = Path.Combine(fixture, "missing.db");
        using var unavailableSqlite = new SqliteProvider($"Data Source={missing};Mode=ReadWrite;Pooling=False;Default Timeout=1");
        using var closed = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        closed.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var solana = await OpenFreshValidator();
        var unavailableSolana = await OpenFreshValidator($"http://127.0.0.1:{((IPEndPoint)closed.LocalEndPoint!).Port}");
        try
        {
            var active = await solana.ActivateProviderAsync();
            Assert.IsTrue(!active.IsError && active.Result, active.Message);
            active = await sqlite.ActivateProviderAsync();
            Assert.IsTrue(!active.IsError && active.Result, active.Message);

            await NativePriorityRouting.VerifyAsync(solana, sqlite, unavailableSolana, unavailableSqlite, sync);
            Assert.IsFalse(File.Exists(missing));
            var records = await solana.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
            Assert.IsFalse(records.IsError, records.Message);
            var fixtures = records.Result.ToArray();
            CollectionAssert.AreEquivalent(
                new[] { $"priority routing {solana.ProviderType.Value}", $"priority routing {sqlite.ProviderType.Value}" },
                fixtures.Select(record => record.Name).ToArray());
            foreach (var record in fixtures)
            {
                var removed = await solana.DeleteHolonAsync(record.Id);
                Assert.IsTrue(!removed.IsError && removed.IsDeleted && removed.DeletedCount == 1, removed.Message);
            }

            var current = await OpenFreshValidator();
            try
            {
                var remaining = await current.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
                Assert.IsFalse(remaining.IsError, remaining.Message);
                Assert.IsFalse(remaining.Result.Any(), "Native finalized history must project no active fixture records.");
            }
            finally
            {
                await current.DeActivateProviderAsync();
            }
        }
        finally
        {
            await solana.DeActivateProviderAsync();
            await unavailableSolana.DeActivateProviderAsync();
            await sqlite.DeActivateProviderAsync();
        }
    }
}
