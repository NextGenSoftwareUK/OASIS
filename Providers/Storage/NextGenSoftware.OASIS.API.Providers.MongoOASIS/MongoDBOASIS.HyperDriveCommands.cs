using System;
using System.Collections.Generic;
using System.Text.Json;
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
        public async Task<OASISResult<HostedSyncCommandClaim>> ClaimCommandsAsync(string workerId,
            int maximumCount, DateTime leaseUntilUtc, CancellationToken cancellationToken)
        {
            var result = new OASISResult<HostedSyncCommandClaim>
            {
                Result = new HostedSyncCommandClaim { Items = Array.Empty<HostedSyncCommandItem>() }
            };
            bool globalLeaseAcquired = false;
            if (string.IsNullOrWhiteSpace(workerId) || maximumCount <= 0 || leaseUntilUtc <= DateTime.UtcNow)
                return CommandError(result, "MONGO_COMMAND_CLAIM_INVALID",
                    "WorkerId, a positive maximum count and a future lease expiry are required.");
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                DateTime now = DateTime.UtcNow;
                var leaseFilter = Builders<BsonDocument>.Filter.Eq("_id", "ordered-executor") &
                    (Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId) |
                     Builders<BsonDocument>.Filter.Eq("leaseUntilUtc", BsonNull.Value) |
                     Builders<BsonDocument>.Filter.Lte("leaseUntilUtc", now));
                var lease = await Database.MongoDB.GetCollection<BsonDocument>(SyncCommandLeasesCollection)
                    .FindOneAndUpdateAsync(leaseFilter,
                        Builders<BsonDocument>.Update.Set("leaseOwner", workerId).Set("leaseUntilUtc", leaseUntilUtc),
                        new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After },
                        cancellationToken).ConfigureAwait(false);
                if (lease == null)
                {
                    result.Result = new HostedSyncCommandClaim { LeaseAcquired = false };
                    result.IsLoaded = true;
                    result.Message = "Another ONODE worker owns the ordered command-execution lease.";
                    return result;
                }
                globalLeaseAcquired = true;
                var collection = Database.MongoDB.GetCollection<BsonDocument>(SyncCommandsCollection);
                var claimed = new List<HostedSyncCommandItem>();
                for (int index = 0; index < maximumCount; index++)
                {
                    DateTime claimUtc = DateTime.UtcNow;
                    var incomplete = Builders<BsonDocument>.Filter.Ne("status", "completed");
                    var head = await collection.Find(incomplete)
                        .Sort(Builders<BsonDocument>.Sort.Ascending("commandSequence"))
                        .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                    if (head == null) break;
                    bool pendingReady = head["status"].AsString == "pending" &&
                        head["nextAttemptUtc"].ToUniversalTime() <= claimUtc && head["leaseOwner"].IsBsonNull;
                    bool expiredProcessing = head["status"].AsString == "processing" &&
                        !head["leaseUntilUtc"].IsBsonNull && head["leaseUntilUtc"].ToUniversalTime() <= claimUtc;
                    if (!pendingReady && !expiredProcessing) break;
                    var available = Builders<BsonDocument>.Filter.Eq("_id", head["_id"]) &
                        (Builders<BsonDocument>.Filter.And(
                            Builders<BsonDocument>.Filter.Eq("status", "pending"),
                            Builders<BsonDocument>.Filter.Lte("nextAttemptUtc", claimUtc),
                            Builders<BsonDocument>.Filter.Eq("leaseOwner", BsonNull.Value)) |
                         Builders<BsonDocument>.Filter.And(
                            Builders<BsonDocument>.Filter.Eq("status", "processing"),
                            Builders<BsonDocument>.Filter.Lte("leaseUntilUtc", claimUtc)));
                    var update = Builders<BsonDocument>.Update
                        .Set("leaseOwner", workerId).Set("leaseUntilUtc", leaseUntilUtc)
                        .Set("status", "processing").Inc("attemptCount", 1);
                    var document = await collection.FindOneAndUpdateAsync(available, update,
                        new FindOneAndUpdateOptions<BsonDocument>
                        {
                            ReturnDocument = ReturnDocument.After
                        }, cancellationToken).ConfigureAwait(false);
                    if (document == null) break;
                    claimed.Add(ToCommandItem(document));
                }
                result.Result = new HostedSyncCommandClaim
                {
                    LeaseAcquired = true,
                    Items = claimed
                };
                result.IsLoaded = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                if (globalLeaseAcquired)
                {
                    try { await ReleaseCommandWorkerAfterClaimFailureAsync(workerId).ConfigureAwait(false); }
                    catch (Exception releaseException) { ex = new AggregateException(ex, releaseException); }
                }
                result = CommandError(result, "MONGO_COMMAND_CLAIM_FAILED",
                    "Hosted commands could not be claimed.");
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<bool>> CompleteCommandAsync(Guid operationId, string workerId,
            HyperDriveCommandOutcome outcome, CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            if (operationId == Guid.Empty || string.IsNullOrWhiteSpace(workerId) || outcome == null ||
                outcome.OperationId != operationId || outcome.CompletedUtc.Kind != DateTimeKind.Utc)
                return CommandError(result, "MONGO_COMMAND_COMPLETION_INVALID",
                    "A matching operation, worker, outcome and UTC completion time are required.");
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                using var session = await Database.MongoClient.StartSessionAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                session.StartTransaction();
                try
                {
                    var commands = Database.MongoDB.GetCollection<BsonDocument>(SyncCommandsCollection);
                    var leaseFilter = Builders<BsonDocument>.Filter.Eq("_id", operationId.ToString("D")) &
                        Builders<BsonDocument>.Filter.Eq("status", "processing") &
                        Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId) &
                        Builders<BsonDocument>.Filter.Gt("leaseUntilUtc", DateTime.UtcNow);
                    var command = await commands.Find(session, leaseFilter).FirstOrDefaultAsync(cancellationToken)
                        .ConfigureAwait(false);
                    if (command == null)
                    {
                        await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                        return CommandError(result, "MONGO_COMMAND_LEASE_LOST",
                            "The hosted command lease is missing, expired, or owned by another worker.");
                    }

                    Guid avatarId = Guid.Parse(command["avatarId"].AsString);
                    Guid versionId = Guid.Parse(command["versionId"].AsString);
                    string payload = JsonSerializer.Serialize(outcome);
                    string entityKey = AvatarEntityKey(avatarId, HyperDriveEntityTypes.CommandResult, operationId);
                    DateTime changedUtc = outcome.CompletedUtc;
                    await Database.MongoDB.GetCollection<BsonDocument>(SyncEntitiesCollection).ReplaceOneAsync(session,
                        Builders<BsonDocument>.Filter.Eq("_id", entityKey), new BsonDocument
                        {
                            { "_id", entityKey }, { "avatarId", avatarId.ToString("D") }, { "audience", "avatar" },
                            { "entityType", HyperDriveEntityTypes.CommandResult },
                            { "entityId", operationId.ToString("D") }, { "versionId", versionId.ToString("D") },
                            { "isDeleted", false }, { "payloadJson", payload }, { "changedUtc", changedUtc }
                        }, new ReplaceOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                    long sequence = await NextChangeSequenceAsync(session, cancellationToken).ConfigureAwait(false);
                    await Database.MongoDB.GetCollection<BsonDocument>(SyncChangesCollection).InsertOneAsync(session,
                        new BsonDocument
                        {
                            { "_id", sequence }, { "sequence", sequence },
                            { "changeId", $"command:{operationId:D}" }, { "avatarId", avatarId.ToString("D") },
                            { "audience", "avatar" }, { "entityType", HyperDriveEntityTypes.CommandResult },
                            { "entityId", operationId.ToString("D") }, { "kind", (int)SyncOperationKind.Upsert },
                            { "versionId", versionId.ToString("D") },
                            { "previousVersionId", Guid.Empty.ToString("D") }, { "payloadJson", payload },
                            { "changedUtc", changedUtc }
                        }, cancellationToken: cancellationToken).ConfigureAwait(false);
                    var completed = await commands.UpdateOneAsync(session, leaseFilter,
                        Builders<BsonDocument>.Update.Set("status", "completed")
                            .Set("completedUtc", changedUtc).Set("leaseOwner", BsonNull.Value)
                            .Set("leaseUntilUtc", BsonNull.Value), cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    if (completed.ModifiedCount != 1)
                        throw new InvalidOperationException("The command lease changed before completion committed.");
                    await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                    result.Result = true;
                    result.IsSaved = true;
                }
                catch
                {
                    if (session.IsInTransaction)
                        await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                result = CommandError(result, "MONGO_COMMAND_COMPLETION_FAILED",
                    "The command outcome and private change feed were not committed.");
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<bool>> RenewCommandLeaseAsync(Guid operationId, string workerId,
            DateTime leaseUntilUtc, CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            if (operationId == Guid.Empty || string.IsNullOrWhiteSpace(workerId) || leaseUntilUtc <= DateTime.UtcNow)
                return CommandError(result, "MONGO_COMMAND_RENEW_INVALID",
                    "Operation, lease-owning worker and a future lease expiry are required.");
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                DateTime now = DateTime.UtcNow;
                var global = await Database.MongoDB.GetCollection<BsonDocument>(SyncCommandLeasesCollection)
                    .UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", "ordered-executor") &
                        Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId) &
                        Builders<BsonDocument>.Filter.Gt("leaseUntilUtc", now),
                        Builders<BsonDocument>.Update.Set("leaseUntilUtc", leaseUntilUtc),
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                if (global.MatchedCount != 1)
                    return CommandError(result, "MONGO_COMMAND_WORKER_LEASE_LOST",
                        "The ordered command worker lease expired or changed owner.");
                var item = await Database.MongoDB.GetCollection<BsonDocument>(SyncCommandsCollection)
                    .UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", operationId.ToString("D")) &
                        Builders<BsonDocument>.Filter.Eq("status", "processing") &
                        Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId) &
                        Builders<BsonDocument>.Filter.Gt("leaseUntilUtc", now),
                        Builders<BsonDocument>.Update.Set("leaseUntilUtc", leaseUntilUtc),
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                if (item.MatchedCount != 1)
                    return CommandError(result, "MONGO_COMMAND_LEASE_LOST",
                        "The command lease expired or changed owner.");
                result.Result = true;
                result.IsSaved = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                result = CommandError(result, "MONGO_COMMAND_RENEW_FAILED", "The command lease was not renewed.");
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<bool>> FailCommandAttemptAsync(Guid operationId, string workerId,
            string errorCode, string message, DateTime nextAttemptUtc, CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            if (operationId == Guid.Empty || string.IsNullOrWhiteSpace(workerId) ||
                string.IsNullOrWhiteSpace(errorCode) || nextAttemptUtc <= DateTime.UtcNow)
                return CommandError(result, "MONGO_COMMAND_FAILURE_INVALID",
                    "Operation, worker, error code and a future retry time are required.");
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", operationId.ToString("D")) &
                    Builders<BsonDocument>.Filter.Eq("status", "processing") &
                    Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId);
                var update = Builders<BsonDocument>.Update.Set("status", "pending")
                    .Set("nextAttemptUtc", nextAttemptUtc).Set("lastErrorCode", errorCode)
                    .Set("lastErrorMessage", message ?? string.Empty).Set("leaseOwner", BsonNull.Value)
                    .Set("leaseUntilUtc", BsonNull.Value);
                var updated = await Database.MongoDB.GetCollection<BsonDocument>(SyncCommandsCollection)
                    .UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (updated.ModifiedCount != 1)
                    return CommandError(result, "MONGO_COMMAND_LEASE_LOST",
                        "The hosted command lease is missing or owned by another worker.");
                result.Result = true;
                result.IsSaved = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                result = CommandError(result, "MONGO_COMMAND_RETRY_FAILED",
                    "The command retry state was not persisted.");
                result.Exception = ex;
            }
            return result;
        }

        public async Task<OASISResult<bool>> ReleaseCommandWorkerAsync(string workerId,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            if (string.IsNullOrWhiteSpace(workerId))
                return CommandError(result, "MONGO_COMMAND_WORKER_REQUIRED", "WorkerId is required.");
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                var filter = Builders<BsonDocument>.Filter.Eq("status", "processing") &
                    Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId);
                var update = Builders<BsonDocument>.Update.Set("status", "pending")
                    .Set("nextAttemptUtc", DateTime.UtcNow).Set("leaseOwner", BsonNull.Value)
                    .Set("leaseUntilUtc", BsonNull.Value);
                await Database.MongoDB.GetCollection<BsonDocument>(SyncCommandsCollection)
                    .UpdateManyAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
                var leaseFilter = Builders<BsonDocument>.Filter.Eq("_id", "ordered-executor") &
                    Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId);
                var released = await Database.MongoDB.GetCollection<BsonDocument>(SyncCommandLeasesCollection)
                    .UpdateOneAsync(leaseFilter, Builders<BsonDocument>.Update
                        .Set("leaseOwner", BsonNull.Value).Set("leaseUntilUtc", BsonNull.Value),
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                if (released.MatchedCount != 1)
                    return CommandError(result, "MONGO_COMMAND_WORKER_LEASE_LOST",
                        "The ordered command worker lease is no longer owned by this worker.");
                result.Result = true;
                result.IsSaved = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                result = CommandError(result, "MONGO_COMMAND_RELEASE_FAILED",
                    "The command worker leases were not released.");
                result.Exception = ex;
            }
            return result;
        }

        private static HostedSyncCommandItem ToCommandItem(BsonDocument document) => new HostedSyncCommandItem
        {
            OperationId = Guid.Parse(document["operationId"].AsString),
            AvatarId = Guid.Parse(document["avatarId"].AsString),
            DeviceId = Guid.Parse(document["deviceId"].AsString),
            DeviceSequence = document["deviceSequence"].AsInt64,
            CommandSequence = document["commandSequence"].AsInt64,
            EntityId = Guid.Parse(document["entityId"].AsString),
            EntityType = document["entityType"].AsString,
            VersionId = Guid.Parse(document["versionId"].AsString),
            PayloadJson = document["payloadJson"].AsString,
            AttemptCount = document["attemptCount"].AsInt32
        };

        private async Task ReleaseCommandWorkerAfterClaimFailureAsync(string workerId)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", "ordered-executor") &
                Builders<BsonDocument>.Filter.Eq("leaseOwner", workerId);
            var released = await Database.MongoDB.GetCollection<BsonDocument>(SyncCommandLeasesCollection)
                .UpdateOneAsync(filter, Builders<BsonDocument>.Update.Set("leaseOwner", BsonNull.Value)
                    .Set("leaseUntilUtc", BsonNull.Value), cancellationToken: CancellationToken.None)
                .ConfigureAwait(false);
            if (released.MatchedCount != 1)
                throw new InvalidOperationException("The ordered command lease could not be released after claim failure.");
        }

        private static OASISResult<T> CommandError<T>(OASISResult<T> result, string code, string message)
        {
            result.IsError = true;
            result.ErrorCount = 1;
            result.ErrorCode = code;
            result.Message = message;
            return result;
        }
    }
}
