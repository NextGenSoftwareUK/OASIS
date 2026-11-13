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
