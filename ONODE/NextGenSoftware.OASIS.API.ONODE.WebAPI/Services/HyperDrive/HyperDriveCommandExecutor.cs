using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive
{
    /// <summary>Executes canonical offline commands through the same domain managers used by online APIs.</summary>
    public sealed class HyperDriveCommandExecutor
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly IOASISStorageProvider _provider;

        public HyperDriveCommandExecutor(IOASISStorageProvider provider) =>
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));

        public async Task<HyperDriveCommandOutcome> ExecuteAsync(HostedSyncCommandItem command,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command == null) throw new ArgumentNullException(nameof(command));
            try
            {
                object value;
                if (command.EntityType == HyperDriveEntityTypes.QuestProgress)
                    value = await ExecuteQuestProgressAsync(command).ConfigureAwait(false);
                else if (command.EntityType == HyperDriveEntityTypes.InventoryItem)
                    value = await ExecuteInventoryGrantAsync(command).ConfigureAwait(false);
                else if (command.EntityType == HyperDriveEntityTypes.GeoNftCollection)
                    value = await ExecuteGeoNftCollectionAsync(command).ConfigureAwait(false);
                else if (command.EntityType == HyperDriveEntityTypes.AvatarGameplay)
                {
                    if (!(_provider is IHostedAvatarGameplayCommandStore gameplayStore))
                        throw new HyperDriveCommandRejectedException("COMMAND_PROVIDER_NOT_SUPPORTED",
                            "The hosted provider does not implement atomic avatar gameplay commands.");
                    var applied = await gameplayStore.ApplyAvatarGameplayCommandAsync(command, cancellationToken).ConfigureAwait(false);
                    RejectIfError(applied.IsError, applied.ErrorCode, applied.Message);
                    value = applied.Result;
                }
                else if (command.EntityType == HyperDriveEntityTypes.QuestLifecycle)
                    value = await ExecuteQuestLifecycleAsync(command).ConfigureAwait(false);
                else
                    return Outcome(command.OperationId, false, "COMMAND_TYPE_NOT_SUPPORTED",
                        $"No hosted command executor is registered for '{command.EntityType}'.", null);
                return Outcome(command.OperationId, true, "COMMAND_COMPLETED", "The domain command completed.",
                    JsonSerializer.Serialize(value, JsonOptions));
            }
            catch (HyperDriveCommandRejectedException ex)
            {
                return Outcome(command.OperationId, false, ex.Code, ex.Message, null);
            }
        }

        private async Task<object> ExecuteQuestProgressAsync(HostedSyncCommandItem command)
        {
            var payload = Deserialize<HyperDriveQuestProgressCommand>(command);
            EnsureStarDna();
            var questManager = new QuestManager(_provider, command.AvatarId, STARDNAManager.STARDNA,
                OASISBootLoader.OASISBootLoader.OASISDNA);
            var applied = await questManager.ApplyQuestProgressAsync(command.AvatarId, command.EntityId,
                string.IsNullOrWhiteSpace(payload.GameSource) ? "ODOOM" : payload.GameSource,
                new QuestProgressDelta
                {
                    OperationId = command.OperationId,
                    ActiveObjectiveId = payload.ActiveObjectiveId,
                    MonstersKilledDelta = payload.MonstersKilledDelta,
                    MonsterKilledClassname = payload.MonsterKilledClassname,
                    XpEarnedDelta = payload.XpEarnedDelta,
                    KeysCollectedDelta = payload.KeysCollectedDelta,
                    ArmorCollectedDelta = payload.ArmorCollectedDelta,
                    HealthCollectedDelta = payload.HealthCollectedDelta,
                    WeaponsCollectedDelta = payload.WeaponsCollectedDelta,
                    PowerupsCollectedDelta = payload.PowerupsCollectedDelta,
                    AmmoCollectedDelta = payload.AmmoCollectedDelta,
                    ItemCollectedName = payload.ItemCollectedName,
                    GeoHotSpotId = payload.GeoHotSpotId,
                    GenericItemPickup = payload.GenericItemPickup,
                    LevelTimeSeconds = payload.LevelTimeSeconds
                }).ConfigureAwait(false);
            RejectIfError(applied?.IsError == true, "QUEST_PROGRESS_REJECTED", applied?.Message);

            var avatarManager = new AvatarManager(_provider, OASISBootLoader.OASISBootLoader.OASISDNA);
            var itemManager = new InventoryItemManager(_provider, command.AvatarId, STARDNAManager.STARDNA,
                OASISBootLoader.OASISBootLoader.OASISDNA);
            foreach (Guid rewardId in applied.Result.InventoryItemsToGrant)
            {
                var reward = await itemManager.LoadAsync(command.AvatarId, rewardId).ConfigureAwait(false);
                RejectIfError(reward?.IsError == true || reward?.Result == null,
                    "QUEST_REWARD_NOT_FOUND", reward?.Message ?? $"Quest reward '{rewardId:D}' was not found.");
                var granted = await avatarManager.AddItemToAvatarInventoryAsync(command.AvatarId, reward.Result,
                    operationId: DeriveChildOperationId(command.OperationId, rewardId)).ConfigureAwait(false);
                RejectIfError(granted?.IsError == true, "QUEST_REWARD_REJECTED", granted?.Message);
            }
            return new HyperDriveQuestProgressProjection
            {
                QuestId = command.EntityId,
                AvatarId = command.AvatarId,
                QuestCompleted = applied.Result.QuestCompleted,
                ObjectivesCompleted = applied.Result.ObjectivesCompleted,
                CompletedObjectiveIds = applied.Result.CompletedObjectives.Select(x => x.Id).ToArray(),
                CompletedQuestId = applied.Result.CompletedQuestId,
                CompletedQuestTitle = applied.Result.CompletedQuestTitle,
                PercentComplete = applied.Result.PercentComplete,
                Message = applied.Result.Message,
                InventoryItemsToGrant = applied.Result.InventoryItemsToGrant.ToArray(),
                CrossGameEventsToDispatch = applied.Result.CrossGameEventsToDispatch.Select(x =>
                    new HyperDriveCrossGameEventProjection
                    {
                        EventType = x.EventType, TargetGame = x.TargetGame, TargetMap = x.TargetMap,
                        EntityClassname = x.EntityClassname, SpawnCount = x.SpawnCount,
                        EntityCategory = x.EntityCategory, PortalId = x.PortalId,
                        NarrationText = x.NarrationText, AudioUrl = x.AudioUrl, AudioTitle = x.AudioTitle,
                        VideoUrl = x.VideoUrl, VideoTitle = x.VideoTitle, WebsiteUrl = x.WebsiteUrl,
                        ImageUrl = x.ImageUrl, ImageTitle = x.ImageTitle, AnimationKey = x.AnimationKey
                    }).ToArray()
            };
        }

        private async Task<object> ExecuteQuestLifecycleAsync(HostedSyncCommandItem command)
        {
            var payload = Deserialize<HyperDriveQuestLifecycleCommand>(command);
            EnsureStarDna();
            var manager = new QuestManager(_provider, command.AvatarId, STARDNAManager.STARDNA,
                OASISBootLoader.OASISBootLoader.OASISDNA);
            NextGenSoftware.OASIS.Common.OASISResult<bool> applied;
            if (payload.Action == HyperDriveQuestLifecycleAction.Start)
                applied = await manager.StartQuestAsync(command.AvatarId, command.EntityId, payload.Notes, command.OperationId).ConfigureAwait(false);
            else if (payload.Action == HyperDriveQuestLifecycleAction.Complete)
                applied = await manager.CompleteQuestAsync(command.AvatarId, command.EntityId, payload.Notes, command.OperationId).ConfigureAwait(false);
            else if (payload.Action == HyperDriveQuestLifecycleAction.CompleteObjective && payload.ObjectiveId.HasValue && payload.ObjectiveId != Guid.Empty)
                applied = await manager.CompleteQuestObjectiveAsync(command.AvatarId, command.EntityId, payload.ObjectiveId.Value,
                    payload.GameSource, payload.Notes, command.OperationId).ConfigureAwait(false);
            else throw new HyperDriveCommandRejectedException("QUEST_LIFECYCLE_REJECTED", "Unsupported quest lifecycle action.");
            RejectIfError(applied.IsError, "QUEST_LIFECYCLE_REJECTED", applied.Message);
            return applied.Result;
        }

        private async Task<object> ExecuteInventoryGrantAsync(HostedSyncCommandItem command)
        {
            var payload = Deserialize<HyperDriveInventoryGrantCommand>(command);
            if (!Enum.IsDefined(typeof(InventoryItemType), payload.ItemType))
                throw new HyperDriveCommandRejectedException("COMMAND_PAYLOAD_INVALID",
                    $"Inventory item type '{payload.ItemType}' is invalid.");
            Uri image2DUri = ParseOptionalAbsoluteUri(payload.Image2DUri, "Image2DUri");
            Uri object3DUri = ParseOptionalAbsoluteUri(payload.Object3DUri, "Object3DUri");
            var item = new InventoryItem
            {
                Id = command.EntityId, Name = payload.Name, Description = payload.Description,
                Image2D = payload.Image2D, Image2DURI = image2DUri, Object3D = payload.Object3D,
                Object3DURI = object3DUri, Quantity = payload.Quantity, Stack = payload.Stack,
                GameSource = payload.GameSource, ItemType = (InventoryItemType)payload.ItemType,
                NftId = payload.NftId, GeoNFTId = payload.GeoNftId, Rarity = payload.Rarity,
                MaxQuantity = payload.MaxQuantity, Weight = payload.Weight, IsUsable = payload.IsUsable,
                IsTradeable = payload.IsTradeable
            };
            var applied = await new AvatarManager(_provider, OASISBootLoader.OASISBootLoader.OASISDNA)
                .AddItemToAvatarInventoryAsync(command.AvatarId, item, operationId: command.OperationId)
                .ConfigureAwait(false);
            RejectIfError(applied?.IsError == true, "INVENTORY_GRANT_REJECTED", applied?.Message);
            return ProjectInventoryItem(applied.Result);
        }

        private async Task<object> ExecuteGeoNftCollectionAsync(HostedSyncCommandItem command)
        {
            var request = Deserialize<HyperDriveGeoNftCollectionCommand>(command);
            if (!Enum.IsDefined(typeof(InventoryItemType), request.ItemType))
                throw new HyperDriveCommandRejectedException("COMMAND_PAYLOAD_INVALID",
                    $"Inventory item type '{request.ItemType}' is invalid.");
            Uri object3DUri = ParseOptionalAbsoluteUri(request.Object3DUri, "Object3DUri");
            var nftManager = new NFTManager(_provider, command.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
            var loaded = await nftManager.LoadWeb4GeoNftAsync(command.EntityId).ConfigureAwait(false);
            RejectIfError(loaded?.IsError == true || loaded?.Result == null, "GEONFT_NOT_FOUND", loaded?.Message);
            var nft = loaded.Result;
            var item = new InventoryItem
            {
                Name = nft.Title, Description = nft.Description, Image2D = nft.Image,
                Image2DURI = string.IsNullOrWhiteSpace(nft.ImageUrl) ? null : new Uri(nft.ImageUrl),
                Object3D = request.Object3D, Object3DURI = object3DUri,
                Quantity = request.Quantity, Stack = request.Stack, GameSource = request.GameSource,
                ItemType = (InventoryItemType)request.ItemType, GeoNFTId = command.EntityId,
                Rarity = nft.MetaData != null && nft.MetaData.TryGetValue("OurWorld.Rarity", out var rarity)
                    ? rarity : "Common"
            };
            var applied = await new AvatarManager(_provider, OASISBootLoader.OASISBootLoader.OASISDNA)
                .CollectGeoNFTInventoryAsync(command.AvatarId, nft, item, operationId: command.OperationId)
                .ConfigureAwait(false);
            RejectIfError(applied?.IsError == true, "GEONFT_COLLECTION_REJECTED", applied?.Message);
            return ProjectInventoryItem(applied.Result);
        }

        private static HyperDriveInventoryItemProjection ProjectInventoryItem(IInventoryItem item)
        {
            if (item == null)
                throw new HyperDriveCommandRejectedException("INVENTORY_RESULT_MISSING",
                    "The inventory command completed without returning its authoritative item.");
            return new HyperDriveInventoryItemProjection
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Quantity = item.Quantity,
                GameSource = item.GameSource,
                ItemType = (int)item.ItemType,
                NftId = item.NftId,
                GeoNftId = item.GeoNFTId,
                Rarity = item.Rarity,
                MaxQuantity = item.MaxQuantity,
                Weight = item.Weight,
                IsUsable = item.IsUsable,
                IsTradeable = item.IsTradeable
            };
        }

        private static T Deserialize<T>(HostedSyncCommandItem command)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(command.PayloadJson, JsonOptions) ??
                    throw new JsonException("The command payload is null.");
            }
            catch (JsonException ex)
            {
                throw new HyperDriveCommandRejectedException("COMMAND_PAYLOAD_INVALID", ex.Message);
            }
        }

        private static void EnsureStarDna()
        {
            if (STARDNAManager.STARDNA != null) return;
            var loaded = STARDNAManager.LoadDNA();
            if (loaded == null || loaded.IsError || loaded.Result == null)
                throw new InvalidOperationException(loaded?.Message ?? "STAR DNA could not be loaded.");
        }

        private static Uri ParseOptionalAbsoluteUri(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (Uri.TryCreate(value, UriKind.Absolute, out Uri parsed)) return parsed;
            throw new HyperDriveCommandRejectedException("COMMAND_PAYLOAD_INVALID",
                $"{fieldName} must be an absolute URI.");
        }

        private static void RejectIfError(bool failed, string code, string message)
        {
            if (failed) throw new HyperDriveCommandRejectedException(code,
                string.IsNullOrWhiteSpace(message) ? code : message);
        }

        private static Guid DeriveChildOperationId(Guid parentId, Guid childId)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{parentId:D}:{childId:D}"));
            var bytes = new byte[16];
            Buffer.BlockCopy(hash, 0, bytes, 0, bytes.Length);
            return new Guid(bytes);
        }

        private static HyperDriveCommandOutcome Outcome(Guid operationId, bool succeeded, string code,
            string message, string resultJson) => new HyperDriveCommandOutcome
        {
            OperationId = operationId, Succeeded = succeeded, Code = code, Message = message,
            ResultJson = resultJson, CompletedUtc = DateTime.UtcNow
        };

        private sealed class HyperDriveCommandRejectedException : Exception
        {
            public string Code { get; }
            public HyperDriveCommandRejectedException(string code, string message) : base(message) => Code = code;
        }
    }
}
