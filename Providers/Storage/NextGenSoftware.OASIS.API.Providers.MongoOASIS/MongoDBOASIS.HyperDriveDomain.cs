using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Holon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public partial class MongoDBOASIS
    {
        private const string DomainMutationReceiptsCollection = "HyperDriveDomainMutationReceipts";

        /// <summary>
        /// Public contract for full-runtime callers. Sync ingestion uses the session-aware overload below so
        /// the domain document and sync receipt share one MongoDB transaction.
        /// </summary>
        public async Task<OASISResult<HostedDomainMutationResult>> ApplyDomainMutationAsync(
            HostedSyncFanOutItem mutation, CancellationToken cancellationToken)
        {
            var result = new OASISResult<HostedDomainMutationResult>();
            if (mutation == null || mutation.OperationId == Guid.Empty || mutation.AvatarId == Guid.Empty ||
                mutation.EntityId == Guid.Empty || mutation.VersionId == Guid.Empty ||
                string.IsNullOrWhiteSpace(mutation.EntityType) ||
                (mutation.Kind != SyncOperationKind.Upsert && mutation.Kind != SyncOperationKind.Delete) ||
                (mutation.Kind == SyncOperationKind.Upsert && string.IsNullOrWhiteSpace(mutation.PayloadJson)))
                return DomainError(result, "MONGO_DOMAIN_MUTATION_INVALID",
                    "OperationId, AvatarId, EntityId, VersionId, EntityType and a supported mutation kind are required; upserts also require a payload.");
            try
            {
                await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
                using (var session = await Database.MongoClient.StartSessionAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false))
                {
                    session.StartTransaction();
                    try
                    {
                        var operation = new SyncOperation
                        {
                            OperationId = mutation.OperationId,
                            AvatarId = mutation.AvatarId,
                            EntityId = mutation.EntityId,
                            EntityType = mutation.EntityType,
                            Kind = mutation.Kind,
                            VersionId = mutation.VersionId,
                            PayloadJson = mutation.PayloadJson
                        };
                        result = await ApplyDomainMutationWithReceiptInTransactionAsync(session, operation, cancellationToken)
                            .ConfigureAwait(false);
                        if (result.IsError)
                        {
                            await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                            return result;
                        }
                        await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        if (session.IsInTransaction)
                            await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                        throw;
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                result = DomainError(result, "MONGO_DOMAIN_MUTATION_FAILED",
                    "The authoritative OASIS domain mutation was not committed.");
                result.Exception = ex;
            }
            return result;
        }

        private async Task<OASISResult<HostedDomainMutationResult>> ApplyDomainMutationWithReceiptInTransactionAsync(
            IClientSessionHandle session, SyncOperation operation, CancellationToken cancellationToken)
        {
            var receipts = Database.MongoDB.GetCollection<BsonDocument>(DomainMutationReceiptsCollection);
            string operationId = operation.OperationId.ToString("D");
            var existing = await receipts.Find(session, Builders<BsonDocument>.Filter.Eq("_id", operationId))
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (existing != null)
            {
                bool matches = existing["avatarId"].AsString == operation.AvatarId.ToString("D") &&
                    existing["entityId"].AsString == operation.EntityId.ToString("D") &&
                    existing["entityType"].AsString == operation.EntityType &&
                    existing["kind"].AsInt32 == (int)operation.Kind &&
                    existing["versionId"].AsString == operation.VersionId.ToString("D") &&
                    BsonNullableString(existing["payloadJson"]) == operation.PayloadJson;
                if (!matches)
                    return DomainError(new OASISResult<HostedDomainMutationResult>(),
                        "HOSTED_DOMAIN_OPERATION_REUSE_MISMATCH",
                        "The domain operation id was already used for different immutable content.");
                return new OASISResult<HostedDomainMutationResult>
                {
                    Result = new HostedDomainMutationResult
                    {
                        AlreadyApplied = true,
                        VersionId = Guid.Parse(existing["versionId"].AsString)
                    },
                    IsSaved = true
                };
            }

            var applied = await ApplyDomainMutationInTransactionAsync(session, operation, cancellationToken)
                .ConfigureAwait(false);
            if (applied.IsError) return applied;
            var receipt = new BsonDocument
            {
                { "_id", operationId },
                { "avatarId", operation.AvatarId.ToString("D") },
                { "entityId", operation.EntityId.ToString("D") },
                { "entityType", operation.EntityType },
                { "kind", (int)operation.Kind },
                { "versionId", operation.VersionId.ToString("D") },
                { "payloadJson", operation.PayloadJson == null ? BsonNull.Value : operation.PayloadJson },
                { "committedUtc", DateTime.UtcNow }
            };
            await receipts.InsertOneAsync(session, receipt, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return applied;
        }

        private async Task<OASISResult<HostedDomainMutationResult>> ApplyDomainMutationInTransactionAsync(
            IClientSessionHandle session, SyncOperation operation, CancellationToken cancellationToken)
        {
            var result = new OASISResult<HostedDomainMutationResult>();
            if (!IsHolonDomainEntityType(operation.EntityType))
                return DomainError(result, "HOSTED_DOMAIN_CODEC_NOT_REGISTERED",
                    $"No authoritative domain codec is registered for '{operation.EntityType}'.");

            var collection = Database.MongoDB.GetCollection<Holon>("Holon");
            var filter = Builders<Holon>.Filter.Eq(x => x.HolonId, operation.EntityId);
            Holon current = await collection.Find(session, filter).FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (operation.Kind == SyncOperationKind.Delete)
            {
                if (current == null)
                    return DomainError(result, "HOSTED_DOMAIN_ENTITY_NOT_FOUND",
                        $"Holon '{operation.EntityId}' does not exist and cannot be deleted.");
                current.IsActive = false;
                current.DeletedDate = DateTime.UtcNow;
                current.DeletedByAvatarId = operation.AvatarId.ToString("D");
                current.PreviousVersionId = current.VersionId;
                current.VersionId = operation.VersionId;
                current.Version++;
                var deleted = await collection.ReplaceOneAsync(session, filter, current,
                    new ReplaceOptions { IsUpsert = false }, cancellationToken).ConfigureAwait(false);
                if (!deleted.IsAcknowledged || deleted.MatchedCount != 1)
                    return DomainError(result, "HOSTED_DOMAIN_DELETE_NOT_COMMITTED",
                        $"Holon '{operation.EntityId}' was not durably soft-deleted.");
            }
            else
            {
                Holon incoming;
                try
                {
                    incoming = JsonSerializer.Deserialize<Holon>(operation.PayloadJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException ex)
                {
                    result = DomainError(result, "HOSTED_DOMAIN_PAYLOAD_INVALID",
                        "The holon payload does not match the authoritative OASIS holon contract.");
                    result.Exception = ex;
                    return result;
                }
                if (incoming == null)
                    return DomainError(result, "HOSTED_DOMAIN_PAYLOAD_INVALID", "The holon payload is null.");
                if (!HolonTypeMatchesEntityType(operation.EntityType, incoming.HolonType))
                    return DomainError(result, "HOSTED_DOMAIN_TYPE_MISMATCH",
                        $"HolonType '{incoming.HolonType}' does not match synchronized entity type '{operation.EntityType}'.");
                if (incoming.HolonId != Guid.Empty && incoming.HolonId != operation.EntityId)
                    return DomainError(result, "HOSTED_DOMAIN_ID_MISMATCH",
                        "The payload HolonId does not match the synchronized EntityId.");
                if (!string.IsNullOrWhiteSpace(incoming.CreatedByAvatarId) &&
                    !string.Equals(incoming.CreatedByAvatarId, operation.AvatarId.ToString("D"),
                        StringComparison.OrdinalIgnoreCase))
                    return DomainError(result, "HOSTED_DOMAIN_AVATAR_SCOPE_MISMATCH",
                        "The payload owner does not match the authenticated avatar.");

                incoming.HolonId = operation.EntityId;
                incoming.CreatedByAvatarId = operation.AvatarId.ToString("D");
                incoming.ModifiedByAvatarId = operation.AvatarId.ToString("D");
                incoming.ModifiedDate = DateTime.UtcNow;
                incoming.VersionId = operation.VersionId;
                incoming.IsActive = true;
                incoming.DeletedDate = DateTime.MinValue;
                incoming.CreatedProviderType = new EnumValue<ProviderType>(
                    NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS);
                if (current == null)
                {
                    incoming.Id = ObjectId.GenerateNewId().ToString();
                    incoming.CreatedDate = incoming.CreatedDate == DateTime.MinValue
                        ? DateTime.UtcNow : incoming.CreatedDate;
                    incoming.Version = Math.Max(1, incoming.Version);
                }
                else
                {
                    incoming.Id = current.Id;
                    incoming.CreatedDate = current.CreatedDate;
                    incoming.CreatedByAvatarId = current.CreatedByAvatarId;
                    incoming.PreviousVersionId = current.VersionId;
                    incoming.Version = current.Version + 1;
                    incoming.ProviderUniqueStorageKey = current.ProviderUniqueStorageKey;
                }
                incoming.ProviderUniqueStorageKey[
                    NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS] = incoming.Id;
                var saved = await collection.ReplaceOneAsync(session, filter, incoming,
                    new ReplaceOptions { IsUpsert = current == null }, cancellationToken).ConfigureAwait(false);
                if (!saved.IsAcknowledged)
                    return DomainError(result, "HOSTED_DOMAIN_SAVE_NOT_COMMITTED",
                        $"Holon '{operation.EntityId}' was not durably saved.");
            }

            result.Result = new HostedDomainMutationResult { VersionId = operation.VersionId };
            result.IsSaved = true;
            return result;
        }

        private static bool IsHolonDomainEntityType(string entityType) =>
            entityType == HyperDriveEntityTypes.Holon ||
            entityType == HyperDriveEntityTypes.Quest ||
            entityType == HyperDriveEntityTypes.InventoryItem ||
            entityType == HyperDriveEntityTypes.GeoNft ||
            entityType == HyperDriveEntityTypes.GeoNftCollection;

        private static bool IsGlobalDomainEntityType(string entityType) =>
            entityType == HyperDriveEntityTypes.Quest ||
            entityType == HyperDriveEntityTypes.GeoNft ||
            entityType == HyperDriveEntityTypes.GeoNftCollection;

        private static string SyncAudienceForEntityType(string entityType) =>
            IsGlobalDomainEntityType(entityType) ? "global" : "avatar";

        private static bool HolonTypeMatchesEntityType(string entityType, HolonType holonType)
        {
            if (entityType == HyperDriveEntityTypes.Holon)
                return true;
            if (entityType == HyperDriveEntityTypes.Quest)
                return holonType == HolonType.Quest;
            if (entityType == HyperDriveEntityTypes.InventoryItem)
                return holonType == HolonType.InventoryItem;
            if (entityType == HyperDriveEntityTypes.GeoNft)
                return holonType == HolonType.Web4GeoNFT || holonType == HolonType.Web5GeoNFT;
            if (entityType == HyperDriveEntityTypes.GeoNftCollection)
                return holonType == HolonType.Web4GeoNFTCollection || holonType == HolonType.Web5GeoNFTCollection;
            return false;
        }

        private static string EntityTypeForHolon(Holon holon)
        {
            if (holon == null) return HyperDriveEntityTypes.Holon;
            switch (holon.HolonType)
            {
                case HolonType.Quest: return HyperDriveEntityTypes.Quest;
                case HolonType.InventoryItem: return HyperDriveEntityTypes.InventoryItem;
                case HolonType.Web4GeoNFT:
                case HolonType.Web5GeoNFT: return HyperDriveEntityTypes.GeoNft;
                case HolonType.Web4GeoNFTCollection:
                case HolonType.Web5GeoNFTCollection: return HyperDriveEntityTypes.GeoNftCollection;
                default: return HyperDriveEntityTypes.Holon;
            }
        }

        private static OASISResult<HostedDomainMutationResult> DomainError(
            OASISResult<HostedDomainMutationResult> result, string code, string message)
        {
            result.IsError = true;
            result.ErrorCount = 1;
            result.ErrorCode = code;
            result.Message = message;
            return result;
        }

        private static string BsonNullableString(BsonValue value) =>
            value == null || value.IsBsonNull ? null : value.AsString;
    }
}
