using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public partial class MongoDBOASIS
    {
        private const string SyncOperationsCollection = "HyperDriveSyncOperations";
        private const string SyncEntitiesCollection = "HyperDriveSyncEntities";
        private const string SyncChangesCollection = "HyperDriveSyncChanges";
        private const string SyncDevicesCollection = "HyperDriveSyncDevices";
        private const string SyncCountersCollection = "HyperDriveSyncCounters";
        private const string SyncCheckpointsCollection = "HyperDriveSyncCheckpoints";
        private const string SyncFanOutCollection = "HyperDriveSyncFanOut";
        private const string SyncFanOutLeasesCollection = "HyperDriveSyncFanOutLeases";
        private const string SyncPeerBindingsCollection = "HyperDriveSyncPeerBindings";
        private const string SyncSnapshotsCollection = "HyperDriveSyncSnapshots";
        private const string SyncSnapshotItemsCollection = "HyperDriveSyncSnapshotItems";
        private const string SyncRetentionCollection = "HyperDriveSyncRetention";
        private const string SyncCommandsCollection = "HyperDriveSyncCommands";
        private const string SyncCommandLeasesCollection = "HyperDriveSyncCommandLeases";
        private readonly SemaphoreSlim _syncInitializationLock = new SemaphoreSlim(1, 1);
        private bool _syncInitialized;

        public async Task<OASISResult<HostedSyncOperationBatchResult>> ApplyOperationsAsync(
            Guid authenticatedAvatarId, Guid deviceId, IReadOnlyList<SyncOperation> operations,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<HostedSyncOperationBatchResult>();
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                using (var session = await Database.MongoClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    var operationResults = await session.WithTransactionAsync(async (transactionSession, transactionCancellationToken) =>
                    {
                        var transactionResults = new List<SyncOperationResult>(operations.Count);
                        foreach (var operation in operations)
                            transactionResults.Add(await ApplyOperationAsync(transactionSession, authenticatedAvatarId, deviceId,
                                operation, transactionCancellationToken).ConfigureAwait(false));

                        await InjectHostedSyncFaultAsync(HostedMongoSyncTransactionBoundary.BatchReadyToCommit,
                            transactionCancellationToken).ConfigureAwait(false);
                        return transactionResults;
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);
                    result.Result = new HostedSyncOperationBatchResult { OperationResults = operationResults };
                    result.IsSaved = true;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetSyncError(result, "MONGO_HOSTED_SYNC_APPLY_FAILED", "The hosted sync operation batch was not committed.", ex); }
            return result;
        }

        public async Task<OASISResult<HostedSyncChangeBatch>> ReadChangesAsync(
            Guid authenticatedAvatarId, Guid deviceId, string checkpoint, Guid snapshotId,
            int snapshotPageIndex, int maximumCount, CancellationToken cancellationToken)
        {
            var result = new OASISResult<HostedSyncChangeBatch>();
            if (authenticatedAvatarId == Guid.Empty || deviceId == Guid.Empty || maximumCount <= 0)
            {
                SetSyncError(result, "MONGO_HOSTED_SYNC_PULL_INVALID",
                    "AvatarId, DeviceId and a positive maximum change count are required.", null);
                return result;
            }
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                if (snapshotId != Guid.Empty)
                {
                    var snapshotPage = await ReadSnapshotPageAsync(authenticatedAvatarId, deviceId, snapshotId,
                        snapshotPageIndex, maximumCount, cancellationToken).ConfigureAwait(false);
                    if (snapshotPage.ErrorCode == "MONGO_HOSTED_SNAPSHOT_UNKNOWN")
                        return await CreateSnapshotAsync(authenticatedAvatarId, deviceId, maximumCount,
                            cancellationToken, snapshotId).ConfigureAwait(false);
                    return snapshotPage;
                }
                using (var session = await Database.MongoClient.StartSessionAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false))
                {
                    session.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot,
                        writeConcern: WriteConcern.WMajority));
                    try
                    {
                        long afterSequence;
                        try
                        {
                            afterSequence = await DecodeCheckpointAsync(session, checkpoint, authenticatedAvatarId,
                                cancellationToken).ConfigureAwait(false);
                        }
                        catch (UnknownSyncCheckpointException)
                        {
                            await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                            return await CreateSnapshotAsync(authenticatedAvatarId, deviceId, maximumCount,
                                cancellationToken).ConfigureAwait(false);
                        }
                        var retention = await Database.MongoDB.GetCollection<BsonDocument>(SyncRetentionCollection)
                            .Find(session, Builders<BsonDocument>.Filter.Eq("_id", "global"))
                            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                        long retentionFloor = retention == null ? 0L : retention["floorSequence"].AsInt64;
                        if (afterSequence < retentionFloor)
                        {
                            await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                            return await CreateSnapshotAsync(authenticatedAvatarId, deviceId, maximumCount,
                                cancellationToken).ConfigureAwait(false);
                        }
                        var collection = Database.MongoDB.GetCollection<BsonDocument>(SyncChangesCollection);
                        var visibility = Builders<BsonDocument>.Filter.Eq("avatarId", authenticatedAvatarId.ToString("D")) |
                            Builders<BsonDocument>.Filter.Eq("audience", "global");
                        var filter = visibility &
                            Builders<BsonDocument>.Filter.Gt("sequence", afterSequence);
                        var documents = await collection.Find(session, filter)
                            .Sort(Builders<BsonDocument>.Sort.Ascending("sequence"))
                            .Limit(maximumCount + 1).ToListAsync(cancellationToken).ConfigureAwait(false);
                        bool hasMore = documents.Count > maximumCount;
                        if (hasMore) documents.RemoveAt(documents.Count - 1);
                        long nextSequence;
                        if (hasMore)
                            nextSequence = documents[documents.Count - 1]["sequence"].AsInt64;
                        else
                        {
                            var counter = await Database.MongoDB.GetCollection<BsonDocument>(SyncCountersCollection)
                                .Find(session, Builders<BsonDocument>.Filter.Eq("_id", "changes"))
                                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                            nextSequence = counter == null ? afterSequence : Math.Max(afterSequence, counter["value"].AsInt64);
                        }
                        var deviceUpdate = Builders<BsonDocument>.Update
                            .SetOnInsert("avatarId", authenticatedAvatarId.ToString("D"))
                            .SetOnInsert("deviceId", deviceId.ToString("D"))
                            .SetOnInsert("lastSequence", 0L).Max("lastPullSequence", nextSequence)
                            .Set("lastSeenUtc", DateTime.UtcNow);
                        await Database.MongoDB.GetCollection<BsonDocument>(SyncDevicesCollection)
                            .UpdateOneAsync(session, Builders<BsonDocument>.Filter.Eq("_id", AvatarDeviceKey(authenticatedAvatarId, deviceId)),
                                deviceUpdate, new UpdateOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                        result.Result = new HostedSyncChangeBatch
                        {
                            Changes = documents.Select(ToRemoteChange).ToArray(),
                            NextCheckpoint = await EncodeCheckpointAsync(session, authenticatedAvatarId, nextSequence,
                                cancellationToken).ConfigureAwait(false), HasMoreChanges = hasMore
                        };
                        await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                        result.IsLoaded = true;
                    }
                    catch
                    {
                        if (session.IsInTransaction) await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                        throw;
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetSyncError(result, "MONGO_HOSTED_SYNC_PULL_FAILED", "The hosted change feed could not be read.", ex); }
            return result;
        }

        private async Task<OASISResult<HostedSyncChangeBatch>> CreateSnapshotAsync(Guid avatarId,
            Guid deviceId, int maximumCount, CancellationToken cancellationToken,
            Guid replacesSnapshotId = default)
        {
            Guid snapshotId = Guid.NewGuid();
            DateTime expiresUtc = DateTime.UtcNow.AddHours(24);
            using (var session = await Database.MongoClient.StartSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                session.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot,
                    writeConcern: WriteConcern.WMajority));
                try
                {
                    var counter = await Database.MongoDB.GetCollection<BsonDocument>(SyncCountersCollection)
                        .Find(session, Builders<BsonDocument>.Filter.Eq("_id", "changes"))
                        .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                    long watermark = counter == null ? 0L : counter["value"].AsInt64;
                    string checkpoint = await EncodeCheckpointAsync(session, avatarId, watermark, cancellationToken)
                        .ConfigureAwait(false);
                    var visibility = Builders<BsonDocument>.Filter.Eq("avatarId", avatarId.ToString("D")) |
                        Builders<BsonDocument>.Filter.Eq("audience", "global");
                    var entities = await Database.MongoDB.GetCollection<BsonDocument>(SyncEntitiesCollection)
                        .Find(session, visibility)
                        .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
                        .ToListAsync(cancellationToken).ConfigureAwait(false);
                    var header = new BsonDocument
                    {
                        { "_id", snapshotId.ToString("D") }, { "snapshotId", snapshotId.ToString("D") },
                        { "avatarId", avatarId.ToString("D") }, { "deviceId", deviceId.ToString("D") },
                        { "watermark", watermark }, { "checkpoint", checkpoint }, { "pageSize", maximumCount },
                        { "itemCount", entities.Count }, { "createdUtc", DateTime.UtcNow }, { "expiresUtc", expiresUtc }
                    };
                    await Database.MongoDB.GetCollection<BsonDocument>(SyncSnapshotsCollection)
                        .InsertOneAsync(session, header, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (entities.Count != 0)
                    {
                        var items = entities.Select((entity, index) => new BsonDocument
                        {
                            { "_id", $"{snapshotId:D}:{index:D12}" }, { "snapshotId", snapshotId.ToString("D") },
                            { "ordinal", index }, { "avatarId", avatarId.ToString("D") },
                            { "entityType", entity["entityType"] }, { "entityId", entity["entityId"] },
                            { "kind", entity["isDeleted"].AsBoolean ? (int)SyncOperationKind.Delete : (int)SyncOperationKind.Upsert },
                            { "versionId", entity["versionId"] }, { "previousVersionId", Guid.Empty.ToString("D") },
                            { "payloadJson", entity["payloadJson"] }, { "changedUtc", entity["changedUtc"] },
                            { "changeId", $"snapshot:{snapshotId:D}:{index}" }, { "expiresUtc", expiresUtc }
                        }).ToList();
                        await Database.MongoDB.GetCollection<BsonDocument>(SyncSnapshotItemsCollection)
                            .InsertManyAsync(session, items, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    if (session.IsInTransaction) await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }
            var page = await ReadSnapshotPageAsync(avatarId, deviceId, snapshotId, 0, maximumCount,
                cancellationToken).ConfigureAwait(false);
            if (!page.IsError && page.Result != null)
                page.Result.ReplacesSnapshotId = replacesSnapshotId;
            return page;
        }

        private async Task<OASISResult<HostedSyncChangeBatch>> ReadSnapshotPageAsync(Guid avatarId,
            Guid deviceId, Guid snapshotId, int pageIndex, int maximumCount, CancellationToken cancellationToken)
        {
            var result = new OASISResult<HostedSyncChangeBatch>();
            if (pageIndex < 0)
            {
                SetSyncError(result, "MONGO_HOSTED_SNAPSHOT_PAGE_INVALID", "Snapshot page index cannot be negative.", null);
                return result;
            }
            var header = await Database.MongoDB.GetCollection<BsonDocument>(SyncSnapshotsCollection)
                .Find(Builders<BsonDocument>.Filter.Eq("_id", snapshotId.ToString("D")) &
                      Builders<BsonDocument>.Filter.Eq("avatarId", avatarId.ToString("D")) &
                      Builders<BsonDocument>.Filter.Eq("deviceId", deviceId.ToString("D")) &
                      Builders<BsonDocument>.Filter.Gt("expiresUtc", DateTime.UtcNow))
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (header == null)
            {
                SetSyncError(result, "MONGO_HOSTED_SNAPSHOT_UNKNOWN",
                    "The snapshot is missing, expired, or belongs to another avatar/device.", null);
                return result;
            }
            int pageSize = header["pageSize"].AsInt32;
            if (pageSize <= 0)
            {
                SetSyncError(result, "MONGO_HOSTED_SNAPSHOT_PAGE_SIZE_INVALID",
                    "The materialized snapshot has an invalid page size.", null);
                return result;
            }
            int itemCount = header["itemCount"].AsInt32;
            HostedHyperDriveSnapshotPageWindow window;
            try
            {
                window = HostedHyperDriveSnapshotPaging.GetWindow(itemCount, pageSize, pageIndex);
            }
            catch (ArgumentOutOfRangeException)
            {
                SetSyncError(result, "MONGO_HOSTED_SNAPSHOT_PAGE_OUT_OF_RANGE",
                    "The requested snapshot page is outside the materialized snapshot.", null);
                return result;
            }
            long offset = window.Offset;
            var documents = await Database.MongoDB.GetCollection<BsonDocument>(SyncSnapshotItemsCollection)
                .Find(Builders<BsonDocument>.Filter.Eq("snapshotId", snapshotId.ToString("D")) &
                      Builders<BsonDocument>.Filter.Gte("ordinal", offset))
                .Sort(Builders<BsonDocument>.Sort.Ascending("ordinal")).Limit(pageSize)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            bool complete = HostedHyperDriveSnapshotPaging.IsComplete(itemCount, offset, documents.Count);
            if (complete)
            {
                var deviceUpdate = Builders<BsonDocument>.Update
                    .SetOnInsert("avatarId", avatarId.ToString("D"))
                    .SetOnInsert("deviceId", deviceId.ToString("D"))
                    .SetOnInsert("lastSequence", 0L)
                    .Max("lastPullSequence", header["watermark"].AsInt64)
                    .Set("lastSeenUtc", DateTime.UtcNow);
                await Database.MongoDB.GetCollection<BsonDocument>(SyncDevicesCollection)
                    .UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", AvatarDeviceKey(avatarId, deviceId)),
                        deviceUpdate, new UpdateOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
            }
            result.Result = new HostedSyncChangeBatch
            {
                Changes = documents.Select(ToRemoteChange).ToArray(),
                NextCheckpoint = header["checkpoint"].AsString,
                HasMoreChanges = !complete,
                IsAuthoritativeSnapshot = true,
                SnapshotId = snapshotId,
                SnapshotPageIndex = pageIndex,
                SnapshotComplete = complete
            };
            result.IsLoaded = true;
            return result;
        }

        public async Task<OASISResult<bool>> BindPeerAsync(Guid authenticatedAvatarId, Guid deviceId,
            string nodeId, string publicKey, CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            if (authenticatedAvatarId == Guid.Empty || deviceId == Guid.Empty || string.IsNullOrWhiteSpace(nodeId) ||
                string.IsNullOrWhiteSpace(publicKey))
            {
                SetSyncError(result, "MONGO_PEER_BINDING_INVALID", "Avatar, device, node and public key are required.", null);
                return result;
            }

            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                var bindings = Database.MongoDB.GetCollection<BsonDocument>(SyncPeerBindingsCollection);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", nodeId) &
                    Builders<BsonDocument>.Filter.Eq("avatarId", authenticatedAvatarId.ToString("D"));
                var update = Builders<BsonDocument>.Update
                    .SetOnInsert("avatarId", authenticatedAvatarId.ToString("D"))
                    .Set("deviceId", deviceId.ToString("D"))
                    .Set("publicKey", publicKey)
                    .Set("updatedUtc", DateTime.UtcNow);
                await bindings.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken)
                    .ConfigureAwait(false);
                result.Result = true;
                result.IsSaved = true;
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                SetSyncError(result, "MONGO_PEER_ALREADY_BOUND", "The ONET node identity is already bound to another avatar.", ex);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                SetSyncError(result, "MONGO_PEER_BINDING_FAILED", "The ONET peer binding was not persisted.", ex);
            }
            return result;
        }

        public async Task<OASISResult<Guid>> ResolvePeerAvatarAsync(string nodeId, CancellationToken cancellationToken)
        {
            var result = new OASISResult<Guid>();
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                SetSyncError(result, "MONGO_PEER_ID_REQUIRED", "An ONET node identifier is required.", null);
                return result;
            }
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                var document = await Database.MongoDB.GetCollection<BsonDocument>(SyncPeerBindingsCollection)
                    .Find(Builders<BsonDocument>.Filter.Eq("_id", nodeId)).FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (document == null)
                {
                    SetSyncError(result, "MONGO_PEER_NOT_BOUND", "The ONET node identity is not bound to an avatar.", null);
                    return result;
                }
                result.Result = Guid.Parse(document["avatarId"].AsString);
                result.IsLoaded = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                SetSyncError(result, "MONGO_PEER_RESOLUTION_FAILED", "The ONET peer binding could not be resolved.", ex);
            }
            return result;
        }

        private async Task<SyncOperationResult> ApplyOperationAsync(IClientSessionHandle session, Guid avatarId,
            Guid deviceId, SyncOperation operation, CancellationToken cancellationToken)
        {
            var operations = Database.MongoDB.GetCollection<BsonDocument>(SyncOperationsCollection);
            string operationId = operation.OperationId.ToString("D");
            var existing = await operations.Find(session, Builders<BsonDocument>.Filter.Eq("_id", operationId))
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (existing != null)
            {
                EnsureOperationReplayMatches(existing, avatarId, deviceId, operation);
                return OperationResultFromDocument(existing, SyncOperationDisposition.AlreadyApplied);
            }

            var devices = Database.MongoDB.GetCollection<BsonDocument>(SyncDevicesCollection);
            string deviceKey = AvatarDeviceKey(avatarId, deviceId);
            var device = await devices.Find(session, Builders<BsonDocument>.Filter.Eq("_id", deviceKey))
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            long lastSequence = device == null ? 0 : device["lastSequence"].AsInt64;
            if (operation.DeviceSequence != lastSequence + 1)
                throw new InvalidOperationException(
                    $"Device sequence gap for '{deviceKey}': expected {lastSequence + 1} but received {operation.DeviceSequence}. The batch was not committed.");

            if (operation.Kind == SyncOperationKind.Command)
            {
                if (operation.EntityType != HyperDriveEntityTypes.QuestProgress &&
                    operation.EntityType != HyperDriveEntityTypes.InventoryItem &&
                    operation.EntityType != HyperDriveEntityTypes.GeoNftCollection)
                    return await RecordTerminalOperationAsync(session, operation, SyncOperationDisposition.Rejected,
                        "COMMAND_TYPE_NOT_SUPPORTED", $"No hosted command handler is registered for '{operation.EntityType}'.",
                        true, cancellationToken).ConfigureAwait(false);

                DateTime queuedUtc = DateTime.UtcNow;
                long commandSequence = await NextCounterValueAsync(session, "command-sequence", cancellationToken)
                    .ConfigureAwait(false);
                await Database.MongoDB.GetCollection<BsonDocument>(SyncCommandsCollection)
                    .InsertOneAsync(session, new BsonDocument
                    {
                        { "_id", operationId }, { "operationId", operationId },
                        { "avatarId", avatarId.ToString("D") }, { "deviceId", deviceId.ToString("D") },
                        { "deviceSequence", operation.DeviceSequence }, { "commandSequence", commandSequence },
                        { "entityId", operation.EntityId.ToString("D") }, { "entityType", operation.EntityType },
                        { "versionId", operation.VersionId.ToString("D") }, { "payloadJson", operation.PayloadJson },
                        { "status", "pending" }, { "attemptCount", 0 }, { "queuedUtc", queuedUtc },
                        { "nextAttemptUtc", queuedUtc }, { "leaseOwner", BsonNull.Value },
                        { "leaseUntilUtc", BsonNull.Value }, { "completedUtc", BsonNull.Value },
                        { "lastErrorCode", BsonNull.Value }, { "lastErrorMessage", BsonNull.Value }
                    }, cancellationToken: cancellationToken).ConfigureAwait(false);
                return await RecordTerminalOperationAsync(session, operation, SyncOperationDisposition.Accepted,
                    "COMMAND_QUEUED", "The domain command was durably queued for ordered execution.", true,
                    cancellationToken, operation.VersionId).ConfigureAwait(false);
            }

            if (operation.EntityType == HyperDriveEntityTypes.Avatar ||
                operation.EntityType == HyperDriveEntityTypes.AvatarDetail ||
                operation.EntityType == HyperDriveEntityTypes.QuestProgress)
                return await RecordTerminalOperationAsync(session, operation, SyncOperationDisposition.Rejected,
                    "HOSTED_DOMAIN_SERVER_AUTHORITATIVE",
                    $"'{operation.EntityType}' is a server-authoritative projection and cannot be overwritten by an Edge entity mutation.",
                    true, cancellationToken).ConfigureAwait(false);

            if (HyperDriveEntityTypes.IsHostedDomainType(operation.EntityType))
            {
                var domain = await ApplyDomainMutationWithReceiptInTransactionAsync(session, operation, cancellationToken)
                    .ConfigureAwait(false);
                if (domain.IsError)
                    return await RecordTerminalOperationAsync(session, operation, SyncOperationDisposition.Rejected,
                        domain.ErrorCode, domain.Message, true, cancellationToken).ConfigureAwait(false);
                await InjectHostedSyncFaultAsync(HostedMongoSyncTransactionBoundary.DomainMutationWritten,
                    cancellationToken).ConfigureAwait(false);
            }

            var entities = Database.MongoDB.GetCollection<BsonDocument>(SyncEntitiesCollection);
            string entityKey = AvatarEntityKey(avatarId, operation.EntityType, operation.EntityId);
            var entity = await entities.Find(session, Builders<BsonDocument>.Filter.Eq("_id", entityKey))
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            Guid currentVersionId = entity == null ? Guid.Empty : Guid.Parse(entity["versionId"].AsString);
            if (operation.BaseVersionId != currentVersionId)
                return await RecordTerminalOperationAsync(session, operation, SyncOperationDisposition.Conflict,
                    "BASE_VERSION_CONFLICT", "The entity changed after the submitted base version.", true, cancellationToken, currentVersionId).ConfigureAwait(false);

            Guid versionId = operation.VersionId;
            long changeSequence = await NextChangeSequenceAsync(session, cancellationToken).ConfigureAwait(false);
            DateTime changedUtc = DateTime.UtcNow;
            var entityDocument = new BsonDocument
            {
                { "_id", entityKey }, { "avatarId", avatarId.ToString("D") },
                { "audience", SyncAudienceForEntityType(operation.EntityType) },
                { "entityType", operation.EntityType }, { "entityId", operation.EntityId.ToString("D") },
                { "versionId", versionId.ToString("D") }, { "isDeleted", operation.Kind == SyncOperationKind.Delete },
                { "payloadJson", operation.Kind == SyncOperationKind.Delete || operation.PayloadJson == null ? BsonNull.Value : operation.PayloadJson },
                { "changedUtc", changedUtc }
            };
            await entities.ReplaceOneAsync(session, Builders<BsonDocument>.Filter.Eq("_id", entityKey), entityDocument,
                new ReplaceOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
            await InjectHostedSyncFaultAsync(HostedMongoSyncTransactionBoundary.EntityWritten, cancellationToken)
                .ConfigureAwait(false);

            var change = new BsonDocument
            {
                { "_id", changeSequence }, { "sequence", changeSequence }, { "changeId", changeSequence.ToString() },
                { "avatarId", avatarId.ToString("D") }, { "entityType", operation.EntityType },
                { "audience", SyncAudienceForEntityType(operation.EntityType) },
                { "entityId", operation.EntityId.ToString("D") }, { "kind", (int)operation.Kind },
                { "versionId", versionId.ToString("D") }, { "previousVersionId", currentVersionId.ToString("D") },
                { "payloadJson", operation.PayloadJson == null ? BsonNull.Value : operation.PayloadJson }, { "changedUtc", changedUtc }
            };
            await Database.MongoDB.GetCollection<BsonDocument>(SyncChangesCollection)
                .InsertOneAsync(session, change, cancellationToken: cancellationToken).ConfigureAwait(false);
            await InjectHostedSyncFaultAsync(HostedMongoSyncTransactionBoundary.ChangeFeedWritten, cancellationToken)
                .ConfigureAwait(false);

            var fanOut = new BsonDocument
            {
                { "_id", operation.OperationId.ToString("D") }, { "avatarId", avatarId.ToString("D") },
                { "sequence", changeSequence },
                { "entityId", operation.EntityId.ToString("D") }, { "entityType", operation.EntityType },
                { "kind", (int)operation.Kind }, { "versionId", versionId.ToString("D") },
                { "payloadJson", operation.PayloadJson == null ? BsonNull.Value : operation.PayloadJson },
                { "createdUtc", changedUtc }, { "attemptCount", 0 }, { "nextAttemptUtc", changedUtc },
                { "leaseOwner", BsonNull.Value }, { "leaseUntilUtc", BsonNull.Value },
                { "completedUtc", BsonNull.Value }, { "lastErrorCode", BsonNull.Value },
                { "lastErrorMessage", BsonNull.Value }
            };
            await Database.MongoDB.GetCollection<BsonDocument>(SyncFanOutCollection)
                .InsertOneAsync(session, fanOut, cancellationToken: cancellationToken).ConfigureAwait(false);
            await InjectHostedSyncFaultAsync(HostedMongoSyncTransactionBoundary.FanOutWritten, cancellationToken)
                .ConfigureAwait(false);

            var accepted = await RecordTerminalOperationAsync(session, operation, SyncOperationDisposition.Accepted,
                null, null, true, cancellationToken, versionId).ConfigureAwait(false);
            return accepted;
        }

        private async Task<SyncOperationResult> RecordTerminalOperationAsync(IClientSessionHandle session,
            SyncOperation operation, SyncOperationDisposition disposition, string code, string message,
            bool advanceDeviceSequence, CancellationToken cancellationToken, Guid resultVersionId = default)
        {
            var document = new BsonDocument
            {
                { "_id", operation.OperationId.ToString("D") }, { "avatarId", operation.AvatarId.ToString("D") },
                { "deviceId", operation.DeviceId.ToString("D") }, { "deviceSequence", operation.DeviceSequence },
                { "entityId", operation.EntityId.ToString("D") }, { "entityType", operation.EntityType },
                { "kind", (int)operation.Kind }, { "baseVersionId", operation.BaseVersionId.ToString("D") },
                { "versionId", operation.VersionId.ToString("D") },
                { "payloadJson", operation.PayloadJson == null ? BsonNull.Value : operation.PayloadJson },
                { "disposition", (int)disposition }, { "resultVersionId", resultVersionId.ToString("D") },
                { "code", code == null ? BsonNull.Value : code }, { "message", message == null ? BsonNull.Value : message },
                { "completedUtc", DateTime.UtcNow }
            };
            await Database.MongoDB.GetCollection<BsonDocument>(SyncOperationsCollection)
                .InsertOneAsync(session, document, cancellationToken: cancellationToken).ConfigureAwait(false);
            await InjectHostedSyncFaultAsync(HostedMongoSyncTransactionBoundary.OperationResultWritten, cancellationToken)
                .ConfigureAwait(false);
            if (advanceDeviceSequence)
            {
                string key = AvatarDeviceKey(operation.AvatarId, operation.DeviceId);
                var update = Builders<BsonDocument>.Update
                    .Set("avatarId", operation.AvatarId.ToString("D"))
                    .Set("deviceId", operation.DeviceId.ToString("D"))
                    .Set("lastSequence", operation.DeviceSequence)
                    .Set("lastSeenUtc", DateTime.UtcNow);
                await Database.MongoDB.GetCollection<BsonDocument>(SyncDevicesCollection)
                    .UpdateOneAsync(session, Builders<BsonDocument>.Filter.Eq("_id", key), update,
                        new UpdateOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                await InjectHostedSyncFaultAsync(HostedMongoSyncTransactionBoundary.DeviceSequenceWritten,
                    cancellationToken).ConfigureAwait(false);
            }
            return new SyncOperationResult { OperationId = operation.OperationId, Disposition = disposition,
                ResultVersionId = resultVersionId, Code = code, Message = message };
        }

        private async Task<long> NextChangeSequenceAsync(IClientSessionHandle session, CancellationToken cancellationToken)
            => await NextCounterValueAsync(session, "changes", cancellationToken).ConfigureAwait(false);

        private async Task<long> NextCounterValueAsync(IClientSessionHandle session, string counterId,
            CancellationToken cancellationToken)
        {
            var options = new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
            var counter = await Database.MongoDB.GetCollection<BsonDocument>(SyncCountersCollection)
                .FindOneAndUpdateAsync(session, Builders<BsonDocument>.Filter.Eq("_id", counterId),
                    Builders<BsonDocument>.Update.Inc("value", 1L), options, cancellationToken).ConfigureAwait(false);
            return counter["value"].AsInt64;
        }

        private Task InjectHostedSyncFaultAsync(HostedMongoSyncTransactionBoundary boundary,
            CancellationToken cancellationToken) => _hostedSyncFaultInjector == null
                ? Task.CompletedTask
                : _hostedSyncFaultInjector.OnBoundaryAsync(boundary, cancellationToken);

        private async Task EnsureSyncInitializedAsync(CancellationToken cancellationToken)
        {
            if (Database == null || !IsProviderActivated)
            {
                var activation = await ActivateProviderAsync().ConfigureAwait(false);
                if (activation.IsError || !activation.Result) throw new InvalidOperationException(activation.Message ?? "MongoDBOASIS activation failed.");
            }
            if (_syncInitialized) return;
            await _syncInitializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_syncInitialized) return;
                var operations = Database.MongoDB.GetCollection<BsonDocument>(SyncOperationsCollection);
                await operations.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("avatarId").Ascending("deviceId").Ascending("deviceSequence"),
                    new CreateIndexOptions { Unique = true }), cancellationToken: cancellationToken).ConfigureAwait(false);
                await operations.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("versionId"),
                    new CreateIndexOptions<BsonDocument>
                    {
                        Unique = true,
                        PartialFilterExpression = Builders<BsonDocument>.Filter.Exists("versionId", true)
                    }), cancellationToken: cancellationToken).ConfigureAwait(false);
                var changes = Database.MongoDB.GetCollection<BsonDocument>(SyncChangesCollection);
                await changes.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("avatarId").Ascending("sequence")),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                await Database.MongoDB.GetCollection<BsonDocument>(SyncCommandsCollection).Indexes.CreateOneAsync(
                    new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys
                        .Ascending("status").Ascending("nextAttemptUtc").Ascending("commandSequence")),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                await changes.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("audience").Ascending("sequence")),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                var checkpoints = Database.MongoDB.GetCollection<BsonDocument>(SyncCheckpointsCollection);
                await checkpoints.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("token"), new CreateIndexOptions { Unique = true }),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                var fanOut = Database.MongoDB.GetCollection<BsonDocument>(SyncFanOutCollection);
                await fanOut.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("completedUtc").Ascending("sequence")),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                var snapshots = Database.MongoDB.GetCollection<BsonDocument>(SyncSnapshotsCollection);
                await snapshots.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("expiresUtc"),
                    new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                var snapshotItems = Database.MongoDB.GetCollection<BsonDocument>(SyncSnapshotItemsCollection);
                await snapshotItems.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("snapshotId").Ascending("ordinal"),
                    new CreateIndexOptions { Unique = true }), cancellationToken: cancellationToken).ConfigureAwait(false);
                await snapshotItems.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("expiresUtc"),
                    new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                await Database.MongoDB.GetCollection<BsonDocument>(SyncFanOutLeasesCollection)
                    .UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", "ordered-dispatcher"),
                        Builders<BsonDocument>.Update.SetOnInsert("leaseOwner", BsonNull.Value)
                            .SetOnInsert("leaseUntilUtc", BsonNull.Value),
                        new UpdateOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                await Database.MongoDB.GetCollection<BsonDocument>(SyncCommandLeasesCollection)
                    .UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", "ordered-executor"),
                        Builders<BsonDocument>.Update.SetOnInsert("leaseOwner", BsonNull.Value)
                            .SetOnInsert("leaseUntilUtc", BsonNull.Value),
                        new UpdateOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                _syncInitialized = true;
            }
            finally { _syncInitializationLock.Release(); }
        }

        private static SyncOperationResult OperationResultFromDocument(BsonDocument document, SyncOperationDisposition replayDisposition) => new SyncOperationResult
        {
            OperationId = Guid.Parse(document["_id"].AsString),
            Disposition = replayDisposition,
            ResultVersionId = Guid.Parse(document["resultVersionId"].AsString),
            Code = document["code"].IsBsonNull ? null : document["code"].AsString,
            Message = document["message"].IsBsonNull ? null : document["message"].AsString
        };

        private static void EnsureOperationReplayMatches(BsonDocument document, Guid avatarId, Guid deviceId,
            SyncOperation operation)
        {
            bool payloadMatches = document.TryGetValue("payloadJson", out var persistedPayload) &&
                ((persistedPayload.IsBsonNull && operation.PayloadJson == null) ||
                 (persistedPayload.IsString && string.Equals(persistedPayload.AsString, operation.PayloadJson, StringComparison.Ordinal)));
            bool matches = document.TryGetValue("avatarId", out var persistedAvatar) &&
                persistedAvatar.AsString == avatarId.ToString("D") &&
                document.TryGetValue("deviceId", out var persistedDevice) &&
                persistedDevice.AsString == deviceId.ToString("D") &&
                document.TryGetValue("deviceSequence", out var persistedSequence) &&
                persistedSequence.AsInt64 == operation.DeviceSequence &&
                document.TryGetValue("entityId", out var persistedEntity) &&
                persistedEntity.AsString == operation.EntityId.ToString("D") &&
                document.TryGetValue("entityType", out var persistedEntityType) &&
                string.Equals(persistedEntityType.AsString, operation.EntityType, StringComparison.Ordinal) &&
                document.TryGetValue("kind", out var persistedKind) && persistedKind.AsInt32 == (int)operation.Kind &&
                document.TryGetValue("baseVersionId", out var persistedBaseVersion) &&
                persistedBaseVersion.AsString == operation.BaseVersionId.ToString("D") &&
                document.TryGetValue("versionId", out var persistedVersion) &&
                persistedVersion.AsString == operation.VersionId.ToString("D") && payloadMatches;
            if (!matches)
                throw new InvalidOperationException(
                    $"Operation id '{operation.OperationId:D}' was replayed with different immutable content.");
        }

        private static SyncRemoteChange ToRemoteChange(BsonDocument document) => new SyncRemoteChange
        {
            ChangeId = document["changeId"].AsString,
            EntityId = Guid.Parse(document["entityId"].AsString), EntityType = document["entityType"].AsString,
            Kind = (SyncOperationKind)document["kind"].AsInt32,
            VersionId = Guid.Parse(document["versionId"].AsString),
            PreviousVersionId = Guid.Parse(document["previousVersionId"].AsString),
            PayloadJson = document["payloadJson"].IsBsonNull ? null : document["payloadJson"].AsString,
            ChangedUtc = document["changedUtc"].ToUniversalTime()
        };

        private static string AvatarDeviceKey(Guid avatarId, Guid deviceId) => $"{avatarId:D}:{deviceId:D}";
        private static string AvatarEntityKey(Guid avatarId, string entityType, Guid entityId) => $"{avatarId:D}:{entityType}:{entityId:D}";
        private async Task<string> EncodeCheckpointAsync(Guid avatarId, long sequence,
            CancellationToken cancellationToken)
        {
            string key = $"{avatarId:D}:{sequence}";
            var update = Builders<BsonDocument>.Update
                .SetOnInsert("avatarId", avatarId.ToString("D"))
                .SetOnInsert("sequence", sequence)
                .SetOnInsert("token", Guid.NewGuid().ToString("N"))
                .SetOnInsert("createdUtc", DateTime.UtcNow);
            var document = await Database.MongoDB.GetCollection<BsonDocument>(SyncCheckpointsCollection)
                .FindOneAndUpdateAsync(Builders<BsonDocument>.Filter.Eq("_id", key), update,
                    new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After },
                    cancellationToken).ConfigureAwait(false);
            return document["token"].AsString;
        }

        private async Task<string> EncodeCheckpointAsync(IClientSessionHandle session, Guid avatarId,
            long sequence, CancellationToken cancellationToken)
        {
            string key = $"{avatarId:D}:{sequence}";
            var update = Builders<BsonDocument>.Update
                .SetOnInsert("avatarId", avatarId.ToString("D"))
                .SetOnInsert("sequence", sequence)
                .SetOnInsert("token", Guid.NewGuid().ToString("N"))
                .SetOnInsert("createdUtc", DateTime.UtcNow);
            var document = await Database.MongoDB.GetCollection<BsonDocument>(SyncCheckpointsCollection)
                .FindOneAndUpdateAsync(session, Builders<BsonDocument>.Filter.Eq("_id", key), update,
                    new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After },
                    cancellationToken).ConfigureAwait(false);
            return document["token"].AsString;
        }

        private async Task<long> DecodeCheckpointAsync(string checkpoint, Guid avatarId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(checkpoint)) return 0;
            if (!Guid.TryParseExact(checkpoint, "N", out _))
                throw new ArgumentException("The sync checkpoint is malformed.", nameof(checkpoint));
            var filter = Builders<BsonDocument>.Filter.Eq("token", checkpoint) &
                Builders<BsonDocument>.Filter.Eq("avatarId", avatarId.ToString("D"));
            var document = await Database.MongoDB.GetCollection<BsonDocument>(SyncCheckpointsCollection)
                .Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (document == null)
                throw new UnknownSyncCheckpointException();
            long sequence = document["sequence"].AsInt64;
            if (sequence < 0) throw new InvalidOperationException("The persisted sync checkpoint is invalid.");
            return sequence;
        }

        private async Task<long> DecodeCheckpointAsync(IClientSessionHandle session, string checkpoint,
            Guid avatarId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(checkpoint)) return 0;
            if (!Guid.TryParseExact(checkpoint, "N", out _))
                throw new ArgumentException("The sync checkpoint is malformed.", nameof(checkpoint));
            var filter = Builders<BsonDocument>.Filter.Eq("token", checkpoint) &
                Builders<BsonDocument>.Filter.Eq("avatarId", avatarId.ToString("D"));
            var document = await Database.MongoDB.GetCollection<BsonDocument>(SyncCheckpointsCollection)
                .Find(session, filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (document == null) throw new UnknownSyncCheckpointException();
            long sequence = document["sequence"].AsInt64;
            if (sequence < 0) throw new InvalidOperationException("The persisted sync checkpoint is invalid.");
            return sequence;
        }

        private sealed class UnknownSyncCheckpointException : Exception
        {
            public UnknownSyncCheckpointException()
                : base("The sync checkpoint is unknown or belongs to another avatar.") { }
        }

        public async Task<OASISResult<HostedSyncCompactionResult>> CompactSyncHistoryAsync(
            DateTime inactiveDeviceCutoffUtc, CancellationToken cancellationToken)
        {
            var result = new OASISResult<HostedSyncCompactionResult>();
            if (inactiveDeviceCutoffUtc.Kind != DateTimeKind.Utc || inactiveDeviceCutoffUtc >= DateTime.UtcNow)
            {
                SetSyncError(result, "MONGO_SYNC_COMPACTION_CUTOFF_INVALID",
                    "The inactive-device cutoff must be a UTC time in the past.", null);
                return result;
            }
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                using var session = await Database.MongoClient.StartSessionAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                session.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot,
                    writeConcern: WriteConcern.WMajority));
                try
                {
                    var activeDevices = await Database.MongoDB.GetCollection<BsonDocument>(SyncDevicesCollection)
                        .Find(session, Builders<BsonDocument>.Filter.Gte("lastSeenUtc", inactiveDeviceCutoffUtc))
                        .ToListAsync(cancellationToken).ConfigureAwait(false);
                    var counter = await Database.MongoDB.GetCollection<BsonDocument>(SyncCountersCollection)
                        .Find(session, Builders<BsonDocument>.Filter.Eq("_id", "changes"))
                        .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                    long currentSequence = counter == null ? 0L : counter["value"].AsInt64;
                    long safeSequence = HostedHyperDriveCompactionPolicy.CalculateSafeSequence(currentSequence,
                        activeDevices.Select(device => device.TryGetValue("lastPullSequence", out var value) &&
                            value.IsInt64 ? value.AsInt64 : 0L));
                    await Database.MongoDB.GetCollection<BsonDocument>(SyncRetentionCollection)
                        .UpdateOneAsync(session, Builders<BsonDocument>.Filter.Eq("_id", "global"),
                            Builders<BsonDocument>.Update.Max("floorSequence", safeSequence)
                                .Set("updatedUtc", DateTime.UtcNow),
                            new UpdateOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                    var deletedChanges = await Database.MongoDB.GetCollection<BsonDocument>(SyncChangesCollection)
                        .DeleteManyAsync(session, Builders<BsonDocument>.Filter.Lte("sequence", safeSequence),
                            new DeleteOptions(), cancellationToken).ConfigureAwait(false);
                    var deletedCheckpoints = await Database.MongoDB.GetCollection<BsonDocument>(SyncCheckpointsCollection)
                        .DeleteManyAsync(session, Builders<BsonDocument>.Filter.Lt("sequence", safeSequence),
                            new DeleteOptions(), cancellationToken).ConfigureAwait(false);
                    var expiredHeaders = await Database.MongoDB.GetCollection<BsonDocument>(SyncSnapshotsCollection)
                        .Find(session, Builders<BsonDocument>.Filter.Lt("expiresUtc", DateTime.UtcNow))
                        .Project(Builders<BsonDocument>.Projection.Include("snapshotId"))
                        .ToListAsync(cancellationToken).ConfigureAwait(false);
                    var expiredIds = expiredHeaders.Select(x => x["snapshotId"].AsString).ToArray();
                    long deletedSnapshots = 0;
                    if (expiredIds.Length != 0)
                    {
                        var deletedItems = await Database.MongoDB.GetCollection<BsonDocument>(SyncSnapshotItemsCollection)
                            .DeleteManyAsync(session, Builders<BsonDocument>.Filter.In("snapshotId", expiredIds),
                                new DeleteOptions(), cancellationToken).ConfigureAwait(false);
                        var deletedHeaders = await Database.MongoDB.GetCollection<BsonDocument>(SyncSnapshotsCollection)
                            .DeleteManyAsync(session, Builders<BsonDocument>.Filter.In("snapshotId", expiredIds),
                                new DeleteOptions(), cancellationToken).ConfigureAwait(false);
                        deletedSnapshots = deletedHeaders.DeletedCount + deletedItems.DeletedCount;
                    }
                    await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                    result.Result = new HostedSyncCompactionResult
                    {
                        SafeSequence = safeSequence, DeletedChangeCount = deletedChanges.DeletedCount,
                        DeletedCheckpointCount = deletedCheckpoints.DeletedCount,
                        DeletedSnapshotCount = deletedSnapshots
                    };
                    result.IsSaved = true;
                }
                catch
                {
                    if (session.IsInTransaction) await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                SetSyncError(result, "MONGO_SYNC_COMPACTION_FAILED",
                    "Hosted synchronization history was not compacted.", ex);
            }
            return result;
        }

        public async Task<OASISResult<HostedFanOutClaim>> ClaimFanOutAsync(
            string workerId, int maximumCount, DateTime leaseUntilUtc, CancellationToken cancellationToken)
        {
            var result = new OASISResult<HostedFanOutClaim>();
            bool globalLeaseAcquired = false;
            if (string.IsNullOrWhiteSpace(workerId) || maximumCount <= 0 || leaseUntilUtc <= DateTime.UtcNow)
            {
                SetSyncError(result, "MONGO_FANOUT_CLAIM_INVALID", "A worker id, positive batch size and future lease expiry are required.", null);
                return result;
            }
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                DateTime now = DateTime.UtcNow;
                var leaseFilter = Builders<BsonDocument>.Filter.Eq("_id", "ordered-dispatcher") &
                    (Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId) |
                     Builders<BsonDocument>.Filter.Eq("leaseUntilUtc", BsonNull.Value) |
                     Builders<BsonDocument>.Filter.Lte("leaseUntilUtc", now));
                var lease = await Database.MongoDB.GetCollection<BsonDocument>(SyncFanOutLeasesCollection)
                    .FindOneAndUpdateAsync(leaseFilter,
                        Builders<BsonDocument>.Update.Set("leaseOwner", workerId)
                            .Set("leaseUntilUtc", leaseUntilUtc),
                        new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
                        cancellationToken).ConfigureAwait(false);
                if (lease == null)
                {
                    result.Result = new HostedFanOutClaim { LeaseAcquired = false };
                    result.IsLoaded = true;
                    result.Message = "Another ONODE worker owns the ordered fan-out lease.";
                    return result;
                }
                globalLeaseAcquired = true;
                var collection = Database.MongoDB.GetCollection<BsonDocument>(SyncFanOutCollection);
                var claimed = new List<HostedSyncFanOutItem>();
                for (int index = 0; index < maximumCount; index++)
                {
                    DateTime claimUtc = DateTime.UtcNow;
                    // Inspect the oldest incomplete record first. A deferred or leased head item is a
                    // hard ordering barrier: later accepted mutations must never overtake it.
                    var head = await collection.Find(Builders<BsonDocument>.Filter.Eq("completedUtc", BsonNull.Value))
                        .Sort(Builders<BsonDocument>.Sort.Ascending("sequence"))
                        .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                    if (head == null) break;
                    bool retryReady = head["nextAttemptUtc"].ToUniversalTime() <= claimUtc;
                    bool itemLeaseAvailable = head["leaseUntilUtc"].IsBsonNull ||
                        head["leaseUntilUtc"].ToUniversalTime() <= claimUtc ||
                        (!head["leaseOwner"].IsBsonNull && head["leaseOwner"].AsString == workerId);
                    if (!retryReady || !itemLeaseAvailable) break;

                    var filter = Builders<BsonDocument>.Filter.Eq("_id", head["_id"]) &
                        Builders<BsonDocument>.Filter.Eq("completedUtc", BsonNull.Value) &
                        Builders<BsonDocument>.Filter.Lte("nextAttemptUtc", claimUtc) &
                        (Builders<BsonDocument>.Filter.Eq("leaseUntilUtc", BsonNull.Value) |
                         Builders<BsonDocument>.Filter.Lte("leaseUntilUtc", claimUtc) |
                         Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId));
                    var update = Builders<BsonDocument>.Update.Set("leaseOwner", workerId)
                        .Set("leaseUntilUtc", leaseUntilUtc).Inc("attemptCount", 1);
                    var document = await collection.FindOneAndUpdateAsync(filter, update,
                        new FindOneAndUpdateOptions<BsonDocument>
                        {
                            ReturnDocument = ReturnDocument.After
                        }, cancellationToken).ConfigureAwait(false);
                    if (document == null) break;
                    claimed.Add(ToFanOutItem(document));
                }
                result.Result = new HostedFanOutClaim { LeaseAcquired = true, Items = claimed };
                result.IsLoaded = true;
            }
            catch (OperationCanceledException)
            {
                if (globalLeaseAcquired)
                    await ReleaseClaimAfterFailureAsync(workerId).ConfigureAwait(false);
                throw;
            }
            catch (Exception ex)
            {
                Exception failure = ex;
                if (globalLeaseAcquired)
                {
                    try { await ReleaseClaimAfterFailureAsync(workerId).ConfigureAwait(false); }
                    catch (Exception releaseException) { failure = new AggregateException(ex, releaseException); }
                }
                SetSyncError(result, "MONGO_FANOUT_CLAIM_FAILED", "Hosted fan-out work could not be claimed.", failure);
            }
            return result;
        }

        private async Task ReleaseClaimAfterFailureAsync(string workerId)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", "ordered-dispatcher") &
                Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId);
            var update = Builders<BsonDocument>.Update.Set("leaseOwner", BsonNull.Value)
                .Set("leaseUntilUtc", BsonNull.Value);
            var released = await Database.MongoDB.GetCollection<BsonDocument>(SyncFanOutLeasesCollection)
                .UpdateOneAsync(filter, update, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            if (released.MatchedCount != 1)
                throw new InvalidOperationException("The ordered fan-out lease could not be released after claim failure.");
        }

        public async Task<OASISResult<bool>> ReleaseFanOutWorkerAsync(string workerId,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            if (string.IsNullOrWhiteSpace(workerId))
            {
                SetSyncError(result, "MONGO_FANOUT_RELEASE_INVALID", "A fan-out worker id is required.", null);
                return result;
            }
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", "ordered-dispatcher") &
                    Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId);
                var update = Builders<BsonDocument>.Update.Set("leaseOwner", BsonNull.Value)
                    .Set("leaseUntilUtc", BsonNull.Value);
                var released = await Database.MongoDB.GetCollection<BsonDocument>(SyncFanOutLeasesCollection)
                    .UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (released.ModifiedCount != 1)
                {
                    SetSyncError(result, "MONGO_FANOUT_WORKER_LEASE_LOST",
                        "The ordered fan-out worker lease is no longer owned by this worker.", null);
                    return result;
                }
                result.Result = true;
                result.IsSaved = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetSyncError(result, "MONGO_FANOUT_RELEASE_FAILED", "The ordered fan-out worker lease was not released.", ex); }
            return result;
        }

        public async Task<OASISResult<bool>> RenewFanOutLeaseAsync(Guid operationId, string workerId,
            DateTime leaseUntilUtc, CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            if (operationId == Guid.Empty || string.IsNullOrWhiteSpace(workerId) || leaseUntilUtc <= DateTime.UtcNow)
            {
                SetSyncError(result, "MONGO_FANOUT_RENEW_INVALID",
                    "An operation id, lease-owning worker and future lease expiry are required.", null);
                return result;
            }
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                DateTime now = DateTime.UtcNow;
                var globalFilter = Builders<BsonDocument>.Filter.Eq("_id", "ordered-dispatcher") &
                    Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId) &
                    Builders<BsonDocument>.Filter.Gt("leaseUntilUtc", now);
                var global = await Database.MongoDB.GetCollection<BsonDocument>(SyncFanOutLeasesCollection)
                    .UpdateOneAsync(globalFilter, Builders<BsonDocument>.Update.Set("leaseUntilUtc", leaseUntilUtc),
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                if (global.MatchedCount != 1)
                {
                    SetSyncError(result, "MONGO_FANOUT_WORKER_LEASE_LOST",
                        "The ordered fan-out worker lease expired or changed owner.", null);
                    return result;
                }

                var itemFilter = Builders<BsonDocument>.Filter.Eq("_id", operationId.ToString("D")) &
                    Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId) &
                    Builders<BsonDocument>.Filter.Gt("leaseUntilUtc", now) &
                    Builders<BsonDocument>.Filter.Eq("completedUtc", BsonNull.Value);
                var item = await Database.MongoDB.GetCollection<BsonDocument>(SyncFanOutCollection)
                    .UpdateOneAsync(itemFilter, Builders<BsonDocument>.Update.Set("leaseUntilUtc", leaseUntilUtc),
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                if (item.MatchedCount != 1)
                {
                    SetSyncError(result, "MONGO_FANOUT_LEASE_LOST",
                        "The fan-out item lease expired or changed owner.", null);
                    return result;
                }
                result.Result = true;
                result.IsSaved = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                SetSyncError(result, "MONGO_FANOUT_RENEW_FAILED", "The fan-out lease was not renewed.", ex);
            }
            return result;
        }

        public async Task<OASISResult<bool>> ReleaseFanOutItemsAsync(IReadOnlyList<Guid> operationIds,
            string workerId, CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            if (operationIds == null || operationIds.Count == 0 || operationIds.Any(x => x == Guid.Empty) ||
                string.IsNullOrWhiteSpace(workerId))
            {
                SetSyncError(result, "MONGO_FANOUT_ITEMS_RELEASE_INVALID",
                    "One or more operation ids and their lease-owning worker are required.", null);
                return result;
            }
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                var ids = operationIds.Select(x => x.ToString("D"));
                var filter = Builders<BsonDocument>.Filter.In("_id", ids) &
                    Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId) &
                    Builders<BsonDocument>.Filter.Eq("completedUtc", BsonNull.Value);
                var update = Builders<BsonDocument>.Update.Set("leaseOwner", BsonNull.Value)
                    .Set("leaseUntilUtc", BsonNull.Value);
                var released = await Database.MongoDB.GetCollection<BsonDocument>(SyncFanOutCollection)
                    .UpdateManyAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (released.MatchedCount != operationIds.Count)
                {
                    SetSyncError(result, "MONGO_FANOUT_ITEMS_LEASE_LOST",
                        "One or more unprocessed fan-out leases are no longer owned by this worker.", null);
                    return result;
                }
                result.Result = true;
                result.IsSaved = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                SetSyncError(result, "MONGO_FANOUT_ITEMS_RELEASE_FAILED",
                    "Unprocessed ordered fan-out claims were not released.", ex);
            }
            return result;
        }

        public async Task<OASISResult<bool>> CompleteFanOutAsync(Guid operationId, string workerId,
            CancellationToken cancellationToken)
        {
            return await UpdateFanOutLeaseAsync(operationId, workerId,
                Builders<BsonDocument>.Update.Set("completedUtc", DateTime.UtcNow)
                    .Set("leaseOwner", BsonNull.Value).Set("leaseUntilUtc", BsonNull.Value),
                "MONGO_FANOUT_COMPLETE_FAILED", "Hosted fan-out completion was not persisted.", cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<OASISResult<bool>> FailFanOutAsync(Guid operationId, string workerId,
            string errorCode, string message, DateTime nextAttemptUtc, CancellationToken cancellationToken)
        {
            if (nextAttemptUtc <= DateTime.UtcNow)
            {
                var invalid = new OASISResult<bool>();
                SetSyncError(invalid, "MONGO_FANOUT_RETRY_INVALID", "The next fan-out attempt must be in the future.", null);
                return invalid;
            }
            return await UpdateFanOutLeaseAsync(operationId, workerId,
                Builders<BsonDocument>.Update.Set("nextAttemptUtc", nextAttemptUtc)
                    .Set("lastErrorCode", errorCode == null ? BsonNull.Value : (BsonValue)errorCode)
                    .Set("lastErrorMessage", message == null ? BsonNull.Value : (BsonValue)message)
                    .Set("leaseOwner", BsonNull.Value).Set("leaseUntilUtc", BsonNull.Value),
                "MONGO_FANOUT_FAIL_FAILED", "Hosted fan-out failure state was not persisted.", cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<OASISResult<bool>> UpdateFanOutLeaseAsync(Guid operationId, string workerId,
            UpdateDefinition<BsonDocument> update, string errorCode, string errorMessage,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            if (operationId == Guid.Empty || string.IsNullOrWhiteSpace(workerId))
            {
                SetSyncError(result, errorCode, "An operation id and lease-owning worker id are required.", null);
                return result;
            }
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", operationId.ToString("D")) &
                    Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId) &
                    Builders<BsonDocument>.Filter.Gt("leaseUntilUtc", DateTime.UtcNow) &
                    Builders<BsonDocument>.Filter.Eq("completedUtc", BsonNull.Value);
                var updated = await Database.MongoDB.GetCollection<BsonDocument>(SyncFanOutCollection)
                    .UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (updated.ModifiedCount != 1)
                {
                    SetSyncError(result, "MONGO_FANOUT_LEASE_LOST", "The fan-out lease is missing, expired or owned by another worker.", null);
                    return result;
                }
                result.Result = true;
                result.IsSaved = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetSyncError(result, errorCode, errorMessage, ex); }
            return result;
        }

        private static HostedSyncFanOutItem ToFanOutItem(BsonDocument document) => new HostedSyncFanOutItem
        {
            OperationId = Guid.Parse(document["_id"].AsString),
            AvatarId = Guid.Parse(document["avatarId"].AsString),
            EntityId = Guid.Parse(document["entityId"].AsString),
            EntityType = document["entityType"].AsString,
            Kind = (SyncOperationKind)document["kind"].AsInt32,
            VersionId = Guid.Parse(document["versionId"].AsString),
            PayloadJson = document["payloadJson"].IsBsonNull ? null : document["payloadJson"].AsString,
            AttemptCount = document["attemptCount"].AsInt32
        };

        private static void SetSyncError<T>(OASISResult<T> result, string code, string message, Exception exception)
        {
            result.IsError = true; result.ErrorCount = 1; result.ErrorCode = code; result.Message = message; result.Exception = exception;
        }
    }
}
