using System.Net;
using System.Net.Sockets;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Xunit;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.IntegrationTests;

public partial class TronNativeStorageLifecycleTests
{
    private static async Task VerifyRoutingAsync(TRONOASIS tron, Uri fullNode, Uri solidityNode,
        string network, string chainId, string genesis, string contract, string owner, bool synchronous)
    {
        Assert.Equal("true", Required("GITHUB_ACTIONS"));
        var fixture = Path.GetFullPath(Required("OASIS_TRON_CI_FIXTURE"));
        Assert.Equal(Path.GetFullPath(Required("RUNNER_TEMP")), Path.GetDirectoryName(fixture));
        Assert.StartsWith("oasis-tron.", Path.GetFileName(fixture));
        var path = Path.Combine(fixture, synchronous ? "routing-sync.db" : "routing-async.db");
        Assert.False(File.Exists(path));
        using var sqlite = new SqliteProvider($"Data Source={path};Pooling=False");
        var missing = Path.Combine(fixture, "missing.db");
        using var unavailableSqlite = new SqliteProvider($"Data Source={missing};Mode=ReadWrite;Pooling=False;Default Timeout=1");

        using var closed = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        closed.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var unavailableEndpoint = $"http://127.0.0.1:{((IPEndPoint)closed.LocalEndPoint!).Port}";
        var unavailableTron = new TRONOASIS(unavailableEndpoint, network, chainId,
            fullNodeGrpcEndpoint: unavailableEndpoint, solidityNodeGrpcEndpoint: unavailableEndpoint,
            genesisBlockId: genesis, storageContractAddress: contract, storageOwnerAddress: owner);

        try
        {
            var tronActivation = await tron.ActivateProviderAsync();
            Assert.True(!tronActivation.IsError && tronActivation.Result, tronActivation.Message);
            var sqliteActivation = await sqlite.ActivateProviderAsync();
            Assert.True(!sqliteActivation.IsError && sqliteActivation.Result, sqliteActivation.Message);
            await using var runtime = new TronSdkRuntime(fullNode, solidityNode, genesis);
            var native = new TronStorageBackend(runtime.NativeTransfers, owner, contract,
                () => throw new InvalidOperationException("Native verification must stay read-only."));
            Assert.Empty(await native.ListKeysAsync(string.Empty));

            await NativePriorityRouting.VerifyAsync(tron, sqlite, unavailableTron, unavailableSqlite, synchronous);
            Assert.False(File.Exists(missing));
            var records = await tron.LoadAllHolonsAsync(HolonType.All, loadChildren: false);
            Assert.False(records.IsError, records.Message);
            var fixtures = records.Result.OrderBy(record => record.Name, StringComparer.Ordinal).ToArray();
            Assert.Equal(new[] { $"priority routing {sqlite.ProviderType.Value}", $"priority routing {tron.ProviderType.Value}" },
                fixtures.Select(record => record.Name));
            foreach (var record in fixtures)
            {
                var removed = await tron.DeleteHolonAsync(record.Id);
                Assert.True(!removed.IsError && removed.IsDeleted && removed.Result?.Id == record.Id, removed.Message);
            }
            Assert.Empty(await native.ListKeysAsync(string.Empty));
        }
        finally
        {
            await tron.DeActivateProviderAsync();
            await unavailableTron.DeActivateProviderAsync();
            await sqlite.DeActivateProviderAsync();
        }
    }
}
