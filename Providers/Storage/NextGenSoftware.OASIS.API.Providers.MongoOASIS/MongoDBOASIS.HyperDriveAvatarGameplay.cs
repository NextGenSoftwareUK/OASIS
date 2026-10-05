using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;
using MongoHolon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public partial class MongoDBOASIS : IHostedAvatarGameplayCommandStore
    {
        public async Task<OASISResult<HyperDriveAvatarDetailProjection>> ApplyAvatarGameplayCommandAsync(
            HostedSyncCommandItem command, CancellationToken cancellationToken)
        {
            if (command == null || command.OperationId == Guid.Empty || command.AvatarId == Guid.Empty ||
                command.EntityId != command.AvatarId || command.EntityType != HyperDriveEntityTypes.AvatarGameplay)
                return GameplayRejected("An avatar-scoped command with a stable operation identity is required.");
            HyperDriveAvatarGameplayCommand payload;
            try { payload = HyperDriveJson.Deserialize<HyperDriveAvatarGameplayCommand>(command.PayloadJson); }
            catch (System.Text.Json.JsonException ex) { return GameplayRejected(ex.Message); }
            await EnsureSyncInitializedAsync(cancellationToken).ConfigureAwait(false);
            using var session = await Database.MongoClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            session.StartTransaction();
            try
            {
                var collection = Database.MongoDB.GetCollection<AvatarDetail>("AvatarDetail");
                var detail = await collection.Find(session, x => x.HolonId == command.AvatarId)
                    .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (detail == null) return GameplayRejected("The avatar detail was not found.");
                var canonicalPayload = HyperDriveJson.Serialize(payload);
                if (!TryReadReceipts(detail, out var receipts, out var receiptError))
                    return GameplayReceiptInvalid(receiptError);
                if (payload.Action == HyperDriveAvatarGameplayAction.TransferInventoryToClan)
                    return await ApplyClanTransferAsync(session, collection, detail, command, payload,
                        canonicalPayload, receipts, cancellationToken).ConfigureAwait(false);
                if (payload.Action == HyperDriveAvatarGameplayAction.TransferInventory)
                    return await ApplyTransferAsync(session, collection, detail, command, payload,
                        canonicalPayload, receipts, cancellationToken).ConfigureAwait(false);
                if (receipts.TryGetValue(command.OperationId, out var original))
                    return original == canonicalPayload
                        ? new OASISResult<HyperDriveAvatarDetailProjection>(CreateEdgeAvatarDetailProjection(detail))
                        : GameplayRejected("The operation identity was reused with a different payload.");

                var applied = HyperDriveAvatarGameplay.Apply(CreateEdgeAvatarDetailProjection(detail), payload);
                if (applied.IsError) return applied;
                detail.XP = applied.Result.Xp;
                detail.Karma = applied.Result.Karma;
                detail.ActiveQuestId = applied.Result.ActiveQuestId;
                detail.ActiveObjectiveId = applied.Result.ActiveObjectiveId;
                if (payload.Action == HyperDriveAvatarGameplayAction.AddKarma ||
                    payload.Action == HyperDriveAvatarGameplayAction.DeductKarma)
                {
                    if (!Enum.TryParse(payload.KarmaSourceType, true, out KarmaSourceType sourceType) ||
                        !Enum.IsDefined(typeof(KarmaSourceType), sourceType))
                        return GameplayRejected($"Karma source type '{payload.KarmaSourceType}' is invalid.");
                    bool adding = payload.Action == HyperDriveAvatarGameplayAction.AddKarma;
                    bool positiveTypeValid = Enum.TryParse(payload.KarmaType, true, out KarmaTypePositive positiveType) &&
                        Enum.IsDefined(typeof(KarmaTypePositive), positiveType);
                    bool negativeTypeValid = Enum.TryParse(payload.KarmaType, true, out KarmaTypeNegative negativeType) &&
                        Enum.IsDefined(typeof(KarmaTypeNegative), negativeType);
                    if (adding && !positiveTypeValid)
                        return GameplayRejected($"Positive Karma type '{payload.KarmaType}' is invalid.");
                    if (!adding && !negativeTypeValid)
                        return GameplayRejected($"Negative Karma type '{payload.KarmaType}' is invalid.");
                    int authoritativeAmount = HyperDriveKarmaPolicy.ResolveAmount(adding, payload.KarmaType);
                    if (authoritativeAmount <= 0 || payload.Amount != authoritativeAmount)
                        return GameplayRejected("The Karma amount does not match the authoritative Karma type weighting.");
                    detail.KarmaAkashicRecords ??= new List<KarmaAkashicRecord>();
                    detail.KarmaAkashicRecords.Add(new KarmaAkashicRecord
                    {
                        AvatarId = command.AvatarId,
                        Date = payload.KarmaOccurredAtUtc,
                        Karma = adding ? payload.Amount : -payload.Amount,
                        TotalKarma = applied.Result.Karma,
                        Provider = new EnumValue<ProviderType>(
                            NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS),
                        KarmaSourceTitle = payload.KarmaSourceTitle,
                        KarmaSourceDesc = payload.KarmaSourceDescription,
                        KarmaSource = new EnumValue<KarmaSourceType>(sourceType),
                        KarmaEarntOrLost = new EnumValue<KarmaEarntOrLost>(adding
                            ? KarmaEarntOrLost.Earnt : KarmaEarntOrLost.Lost),
                        KarmaTypePositive = new EnumValue<KarmaTypePositive>(adding
                            ? positiveType : KarmaTypePositive.None),
                        KarmaTypeNegative = new EnumValue<KarmaTypeNegative>(adding
                            ? KarmaTypeNegative.None : negativeType)
                    });
                }
                if (payload.Action == HyperDriveAvatarGameplayAction.UpdateInventory)
                {
                    var update = payload.InventoryUpdate;
                    if (!Enum.IsDefined(typeof(NextGenSoftware.OASIS.API.Core.Enums.InventoryItemType), update.ItemType))
                        return GameplayRejected($"Inventory item type '{update.ItemType}' is invalid.");
                    if (!string.IsNullOrWhiteSpace(update.ImageUrl) &&
                        !Uri.TryCreate(update.ImageUrl, UriKind.Absolute, out _))
                        return GameplayRejected("Inventory image URL must be absolute.");
                    var persisted = detail.Inventory?.SingleOrDefault(x => x.Id == update.Id);
                    if (persisted == null) return GameplayRejected("The inventory stack is missing.");
                    persisted.Name = update.Name;
                    persisted.Description = update.Description;
                    persisted.Quantity = update.Quantity;
                    persisted.GameSource = update.GameSource;
                    persisted.ItemType = (NextGenSoftware.OASIS.API.Core.Enums.InventoryItemType)update.ItemType;
                    persisted.NftId = update.NftId;
                    persisted.GeoNFTId = update.GeoNftId;
                    persisted.Rarity = update.Rarity;
                    persisted.MaxQuantity = update.MaxQuantity;
                    persisted.Weight = update.Weight;
                    persisted.Stack = update.IsStackable;
                    persisted.IsUsable = update.IsUsable;
                    persisted.IsTradeable = update.IsTradeable;
                    persisted.AcquiredOn = update.AcquiredOn;
                    persisted.LastUsedOn = update.LastUsedOn;
                    persisted.Image2DURI = string.IsNullOrWhiteSpace(update.ImageUrl) ? null : new Uri(update.ImageUrl);
                    persisted.Properties = (update.Properties ?? new Dictionary<string, string>())
                        .ToDictionary(pair => pair.Key, pair => (object)pair.Value);
                    persisted.Properties["OurWorld.Value"] = update.Value.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(update.ThumbnailUrl))
                        persisted.Properties["OurWorld.ThumbnailUrl"] = update.ThumbnailUrl;
                }
                var quantities = applied.Result.Inventory.ToDictionary(x => x.Id, x => x.Quantity);
                if (detail.Inventory != null)
                {
                    detail.Inventory = detail.Inventory.Where(x => quantities.ContainsKey(x.Id)).ToList();
                    foreach (var item in detail.Inventory) item.Quantity = quantities[item.Id];
                }
                receipts.Add(command.OperationId, canonicalPayload);
                detail.MetaData[HyperDriveAvatarGameplay.ReceiptMetadataKey] = JsonConvert.SerializeObject(receipts);
                detail.VersionId = Guid.NewGuid();
                await collection.ReplaceOneAsync(session, x => x.Id == detail.Id, detail,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                // The domain change stream projects this document, including its receipt, as one version.
                await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                return new OASISResult<HyperDriveAvatarDetailProjection>(CreateEdgeAvatarDetailProjection(detail));
            }
            finally
            {
                if (session.IsInTransaction) await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        private async Task<OASISResult<HyperDriveAvatarDetailProjection>> ApplyTransferAsync(
            IClientSessionHandle session, IMongoCollection<AvatarDetail> collection, AvatarDetail source,
            HostedSyncCommandItem command, HyperDriveAvatarGameplayCommand payload, string canonicalPayload,
            Dictionary<Guid, string> sourceReceipts, CancellationToken cancellationToken)
        {
            var target = await collection.Find(session, x => x.HolonId == payload.TargetAvatarId.Value)
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (target == null) return GameplayRejected("The target avatar detail was not found.");
            if (!TryReadReceipts(target, out var targetReceipts, out var receiptError))
                return GameplayReceiptInvalid(receiptError);

            bool sourceApplied = sourceReceipts.TryGetValue(command.OperationId, out var sourceOriginal);
            bool targetApplied = targetReceipts.TryGetValue(command.OperationId, out var targetOriginal);
            if (sourceApplied || targetApplied)
            {
                if (!sourceApplied || !targetApplied || sourceOriginal != canonicalPayload || targetOriginal != canonicalPayload)
                    return GameplayRejected("The avatar transfer receipt invariant is inconsistent.");
                return new OASISResult<HyperDriveAvatarDetailProjection>(CreateEdgeAvatarDetailProjection(source));
            }

            var sourceProjection = CreateEdgeAvatarDetailProjection(source);
            var applied = HyperDriveAvatarGameplay.Apply(sourceProjection, payload);
            if (applied.IsError) return applied;

            var sourceItem = source.Inventory?.SingleOrDefault(x => x.Id == payload.InventoryItemId.Value);
            if (sourceItem == null)
                return GameplayRejected("The inventory stack is missing.");
            target.Inventory ??= new List<NextGenSoftware.OASIS.API.Core.Objects.InventoryItem>();
            if (target.Inventory.Any(x => x.Id == sourceItem.Id))
                return GameplayRejected("The target inventory already contains this item identity.");

            var moved = CloneInventoryItem(sourceItem, sourceItem.Quantity);
            source.Inventory.Remove(sourceItem);
            target.Inventory.Add(moved);
            RecordReceipt(source, sourceReceipts, command.OperationId, canonicalPayload);
            RecordReceipt(target, targetReceipts, command.OperationId, canonicalPayload);
            source.VersionId = Guid.NewGuid();
            target.VersionId = Guid.NewGuid();
            await collection.ReplaceOneAsync(session, x => x.Id == source.Id, source,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            await collection.ReplaceOneAsync(session, x => x.Id == target.Id, target,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            return new OASISResult<HyperDriveAvatarDetailProjection>(CreateEdgeAvatarDetailProjection(source));
        }


        private async Task<OASISResult<HyperDriveAvatarDetailProjection>> ApplyClanTransferAsync(
            IClientSessionHandle session, IMongoCollection<AvatarDetail> avatarCollection, AvatarDetail source,
            HostedSyncCommandItem command, HyperDriveAvatarGameplayCommand payload, string canonicalPayload,
            Dictionary<Guid, string> sourceReceipts, CancellationToken cancellationToken)
        {
            if (!payload.TargetClanId.HasValue || payload.TargetClanId == Guid.Empty)
                return GameplayRejected("The target clan identity is required.");

            var holonCollection = Database.MongoDB.GetCollection<MongoHolon>("Holon");
            var clan = await holonCollection.Find(session, x => x.HolonId == payload.TargetClanId.Value &&
                    x.HolonType == HolonType.Clan)
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (clan == null) return GameplayRejected("The target clan was not found.");
            clan.MetaData ??= new Dictionary<string, object>();
            if (!clan.MetaData.TryGetValue(ClanManager.ClanStateMetadataKey, out object stateValue))
                return GameplayRejected("The target clan is missing its canonical persisted state.");

            ClanManager.ClanPersistentState clanState;
            Dictionary<Guid, string> clanReceipts;
            try
            {
                clanState = JsonConvert.DeserializeObject<ClanManager.ClanPersistentState>(stateValue?.ToString() ?? string.Empty);
                clanReceipts = clan.MetaData.TryGetValue(HyperDriveAvatarGameplay.ClanReceiptMetadataKey, out object ledger)
                    ? JsonConvert.DeserializeObject<Dictionary<Guid, string>>(ledger?.ToString() ?? string.Empty)
                        ?? new Dictionary<Guid, string>()
                    : new Dictionary<Guid, string>();
            }
            catch (JsonException ex)
            {
                return GameplayRejected($"The target clan state is invalid: {ex.Message}");
            }
            if (clanState == null || clanState.OwnerAvatarId == Guid.Empty)
                return GameplayRejected("The target clan state is invalid.");

            bool sourceApplied = sourceReceipts.TryGetValue(command.OperationId, out string sourceOriginal);
            bool clanApplied = clanReceipts.TryGetValue(command.OperationId, out string clanOriginal);
            if (sourceApplied || clanApplied)
            {
                if (!sourceApplied || !clanApplied || sourceOriginal != canonicalPayload || clanOriginal != canonicalPayload)
                    return GameplayRejected("The clan transfer receipt invariant is inconsistent.");
                return new OASISResult<HyperDriveAvatarDetailProjection>(CreateEdgeAvatarDetailProjection(source));
            }

            var sourceProjection = CreateEdgeAvatarDetailProjection(source);
            var applied = HyperDriveAvatarGameplay.Apply(sourceProjection, payload);
            if (applied.IsError) return applied;
            var sourceItem = source.Inventory?.SingleOrDefault(x => x.Id == payload.InventoryItemId.Value);
            if (sourceItem == null || sourceItem.Quantity < payload.Amount)
                return GameplayRejected("The inventory stack is missing or has insufficient quantity.");
            clanState.Inventory ??= new List<ClanManager.ClanPersistentInventoryItem>();
            if (clanState.Inventory.Any(x => x.Id == payload.DestinationInventoryItemId.Value))
                return GameplayRejected("The clan inventory already contains the destination item identity.");

            var destination = ClanManager.ToPersistentInventoryItem(sourceItem);
            destination.Id = payload.DestinationInventoryItemId.Value;
            destination.Quantity = payload.Amount;
            sourceItem.Quantity -= payload.Amount;
            if (sourceItem.Quantity == 0) source.Inventory.Remove(sourceItem);
            clanState.Inventory.Add(destination);

            RecordReceipt(source, sourceReceipts, command.OperationId, canonicalPayload);
            clanReceipts.Add(command.OperationId, canonicalPayload);
            clan.MetaData[ClanManager.ClanStateMetadataKey] = JsonConvert.SerializeObject(clanState);
            clan.MetaData[HyperDriveAvatarGameplay.ClanReceiptMetadataKey] = JsonConvert.SerializeObject(clanReceipts);
            source.VersionId = Guid.NewGuid();
            clan.VersionId = Guid.NewGuid();
            await avatarCollection.ReplaceOneAsync(session, x => x.Id == source.Id, source,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            await holonCollection.ReplaceOneAsync(session, x => x.Id == clan.Id, clan,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            return new OASISResult<HyperDriveAvatarDetailProjection>(CreateEdgeAvatarDetailProjection(source));
        }

        private static bool TryReadReceipts(AvatarDetail detail, out Dictionary<Guid, string> receipts,
            out string error)
        {
            detail.MetaData ??= new Dictionary<string, object>();
            receipts = new Dictionary<Guid, string>();
            error = null;
            if (!detail.MetaData.TryGetValue(HyperDriveAvatarGameplay.ReceiptMetadataKey, out var value))
                return true;
            try
            {
                receipts = JsonConvert.DeserializeObject<Dictionary<Guid, string>>(value?.ToString() ?? string.Empty);
                if (receipts != null) return true;
                error = "The avatar gameplay receipt ledger is null.";
                return false;
            }
            catch (JsonException ex)
            {
                error = $"The avatar gameplay receipt ledger is invalid: {ex.Message}";
                return false;
            }
        }

        private static void RecordReceipt(AvatarDetail detail, Dictionary<Guid, string> receipts,
            Guid operationId, string canonicalPayload)
        {
            receipts.Add(operationId, canonicalPayload);
            detail.MetaData[HyperDriveAvatarGameplay.ReceiptMetadataKey] = JsonConvert.SerializeObject(receipts);
        }

        private static NextGenSoftware.OASIS.API.Core.Objects.InventoryItem CloneInventoryItem(
            NextGenSoftware.OASIS.API.Core.Objects.InventoryItem item, int quantity) =>
            new NextGenSoftware.OASIS.API.Core.Objects.InventoryItem
            {
                Id = item.Id, Name = item.Name, Description = item.Description, HolonType = item.HolonType,
                MetaData = item.MetaData == null ? null : new Dictionary<string, object>(item.MetaData),
                Image2D = item.Image2D, Image2DURI = item.Image2DURI, Object3D = item.Object3D,
                Object3DURI = item.Object3DURI, Quantity = quantity, Stack = item.Stack,
                GameSource = item.GameSource, ItemType = item.ItemType, NftId = item.NftId,
                GeoNFTId = item.GeoNFTId, Rarity = item.Rarity, MaxQuantity = item.MaxQuantity,
                Weight = item.Weight, IsUsable = item.IsUsable, IsTradeable = item.IsTradeable,
                AcquiredOn = item.AcquiredOn, LastUsedOn = item.LastUsedOn,
                Properties = item.Properties == null ? null : new Dictionary<string, object>(item.Properties)
            };

        private static OASISResult<HyperDriveAvatarDetailProjection> GameplayRejected(string message) => new OASISResult<HyperDriveAvatarDetailProjection>
        { IsError = true, ErrorCode = "AVATAR_GAMEPLAY_REJECTED", Message = message };

        private static OASISResult<HyperDriveAvatarDetailProjection> GameplayReceiptInvalid(string message) => new OASISResult<HyperDriveAvatarDetailProjection>
        { IsError = true, ErrorCode = "AVATAR_GAMEPLAY_RECEIPT_INVALID", Message = message };
    }
}
