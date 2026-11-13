using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class FirestoreSqliteOutageTests
{
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task SqliteOutageRoutesToFirestore(bool synchronous)
    {
        Assert.AreEqual("127.0.0.1:18180", Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        const string project = "demo-oasis-firestore-outage";
        var db = GoogleNativeFirestore.Database(project);
        var fixture = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_FIRESTORE_CI_FIXTURE")
            ?? throw new InvalidOperationException("An owned Firestore fixture directory is required."));
        Assert.IsTrue(Directory.Exists(fixture));
        var id = Guid.NewGuid();
        var path = Path.Combine(fixture, $"{id:N}.unavailable.db");
        Assert.IsFalse(File.Exists(path));
        using var unavailable = new SqliteProvider($"Data Source={path};Mode=ReadWrite;Pooling=False;Default Timeout=1");
        var firestore = new GoogleCloudOASIS(projectId: project, enableStorage: false, enableFirestore: true, enableBigQuery: false);
        var document = db.Collection("holons").Document(id.ToString());
        const string expected = "Firestore outage replica";
        try
        {
            var activated = await firestore.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Message);
            var bindings = await Task.WhenAll(firestore.CaptureStorageIdentityAsync(),
                firestore.CaptureStorageIdentityAsync());
            foreach (var binding in bindings)
                Assert.IsFalse(binding.IsError, binding.Message);
            var identity = await db.Collection("cosmicStorageIdentity").Document("store").GetSnapshotAsync();
            Assert.AreEqual(identity.GetValue<string>("storeId"), bindings[0].Result.StoreId);
            Assert.AreEqual(bindings[0].Result.BindingFingerprint, bindings[1].Result.BindingFingerprint);
            var reopened = new GoogleCloudOASIS(projectId: project, enableStorage: false, enableFirestore: true, enableBigQuery: false);
            try
            {
                var reopenedActivation = await reopened.ActivateProviderAsync();
                Assert.IsFalse(reopenedActivation.IsError, reopenedActivation.Message);
                var reopenedBinding = await reopened.CaptureStorageIdentityAsync();
                Assert.IsFalse(reopenedBinding.IsError, reopenedBinding.Message);
                Assert.AreEqual(bindings[0].Result.BindingFingerprint, reopenedBinding.Result.BindingFingerprint);
            }
            finally
            {
                await reopened.DeActivateProviderAsync();
            }
            var saved = await firestore.SaveHolonAsync(new Holon { Id = id, Name = expected, IsActive = true }, saveChildren: false);
            Assert.IsFalse(saved.IsError, saved.Message);
            Assert.IsTrue((await document.GetSnapshotAsync()).Exists);

            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = HyperDriveModes.V2,
                StorageProviders = new StorageProviderSettings(),
                SubscriptionConfig = new SubscriptionConfig { PlanType = "Gold" }
            } };
            var manager = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false };
            manager.RegisterProvider(unavailable);
            manager.RegisterProvider(firestore);
            var configured = manager.SetAndReplaceAutoFailOverListForProviders(new[]
            {
                new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS),
                new EnumValue<ProviderType>(ProviderType.GoogleCloudOASIS)
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
            Assert.AreEqual(nameof(ProviderType.GoogleCloudOASIS), route.RootElement.GetProperty("ServingProvider").GetString());
            Assert.IsFalse(File.Exists(path));

            using var staleSqlite = new SqliteProvider($"Data Source={Path.Combine(fixture, $"{id:N}.db")};Pooling=False");
            var sqliteActivation = await staleSqlite.ActivateProviderAsync();
            Assert.IsTrue(!sqliteActivation.IsError && sqliteActivation.Result, sqliteActivation.Message);
            var replica = await staleSqlite.SaveHolonAsync(new Holon { Id = id, Name = expected, IsActive = true }, saveChildren: false);
            Assert.IsFalse(replica.IsError, replica.Message);
            var snapshot = await document.GetSnapshotAsync();
            await document.DeleteAsync(Precondition.LastUpdated(snapshot.UpdateTime!.Value));
            Assert.IsFalse((await document.GetSnapshotAsync()).Exists);
            Assert.AreEqual(id, (await staleSqlite.LoadHolonAsync(id, loadChildren: false)).Result?.Id);

            var healthy = new ProviderManager(null, dna) { IsAutoFailOverEnabled = true };
            healthy.RegisterProvider(firestore);
            healthy.RegisterProvider(staleSqlite);
            Assert.IsFalse(healthy.SetAndReplaceAutoFailOverListForProviders(new[]
            {
                new EnumValue<ProviderType>(ProviderType.GoogleCloudOASIS),
                new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)
            }).IsError);
            var healthyRouter = new OASISHyperDrive(healthy);
            var readFirestore = new StorageOperationRequest
            {
                Operation = "LoadHolon", HolonId = id, LoadChildren = false,
                PreferredProvider = ProviderType.GoogleCloudOASIS
            };
            var missing = synchronous ? healthyRouter.RouteRequest<IHolon>(readFirestore)
                : await healthyRouter.RouteRequestAsync<IHolon>(readFirestore);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, missing.ErrorCode, missing.Message);
            Assert.IsNull(missing.Result);
            Assert.AreEqual(0L, healthy.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
        }
        finally
        {
            try
            {
                var snapshot = await document.GetSnapshotAsync();
                if (snapshot.Exists)
                    await document.DeleteAsync(Precondition.LastUpdated(snapshot.UpdateTime!.Value));
                Assert.IsFalse((await document.GetSnapshotAsync()).Exists);
            }
            finally
            {
                await firestore.DeActivateProviderAsync();
            }
        }
    }
}
