using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using Avatar = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public partial class MongoDBOASIS
    {
        private const string AvatarCaptureId = "avatar-edge-v1";
        private const string AvatarDetailCaptureId = "avatar-detail-edge-v1";

        public async Task<OASISResult<HostedDomainBackfillResult>> BackfillDomainStateAsync(
            int batchSize, CancellationToken cancellationToken)
        {
            var holons = await BackfillHolonDomainStateAsync(batchSize, cancellationToken).ConfigureAwait(false);
            if (holons.IsError) return holons;
            var avatars = await BackfillPrivateProjectionAsync<Avatar>("Avatar", AvatarCaptureId,
                HyperDriveEntityTypes.Avatar, batchSize, x => x.HolonId, x => x.Id, x => x.VersionId,
                x => x.DeletedDate != DateTime.MinValue, x => x.ModifiedDate == DateTime.MinValue ? x.CreatedDate : x.ModifiedDate,
                x => CreateEdgeAvatarProjection(x), cancellationToken).ConfigureAwait(false);
            if (avatars.IsError) return avatars;
            var details = await BackfillPrivateProjectionAsync<AvatarDetail>("AvatarDetail", AvatarDetailCaptureId,
                HyperDriveEntityTypes.AvatarDetail, batchSize, x => x.HolonId, x => x.Id, x => x.VersionId,
                x => x.DeletedDate != DateTime.MinValue, x => x.ModifiedDate == DateTime.MinValue ? x.CreatedDate : x.ModifiedDate,
                x => CreateEdgeAvatarDetailProjection(x), cancellationToken).ConfigureAwait(false);
            if (details.IsError) return details;
            return new OASISResult<HostedDomainBackfillResult>
            {
                IsSaved = true,
                Result = new HostedDomainBackfillResult
                {
                    ProjectedCount = (holons.Result?.ProjectedCount ?? 0) +
                        (avatars.Result?.ProjectedCount ?? 0) + (details.Result?.ProjectedCount ?? 0),
                    RejectedCount = (holons.Result?.RejectedCount ?? 0) +
                        (avatars.Result?.RejectedCount ?? 0) + (details.Result?.RejectedCount ?? 0),
                    CaptureInitialized = holons.Result?.CaptureInitialized == true &&
                        avatars.Result?.CaptureInitialized == true && details.Result?.CaptureInitialized == true
                }
            };
        }

        public async Task<OASISResult<HostedDomainChangeCaptureResult>> CaptureNextDomainChangesAsync(
            int maximumCount, TimeSpan maximumWait, CancellationToken cancellationToken)
        {
            if (maximumCount <= 0 || maximumWait <= TimeSpan.Zero)
            {
                var invalid = new OASISResult<HostedDomainChangeCaptureResult>();
                SetSyncError(invalid, "MONGO_DOMAIN_CAPTURE_INVALID",
                    "A positive batch size and wait duration are required.", null);
                return invalid;
            }
            int perSourceCount = Math.Max(1, maximumCount / 3);
            TimeSpan perSourceWait = TimeSpan.FromTicks(Math.Max(1, maximumWait.Ticks / 3));
            var holons = await CaptureNextHolonDomainChangesAsync(perSourceCount, perSourceWait, cancellationToken)
                .ConfigureAwait(false);
            if (holons.IsError) return holons;
            var avatars = await CapturePrivateProjectionChangesAsync<Avatar>("Avatar", AvatarCaptureId,
                HyperDriveEntityTypes.Avatar, perSourceCount, perSourceWait, x => x.HolonId, x => x.Id,
                x => x.VersionId, x => x.DeletedDate != DateTime.MinValue,
                x => CreateEdgeAvatarProjection(x), cancellationToken).ConfigureAwait(false);
            if (avatars.IsError) return avatars;
            var details = await CapturePrivateProjectionChangesAsync<AvatarDetail>("AvatarDetail", AvatarDetailCaptureId,
                HyperDriveEntityTypes.AvatarDetail, perSourceCount, perSourceWait, x => x.HolonId, x => x.Id,
                x => x.VersionId, x => x.DeletedDate != DateTime.MinValue,
                x => CreateEdgeAvatarDetailProjection(x), cancellationToken).ConfigureAwait(false);
            if (details.IsError) return details;
            return new OASISResult<HostedDomainChangeCaptureResult>
            {
                IsSaved = true,
                Result = new HostedDomainChangeCaptureResult
                {
                    CapturedCount = (holons.Result?.CapturedCount ?? 0) +
                        (avatars.Result?.CapturedCount ?? 0) + (details.Result?.CapturedCount ?? 0),
                    RejectedCount = (holons.Result?.RejectedCount ?? 0) +
                        (avatars.Result?.RejectedCount ?? 0) + (details.Result?.RejectedCount ?? 0),
                    Checkpoint = string.Join("|", new[] { holons.Result?.Checkpoint, avatars.Result?.Checkpoint,
                        details.Result?.Checkpoint }.Where(x => !string.IsNullOrWhiteSpace(x)))
                }
            };
        }

        private async Task<OASISResult<HostedDomainBackfillResult>> BackfillPrivateProjectionAsync<T>(
            string collectionName, string captureId, string entityType, int batchSize,
            Func<T, Guid> avatarId, Func<T, string> sourceId, Func<T, Guid> versionId,
            Func<T, bool> isDeleted, Func<T, DateTime> changedUtc, Func<T, object> project,
            CancellationToken cancellationToken)
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
                if (await states.Find(Builders<BsonDocument>.Filter.Eq("_id", captureId))
                    .AnyAsync(cancellationToken).ConfigureAwait(false))
                {
                    result.Result = new HostedDomainBackfillResult { CaptureInitialized = true };
                    result.IsLoaded = true;
                    return result;
                }
                var preimages = await EnableCollectionPreImagesAsync(collectionName, cancellationToken)
                    .ConfigureAwait(false);
                if (preimages.IsError)
                {
                    SetSyncError(result, preimages.ErrorCode, preimages.Message, preimages.Exception);
                    return result;
                }
                var hello = await Database.MongoClient.GetDatabase("admin")
                    .RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                if (!hello.TryGetValue("operationTime", out BsonValue operationTime) || !operationTime.IsBsonTimestamp)
                {
                    SetSyncError(result, "MONGO_DOMAIN_CAPTURE_OPERATION_TIME_UNAVAILABLE",
                        "MongoDB did not return the replica-set operation time required for lossless backfill.", null);
                    return result;
                }
                string leaseId = captureId + ":backfill";
                if (!await AcquireCaptureLeaseAsync(leaseId, DateTime.UtcNow.AddMinutes(2), cancellationToken)
                    .ConfigureAwait(false))
                {
                    SetSyncError(result, "MONGO_DOMAIN_BACKFILL_LEASE_UNAVAILABLE",
                        $"Another process owns the {captureId} backfill lease.", null);
                    return result;
                }
                int projected = 0;
                int rejected = 0;
                try
                {
                    string afterId = null;
                    var collection = Database.MongoDB.GetCollection<T>(collectionName);
                    while (true)
                    {
                        FilterDefinition<T> filter = afterId == null
                            ? Builders<T>.Filter.Empty : Builders<T>.Filter.Gt("_id", afterId);
                        var page = await collection.Find(filter).Sort(Builders<T>.Sort.Ascending("_id"))
                            .Limit(batchSize).ToListAsync(cancellationToken).ConfigureAwait(false);
                        if (page.Count == 0) break;
                        foreach (T document in page)
                        {
                            Guid owner = avatarId(document);
                            string source = sourceId(document);
                            if (owner == Guid.Empty || string.IsNullOrWhiteSpace(source))
                            {
                                await WriteProjectionDeadLetterAsync($"backfill:{captureId}:{source}", captureId,
                                    source, "The source document has no valid avatar/public identity.", cancellationToken)
                                    .ConfigureAwait(false);
                                rejected++;
                                continue;
                            }
                            Guid version = versionId(document);
                            if (version == Guid.Empty)
                                version = DeterministicVersion($"backfill:{captureId}:{source}:{changedUtc(document).Ticks}");
                            string payload = isDeleted(document) ? null : JsonSerializer.Serialize(project(document));
                            await CommitPrivateProjectionAsync(captureId, source, owner, owner, entityType, version,
                                isDeleted(document), payload, changedUtc(document), null, cancellationToken)
                                .ConfigureAwait(false);
                            projected++;
                        }
                        afterId = sourceId(page[page.Count - 1]);
                        if (!await AcquireCaptureLeaseAsync(leaseId, DateTime.UtcNow.AddMinutes(2), cancellationToken)
                            .ConfigureAwait(false))
                            throw new InvalidOperationException($"The {captureId} backfill lease was lost.");
                        if (page.Count < batchSize) break;
                    }
                    if (rejected == 0)
                        await states.ReplaceOneAsync(Builders<BsonDocument>.Filter.Eq("_id", captureId),
                            new BsonDocument { { "_id", captureId }, { "startAtOperationTime", operationTime },
                                { "updatedUtc", DateTime.UtcNow } },
                            new ReplaceOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                }
                finally { await ReleaseCaptureLeaseAsync(leaseId).ConfigureAwait(false); }
                result.Result = new HostedDomainBackfillResult
                {
                    ProjectedCount = projected, RejectedCount = rejected, CaptureInitialized = rejected == 0
                };
                result.IsSaved = true;
                if (rejected != 0)
                    SetSyncError(result, "MONGO_DOMAIN_BACKFILL_REJECTED",
                        $"{captureId} backfill quarantined {rejected} documents.", null);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                SetSyncError(result, "MONGO_DOMAIN_BACKFILL_FAILED",
                    $"{captureId} state was not prepared for hosted capture.", ex);
            }
            return result;
        }

        private async Task<OASISResult<HostedDomainChangeCaptureResult>> CapturePrivateProjectionChangesAsync<T>(
            string collectionName, string captureId, string entityType, int maximumCount, TimeSpan maximumWait,
            Func<T, Guid> avatarId, Func<T, string> sourceId, Func<T, Guid> versionId,
            Func<T, bool> isDeleted, Func<T, object> project, CancellationToken cancellationToken)
        {
            var result = new OASISResult<HostedDomainChangeCaptureResult>();
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                var states = Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureStateCollection);
                var state = await states.Find(Builders<BsonDocument>.Filter.Eq("_id", captureId))
                    .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (state == null)
                {
                    long existing = await Database.MongoDB.GetCollection<T>(collectionName)
                        .CountDocumentsAsync(Builders<T>.Filter.Empty, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    if (existing != 0)
                    {
                        SetSyncError(result, "MONGO_DOMAIN_CAPTURE_BACKFILL_REQUIRED",
                            $"{captureId} capture requires its durable backfill migration.", null);
                        return result;
                    }
                    var enabled = await EnableCollectionPreImagesAsync(collectionName, cancellationToken)
                        .ConfigureAwait(false);
                    if (enabled.IsError)
                    {
                        SetSyncError(result, enabled.ErrorCode, enabled.Message, enabled.Exception);
                        return result;
                    }
                    var hello = await Database.MongoClient.GetDatabase("admin")
                        .RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1), cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    BsonValue operationTime = hello["operationTime"];
                    await states.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", captureId),
                        Builders<BsonDocument>.Update.SetOnInsert("startAtOperationTime", operationTime)
                            .SetOnInsert("updatedUtc", DateTime.UtcNow), new UpdateOptions { IsUpsert = true },
                        cancellationToken).ConfigureAwait(false);
                    state = await states.Find(Builders<BsonDocument>.Filter.Eq("_id", captureId))
                        .FirstAsync(cancellationToken).ConfigureAwait(false);
                }
                if (!await AcquireCaptureLeaseAsync(captureId, DateTime.UtcNow.Add(maximumWait).AddSeconds(30),
                    cancellationToken).ConfigureAwait(false))
                {
                    result.Result = new HostedDomainChangeCaptureResult();
                    result.IsLoaded = true;
                    return result;
                }
                try
                {
                    var options = new ChangeStreamOptions
                    {
                        FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
                        FullDocumentBeforeChange = ChangeStreamFullDocumentBeforeChangeOption.Required,
                        MaxAwaitTime = maximumWait
                    };
                    if (state.TryGetValue("resumeToken", out BsonValue token) && token.IsBsonDocument)
                        options.ResumeAfter = token.AsBsonDocument;
                    else options.StartAtOperationTime = state["startAtOperationTime"].AsBsonTimestamp;
                    var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<T>>().Match(x =>
                        x.OperationType == ChangeStreamOperationType.Insert ||
                        x.OperationType == ChangeStreamOperationType.Replace ||
                        x.OperationType == ChangeStreamOperationType.Update ||
                        x.OperationType == ChangeStreamOperationType.Delete);
                    using var cursor = await Database.MongoDB.GetCollection<T>(collectionName)
                        .WatchAsync(pipeline, options, cancellationToken).ConfigureAwait(false);
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
                    foreach (ChangeStreamDocument<T> change in cursor.Current.Take(maximumCount))
                    {
                        T document = change.OperationType == ChangeStreamOperationType.Delete
                            ? change.FullDocumentBeforeChange : change.FullDocument;
                        string source = document == null ? change.DocumentKey["_id"].ToString() : sourceId(document);
                        Guid owner = document == null ? Guid.Empty : avatarId(document);
                        if (owner == Guid.Empty || document == null)
                        {
                            await CommitProjectionRejectionAsync(change.ResumeToken.ToJson(), captureId, source,
                                "The captured document/pre-image has no valid avatar identity.", change.ResumeToken,
                                cancellationToken)
                                .ConfigureAwait(false);
                            rejected++;
                            checkpoint = change.ResumeToken.ToJson();
                            continue;
                        }
                        bool deleted = change.OperationType == ChangeStreamOperationType.Delete || isDeleted(document);
                        Guid version = versionId(document);
                        if (version == Guid.Empty) version = DeterministicVersion(change.ResumeToken);
                        string payload = deleted ? null : JsonSerializer.Serialize(project(document));
                        await CommitPrivateProjectionAsync(captureId, source, owner, owner, entityType, version,
                            deleted, payload, DateTime.UtcNow, change.ResumeToken, cancellationToken)
                            .ConfigureAwait(false);
                        captured++;
                        checkpoint = change.ResumeToken.ToJson();
                    }
                    result.Result = new HostedDomainChangeCaptureResult
                    {
                        CapturedCount = captured, RejectedCount = rejected, Checkpoint = checkpoint
                    };
                    result.IsSaved = true;
                }
                finally { await ReleaseCaptureLeaseAsync(captureId).ConfigureAwait(false); }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                SetSyncError(result, "MONGO_DOMAIN_CAPTURE_FAILED",
                    $"{captureId} change stream was not durably projected.", ex);
            }
            return result;
        }

        private async Task CommitPrivateProjectionAsync(string captureId, string sourceKey, Guid avatarId,
            Guid entityId, string entityType, Guid versionId, bool deleted, string payload, DateTime changedUtc,
            BsonDocument resumeToken, CancellationToken cancellationToken)
        {
            using var session = await Database.MongoClient.StartSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            session.StartTransaction();
            try
            {
                string entityKey = AvatarEntityKey(avatarId, entityType, entityId);
                var entities = Database.MongoDB.GetCollection<BsonDocument>(SyncEntitiesCollection);
                var current = await entities.Find(session, Builders<BsonDocument>.Filter.Eq("_id", entityKey))
                    .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                Guid previous = current == null ? Guid.Empty : Guid.Parse(current["versionId"].AsString);
                bool same = current != null && previous == versionId && current["isDeleted"].AsBoolean == deleted;
                if (!same)
                {
                    await entities.ReplaceOneAsync(session, Builders<BsonDocument>.Filter.Eq("_id", entityKey),
                        new BsonDocument { { "_id", entityKey }, { "avatarId", avatarId.ToString("D") },
                            { "audience", "avatar" }, { "entityType", entityType },
                            { "entityId", entityId.ToString("D") }, { "versionId", versionId.ToString("D") },
                            { "isDeleted", deleted }, { "payloadJson", payload == null ? BsonNull.Value : payload },
                            { "changedUtc", changedUtc } }, new ReplaceOptions { IsUpsert = true }, cancellationToken)
                        .ConfigureAwait(false);
                    long sequence = await NextChangeSequenceAsync(session, cancellationToken).ConfigureAwait(false);
                    await Database.MongoDB.GetCollection<BsonDocument>(SyncChangesCollection).InsertOneAsync(session,
                        new BsonDocument { { "_id", sequence }, { "sequence", sequence },
                            { "changeId", resumeToken == null ? $"backfill:{captureId}:{sourceKey}" :
                                $"domain:{captureId}:{resumeToken.ToJson()}" },
                            { "avatarId", avatarId.ToString("D") }, { "audience", "avatar" },
                            { "entityType", entityType }, { "entityId", entityId.ToString("D") },
                            { "kind", (int)(deleted ? SyncOperationKind.Delete : SyncOperationKind.Upsert) },
                            { "versionId", versionId.ToString("D") },
                            { "previousVersionId", previous.ToString("D") },
                            { "payloadJson", payload == null ? BsonNull.Value : payload }, { "changedUtc", changedUtc } },
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                string identityKey = captureId + ":" + sourceKey;
                await Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureIdentityCollection)
                    .ReplaceOneAsync(session, Builders<BsonDocument>.Filter.Eq("_id", identityKey),
                        new BsonDocument { { "_id", identityKey }, { "avatarId", avatarId.ToString("D") },
                            { "entityId", entityId.ToString("D") }, { "entityType", entityType },
                            { "versionId", versionId.ToString("D") } }, new ReplaceOptions { IsUpsert = true },
                        cancellationToken).ConfigureAwait(false);
                if (resumeToken != null)
                    await SaveProjectionCheckpointAsync(session, captureId, resumeToken, cancellationToken)
                        .ConfigureAwait(false);
                await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                if (session.IsInTransaction) await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        private async Task<OASISResult<bool>> EnableCollectionPreImagesAsync(string collectionName,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            try
            {
                var names = await (await Database.MongoDB.ListCollectionNamesAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false)).ToListAsync(cancellationToken).ConfigureAwait(false);
                if (!names.Contains(collectionName, StringComparer.Ordinal))
                    await Database.MongoDB.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                await Database.MongoDB.RunCommandAsync<BsonDocument>(new BsonDocument
                {
                    { "collMod", collectionName },
                    { "changeStreamPreAndPostImages", new BsonDocument("enabled", true) }
                }, cancellationToken: cancellationToken).ConfigureAwait(false);
                result.Result = true;
                result.IsSaved = true;
            }
            catch (Exception ex)
            {
                result.IsError = true; result.ErrorCount = 1;
                result.ErrorCode = "MONGO_DOMAIN_CAPTURE_PREIMAGES_REQUIRED";
                result.Message = $"MongoDB change-stream pre-images are required for lossless {collectionName} capture.";
                result.Exception = ex;
            }
            return result;
        }

        private async Task<bool> AcquireCaptureLeaseAsync(string leaseId, DateTime leaseUntilUtc,
            CancellationToken cancellationToken)
        {
            var leases = Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureLeasesCollection);
            await leases.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", leaseId),
                Builders<BsonDocument>.Update.SetOnInsert("workerId", BsonNull.Value)
                    .SetOnInsert("leaseUntilUtc", BsonNull.Value), new UpdateOptions { IsUpsert = true },
                cancellationToken).ConfigureAwait(false);
            DateTime now = DateTime.UtcNow;
            var lease = await leases.FindOneAndUpdateAsync(Builders<BsonDocument>.Filter.Eq("_id", leaseId) &
                (Builders<BsonDocument>.Filter.Eq("workerId", _domainCaptureWorkerId) |
                 Builders<BsonDocument>.Filter.Eq("leaseUntilUtc", BsonNull.Value) |
                 Builders<BsonDocument>.Filter.Lte("leaseUntilUtc", now)),
                Builders<BsonDocument>.Update.Set("workerId", _domainCaptureWorkerId)
                    .Set("leaseUntilUtc", leaseUntilUtc),
                new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
                cancellationToken).ConfigureAwait(false);
            return lease != null;
        }

        private async Task ReleaseCaptureLeaseAsync(string leaseId)
        {
            var released = await Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureLeasesCollection)
                .UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", leaseId) &
                    Builders<BsonDocument>.Filter.Eq("workerId", _domainCaptureWorkerId),
                    Builders<BsonDocument>.Update.Set("workerId", BsonNull.Value)
                        .Set("leaseUntilUtc", BsonNull.Value), cancellationToken: CancellationToken.None)
                .ConfigureAwait(false);
            if (released.MatchedCount != 1)
                throw new InvalidOperationException($"The domain-capture lease '{leaseId}' was lost before release.");
        }

        private Task SaveProjectionCheckpointAsync(string captureId, BsonDocument token,
            CancellationToken cancellationToken) => SaveProjectionCheckpointAsync(null, captureId, token, cancellationToken);

        private Task SaveProjectionCheckpointAsync(IClientSessionHandle session, string captureId, BsonDocument token,
            CancellationToken cancellationToken)
        {
            var collection = Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureStateCollection);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", captureId);
            var replacement = new BsonDocument { { "_id", captureId }, { "resumeToken", token },
                { "updatedUtc", DateTime.UtcNow } };
            return session == null
                ? collection.ReplaceOneAsync(filter, replacement, new ReplaceOptions { IsUpsert = true }, cancellationToken)
                : collection.ReplaceOneAsync(session, filter, replacement,
                    new ReplaceOptions { IsUpsert = true }, cancellationToken);
        }

        private Task WriteProjectionDeadLetterAsync(string id, string captureId, string sourceKey, string reason,
            CancellationToken cancellationToken) => Database.MongoDB
            .GetCollection<BsonDocument>(DomainCaptureDeadLettersCollection)
            .ReplaceOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id),
                new BsonDocument { { "_id", id }, { "source", captureId },
                    { "sourceKey", sourceKey ?? string.Empty }, { "reason", reason },
                    { "capturedUtc", DateTime.UtcNow } }, new ReplaceOptions { IsUpsert = true }, cancellationToken);

        private async Task CommitProjectionRejectionAsync(string id, string captureId, string sourceKey,
            string reason, BsonDocument resumeToken, CancellationToken cancellationToken)
        {
            using var session = await Database.MongoClient.StartSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            session.StartTransaction();
            try
            {
                await Database.MongoDB.GetCollection<BsonDocument>(DomainCaptureDeadLettersCollection)
                    .ReplaceOneAsync(session, Builders<BsonDocument>.Filter.Eq("_id", id),
                        new BsonDocument { { "_id", id }, { "source", captureId },
                            { "sourceKey", sourceKey ?? string.Empty }, { "reason", reason },
                            { "capturedUtc", DateTime.UtcNow } }, new ReplaceOptions { IsUpsert = true },
                        cancellationToken).ConfigureAwait(false);
                await SaveProjectionCheckpointAsync(session, captureId, resumeToken, cancellationToken)
                    .ConfigureAwait(false);
                await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                if (session.IsInTransaction) await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }
}
