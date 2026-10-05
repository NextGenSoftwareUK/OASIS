using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using Avatar = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;
using Holon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public partial class MongoDBOASIS
    {
        private static readonly HashSet<string> EdgeJsonMetadataKeys = new HashSet<string>(
            new[] { "Objectives", "Rewards", "RewardInventoryItemIds", "PrerequisiteQuestIds", "SubQuestIds" },
            StringComparer.OrdinalIgnoreCase);

        internal static HyperDriveAvatarProjection CreateEdgeAvatarProjection(Avatar avatar)
        {
            if (avatar == null || avatar.HolonId == Guid.Empty)
                throw new ArgumentException("A persisted avatar with its public OASIS identity is required.", nameof(avatar));
            return new HyperDriveAvatarProjection
            {
                AvatarId = avatar.HolonId,
                Username = avatar.Username,
                Title = avatar.Title,
                FirstName = avatar.FirstName,
                LastName = avatar.LastName,
                LastBeamedIn = avatar.LastBeamedIn,
                LastBeamedOut = avatar.LastBeamedOut,
                IsBeamedIn = avatar.IsBeamedIn
            };
        }

        internal static HyperDriveAvatarDetailProjection CreateEdgeAvatarDetailProjection(AvatarDetail detail)
        {
            if (detail == null || detail.HolonId == Guid.Empty)
                throw new ArgumentException("A persisted avatar detail with its public OASIS identity is required.", nameof(detail));
            return new HyperDriveAvatarDetailProjection
            {
                AvatarId = detail.HolonId,
                Username = detail.Username,
                UmaJson = detail.UmaJson,
                Portrait = detail.Portrait,
                Karma = detail.Karma,
                Xp = detail.XP,
                Level = detail.Level,
                ActiveQuestId = detail.ActiveQuestId,
                ActiveObjectiveId = detail.ActiveObjectiveId,
                KarmaHistory = (detail.KarmaAkashicRecords ??
                    Array.Empty<NextGenSoftware.OASIS.API.Core.Objects.KarmaAkashicRecord>())
                    .OrderByDescending(record => record.Date)
                    .Select(record => new HyperDriveKarmaEntryProjection
                    {
                        Date = record.Date,
                        Amount = record.Karma,
                        TotalKarma = record.TotalKarma,
                        Source = record.KarmaSourceTitle,
                        Reason = record.KarmaSourceDesc,
                        KarmaType = record.Karma >= 0
                            ? record.KarmaTypePositive?.Value.ToString()
                            : record.KarmaTypeNegative?.Value.ToString()
                    }).ToArray(),
                AppliedOperationIds = ProjectionReceiptIds(detail, "Inventory.OperationLedger.v1")
                    .Concat(ProjectionReceiptIds(detail, HyperDriveAvatarGameplay.ReceiptMetadataKey)).Distinct().ToArray(),
                Inventory = (detail.Inventory ?? Array.Empty<NextGenSoftware.OASIS.API.Core.Objects.InventoryItem>())
                    .Select(item => new HyperDriveInventoryItemProjection
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        GameSource = item.GameSource,
                        ItemType = (int)item.ItemType,
                        ItemTypeName = item.ItemType.ToString(),
                        NftId = item.NftId,
                        GeoNftId = item.GeoNFTId,
                        Rarity = item.Rarity,
                        MaxQuantity = item.MaxQuantity,
                        Weight = item.Weight,
                        IsStackable = item.Stack,
                        IsUsable = item.IsUsable,
                        IsTradeable = item.IsTradeable,
                        AcquiredOn = item.AcquiredOn,
                        LastUsedOn = item.LastUsedOn,
                        ImageUrl = item.Image2DURI?.ToString(),
                        Value = InventoryDecimal(item, "OurWorld.Value"),
                        ThumbnailUrl = InventoryProperty(item, "OurWorld.ThumbnailUrl"),
                        Properties = InventoryProperties(item)
                    }).ToArray()
            };
        }

        private static string InventoryProperty(NextGenSoftware.OASIS.API.Core.Objects.InventoryItem item, string key) =>
            item.Properties != null && item.Properties.TryGetValue(key, out object value) ? value?.ToString() : null;

        private static decimal InventoryDecimal(NextGenSoftware.OASIS.API.Core.Objects.InventoryItem item, string key) =>
            decimal.TryParse(InventoryProperty(item, key), System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out decimal value) ? value : 0m;

        private static IReadOnlyDictionary<string, string> InventoryProperties(
            NextGenSoftware.OASIS.API.Core.Objects.InventoryItem item) =>
            (item.Properties ?? new Dictionary<string, object>())
                .Where(pair => pair.Key != "OurWorld.Value" && pair.Key != "OurWorld.ThumbnailUrl")
                .ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());

        internal static object CreateEdgeHolonProjection(Holon holon)
        {
            if (holon == null || holon.HolonId == Guid.Empty)
                throw new ArgumentException("A persisted holon with its public OASIS identity is required.", nameof(holon));
            if (holon.HolonType != NextGenSoftware.OASIS.API.Core.Enums.HolonType.InventoryItem)
            {
                var projection = (holon.MetaData ?? new Dictionary<string, object>())
                    .ToDictionary(pair => pair.Key, pair => NormalizeEdgeMetadataValue(pair.Key, pair.Value));
                // Public OASIS identity and lifecycle fields are canonical. Provider storage keys and the Mongo
                // document id are deliberately not part of the portable projection.
                projection["Id"] = holon.HolonId;
                projection["HolonType"] = (int)holon.HolonType;
                projection["Name"] = holon.Name;
                if (!projection.ContainsKey("Title")) projection["Title"] = holon.Name;
                projection["Description"] = holon.Description;
                projection["Version"] = holon.Version;
                projection["VersionId"] = holon.VersionId;
                projection["PreviousVersionId"] = holon.PreviousVersionId;
                projection["CreatedByAvatarId"] = holon.CreatedByAvatarId;
                projection["CreatedOn"] = holon.CreatedDate;
                projection["ModifiedOn"] = holon.ModifiedDate;
                projection["IsActive"] = holon.IsActive;
                return projection;
            }

            return new HyperDriveInventoryItemProjection
            {
                Id = holon.HolonId,
                Name = holon.Name,
                Description = holon.Description,
                Quantity = 1,
                ItemTypeName = NextGenSoftware.OASIS.API.Core.Enums.InventoryItemType.Miscellaneous.ToString()
            };
        }

        private static object NormalizeEdgeMetadataValue(string key, object value)
        {
            if (!EdgeJsonMetadataKeys.Contains(key) || !(value is string text)) return value;
            using (JsonDocument document = JsonDocument.Parse(text))
                return document.RootElement.Clone();
        }

        private static System.Collections.Generic.IEnumerable<Guid> ProjectionReceiptIds(AvatarDetail detail, string key) =>
            detail.MetaData != null && detail.MetaData.TryGetValue(key, out var value) && value != null
                ? Newtonsoft.Json.Linq.JObject.Parse(value.ToString()).Properties().Select(x => Guid.Parse(x.Name))
                : Array.Empty<Guid>();
    }
}
