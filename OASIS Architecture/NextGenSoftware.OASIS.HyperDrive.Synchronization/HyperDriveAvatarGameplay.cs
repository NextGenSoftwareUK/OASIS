using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization
{
    public enum HyperDriveAvatarGameplayAction { AwardXp = 1, ConsumeInventory = 2, SetActiveQuest = 3 }

    /// <summary>Avatar-scoped gameplay intent. Inventory stacks have canonical name/game identity.</summary>
    public sealed class HyperDriveAvatarGameplayCommand
    {
        public HyperDriveAvatarGameplayAction Action { get; set; }
        public int Amount { get; set; }
        public string InventoryName { get; set; }
        public string GameSource { get; set; }
        public Guid? QuestId { get; set; }
        public Guid? ObjectiveId { get; set; }
    }

    public interface IHostedAvatarGameplayCommandStore
    {
        /// <summary>Commits the effect and immutable operation receipt together. Replays must not reapply it.</summary>
        Task<OASISResult<HyperDriveAvatarDetailProjection>> ApplyAvatarGameplayCommandAsync(
            HostedSyncCommandItem command, CancellationToken cancellationToken);
    }

    /// <summary>The same deterministic rules produce the local pending view and hosted authoritative effect.</summary>
    public static class HyperDriveAvatarGameplay
    {
        public const string ReceiptMetadataKey = "Avatar.GameplayOperationLedger.v1";

        public static OASISResult<HyperDriveAvatarDetailProjection> Apply(
            HyperDriveAvatarDetailProjection detail, HyperDriveAvatarGameplayCommand command)
        {
            if (detail == null || command == null) return Reject("An avatar projection and gameplay command are required.");
            switch (command.Action)
            {
                case HyperDriveAvatarGameplayAction.AwardXp:
                    if (command.Amount < 0 || (long)detail.Xp + command.Amount > int.MaxValue)
                        return Reject("XP must be non-negative and within the supported total.");
                    detail.Xp += command.Amount;
                    break;
                case HyperDriveAvatarGameplayAction.ConsumeInventory:
                    if (command.Amount < 1 || string.IsNullOrWhiteSpace(command.InventoryName) || string.IsNullOrWhiteSpace(command.GameSource))
                        return Reject("Consumption requires a positive quantity and the canonical inventory name/game.");
                    var matches = detail.Inventory.Where(x => string.Equals(x.Name, command.InventoryName, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(x.GameSource, command.GameSource, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (matches.Length != 1) return Reject("The inventory stack is missing or ambiguous.");
                    if (matches[0].Quantity < command.Amount) return Reject("The inventory stack has insufficient quantity.");
                    matches[0].Quantity -= command.Amount;
                    if (matches[0].Quantity == 0) detail.Inventory = detail.Inventory.Where(x => x.Id != matches[0].Id).ToArray();
                    break;
                case HyperDriveAvatarGameplayAction.SetActiveQuest:
                    if (command.QuestId == Guid.Empty || command.ObjectiveId == Guid.Empty ||
                        (command.ObjectiveId.HasValue && !command.QuestId.HasValue))
                        return Reject("An objective must belong to a selected quest; use null to clear the tracker.");
                    detail.ActiveQuestId = command.QuestId;
                    detail.ActiveObjectiveId = command.ObjectiveId;
                    break;
                default: return Reject("The avatar gameplay action is unsupported.");
            }
            return new OASISResult<HyperDriveAvatarDetailProjection>(detail);
        }

        private static OASISResult<HyperDriveAvatarDetailProjection> Reject(string message) => new OASISResult<HyperDriveAvatarDetailProjection>
        { IsError = true, ErrorCode = "AVATAR_GAMEPLAY_REJECTED", Message = message };
    }
}
