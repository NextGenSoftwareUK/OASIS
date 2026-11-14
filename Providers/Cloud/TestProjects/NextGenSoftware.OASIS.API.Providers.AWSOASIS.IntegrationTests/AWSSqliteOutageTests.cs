using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
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

                var successor = new Holon { Id = receiptHolon.Id, Version = 2, VersionId = Guid.NewGuid(),
                    PreviousVersionId = Guid.NewGuid(), Name = "DynamoDB replica successor", IsActive = true };
                CosmicConcurrency.SetProviderRevisionId(successor, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
                CommittedHolonReplicaRequest NextReplica(ConditionalHolonOperation operation, Holon snapshot)
                    => new(Guid.NewGuid(), replicaIntent.SourceId, replicaIntent.SourceAuthority, replicaIntent.ActorId,
                        operation, new[] { snapshot }, saveChildren: false);
                var divergent = NextReplica(ConditionalHolonOperation.Save, successor);
                var conflict = await receiver.ApplyBoundCommittedHolonReplicaAsync(divergent, bindings[0].Result);
                Assert.AreEqual("COSMIC_REPLICA_LINEAGE_CONFLICT", conflict.ErrorCode, $"{conflict.Message} {conflict.Exception}");
                var rejectedReceipt = await receiptProvider.ReadCommittedHolonReplicaReceiptAsync(
                    new CommittedHolonReplicaReceiptReadRequest(divergent, bindings[0].Result));
                Assert.IsFalse(rejectedReceipt.IsError, rejectedReceipt.Message);
                Assert.AreEqual(ConditionalHolonReceiptState.Absent, rejectedReceipt.Result.ReceiptState);
                Assert.AreEqual(receiptHolon.VersionId,
                    (await _provider.LoadHolonAsync(receiptHolon.Id, loadChildren: false)).Result?.VersionId);

                successor.PreviousVersionId = receiptHolon.VersionId;
                var advanced = await receiver.ApplyBoundCommittedHolonReplicaAsync(
                    NextReplica(ConditionalHolonOperation.Save, successor), bindings[0].Result);
                Assert.IsFalse(advanced.IsError, $"{advanced.Message} {advanced.Exception}");
                Assert.AreEqual(successor.VersionId,
                    (await _provider.LoadHolonAsync(receiptHolon.Id, loadChildren: false)).Result?.VersionId);
                var tombstone = new Holon { Id = receiptHolon.Id, Version = 3, VersionId = Guid.NewGuid(),
                    PreviousVersionId = successor.VersionId, Name = successor.Name, IsActive = false,
                    DeletedDate = DateTime.UtcNow, DeletedByAvatarId = replicaIntent.ActorId };
                CosmicConcurrency.SetProviderRevisionId(tombstone, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
                var deletion = NextReplica(ConditionalHolonOperation.HardDelete, tombstone);
                var deletedReplica = synchronous ? receiver.ApplyBoundCommittedHolonReplica(deletion, bindings[0].Result)
                    : await receiver.ApplyBoundCommittedHolonReplicaAsync(deletion, bindings[0].Result);
                Assert.IsTrue(!deletedReplica.IsError && deletedReplica.IsDeleted,
                    $"{deletedReplica.Message} {deletedReplica.Exception}");
                var deletionReceipt = await receiptProvider.ReadCommittedHolonReplicaReceiptAsync(
                    new CommittedHolonReplicaReceiptReadRequest(deletion, bindings[0].Result));
                Assert.IsFalse(deletionReceipt.IsError, deletionReceipt.Message);
                Assert.AreEqual(ConditionalHolonReceiptState.Committed, deletionReceipt.Result.ReceiptState);
                var afterDeletion = await _provider.LoadHolonAsync(receiptHolon.Id, loadChildren: false);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, afterDeletion.ErrorCode);
            }
            finally
            {
                await _provider.DeleteHolonAsync(receiptHolon.Id);
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

            var readByKey = new StorageOperationRequest
            {
                Operation = "LoadHolonByProviderKey", ProviderKey = id.ToString("D"), LoadChildren = false,
                PreferredProvider = ProviderType.AWSOASIS
            };
            var missingByKey = synchronous ? healthyRouter.RouteRequest<IHolon>(readByKey)
                : await healthyRouter.RouteRequestAsync<IHolon>(readByKey);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, missingByKey.ErrorCode, missingByKey.Message);
            Assert.IsNull(missingByKey.Result);
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

    [TestMethod]
    public async Task SqliteDispatcherReplicatesToDynamoDb()
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_DYNAMO_OUTAGE_FIXTURE");
        Assert.IsFalse(string.IsNullOrWhiteSpace(fixture), "The owned DynamoDB fixture is required.");
        var connection = $"Data Source={Path.Combine(fixture!, $"{Guid.NewGuid():N}.outbox.db")};Pooling=False";
        using var sqlite = new SqliteProvider(connection);
        var activated = await sqlite.ActivateProviderAsync();
        Assert.IsTrue(!activated.IsError && activated.Result, activated.Message);
        try
        {
            var source = await sqlite.CaptureStorageIdentityAsync();
            var destination = await ((ICosmicStorageIdentityProvider)_provider).CaptureStorageIdentityAsync();
            Assert.IsFalse(source.IsError, source.Message);
            Assert.IsFalse(destination.IsError, destination.Message);
            var actor = Guid.NewGuid();
            var policy = new HolonReplicationActorGrant(new[] { actor }, DateTime.UtcNow.AddMinutes(10));
            var route = new HolonReplicationRouteBinding(source.Result, destination.Result, policy.BindingIdentity);
            var enrollment = new HolonReplicationEnrollment(route.SourceId, new[] { route.TargetId }, source.Result, new[] { route });
            var record = new Holon { Id = Guid.NewGuid(), Name = "Owned DynamoDB dispatcher fixture" };
            var write = new ConditionalHolonWrite(Guid.NewGuid(), actor, source.Result.Authority,
                new[] { new HolonWriteCondition(record.Id, null) });
            var saved = await sqlite.ExecuteReplicatingHolonWriteAsync(
                new ConditionalHolonWriteRequest(write, ConditionalHolonOperation.Save, new[] { record }, saveChildren: false), enrollment);
            Assert.IsTrue(!saved.IsError && saved.IsSaved, saved.Message);
            var committed = saved.Result.Single();
            var target = new CommittedHolonReplicaTarget(route.TargetId, _provider, destination.Result.Authority,
                route.SourceId, source.Result.Authority, policy, "dynamo-fixture", Guid.NewGuid(), policy.BindingIdentity, destination.Result);
            var dispatcher = new HolonOutboxDispatcher(new SqliteHolonReplicationOutbox(connection), new[] { target }, "dynamo-fixture");
            var delivered = await dispatcher.DispatchOnceAsync(route.TargetId, maximumCount: 1);
            Assert.IsFalse(delivered.IsError, delivered.Message);
            Assert.AreEqual(1, delivered.Result.CompletedCount);
            var loaded = await _provider.LoadHolonAsync(record.Id, loadChildren: false);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(committed.VersionId, loaded.Result?.VersionId);
            var request = new CommittedHolonReplicaRequest(write.OperationId, route.SourceId, source.Result.Authority,
                actor, ConditionalHolonOperation.Save, new[] { committed }, false, true, 0, new[] { route });
            var receipt = await ((ICommittedHolonReplicaReceiptStorageProvider)_provider)
                .ReadCommittedHolonReplicaReceiptAsync(new(request, destination.Result));
            Assert.IsFalse(receipt.IsError, receipt.Message);
            Assert.AreEqual(ConditionalHolonReceiptState.Committed, receipt.Result.ReceiptState);
            var restarted = new HolonOutboxDispatcher(new SqliteHolonReplicationOutbox(connection), new[] { target }, "dynamo-restarted");
            var replay = await restarted.DispatchOnceAsync(route.TargetId, maximumCount: 1);
            Assert.IsFalse(replay.IsError, replay.Message);
            Assert.AreEqual(0, replay.Result.CompletedCount);
        }
        finally
        {
            await sqlite.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task DynamoDbPublishesBoundOutboundDelivery()
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_DYNAMO_OUTAGE_FIXTURE");
        Assert.IsFalse(string.IsNullOrWhiteSpace(fixture), "The owned DynamoDB fixture is required.");
        var endpoint = Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL");
        Assert.AreEqual("http://127.0.0.1:18000", endpoint);
        using var native = new AmazonDynamoDBClient(new BasicAWSCredentials("OwnedOutbound", "local-fixture"),
            new AmazonDynamoDBConfig { ServiceURL = endpoint, AuthenticationRegion = "us-east-1", MaxErrorRetry = 0 });
        using var sqlite = new SqliteProvider($"Data Source={Path.Combine(fixture!, $"{Guid.NewGuid():N}.outbound.db")};Pooling=False");
        var activated = await sqlite.ActivateProviderAsync();
        Assert.IsTrue(!activated.IsError && activated.Result, activated.Message);
        try
        {
            var identityProvider = (ICosmicStorageIdentityProvider)_provider;
            var source = await identityProvider.CaptureStorageIdentityAsync();
            var target = await sqlite.CaptureStorageIdentityAsync();
            Assert.IsFalse(source.IsError, source.Message);
            Assert.IsFalse(target.IsError, target.Message);
            var actor = Guid.NewGuid();
            var policy = new HolonReplicationActorGrant(new[] { actor }, DateTime.UtcNow.AddMinutes(10));
            HolonReplicationEnrollment Enroll(CosmicStorageIdentity identity)
            {
                var route = new HolonReplicationRouteBinding(identity, target.Result, policy.BindingIdentity);
                return new(route.SourceId, new[] { route.TargetId }, identity, new[] { route });
            }

            var record = new Holon { Id = Guid.NewGuid(), Name = "DynamoDB outbound publication" };
            var write = new ConditionalHolonWrite(Guid.NewGuid(), actor, source.Result.Authority,
                new[] { new HolonWriteCondition(record.Id, null) });
            var request = new ConditionalHolonWriteRequest(write, ConditionalHolonOperation.Save, new[] { record }, saveChildren: false);
            var publisher = (IBoundAtomicHolonReplicationStorageProvider)_provider;
            var wrongIdentity = new CosmicStorageIdentity(source.Result.Authority, source.Result.EndpointFingerprint, Guid.NewGuid());
            var rejected = await publisher.ExecuteReplicatingHolonWriteAsync(request, Enroll(wrongIdentity));
            Assert.AreEqual("COSMIC_SOURCE_STORAGE_BINDING_MISMATCH", rejected.ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,
                (await _provider.LoadHolonAsync(record.Id, loadChildren: false)).ErrorCode);

            var enrollment = Enroll(source.Result);
            var counterKey = new Dictionary<string, AttributeValue> { ["id"] = new() { S = "outbound:sequence" } };
            var counter = await native.GetItemAsync(new GetItemRequest
            {
                TableName = "OasisHolonWriteReceipts", Key = counterKey, ConsistentRead = true
            });
            try
            {
                await native.PutItemAsync(new PutItemRequest
                {
                    TableName = "OasisHolonWriteReceipts",
                    Item = new(counterKey) { ["sequence"] = new() { S = "invalid" } }
                });
                var invalidCounter = await publisher.ExecuteReplicatingHolonWriteAsync(request, enrollment);
                Assert.AreEqual("COSMIC_CONDITIONAL_WRITE_FAILED", invalidCounter.ErrorCode);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,
                    (await _provider.LoadHolonAsync(record.Id, loadChildren: false)).ErrorCode);
            }
            finally
            {
                if (counter.Item?.Count > 0)
                    await native.PutItemAsync(new PutItemRequest { TableName = "OasisHolonWriteReceipts", Item = counter.Item });
                else
                    await native.DeleteItemAsync(new DeleteItemRequest { TableName = "OasisHolonWriteReceipts", Key = counterKey });
            }

            var committed = await publisher.ExecuteReplicatingHolonWriteAsync(request, enrollment);
            Assert.IsTrue(!committed.IsError && committed.IsSaved, $"{committed.Message} {committed.Exception}");
            var wrongReplay = publisher.ExecuteReplicatingHolonWrite(request, Enroll(wrongIdentity));
            Assert.AreEqual("COSMIC_SOURCE_STORAGE_BINDING_MISMATCH", wrongReplay.ErrorCode);
            Assert.IsNull(wrongReplay.Result);
            var replay = publisher.ExecuteReplicatingHolonWrite(request, enrollment);
            Assert.IsTrue(!replay.IsError && replay.IsSaved, replay.Message);
            Assert.AreEqual(committed.Result.Single().VersionId, replay.Result.Single().VersionId);

            var rows = await native.ScanAsync(new ScanRequest
            {
                TableName = "OasisHolonWriteReceipts", ConsistentRead = true,
                FilterExpression = "operation_id = :operation",
                ExpressionAttributeValues = new() { [":operation"] = new() { S = write.OperationId.ToString("D") } }
            });
            Assert.IsTrue(rows.LastEvaluatedKey == null || rows.LastEvaluatedKey.Count == 0);
            var delivery = rows.Items.Single();
            Assert.AreEqual("Pending", delivery["state"].S);
            Assert.AreEqual("0", delivery["attempt_count"].N);
            Assert.AreEqual(enrollment.TargetIds.Single(), delivery["target_id"].S);
            var payload = delivery["payload"].S;
            Assert.AreEqual(HolonRecoveryEvidenceJson.Digest(payload), delivery["digest"].S);
            var published = CommittedHolonReplicaPayload.ReadForTarget(payload, enrollment.TargetIds.Single());
            Assert.AreEqual(write.OperationId, published.OperationId);
            Assert.AreEqual(committed.Result.Single().VersionId, published.Roots.Single().VersionId);

            var revision = committed.Result.Single();
            var deletion = new ConditionalHolonWrite(Guid.NewGuid(), actor, source.Result.Authority,
                new[] { new HolonWriteCondition(record.Id, CosmicConcurrency.GetProviderRevisionId(revision, source.Result.Authority)) });
            var removed = await publisher.ExecuteReplicatingHolonWriteAsync(
                new ConditionalHolonWriteRequest(deletion, ConditionalHolonOperation.HardDelete, new[] { revision }, saveChildren: false), enrollment);
            Assert.IsTrue(!removed.IsError && removed.IsDeleted, $"{removed.Message} {removed.Exception}");
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,
                (await _provider.LoadHolonAsync(record.Id, loadChildren: false)).ErrorCode);
            var deletionRows = await native.ScanAsync(new ScanRequest
            {
                TableName = "OasisHolonWriteReceipts", ConsistentRead = true,
                FilterExpression = "operation_id = :operation",
                ExpressionAttributeValues = new() { [":operation"] = new() { S = deletion.OperationId.ToString("D") } }
            });
            Assert.IsTrue(deletionRows.LastEvaluatedKey == null || deletionRows.LastEvaluatedKey.Count == 0);
            var deleteDelivery = deletionRows.Items.Single();
            Assert.IsTrue(string.CompareOrdinal(delivery["id"].S, deleteDelivery["id"].S) < 0);
            var tombstone = CommittedHolonReplicaPayload.ReadForTarget(deleteDelivery["payload"].S, enrollment.TargetIds.Single());
            Assert.AreEqual(ConditionalHolonOperation.HardDelete, tombstone.Operation);
            Assert.AreEqual(revision.VersionId, tombstone.Roots.Single().PreviousVersionId);
            Assert.AreEqual(removed.Result.Single().VersionId, tombstone.Roots.Single().VersionId);

            var opened = ((IBoundHolonReplicationOutboxSource)_provider).GetBoundHolonReplicationOutbox(source.Result);
            Assert.IsFalse(opened.IsError, opened.Message);
            var outbox = opened.Result;
            var routeBinding = enrollment.NativeRoutes.Single();
            var head = await outbox.PeekNextAsync(routeBinding.TargetId);
            Assert.IsFalse(head.IsError, head.Message);
            Assert.AreEqual(write.OperationId, head.Result?.OperationId);
            var inventory = await outbox.ReadRouteHeadsAsync();
            Assert.IsFalse(inventory.IsError, inventory.Message);
            Assert.AreEqual(head.Result.Sequence, inventory.Result.Heads.Single().Sequence);
            var wrongOutbox = ((IBoundHolonReplicationOutboxSource)_provider).GetBoundHolonReplicationOutbox(wrongIdentity);
            Assert.IsFalse(wrongOutbox.IsError, wrongOutbox.Message);
            var wrongHead = await wrongOutbox.Result.PeekNextAsync(routeBinding.TargetId);
            Assert.AreEqual("COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH", wrongHead.ErrorCode);
            Assert.IsNull(wrongHead.Result);

            var replicaTarget = new CommittedHolonReplicaTarget(routeBinding.TargetId, sqlite, target.Result.Authority,
                routeBinding.SourceId, source.Result.Authority, policy, "dynamo-source", Guid.NewGuid(),
                policy.BindingIdentity, target.Result);
            var dispatcher = new HolonOutboxDispatcher(outbox, new[] { replicaTarget }, "dynamo-source");
            var saveDelivery = await dispatcher.DispatchOnceAsync(routeBinding.TargetId, maximumCount: 1);
            Assert.IsFalse(saveDelivery.IsError, saveDelivery.Message);
            Assert.AreEqual(1, saveDelivery.Result.CompletedCount);
            Assert.AreEqual(revision.VersionId, (await sqlite.LoadHolonAsync(record.Id, loadChildren: false)).Result?.VersionId);
            var hardDeleteDelivery = await dispatcher.DispatchOnceAsync(routeBinding.TargetId, maximumCount: 1);
            Assert.IsFalse(hardDeleteDelivery.IsError, hardDeleteDelivery.Message);
            Assert.AreEqual(1, hardDeleteDelivery.Result.CompletedCount);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,
                (await sqlite.LoadHolonAsync(record.Id, loadChildren: false)).ErrorCode);
            var restarted = new HolonOutboxDispatcher(outbox, new[] { replicaTarget }, "dynamo-source-restarted");
            var repeated = await restarted.DispatchOnceAsync(routeBinding.TargetId, maximumCount: 1);
            Assert.IsFalse(repeated.IsError, repeated.Message);
            Assert.AreEqual(0, repeated.Result.CompletedCount);

            var contenders = Enumerable.Range(0, 4).Select(_ =>
            {
                var holon = new Holon { Id = Guid.NewGuid(), Name = "Concurrent DynamoDB publication" };
                var intent = new ConditionalHolonWrite(Guid.NewGuid(), actor, source.Result.Authority,
                    new[] { new HolonWriteCondition(holon.Id, null) });
                return (HolonId: holon.Id, Intent: intent, Request: new ConditionalHolonWriteRequest(
                    intent, ConditionalHolonOperation.Save, new[] { holon }, saveChildren: false));
            }).ToArray();
            var results = await Task.WhenAll(contenders.Select(candidate =>
                publisher.ExecuteReplicatingHolonWriteAsync(candidate.Request, enrollment)));
            Assert.IsTrue(results.Any(result => !result.IsError && result.IsSaved));
            var sequences = new HashSet<string> { delivery["sequence"].N, deleteDelivery["sequence"].N };
            for (var index = 0; index < contenders.Length; index++)
            {
                var candidate = contenders[index];
                var observed = await _provider.LoadHolonAsync(candidate.HolonId, loadChildren: false);
                var obligations = await native.ScanAsync(new ScanRequest
                {
                    TableName = "OasisHolonWriteReceipts", ConsistentRead = true,
                    FilterExpression = "operation_id = :operation",
                    ExpressionAttributeValues = new() { [":operation"] = new() { S = candidate.Intent.OperationId.ToString("D") } }
                });
                Assert.IsTrue(obligations.LastEvaluatedKey == null || obligations.LastEvaluatedKey.Count == 0);
                if (observed.ErrorCode == OASISResultErrorCodes.RecordNotFound)
                {
                    Assert.IsTrue(results[index].IsError);
                    Assert.AreEqual(0, obligations.Items.Count);
                    continue;
                }

                Assert.IsFalse(observed.IsError, observed.Message);
                var obligation = obligations.Items.Single();
                Assert.IsTrue(sequences.Add(obligation["sequence"].N), "Committed deliveries must use distinct sequences.");
                var observedPayload = CommittedHolonReplicaPayload.ReadForTarget(
                    obligation["payload"].S, enrollment.TargetIds.Single());
                Assert.AreEqual(observed.Result.VersionId, observedPayload.Roots.Single().VersionId);
            }
        }
        finally
        {
            await sqlite.DeActivateProviderAsync();
        }
    }
}
