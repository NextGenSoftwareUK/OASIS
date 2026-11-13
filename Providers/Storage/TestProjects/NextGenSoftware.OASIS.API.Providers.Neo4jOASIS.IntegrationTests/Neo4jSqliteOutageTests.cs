using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class Neo4jSqliteOutageTests
{
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task SqliteOutageRoutesToNeo4j(bool synchronous)
    {
        var endpoint = new Uri(Environment.GetEnvironmentVariable("OASIS_NEO4J_OUTAGE_URI")
            ?? throw new InvalidOperationException("An owned Neo4j endpoint is required."));
        Assert.IsTrue(endpoint.IsLoopback);
        Assert.AreEqual("bolt", endpoint.Scheme);
        var provider = new Neo4jOASIS(endpoint.AbsoluteUri, "neo4j", "oasis-fixture-only");
        var id = Guid.NewGuid();
        var unavailablePath = Path.Combine(Path.GetTempPath(), $"oasis-neo4j-{id:N}.db");
        Assert.IsFalse(File.Exists(unavailablePath));
        using var sqlite = new SqliteProvider($"Data Source={unavailablePath};Mode=ReadWrite;Pooling=False;Default Timeout=1");
        const string expected = "Neo4j outage replica";
        try
        {
            var activated = await provider.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Message);
            var saved = await provider.SaveHolonAsync(new Holon { Id = id, Name = expected, IsActive = true }, saveChildren: false);
            Assert.IsFalse(saved.IsError, saved.Message);
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = HyperDriveModes.V2,
                StorageProviders = new StorageProviderSettings(),
                SubscriptionConfig = new SubscriptionConfig { PlanType = "Gold" }
            } };
            var manager = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false };
            manager.RegisterProvider(sqlite);
            manager.RegisterProvider(provider);
            var configured = manager.SetAndReplaceAutoFailOverListForProviders(new[]
            {
                new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS),
                new EnumValue<ProviderType>(ProviderType.Neo4jOASIS)
            });
            Assert.IsFalse(configured.IsError, configured.Message);
            var router = new OASISHyperDrive(manager);
            StorageOperationRequest ReadSqlite() => new()
            {
                Operation = "LoadHolon", HolonId = id, LoadChildren = false,
                PreferredProvider = ProviderType.SQLLiteDBOASIS
            };
            var rejected = synchronous ? router.RouteRequest<IHolon>(ReadSqlite())
                : await router.RouteRequestAsync<IHolon>(ReadSqlite());
            Assert.IsTrue(rejected.IsError, rejected.Message);

            manager.IsAutoFailOverEnabled = true;
            var recovered = synchronous ? router.RouteRequest<IHolon>(ReadSqlite())
                : await router.RouteRequestAsync<IHolon>(ReadSqlite());
            Assert.IsFalse(recovered.IsError, recovered.Message);
            Assert.IsTrue(recovered.IsLoaded);
            Assert.AreEqual(id, recovered.Result.Id);
            Assert.AreEqual(expected, recovered.Result.Name);
            using var route = JsonDocument.Parse(recovered.MetaData["hyperDriveRoute"]);
            Assert.AreEqual(nameof(ProviderType.SQLLiteDBOASIS), route.RootElement.GetProperty("RequestedProvider").GetString());
            Assert.AreEqual(nameof(ProviderType.Neo4jOASIS), route.RootElement.GetProperty("ServingProvider").GetString());
            Assert.IsFalse(File.Exists(unavailablePath));

            // A healthy primary's authoritative absence must not expose a stale replica.
            await using var emptyDatabase = new SqliteConnection($"Data Source=oasis-absence-{id:N};Mode=Memory;Cache=Shared;Pooling=False");
            await emptyDatabase.OpenAsync();
            using var emptySqlite = new SqliteProvider(emptyDatabase.ConnectionString);
            var sqliteActivated = await emptySqlite.ActivateProviderAsync();
            Assert.IsFalse(sqliteActivated.IsError, sqliteActivated.Message);
            var healthyManager = new ProviderManager(null, dna) { IsAutoFailOverEnabled = true };
            healthyManager.RegisterProvider(emptySqlite);
            healthyManager.RegisterProvider(provider);
            var healthyConfigured = healthyManager.SetAndReplaceAutoFailOverListForProviders(new[]
            {
                new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS),
                new EnumValue<ProviderType>(ProviderType.Neo4jOASIS)
            });
            Assert.IsFalse(healthyConfigured.IsError, healthyConfigured.Message);
            var healthyRouter = new OASISHyperDrive(healthyManager);
            var missing = synchronous ? healthyRouter.RouteRequest<IHolon>(ReadSqlite())
                : await healthyRouter.RouteRequestAsync<IHolon>(ReadSqlite());
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, missing.ErrorCode);
            Assert.IsNull(missing.Result);

            var keyRequest = new StorageOperationRequest
            {
                Operation = "LoadHolonByProviderKey", ProviderKey = id.ToString(), LoadChildren = false,
                PreferredProvider = ProviderType.SQLLiteDBOASIS
            };
            var missingByKey = synchronous ? healthyRouter.RouteRequest<IHolon>(keyRequest)
                : await healthyRouter.RouteRequestAsync<IHolon>(keyRequest);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, missingByKey.ErrorCode);
            Assert.IsNull(missingByKey.Result);

            var detailRequests = new StorageOperationRequest[]
            {
                new() { Operation = "LoadAvatarDetail", AvatarId = id },
                new() { Operation = "LoadAvatarDetailByUsername", Username = $"absent-{id:N}" },
                new() { Operation = "LoadAvatarDetailByEmail", Email = $"absent-{id:N}@example.invalid" }
            };
            foreach (var request in detailRequests)
            {
                request.PreferredProvider = ProviderType.SQLLiteDBOASIS;
                var missingDetail = synchronous ? healthyRouter.RouteRequest<IAvatarDetail>(request)
                    : await healthyRouter.RouteRequestAsync<IAvatarDetail>(request);
                Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, missingDetail.ErrorCode, request.Operation);
                Assert.IsNull(missingDetail.Result, request.Operation);
            }
            Assert.AreEqual(0L, healthyManager.PerformanceMonitor.GetMetrics(ProviderType.Neo4jOASIS)?.TotalRequests ?? 0);
        }
        finally
        {
            try
            {
                var existing = await provider.LoadHolonAsync(id, loadChildren: false);
                if (existing.Result != null)
                {
                    var deleted = await provider.DeleteHolonAsync(id);
                    Assert.IsFalse(deleted.IsError, deleted.Message);
                    Assert.IsTrue(deleted.IsDeleted);
                }
                var absent = await provider.LoadHolonAsync(id, loadChildren: false);
                Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, absent.ErrorCode);
            }
            finally
            {
                await provider.DeActivateProviderAsync();
            }
        }
    }
}
