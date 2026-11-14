using System.Net;
using System.Net.Sockets;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class AzureCosmosSqliteHyperDriveTests
{
    // Published development key for the loopback Microsoft emulator.
    private const string EmulatorKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    private const string Endpoint = "https://127.0.0.1:8081";

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task RoutesAcrossNativeAzureCosmosAndSqlite(bool sync)
    {
        var directory = Environment.GetEnvironmentVariable("OASIS_AZURE_COSMOS_ROUTING_DIRECTORY");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(directory));
        directory = Path.GetFullPath(directory!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-azure-routing.");

        using var native = new CosmosClient(Endpoint, EmulatorKey, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            RequestTimeout = TimeSpan.FromSeconds(5),
            MaxRetryAttemptsOnRateLimitedRequests = 0,
            HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
        });
        var databaseName = $"oasis-routing-{Guid.NewGuid():N}";
        var database = native.GetDatabase(databaseName);
        await AssertDatabaseAbsentAsync(database);
        var names = new List<string> { "avatarItems", "avatarDetailItems", "holonItems" };
        var cosmos = new AzureCosmosDBOASIS(new Uri(Endpoint), EmulatorKey, databaseName, names);

        var sqlitePath = Path.Combine(directory, sync ? "routing-sync.db" : "routing-async.db");
        Assert.IsFalse(File.Exists(sqlitePath));
        using var sqlite = new SqliteProvider($"Data Source={sqlitePath};Pooling=False");
        using var closed = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        closed.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)closed.LocalEndPoint!).Port;
        var unavailableCosmos = new AzureCosmosDBOASIS(new Uri($"https://127.0.0.1:{port}"), EmulatorKey, databaseName, names);
        var missingPath = Path.Combine(directory, "missing.db");
        using var unavailableSqlite = new SqliteProvider($"Data Source={missingPath};Mode=ReadWrite;Pooling=False;Default Timeout=1");

        try
        {
            var cosmosActivation = await cosmos.ActivateProviderAsync();
            Assert.IsTrue(!cosmosActivation.IsError && cosmosActivation.Result, cosmosActivation.Message);
            var sqliteActivation = await sqlite.ActivateProviderAsync();
            Assert.IsTrue(!sqliteActivation.IsError && sqliteActivation.Result, sqliteActivation.Message);

            await NativePriorityRouting.VerifyAsync(cosmos, sqlite, unavailableCosmos, unavailableSqlite, sync);
            Assert.IsFalse(File.Exists(missingPath));
            await VerifyHealthyMissAsync(cosmos, sqlite, sync);
        }
        finally
        {
            try
            {
                await cosmos.DeActivateProviderAsync();
                await sqlite.DeActivateProviderAsync();
            }
            finally
            {
                try
                {
                    await database.DeleteAsync();
                }
                catch (CosmosException missing) when (missing.StatusCode == HttpStatusCode.NotFound)
                {
                    // Activation may have failed before the owned database was created.
                }
                await AssertDatabaseAbsentAsync(database);
            }
        }
    }

    private static async Task VerifyHealthyMissAsync(AzureCosmosDBOASIS cosmos, SqliteProvider sqlite, bool sync)
    {
        var id = Guid.NewGuid();
        var saved = await sqlite.SaveHolonAsync(new Holon { Id = id, Name = "Owned stale SQLite replica" }, saveChildren: false);
        Assert.IsFalse(saved.IsError, saved.Message);
        Assert.AreEqual(id, (await sqlite.LoadHolonAsync(id, loadChildren: false)).Result?.Id);

        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var manager = new ProviderManager(null, dna) { IsAutoFailOverEnabled = true };
        manager.RegisterProvider(cosmos);
        manager.RegisterProvider(sqlite);
        var source = cosmos.ProviderType.Value;
        var target = sqlite.ProviderType.Value;
        Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[]
        {
            new EnumValue<ProviderType>(source), new EnumValue<ProviderType>(target)
        }).IsError);

        var router = new OASISHyperDrive(manager);
        var request = new StorageOperationRequest
        {
            Operation = "LoadHolon", HolonId = id, PreferredProvider = source, LoadChildren = false
        };
        var result = sync ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, result.ErrorCode, result.Message);
        Assert.IsNull(result.Result);
        Assert.AreEqual(0L, manager.PerformanceMonitor.GetMetrics(target)?.TotalRequests ?? 0);
    }

    private static async Task AssertDatabaseAbsentAsync(Database database)
    {
        try
        {
            await database.ReadAsync();
            Assert.Fail("The exact disposable Cosmos database still exists.");
        }
        catch (CosmosException missing) when (missing.StatusCode == HttpStatusCode.NotFound)
        {
            // Native absence is the expected result.
        }
    }
}
