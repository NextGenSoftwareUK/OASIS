using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using Holon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public partial class MongoDBOASIS
    {
        private const string DomainCaptureStateCollection = "HyperDriveDomainCaptureState";
        private const string DomainCaptureIdentityCollection = "HyperDriveDomainCaptureIdentity";
        private const string DomainCaptureDeadLettersCollection = "HyperDriveDomainCaptureDeadLetters";
        private const string DomainCaptureLeasesCollection = "HyperDriveDomainCaptureLeases";
        private const string HolonCaptureId = "holon-v4";
        private readonly string _domainCaptureWorkerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

        private async Task<OASISResult<HostedDomainBackfillResult>> BackfillHolonDomainStateAsync(
            int batchSize, CancellationToken cancellationToken)
        {
            var result = new OASISResult<HostedDomainBackfillResult>();
            if (batchSize <= 0)
            {
                SetSyncError(result, "MONGO_DOMAIN_BACKFILL_INVALID", "A positive batch size is required.", null);
                return result;
            }
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                var states = Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureStateCollection);
                if (await states.Find(Builders<BsonDocument>.Filter.Eq("_id", HolonCaptureId))
                    .AnyAsync(cancellationToken).ConfigureAwait(false))
                {
                    result.Result = new HostedDomainBackfillResult { CaptureInitialized = true };
                    result.IsLoaded = true;
                    result.Message = "Holon domain capture was already initialized.";
                    return result;
                }
                var preImages = await EnableHolonPreImagesAsync(cancellationToken).ConfigureAwait(false);
                if (preImages.IsError)
                {
                    SetSyncError(result, preImages.ErrorCode, preImages.Message, preImages.Exception);
                    return result;
                }
                string migrationLeaseId = $"{HolonCaptureId}:backfill";
                var leases = Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureLeasesCollection);
                await leases.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", migrationLeaseId),
                    Builders<BsonDocument>.Update.SetOnInsert("workerId", BsonNull.Value)
                        .SetOnInsert("leaseUntilUtc", BsonNull.Value),
                    new UpdateOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                DateTime leaseNow = DateTime.UtcNow;
                var migrationLease = await leases.FindOneAndUpdateAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", migrationLeaseId) &
                    (Builders<BsonDocument>.Filter.Eq("workerId", _domainCaptureWorkerId) |
                     Builders<BsonDocument>.Filter.Eq("leaseUntilUtc", BsonNull.Value) |
                     Builders<BsonDocument>.Filter.Lte("leaseUntilUtc", leaseNow)),
                    Builders<BsonDocument>.Update.Set("workerId", _domainCaptureWorkerId)
                        .Set("leaseUntilUtc", leaseNow.AddMinutes(2)),
                    new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
                    cancellationToken).ConfigureAwait(false);
                if (migrationLease == null)
                {
                    SetSyncError(result, "MONGO_DOMAIN_BACKFILL_LEASE_UNAVAILABLE",
                        "Another process owns the domain-backfill lease.", null);
                    return result;
                }
                await NormalizeLegacyInventoryPayloadsAsync(SyncEntitiesCollection, cancellationToken)
                    .ConfigureAwait(false);
                await NormalizeLegacyInventoryPayloadsAsync(SyncChangesCollection, cancellationToken)
                    .ConfigureAwait(false);
                var hello = await Database.MongoClient.GetDatabase("admin")
                    .RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                if (!hello.TryGetValue("operationTime", out var operationTime) || !operationTime.IsBsonTimestamp)
                {
                    SetSyncError(result, "MONGO_DOMAIN_CAPTURE_OPERATION_TIME_UNAVAILABLE",
                        "MongoDB did not return the replica-set operation time required for online backfill.", null);
                    return result;
                }

                int projected = 0;
                int rejected = 0;
                string lastId = null;
                var holons = Database.MongoDB.GetCollection<Holon>("Holon");
                while (true)
                {
                    var filter = lastId == null ? FilterDefinition<Holon>.Empty :
                        Builders<Holon>.Filter.Gt(x => x.Id, lastId);
                    var batch = await holons.Find(filter).SortBy(x => x.Id).Limit(batchSize)
                        .ToListAsync(cancellationToken).ConfigureAwait(false);
                    if (batch.Count == 0) break;
                    DateTime renewNow = DateTime.UtcNow;
                    var renewed = await leases.UpdateOneAsync(
                        Builders<BsonDocument>.Filter.Eq("_id", migrationLeaseId) &
                        Builders<BsonDocument>.Filter.Eq("workerId", _domainCaptureWorkerId) &
                        Builders<BsonDocument>.Filter.Gt("leaseUntilUtc", renewNow),
                        Builders<BsonDocument>.Update.Set("leaseUntilUtc", renewNow.AddMinutes(2)),
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (renewed.MatchedCount != 1)
                    {
                        SetSyncError(result, "MONGO_DOMAIN_BACKFILL_LEASE_LOST",
                            "The domain-backfill lease expired or changed owner.", null);
                        return result;
                    }
                    foreach (var holon in batch)
                    {
                        var backfilled = await BackfillHolonAsync(holon, cancellationToken).ConfigureAwait(false);
                        if (backfilled.IsError)
                        {
                            result.IsError = true;
                            result.ErrorCount = 1;
                            result.ErrorCode = backfilled.ErrorCode;
                            result.Message = backfilled.Message;
                            result.Exception = backfilled.Exception;
                            return result;
                        }
                        if (backfilled.Result) projected++; else rejected++;
                    }
                    lastId = batch[batch.Count - 1].Id;
                    if (batch.Count < batchSize) break;
                }
                if (rejected != 0)
                {
                    await leases.UpdateOneAsync(
                        Builders<BsonDocument>.Filter.Eq("_id", migrationLeaseId) &
                        Builders<BsonDocument>.Filter.Eq("workerId", _domainCaptureWorkerId),
                        Builders<BsonDocument>.Update.Set("workerId", BsonNull.Value)
                            .Set("leaseUntilUtc", BsonNull.Value),
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                    result.Result = new HostedDomainBackfillResult
                    {
                        ProjectedCount = projected, RejectedCount = rejected, CaptureInitialized = false
                    };
                    SetSyncError(result, "MONGO_DOMAIN_BACKFILL_REJECTED_DOCUMENTS",
                        $"Backfill quarantined {rejected} Holon documents. Repair every dead letter before retrying.", null);
                    return result;
                }
                await states.InsertOneAsync(new BsonDocument
                {
                    { "_id", HolonCaptureId }, { "startAtOperationTime", operationTime },
                    { "updatedUtc", DateTime.UtcNow }, { "backfilledUtc", DateTime.UtcNow }
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
                await leases.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", migrationLeaseId) &
                    Builders<BsonDocument>.Filter.Eq("workerId", _domainCaptureWorkerId),
                    Builders<BsonDocument>.Update.Set("workerId", BsonNull.Value)
                        .Set("leaseUntilUtc", BsonNull.Value).Set("completedUtc", DateTime.UtcNow),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                result.Result = new HostedDomainBackfillResult
                {
                    ProjectedCount = projected, CaptureInitialized = true
                };
                result.IsSaved = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                SetSyncError(result, "MONGO_DOMAIN_BACKFILL_FAILED",
                    "Existing OASIS domain state was not prepared for hosted capture.", ex);
            }
            return result;
        }

        private async Task NormalizeLegacyInventoryPayloadsAsync(string collectionName,
            CancellationToken cancellationToken)
        {
            var collection = Database.MongoDB.GetCollection<BsonDocument>(collectionName);
            var filter = Builders<BsonDocument>.Filter.Eq("entityType", HyperDriveEntityTypes.InventoryItem) &
                Builders<BsonDocument>.Filter.Type("payloadJson", BsonType.String);
            var documents = await collection.Find(filter).ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var document in documents)
            {
                var payload = BsonDocument.Parse(document["payloadJson"].AsString);
                if (!payload.TryGetValue(nameof(HyperDriveInventoryItemProjection.ItemType), out var itemType) ||
                    !itemType.IsString)
                    continue;
                if (!Enum.TryParse(itemType.AsString, true, out InventoryItemType parsed) ||
                    !Enum.IsDefined(typeof(InventoryItemType), parsed))
                    throw new InvalidOperationException(
                        $"Inventory sync payload '{document["_id"]}' contains unknown item type '{itemType.AsString}'.");
                payload[nameof(HyperDriveInventoryItemProjection.ItemType)] = (int)parsed;
                await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", document["_id"]),
                    Builders<BsonDocument>.Update.Set("payloadJson", payload.ToJson()),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<OASISResult<HostedDomainChangeCaptureResult>> CaptureNextHolonDomainChangesAsync(
            int maximumCount, TimeSpan maximumWait, CancellationToken cancellationToken)
        {
            var result = new OASISResult<HostedDomainChangeCaptureResult>();
            if (maximumCount <= 0 || maximumWait <= TimeSpan.Zero)
            {
                SetSyncError(result, "MONGO_DOMAIN_CAPTURE_INVALID",
                    "A positive batch size and wait duration are required.", null);
                return result;
            }
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                var stateCollection = Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureStateCollection);
                var state = await stateCollection.Find(Builders<BsonDocument>.Filter.Eq("_id", HolonCaptureId))
                    .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (state == null)
                {
                    var preImages = await EnableHolonPreImagesAsync(cancellationToken).ConfigureAwait(false);
                    if (preImages.IsError)
                    {
                        SetSyncError(result, preImages.ErrorCode, preImages.Message, preImages.Exception);
                        return result;
                    }
                    long existingHolons = await Database.MongoDB.GetCollection<Holon>("Holon")
                        .CountDocumentsAsync(FilterDefinition<Holon>.Empty, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    if (existingHolons != 0)
                    {
                        SetSyncError(result, "MONGO_DOMAIN_CAPTURE_BACKFILL_REQUIRED",
                            "Domain capture cannot start over a non-empty Holon collection until the durable backfill migration has completed.", null);
                        return result;
                    }
                    var hello = await Database.MongoClient.GetDatabase("admin")
                        .RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    if (!hello.TryGetValue("operationTime", out var operationTime) || !operationTime.IsBsonTimestamp)
                    {
                        SetSyncError(result, "MONGO_DOMAIN_CAPTURE_OPERATION_TIME_UNAVAILABLE",
                            "MongoDB did not return the replica-set operation time required to initialize lossless capture.", null);
                        return result;
                    }
                    await stateCollection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", HolonCaptureId),
                        Builders<BsonDocument>.Update.SetOnInsert("startAtOperationTime", operationTime)
                            .SetOnInsert("updatedUtc", DateTime.UtcNow),
                        new UpdateOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                    state = await stateCollection.Find(Builders<BsonDocument>.Filter.Eq("_id", HolonCaptureId))
                        .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                }

                DateTime now = DateTime.UtcNow;
                DateTime leaseUntil = now.Add(maximumWait).AddSeconds(30);
                var leases = Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureLeasesCollection);
                await leases.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", HolonCaptureId),
                    Builders<BsonDocument>.Update.SetOnInsert("workerId", BsonNull.Value)
                        .SetOnInsert("leaseUntilUtc", BsonNull.Value),
                    new UpdateOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                var leaseFilter = Builders<BsonDocument>.Filter.Eq("_id", HolonCaptureId) &
                    (Builders<BsonDocument>.Filter.Eq("workerId", _domainCaptureWorkerId) |
                     Builders<BsonDocument>.Filter.Eq("leaseUntilUtc", BsonNull.Value) |
                     Builders<BsonDocument>.Filter.Lte("leaseUntilUtc", now));
                var lease = await leases.FindOneAndUpdateAsync(leaseFilter,
                        Builders<BsonDocument>.Update.Set("workerId", _domainCaptureWorkerId)
                            .Set("leaseUntilUtc", leaseUntil),
                        new FindOneAndUpdateOptions<BsonDocument>
                        {
                            IsUpsert = false,
                            ReturnDocument = ReturnDocument.After
                        }, cancellationToken).ConfigureAwait(false);
                if (lease == null)
                {
                    result.Result = new HostedDomainChangeCaptureResult();
                    result.IsLoaded = true;
                    result.Message = "Another ONODE owns the hosted domain-capture lease.";
                    return result;
                }
                var options = new ChangeStreamOptions
                {
                    FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                    FullDocumentBeforeChange = ChangeStreamFullDocumentBeforeChangeOption.Required,
                    MaxAwaitTime = maximumWait
                };
                if (state != null && state.TryGetValue("resumeToken", out var token) && token.IsBsonDocument)
                    options.ResumeAfter = token.AsBsonDocument;
                else if (state.TryGetValue("startAtOperationTime", out var start) && start.IsBsonTimestamp)
                    options.StartAtOperationTime = start.AsBsonTimestamp;

                var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<Holon>>()
                    .Match(x => x.OperationType == ChangeStreamOperationType.Insert ||
                        x.OperationType == ChangeStreamOperationType.Replace ||
                        x.OperationType == ChangeStreamOperationType.Update ||
                        x.OperationType == ChangeStreamOperationType.Delete);
                using (var cursor = await Database.MongoDB.GetCollection<Holon>("Holon")
                    .WatchAsync(pipeline, options, cancellationToken).ConfigureAwait(false))
                {
                    if (!await WaitForNonEmptyChangeBatchAsync(cursor, maximumWait, cancellationToken)
                        .ConfigureAwait(false))
                    {
                        result.Result = new HostedDomainChangeCaptureResult();
                        result.IsLoaded = true;
                        return result;
                    }

                    int captured = 0;
                    int rejected = 0;
                    string checkpoint = null;
                    foreach (var change in cursor.Current.Take(maximumCount))
                    {
                        var projected = await ProjectDomainChangeAsync(change, cancellationToken)
                            .ConfigureAwait(false);
                        if (projected.IsError)
                        {
                            result.IsError = true;
                            result.ErrorCount = 1;
                            result.ErrorCode = projected.ErrorCode;
                            result.Message = projected.Message;
                            result.Exception = projected.Exception;
                            return result;
                        }
                        if (projected.Result) captured++;
                        else if (projected.Message == "DOMAIN_CAPTURE_REJECTED") rejected++;
                        checkpoint = change.ResumeToken.ToJson();
                    }
                    result.Result = new HostedDomainChangeCaptureResult
                    {
                        CapturedCount = captured,
                        RejectedCount = rejected,
                        Checkpoint = checkpoint
                    };
                    result.IsSaved = true;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                SetSyncError(result, "MONGO_DOMAIN_CAPTURE_FAILED",
                    "The hosted domain change stream was not durably projected.", ex);
            }
            return result;
        }

        private static async Task<bool> WaitForNonEmptyChangeBatchAsync<T>(IAsyncCursor<T> cursor,
            TimeSpan maximumWait, CancellationToken cancellationToken)
        {
            DateTime deadline = DateTime.UtcNow.Add(maximumWait);
            do
            {
                if (!await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    return false;
                if (cursor.Current.Any())
                    return true;
            }
            while (DateTime.UtcNow < deadline);
            return false;
        }

        private async Task<OASISResult<bool>> BackfillHolonAsync(Holon holon,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            using (var session = await Database.MongoClient.StartSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                var transactionResult = await session.WithTransactionAsync(async (transactionSession,
                    transactionCancellationToken) =>
                {
                    string rejection = null;
                    if (holon == null || holon.HolonId == Guid.Empty)
                        rejection = "The existing Holon did not contain its public OASIS identity.";
                    if (!Guid.TryParse(holon?.CreatedByAvatarId, out var avatarId))
                        rejection = "The existing Holon did not contain a valid owning avatar identity.";
                    string payload = null;
                    if (rejection == null && holon.DeletedDate == DateTime.MinValue)
                    {
                        try
                        {
                            payload = JsonSerializer.Serialize(CreateEdgeHolonProjection(holon), new JsonSerializerOptions
                            {
                                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                            });
                        }
                        catch (Exception ex)
                        {
                            rejection = $"The existing Holon could not be serialized: {ex.Message}";
                        }
                    }
                    if (rejection != null)
                    {
                        string sourceKey = holon?.Id ?? $"missing:{Guid.NewGuid():N}";
                        await Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureDeadLettersCollection)
                            .ReplaceOneAsync(transactionSession, Builders<BsonDocument>.Filter.Eq("_id", $"backfill:{sourceKey}"),
                                new BsonDocument
                                {
                                    { "_id", $"backfill:{sourceKey}" }, { "source", HolonCaptureId },
                                    { "sourceKey", sourceKey }, { "reason", rejection },
                                    { "capturedUtc", DateTime.UtcNow }
                                }, new ReplaceOptions { IsUpsert = true }, transactionCancellationToken).ConfigureAwait(false);
                        return false;
                    }

                    Guid versionId = holon.VersionId == Guid.Empty
                        ? DeterministicVersion($"backfill:{holon.Id}:{holon.ModifiedDate.Ticks}:{holon.Version}")
                        : holon.VersionId;
                    string entityType = EntityTypeForHolon(holon);
                    string entityKey = AvatarEntityKey(avatarId, entityType, holon.HolonId);
                    await Database.MongoDB.GetCollection<BsonDocument>(SyncEntitiesCollection)
                        .ReplaceOneAsync(transactionSession, Builders<BsonDocument>.Filter.Eq("_id", entityKey),
                            new BsonDocument
                            {
                                { "_id", entityKey }, { "avatarId", avatarId.ToString("D") },
                                { "audience", SyncAudienceForEntityType(entityType) },
                                { "entityType", entityType },
                                { "entityId", holon.HolonId.ToString("D") },
                                { "versionId", versionId.ToString("D") },
                                { "isDeleted", holon.DeletedDate != DateTime.MinValue },
                                { "payloadJson", payload == null ? BsonNull.Value : payload },
                                { "changedUtc", holon.ModifiedDate == DateTime.MinValue ? holon.CreatedDate : holon.ModifiedDate }
                            }, new ReplaceOptions { IsUpsert = true }, transactionCancellationToken).ConfigureAwait(false);
                    string changeId = $"backfill:{HolonCaptureId}:{holon.Id}";
                    var changes = Database.MongoDB.GetCollection<BsonDocument>(SyncChangesCollection);
                    if (!await changes.Find(transactionSession, Builders<BsonDocument>.Filter.Eq("changeId", changeId))
                        .AnyAsync(transactionCancellationToken).ConfigureAwait(false))
                    {
                        long sequence = await NextChangeSequenceAsync(transactionSession, transactionCancellationToken).ConfigureAwait(false);
                        await changes.InsertOneAsync(transactionSession, new BsonDocument
                            {
                                { "_id", sequence }, { "sequence", sequence }, { "changeId", changeId },
                                { "avatarId", avatarId.ToString("D") },
                                { "audience", SyncAudienceForEntityType(entityType) },
                                { "entityType", entityType },
                                { "entityId", holon.HolonId.ToString("D") },
                                { "kind", (int)(holon.DeletedDate == DateTime.MinValue
                                    ? SyncOperationKind.Upsert : SyncOperationKind.Delete) },
                                { "versionId", versionId.ToString("D") },
                                { "previousVersionId", Guid.Empty.ToString("D") },
                                { "payloadJson", payload == null ? BsonNull.Value : payload },
                                { "changedUtc", holon.ModifiedDate == DateTime.MinValue ? holon.CreatedDate : holon.ModifiedDate }
                            }, cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                    }
                    await Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureIdentityCollection)
                        .ReplaceOneAsync(transactionSession, Builders<BsonDocument>.Filter.Eq("_id", holon.Id),
                            new BsonDocument
                            {
                                { "_id", holon.Id }, { "avatarId", avatarId.ToString("D") },
                                { "entityId", holon.HolonId.ToString("D") }, { "entityType", entityType },
                                { "versionId", versionId.ToString("D") }
                            }, new ReplaceOptions { IsUpsert = true }, transactionCancellationToken).ConfigureAwait(false);
                    await Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureDeadLettersCollection)
                        .DeleteOneAsync(transactionSession,
                            Builders<BsonDocument>.Filter.Eq("_id", $"backfill:{holon.Id}"),
                            new DeleteOptions(), transactionCancellationToken).ConfigureAwait(false);
                    return true;
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
                result.Result = transactionResult;
                result.IsSaved = true;
                if (!transactionResult)
                    result.Message = "DOMAIN_CAPTURE_REJECTED";
            }
            return result;
        }

        private async Task<OASISResult<bool>> ProjectDomainChangeAsync(ChangeStreamDocument<Holon> change,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            using (var session = await Database.MongoClient.StartSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                session.StartTransaction();
                try
                {
                    string sourceKey = change.DocumentKey["_id"].ToString();
                    var identities = Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureIdentityCollection);
                    var identity = await identities.Find(session,
                        Builders<BsonDocument>.Filter.Eq("_id", sourceKey))
                        .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                    Guid avatarId;
                    Guid entityId;
                    Guid versionId;
                    string entityType;
                    string payload = null;
                    SyncOperationKind kind;
                    string rejection = null;

                    if (change.OperationType == ChangeStreamOperationType.Delete)
                    {
                        kind = SyncOperationKind.Delete;
                        Holon beforeDelete = change.FullDocumentBeforeChange;
                        if (identity == null && beforeDelete != null && beforeDelete.HolonId != Guid.Empty &&
                            Guid.TryParse(beforeDelete.CreatedByAvatarId, out var preImageAvatarId))
                        {
                            avatarId = preImageAvatarId;
                            entityId = beforeDelete.HolonId;
                        }
                        else
                        {
                            avatarId = identity == null ? Guid.Empty : Guid.Parse(identity["avatarId"].AsString);
                            entityId = identity == null ? Guid.Empty : Guid.Parse(identity["entityId"].AsString);
                        }
                        if (identity == null)
                        {
                            if (beforeDelete == null)
                                rejection = "A hard-deleted Holon had neither a captured identity nor the required MongoDB pre-image.";
                            else if (avatarId == Guid.Empty || entityId == Guid.Empty)
                                rejection = "The hard-delete pre-image did not contain valid OASIS avatar and Holon identities.";
                        }
                        versionId = DeterministicVersion(change.ResumeToken);
                        entityType = identity != null && identity.TryGetValue("entityType", out var storedEntityType)
                            ? storedEntityType.AsString
                            : EntityTypeForHolon(beforeDelete);
                    }
                    else
                    {
                        kind = change.FullDocument != null && change.FullDocument.DeletedDate != DateTime.MinValue
                            ? SyncOperationKind.Delete : SyncOperationKind.Upsert;
                        entityId = change.FullDocument?.HolonId ?? Guid.Empty;
                        if (change.FullDocument == null || entityId == Guid.Empty)
                            rejection = "The captured Holon did not contain its public OASIS identity.";
                        if (!Guid.TryParse(change.FullDocument?.CreatedByAvatarId, out avatarId))
                            rejection = "The captured Holon did not contain a valid owning avatar identity.";
                        versionId = change.FullDocument?.VersionId ?? Guid.Empty;
                        if (versionId == Guid.Empty) versionId = DeterministicVersion(change.ResumeToken);
                        entityType = EntityTypeForHolon(change.FullDocument);
                        if (kind == SyncOperationKind.Upsert && rejection == null)
                        {
                            try
                            {
                                payload = JsonSerializer.Serialize(CreateEdgeHolonProjection(change.FullDocument),
                                    new JsonSerializerOptions
                                    {
                                        ReferenceHandler = ReferenceHandler.IgnoreCycles,
                                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                                    });
                            }
                            catch (Exception ex)
                            {
                                rejection = $"The captured Holon could not be serialized: {ex.Message}";
                            }
                        }
                    }

                    if (rejection != null)
                    {
                        await Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureDeadLettersCollection)
                            .ReplaceOneAsync(session,
                                Builders<BsonDocument>.Filter.Eq("_id", change.ResumeToken.ToJson()),
                                new BsonDocument
                                {
                                    { "_id", change.ResumeToken.ToJson() }, { "source", HolonCaptureId },
                                    { "sourceKey", sourceKey }, { "reason", rejection },
                                    { "capturedUtc", DateTime.UtcNow }
                                }, new ReplaceOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                        await SaveDomainCaptureCheckpointAsync(session, change.ResumeToken, cancellationToken)
                            .ConfigureAwait(false);
                        await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                        result.Result = false;
                        result.IsSaved = true;
                        return result;
                    }

                    string entityKey = AvatarEntityKey(avatarId, entityType, entityId);
                    var entities = Database.MongoDB.GetCollection<BsonDocument>(SyncEntitiesCollection);
                    var current = await entities.Find(session, Builders<BsonDocument>.Filter.Eq("_id", entityKey))
                        .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                    Guid previousVersion = current == null ? Guid.Empty : Guid.Parse(current["versionId"].AsString);
                    bool alreadyProjected = previousVersion == versionId &&
                        current["isDeleted"].AsBoolean == (kind == SyncOperationKind.Delete);
                    if (!alreadyProjected)
                    {
                        DateTime changedUtc = DateTime.UtcNow;
                        await entities.ReplaceOneAsync(session, Builders<BsonDocument>.Filter.Eq("_id", entityKey),
                            new BsonDocument
                            {
                                { "_id", entityKey }, { "avatarId", avatarId.ToString("D") },
                                { "audience", SyncAudienceForEntityType(entityType) },
                                { "entityType", entityType }, { "entityId", entityId.ToString("D") },
                                { "versionId", versionId.ToString("D") }, { "isDeleted", kind == SyncOperationKind.Delete },
                                { "payloadJson", payload == null ? BsonNull.Value : payload }, { "changedUtc", changedUtc }
                            }, new ReplaceOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                        long sequence = await NextChangeSequenceAsync(session, cancellationToken).ConfigureAwait(false);
                        await Database.MongoDB.GetCollection<BsonDocument>(SyncChangesCollection)
                            .InsertOneAsync(session, new BsonDocument
                            {
                                { "_id", sequence }, { "sequence", sequence },
                                { "changeId", $"domain:{change.ResumeToken.ToJson()}" },
                                { "avatarId", avatarId.ToString("D") }, { "entityType", entityType },
                                { "audience", SyncAudienceForEntityType(entityType) },
                                { "entityId", entityId.ToString("D") }, { "kind", (int)kind },
                                { "versionId", versionId.ToString("D") },
                                { "previousVersionId", previousVersion.ToString("D") },
                                { "payloadJson", payload == null ? BsonNull.Value : payload }, { "changedUtc", changedUtc }
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }

                    if (change.OperationType == ChangeStreamOperationType.Delete)
                        await identities.DeleteOneAsync(session, Builders<BsonDocument>.Filter.Eq("_id", sourceKey),
                            new DeleteOptions(), cancellationToken).ConfigureAwait(false);
                    else
                        await identities.ReplaceOneAsync(session, Builders<BsonDocument>.Filter.Eq("_id", sourceKey),
                            new BsonDocument
                            {
                                { "_id", sourceKey }, { "avatarId", avatarId.ToString("D") },
                                { "entityId", entityId.ToString("D") }, { "entityType", entityType },
                                { "versionId", versionId.ToString("D") }
                            }, new ReplaceOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                    await SaveDomainCaptureCheckpointAsync(session, change.ResumeToken, cancellationToken)
                        .ConfigureAwait(false);
                    await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                    result.Result = !alreadyProjected;
                    result.IsSaved = true;
                    result.Message = alreadyProjected ? "DOMAIN_CAPTURE_ALREADY_PROJECTED" : "DOMAIN_CAPTURE_PROJECTED";
                }
                catch
                {
                    if (session.IsInTransaction)
                        await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }
            return result;
        }

        private Task SaveDomainCaptureCheckpointAsync(IClientSessionHandle session, BsonDocument resumeToken,
            CancellationToken cancellationToken) => Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureStateCollection)
            .ReplaceOneAsync(session, Builders<BsonDocument>.Filter.Eq("_id", HolonCaptureId),
                new BsonDocument
                {
                    { "_id", HolonCaptureId }, { "resumeToken", resumeToken }, { "updatedUtc", DateTime.UtcNow }
                }, new ReplaceOptions { IsUpsert = true }, cancellationToken);

        private static Guid DeterministicVersion(BsonDocument resumeToken)
            => DeterministicVersion(resumeToken.ToJson());

        private static Guid DeterministicVersion(string value)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                byte[] guid = new byte[16];
                Buffer.BlockCopy(hash, 0, guid, 0, guid.Length);
                return new Guid(guid);
            }
        }

        private async Task<OASISResult<bool>> EnableHolonPreImagesAsync(CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            try
            {
                var collections = await Database.MongoDB.ListCollectionNamesAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                var names = await collections.ToListAsync(cancellationToken).ConfigureAwait(false);
                if (!names.Contains("Holon", StringComparer.Ordinal))
                    await Database.MongoDB.CreateCollectionAsync("Holon", cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                await Database.MongoDB.RunCommandAsync<BsonDocument>(new BsonDocument
                {
                    { "collMod", "Holon" },
                    { "changeStreamPreAndPostImages", new BsonDocument("enabled", true) }
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
                result.Result = true;
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.ErrorCount = 1;
                result.ErrorCode = "MONGO_DOMAIN_CAPTURE_PREIMAGES_REQUIRED";
                result.Message = "MongoDB change-stream pre-images are required for lossless hard-delete capture.";
                result.Exception = ex;
            }
            return result;
        }
    }
}
