using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization
{
    /// <summary>
    /// Stable wire names for OASIS entities synchronized between an Edge ONODE and a hosted ONODE.
    /// These values are persistence contracts: changing a value creates a different logical entity type.
    /// </summary>
    public static class HyperDriveEntityTypes
    {
        public const string Holon = "oasis.holon.v1";
        public const string Avatar = "oasis.avatar.v1";
        public const string AvatarDetail = "oasis.avatar-detail.v1";
        public const string AvatarGameplay = "oasis.avatar-gameplay.v1";
        public const string Quest = "star.quest.v1";
        public const string QuestProgress = "star.quest-progress.v1";
        public const string QuestLifecycle = "star.quest-lifecycle.v1";
        public const string InventoryItem = "star.inventory-item.v1";
        public const string GeoNft = "star.geonft.v1";
        public const string GeoNftCollection = "star.geonft-collection.v1";
        public const string CommandResult = "oasis.command-result.v1";

        private static readonly HashSet<string> HostedDomainTypes = new HashSet<string>(
            new[] { Holon, Avatar, AvatarDetail, AvatarGameplay, Quest, QuestProgress, QuestLifecycle, InventoryItem, GeoNft, GeoNftCollection },
            StringComparer.Ordinal);

        /// <summary>
        /// Returns true only for a versioned entity type whose hosted domain projection is part of
        /// the OASIS contract. Custom application types remain valid sync documents, but must not be
        /// applied to the full OASIS provider model without a separately registered codec.
        /// </summary>
        public static bool IsHostedDomainType(string entityType) =>
            entityType != null && HostedDomainTypes.Contains(entityType);
    }
}
