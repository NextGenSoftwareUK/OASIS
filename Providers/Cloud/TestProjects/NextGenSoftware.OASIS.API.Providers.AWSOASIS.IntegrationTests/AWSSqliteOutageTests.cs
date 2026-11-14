using System.Text.Json;
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

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task SqliteOutageRoutesToDynamoDb(bool synchronous)
    {
        var id = Guid.NewGuid();
        var unavailablePath = Path.GetFullPath($"TestResults/AWSRoutingFiles/{id:N}/unavailable.db");
        Assert.IsFalse(File.Exists(unavailablePath));
        using var sqlite = new SqliteProvider($"Data Source={unavailablePath};Mode=ReadWrite;Pooling=False;Default Timeout=1");
        const string expected = "DynamoDB outage replica";
        try
        {
            var identityProvider = (ICosmicStorageIdentityProvider)_provider;
            var bindings = await Task.WhenAll(identityProvider.CaptureStorageIdentityAsync(),
                identityProvider.CaptureStorageIdentityAsync());
            foreach (var binding in bindings)
                Assert.IsFalse(binding.IsError, $"{binding.Message} {binding.Exception}");
            Assert.AreEqual(ProviderType.AWSOASIS, bindings[0].Result.Authority);
            Assert.AreEqual(bindings[0].Result.BindingFingerprint, bindings[1].Result.BindingFingerprint);
            var receiptHolon = new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid(), IsActive = true };
            CosmicConcurrency.SetProviderRevisionId(receiptHolon, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
            var replicaIntent = new CommittedHolonReplicaRequest(Guid.NewGuid(), "owned-sqlite",
                ProviderType.SQLLiteDBOASIS, Guid.NewGuid(), ConditionalHolonOperation.Save, new[] { receiptHolon }, saveChildren: false);
            var receiptProvider = (ICommittedHolonReplicaReceiptStorageProvider)_provider;
            var receipt = await receiptProvider.ReadCommittedHolonReplicaReceiptAsync(
                new CommittedHolonReplicaReceiptReadRequest(replicaIntent, bindings[0].Result));
            Assert.IsFalse(receipt.IsError, $"{receipt.Message} {receipt.Exception}");
            Assert.AreEqual(ConditionalHolonReceiptState.Absent, receipt.Result.ReceiptState);
            var wrongStore = new CosmicStorageIdentity(ProviderType.AWSOASIS, bindings[0].Result.EndpointFingerprint, Guid.NewGuid());
            var mismatch = await receiptProvider.ReadCommittedHolonReplicaReceiptAsync(
                new CommittedHolonReplicaReceiptReadRequest(replicaIntent, wrongStore));
            Assert.AreEqual("COSMIC_REPLICA_RECEIPT_STORAGE_MISMATCH", mismatch.ErrorCode);
            var receiver = (IBoundCommittedHolonReplicaStorageProvider)_provider;
            var applied = synchronous ? receiver.ApplyBoundCommittedHolonReplica(replicaIntent, bindings[0].Result)
                : await receiver.ApplyBoundCommittedHolonReplicaAsync(replicaIntent, bindings[0].Result);
            try
            {
                Assert.IsFalse(applied.IsError, $"{applied.Message} {applied.Exception}");
                var replay = await receiver.ApplyBoundCommittedHolonReplicaAsync(replicaIntent, bindings[0].Result);
                Assert.IsFalse(replay.IsError, replay.Message);
                Assert.AreEqual(receiptHolon.VersionId, replay.Result.Single().VersionId);
                var committedReceipt = await receiptProvider.ReadCommittedHolonReplicaReceiptAsync(
                    new CommittedHolonReplicaReceiptReadRequest(replicaIntent, bindings[0].Result));
                Assert.IsFalse(committedReceipt.IsError, committedReceipt.Message);
                Assert.AreEqual(ConditionalHolonReceiptState.Committed, committedReceipt.Result.ReceiptState);
            }
            finally
            {
                await _provider.DeleteHolonAsync(receiptHolon.Id, softDelete: false);
            }

            var saved = await _provider.SaveHolonAsync(new Holon { Id = id, Name = expected, IsActive = true }, saveChildren: false);
            Assert.IsFalse(saved.IsError, saved.Message);
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = HyperDriveModes.V2,
                StorageProviders = new StorageProviderSettings(),
                SubscriptionConfig = new SubscriptionConfig { PlanType = "Gold" }
            } };
            var manager = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false };
            manager.RegisterProvider(sqlite);
            manager.RegisterProvider(_provider);
            var configured = manager.SetAndReplaceAutoFailOverListForProviders(new[]
            {
                new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS),
                new EnumValue<ProviderType>(ProviderType.AWSOASIS)
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
            Assert.AreEqual(nameof(ProviderType.AWSOASIS), route.RootElement.GetProperty("ServingProvider").GetString());
            Assert.IsFalse(File.Exists(unavailablePath));

            var fixture = Environment.GetEnvironmentVariable("OASIS_DYNAMO_OUTAGE_FIXTURE");
            Assert.IsFalse(string.IsNullOrWhiteSpace(fixture), "The owned DynamoDB fixture is required.");
            using var staleSqlite = new SqliteProvider($"Data Source={Path.Combine(fixture!, $"{id:N}.db")};Pooling=False");
            var activated = await staleSqlite.ActivateProviderAsync();
            Assert.IsTrue(!activated.IsError && activated.Result, activated.Message);
            var replica = await staleSqlite.SaveHolonAsync(new Holon { Id = id, Name = expected, IsActive = true }, saveChildren: false);
            Assert.IsFalse(replica.IsError, replica.Message);
            var deleted = await _provider.DeleteHolonAsync(id);
            Assert.IsTrue(!deleted.IsError && deleted.IsDeleted, deleted.Message);
            Assert.AreEqual(id, (await staleSqlite.LoadHolonAsync(id, loadChildren: false)).Result?.Id);

            var healthy = new ProviderManager(null, dna) { IsAutoFailOverEnabled = true };
            healthy.RegisterProvider(_provider);
            healthy.RegisterProvider(staleSqlite);
            Assert.IsFalse(healthy.SetAndReplaceAutoFailOverListForProviders(new[]
            {
                new EnumValue<ProviderType>(ProviderType.AWSOASIS),
                new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)
            }).IsError);
            var healthyRouter = new OASISHyperDrive(healthy);
            var readAws = new StorageOperationRequest
            {
                Operation = "LoadHolon", HolonId = id, LoadChildren = false,
                PreferredProvider = ProviderType.AWSOASIS
            };
            var missing = synchronous ? healthyRouter.RouteRequest<IHolon>(readAws)
                : await healthyRouter.RouteRequestAsync<IHolon>(readAws);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, missing.ErrorCode, missing.Message);
            Assert.IsNull(missing.Result);
            Assert.AreEqual(0L, healthy.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
        }
        finally
        {
            var existing = await _provider.LoadHolonAsync(id, loadChildren: false);
            if (existing.Result != null)
            {
                var deleted = await _provider.DeleteHolonAsync(id);
                Assert.IsFalse(deleted.IsError, deleted.Message);
                Assert.IsTrue(deleted.IsDeleted);
            }
            var absent = await _provider.LoadHolonAsync(id, loadChildren: false);
            Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, absent.ErrorCode);
        }
    }
}
