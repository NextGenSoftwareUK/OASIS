using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using Holon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public partial class MongoDBOASIS
    {
        private async Task<OASISResult<bool>> RefreshGeoNftCollectionAvailabilityAsync(Guid avatarId,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            try
            {
                var holons = await Database.MongoDB.GetCollection<Holon>("Holon")
                    .Find(Builders<Holon>.Filter.Eq(x => x.HolonType, HolonType.Web4GeoNFT) &
                          Builders<Holon>.Filter.Eq(x => x.IsActive, true))
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
                if (holons.Count == 0)
                    return await DeleteGeoNftCollectionAvailabilityAsync(avatarId, cancellationToken)
                        .ConfigureAwait(false);
                var loadedAvatars = await LoadAllAvatarDetailsAsync().ConfigureAwait(false);
                if (loadedAvatars == null || loadedAvatars.IsError || loadedAvatars.Result == null)
                    return AvailabilityError(result, "MONGO_GEONFT_AVAILABILITY_AVATARS_UNAVAILABLE",
                        loadedAvatars?.Message ?? "Authoritative avatar details were unavailable.", loadedAvatars?.Exception);
                var avatars = loadedAvatars.Result.ToList();
                if (!avatars.Any(avatar => avatar.Id == avatarId))
                    return AvailabilityError(result, "MONGO_GEONFT_AVAILABILITY_AVATAR_NOT_FOUND",
                        "The authenticated avatar detail was not found.", null);

                var nfts = holons.Select(ToWeb4GeoNft).OrderBy(nft => nft.Id).ToList();
                DateTime now = DateTime.UtcNow;
                var projection = new HyperDriveGeoNftCollectionAvailabilityProjection
                {
                    AvatarId = avatarId,
                    Items = nfts.Select(nft => ToAvailabilityItem(
                        GeoNFTCollectionPolicy.EvaluateForAvatar(nft, avatarId, avatars, now))).ToArray()
                };
                string payload = HyperDriveJson.Serialize(projection);
                Guid versionId = ContentVersion(payload);
                string entityKey = AvatarEntityKey(avatarId,
                    HyperDriveEntityTypes.GeoNftCollectionAvailability, avatarId);

                using var session = await Database.MongoClient.StartSessionAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                session.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot,
                    writeConcern: WriteConcern.WMajority));
                try
                {
                    var entities = Database.MongoDB.GetCollection<BsonDocument>(SyncEntitiesCollection);
                    var current = await entities.Find(session, Builders<BsonDocument>.Filter.Eq("_id", entityKey))
                        .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                    Guid previousVersion = current == null ? Guid.Empty : Guid.Parse(current["versionId"].AsString);
                    if (previousVersion != versionId)
                    {
                        DateTime changedUtc = DateTime.UtcNow;
                        var entity = new BsonDocument
                        {
                            { "_id", entityKey }, { "avatarId", avatarId.ToString("D") }, { "audience", "avatar" },
                            { "entityType", HyperDriveEntityTypes.GeoNftCollectionAvailability },
                            { "entityId", avatarId.ToString("D") }, { "versionId", versionId.ToString("D") },
                            { "isDeleted", false }, { "payloadJson", payload }, { "changedUtc", changedUtc }
                        };
                        await entities.ReplaceOneAsync(session, Builders<BsonDocument>.Filter.Eq("_id", entityKey),
                            entity, new ReplaceOptions { IsUpsert = true }, cancellationToken).ConfigureAwait(false);
                        long sequence = await NextChangeSequenceAsync(session, cancellationToken).ConfigureAwait(false);
                        await Database.MongoDB.GetCollection<BsonDocument>(SyncChangesCollection).InsertOneAsync(session,
                            new BsonDocument
                            {
                                { "_id", sequence }, { "sequence", sequence }, { "changeId", sequence.ToString() },
                                { "avatarId", avatarId.ToString("D") }, { "audience", "avatar" },
                                { "entityType", HyperDriveEntityTypes.GeoNftCollectionAvailability },
                                { "entityId", avatarId.ToString("D") }, { "kind", (int)SyncOperationKind.Upsert },
                                { "versionId", versionId.ToString("D") },
                                { "previousVersionId", previousVersion.ToString("D") },
                                { "payloadJson", payload }, { "changedUtc", changedUtc }
                            }, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    if (session.IsInTransaction)
                        await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                    throw;
                }
                result.Result = true;
                result.IsSaved = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                return AvailabilityError(result, "MONGO_GEONFT_AVAILABILITY_REFRESH_FAILED",
                    "The private GeoNFT availability projection was not committed.", ex);
            }
            return result;
        }

        private async Task<OASISResult<bool>> DeleteGeoNftCollectionAvailabilityAsync(Guid avatarId,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            string entityKey = AvatarEntityKey(avatarId,
                HyperDriveEntityTypes.GeoNftCollectionAvailability, avatarId);
            using var session = await Database.MongoClient.StartSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            session.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot,
                writeConcern: WriteConcern.WMajority));
            try
            {
                var entities = Database.MongoDB.GetCollection<BsonDocument>(SyncEntitiesCollection);
                var current = await entities.Find(session, Builders<BsonDocument>.Filter.Eq("_id", entityKey))
                    .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (current != null && !current["isDeleted"].AsBoolean)
                {
                    Guid previousVersion = Guid.Parse(current["versionId"].AsString);
                    Guid versionId = ContentVersion($"deleted:{avatarId:D}");
                    DateTime changedUtc = DateTime.UtcNow;
                    var tombstone = new BsonDocument
                    {
                        { "_id", entityKey }, { "avatarId", avatarId.ToString("D") }, { "audience", "avatar" },
                        { "entityType", HyperDriveEntityTypes.GeoNftCollectionAvailability },
                        { "entityId", avatarId.ToString("D") }, { "versionId", versionId.ToString("D") },
                        { "isDeleted", true }, { "payloadJson", BsonNull.Value }, { "changedUtc", changedUtc }
                    };
                    await entities.ReplaceOneAsync(session, Builders<BsonDocument>.Filter.Eq("_id", entityKey),
                        tombstone, new ReplaceOptions { IsUpsert = false }, cancellationToken).ConfigureAwait(false);
                    long sequence = await NextChangeSequenceAsync(session, cancellationToken).ConfigureAwait(false);
                    await Database.MongoDB.GetCollection<BsonDocument>(SyncChangesCollection).InsertOneAsync(session,
                        new BsonDocument
                        {
                            { "_id", sequence }, { "sequence", sequence }, { "changeId", sequence.ToString() },
                            { "avatarId", avatarId.ToString("D") }, { "audience", "avatar" },
                            { "entityType", HyperDriveEntityTypes.GeoNftCollectionAvailability },
                            { "entityId", avatarId.ToString("D") }, { "kind", (int)SyncOperationKind.Delete },
                            { "versionId", versionId.ToString("D") },
                            { "previousVersionId", previousVersion.ToString("D") },
                            { "payloadJson", BsonNull.Value }, { "changedUtc", changedUtc }
                        }, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                result.Result = true;
                result.IsSaved = true;
                return result;
            }
            catch
            {
                if (session.IsInTransaction)
                    await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        private static IWeb4GeoSpatialNFT ToWeb4GeoNft(Holon holon)
        {
            var json = JsonConvert.SerializeObject(CreateEdgeHolonProjection(holon));
            var nft = JsonConvert.DeserializeObject<Web4OASISGeoSpatialNFT>(json);
            if (nft == null || nft.Id == Guid.Empty)
                throw new InvalidOperationException("A persisted WEB4 GeoNFT could not be projected with its public identity.");
            return nft;
        }

        private static HyperDriveGeoNftCollectionAvailabilityItem ToAvailabilityItem(GeoNFTCollectionStatus status) =>
            new HyperDriveGeoNftCollectionAvailabilityItem
            {
                GeoNftId = status.GeoNFTId, CanCollect = status.CanCollect, Reason = status.Reason,
                NextCollectAtUtc = status.NextCollectAtUtc,
                PlayerCollectionCount = status.PlayerCollectionCount,
                GlobalCollectionCount = status.GlobalCollectionCount
            };

        private static Guid ContentVersion(string payload)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            var bytes = new byte[16];
            Buffer.BlockCopy(hash, 0, bytes, 0, bytes.Length);
            return new Guid(bytes);
        }

        private static OASISResult<bool> AvailabilityError(OASISResult<bool> result, string code,
            string message, Exception exception)
        {
            result.IsError = true;
            result.ErrorCount = 1;
            result.ErrorCode = code;
            result.Message = message;
            result.Exception = exception;
            return result;
        }
    }
}
