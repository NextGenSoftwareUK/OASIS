using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using Avatar = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public partial class MongoDBOASIS
    {
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
                        NftId = item.NftId,
                        GeoNftId = item.GeoNFTId,
                        Rarity = item.Rarity,
                        MaxQuantity = item.MaxQuantity,
                        Weight = item.Weight,
                        IsUsable = item.IsUsable,
                        IsTradeable = item.IsTradeable
                    }).ToArray()
            };
        }

        private static System.Collections.Generic.IEnumerable<Guid> ProjectionReceiptIds(AvatarDetail detail, string key) =>
            detail.MetaData != null && detail.MetaData.TryGetValue(key, out var value) && value != null
                ? Newtonsoft.Json.Linq.JObject.Parse(value.ToString()).Properties().Select(x => Guid.Parse(x.Name))
                : Array.Empty<Guid>();
    }
}
