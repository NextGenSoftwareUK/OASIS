using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization
{
    public enum HyperDriveAvatarGameplayAction
    {
        AwardXp = 1,
        ConsumeInventory = 2,
        SetActiveQuest = 3,
        TransferInventory = 4,
        UpdateInventory = 5,
        TransferInventoryToClan = 6,
        AddKarma = 7,
        DeductKarma = 8
    }

    /// <summary>Avatar-scoped gameplay intent. Inventory stacks have canonical name/game identity.</summary>
    public sealed class HyperDriveAvatarGameplayCommand
    {
        public HyperDriveAvatarGameplayAction Action { get; set; }
        public int Amount { get; set; }
        public string InventoryName { get; set; }
        public string GameSource { get; set; }
        public Guid? InventoryItemId { get; set; }
        public Guid? TargetAvatarId { get; set; }
        public Guid? TargetClanId { get; set; }
        public Guid? DestinationInventoryItemId { get; set; }
        public Guid? QuestId { get; set; }
        public Guid? ObjectiveId { get; set; }
        public HyperDriveInventoryItemProjection InventoryUpdate { get; set; }
        public string KarmaSourceType { get; set; }
        public string KarmaType { get; set; }
        /// <summary>
        /// Immutable policy identity used to calculate <see cref="Amount"/>. Older v3 payloads that predate this
        /// field deserialize to v1 through the initializer, preserving their original canonical interpretation.
        /// </summary>
        public string KarmaPolicyVersion { get; set; } = HyperDriveKarmaPolicy.CurrentVersion;
        public string KarmaSourceTitle { get; set; }
        public string KarmaSourceDescription { get; set; }
        public DateTime KarmaOccurredAtUtc { get; set; }
    }

    public interface IHostedAvatarGameplayCommandStore
    {
        /// <summary>Commits the effect and immutable operation receipt together. Replays must not reapply it.</summary>
        Task<OASISResult<HyperDriveAvatarDetailProjection>> ApplyAvatarGameplayCommandAsync(
            HostedSyncCommandItem command, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Versioned Karma weighting shared by lightweight clients and hosted Core. Names are used on the wire so the
    /// Unity/NativeAOT client does not need a reference to the full OASIS enum/runtime assembly.
    /// </summary>
    public static class HyperDriveKarmaPolicy
    {
        public const string CurrentVersion = "oasis.karma-policy.v1";

        public static int ResolveAmount(bool positive, string karmaType)
        {
            if (string.IsNullOrWhiteSpace(karmaType)) return 0;
            return positive ? ResolvePositive(karmaType) : ResolveNegative(karmaType);
        }

        private static int ResolvePositive(string value) => value switch
        {
            "CreateAvatar" => 25, "CompleteProfile" => 40, "CreateBenevolentOApp" => 75,
            "DeployBenevolentOApp" => 100, "CreateBenevolentQuest" => 60, "CompleteBenevolentQuest" => 80,
            "GiftSeeds" => 50, "DonateSeeds" => 75, "PlaceBenevolentGeoNFT" => 25,
            "CollectBenevolentGeoNFT" => 15, "MintBenevolentNFT" => 20, "MintBenevolentGeoNFT" => 20,
            "PayWithSeeds" => 50, "DonateWithSeeds" => 75, "RewardWithSeeds" => 60,
            "SendInviteToJoinSeeds" => 30, "AcceptInviteToJoinSeeds" => 25,
            "ContributingToTheOASISWithCode" => 500, "ContributingToTheOASISWithPR" => 450,
            "ContributingToTheOASISWithSupport" => 300, "ContributingToTheOASISWithMarketing" => 450,
            "ContributingToTheOASISWithFunding" => 500, "ContributingToTheOASISWithSales" => 450,
            "ContributingToTheOASIS" => 300, "OurWorldPickupLitter" => 150,
            "OurWorldHelpOtherPlayer" => 100, "OurWorldHelpOtherPlayers" => 150,
            "OurWorldDefendPlayer" => 125, "OurWorldDefendPlayers" => 175, "OurWorldDefendBase" => 150,
            "OurWorldBeSelfless" => 175, "OurWorldBeAHero" => 200, "OurWorldBeASuperHero" => 250,
            "OurWorldBeSuperman" => 300, "OurWorlBeATeamPlayer" => 150, "OurWorldLevelUp" => 100,
            "OurWorldBeingPresent" => 100, "OurWorldBeingDetermined" => 125,
            "OurWorldBeingMindful" => 100, "OurWorldBeingHappy" => 100, "OurWorldBeingPeaceful" => 100,
            "OurWorldBeingWise" => 125, "OurWorldBeingPositive" => 100, "OurWorldBeingFast" => 100,
            "OurWorldBeingSuperFast" => 150, "OurWorldBeingStrong" => 100,
            "OurWorldBeingSuperStrong" => 150, "OurWorldBeingGrateful" => 100,
            "OurWorldSpeakingYourTruth" => 125, "OurWorldOther" => 75, "PickupLitter" => 200,
            "HelpOtherPerson" => 225, "HelpOtherPeople" => 275, "DefendOtherPerson" => 250,
            "DefendOtherPeople" => 300, "BeSelfless" => 250, "BeAHero" => 280, "BeASuperHero" => 330,
            "BeSuperman" => 400, "BeATeamPlayer" => 200, "LevelUp" => 200, "BeingPresent" => 215,
            "BeingDetermined" => 200, "BeingMindful" => 215, "BeingHappy" => 215, "BeingPeaceful" => 215,
            "BeingWise" => 225, "BeingPositive" => 215, "BeingFast" => 200, "BeingSuperFast" => 225,
            "BeingStrong" => 215, "BeingSuperStrong" => 250, "BeingGrateful" => 215,
            "SpeakingYourTruth" => 225, "NutritionEatDrinkHealthy" => 200, "SelfHelpImprovement" => 215,
            "HelpingTheEnvironment" => 400, "HelpingAnimals" => 400, "KeepThePeace" => 400,
            "HelpLocalCommunity" => 400, "HelpElderly" => 450, "HelpTheHomeless" => 450,
            "HelpKidsOnStreets" => 500, "HelpKids" => 450, "Volunteering" => 350,
            "ContributingTowardsAGoodCauseContributor" => 350,
            "ContributingTowardsAGoodCauseSharer" => 350,
            "ContributingTowardsAGoodCauseAdministrator" => 350,
            "ContributingTowardsAGoodCauseCreatorOrganiser" => 777,
            "ContributingTowardsAGoodCauseFunder" => 500,
            "ContributingTowardsAGoodCauseSpeaker" => 350,
            "ContributingTowardsAGoodCausePeacefulProtesterActivist" => 350,
            "Other" => 210, "SpendingTimeInNature" => 11, "VisitingNewNatureLocation" => 333,
            _ => 0
        };

        private static int ResolveNegative(string value) => value switch
        {
            "AttackPhysciallyOtherPersonOrPeople" => 2000, "AttackVerballyOtherPersonOrPeople" => 500,
            "BeingSelfish" => 50, "DisrespectPersonOrPeople" => 50, "DropLitter" => 500,
            "HarmingAnimals" => 2000, "HarmingChildren" => 2000, "HarmingNature" => 2000,
            "NotTeamPlayer" => 15, "NutritionEatDrinkUnhealthy" => 10, "SpamAbuse" => 150,
            "OurWorldAttackOtherPlayer" => 200, "OurWorldBeSelfish" => 50,
            "OurWorldDisrespectOtherPlayer" => 20, "OurWorldDropLitter" => 250,
            "OurWorldNotTeamPlayer" => 15, "Other" => 5,
            _ => 0
        };
    }

    /// <summary>The same deterministic rules produce the local pending view and hosted authoritative effect.</summary>
    public static class HyperDriveAvatarGameplay
    {
        public const string ReceiptMetadataKey = "Avatar.GameplayOperationLedger.v1";
        public const string ClanReceiptMetadataKey = "Clan.InventoryOperationLedger.v1";
        public const string ClanStateMetadataKey = "OASIS.Clan.State.v1";

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
                    if (command.Amount < 1 || (!command.InventoryItemId.HasValue &&
                        (string.IsNullOrWhiteSpace(command.InventoryName) || string.IsNullOrWhiteSpace(command.GameSource))))
                        return Reject("Consumption requires a positive quantity and either an item identity or canonical inventory name/game.");
                    var matches = command.InventoryItemId.HasValue
                        ? detail.Inventory.Where(x => x.Id == command.InventoryItemId.Value).ToArray()
                        : detail.Inventory.Where(x => string.Equals(x.Name, command.InventoryName, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(x.GameSource, command.GameSource, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (matches.Length != 1) return Reject("The inventory stack is missing or ambiguous.");
                    if (matches[0].Quantity < command.Amount) return Reject("The inventory stack has insufficient quantity.");
                    matches[0].Quantity -= command.Amount;
                    if (matches[0].Quantity == 0) detail.Inventory = detail.Inventory.Where(x => x.Id != matches[0].Id).ToArray();
                    break;
                case HyperDriveAvatarGameplayAction.TransferInventory:
                    if (!command.InventoryItemId.HasValue || command.InventoryItemId == Guid.Empty ||
                        !command.TargetAvatarId.HasValue || command.TargetAvatarId == Guid.Empty || command.TargetAvatarId == detail.AvatarId)
                        return Reject("Transfer requires an item identity and a different target avatar.");
                    var transferred = detail.Inventory.SingleOrDefault(x => x.Id == command.InventoryItemId.Value);
                    if (transferred == null) return Reject("The inventory stack is missing.");
                    // Ownership follows the stable item identity, so a transfer moves the complete stack.
                    detail.Inventory = detail.Inventory.Where(x => x.Id != transferred.Id).ToArray();
                    break;
                case HyperDriveAvatarGameplayAction.UpdateInventory:
                    if (!command.InventoryItemId.HasValue || command.InventoryItemId == Guid.Empty ||
                        command.InventoryUpdate == null || command.InventoryUpdate.Id != command.InventoryItemId ||
                        string.IsNullOrWhiteSpace(command.InventoryUpdate.Name) ||
                        string.IsNullOrWhiteSpace(command.InventoryUpdate.GameSource) ||
                        command.InventoryUpdate.Quantity < 1 || command.InventoryUpdate.MaxQuantity < 0 ||
                        command.InventoryUpdate.Weight < 0)
                        return Reject("Inventory update requires the matching stable item identity and valid complete item state.");
                    var updatedMatches = detail.Inventory.Where(x => x.Id == command.InventoryItemId.Value).ToArray();
                    if (updatedMatches.Length != 1) return Reject("The inventory stack is missing or ambiguous.");
                    detail.Inventory = detail.Inventory.Select(x => x.Id == command.InventoryItemId.Value
                        ? command.InventoryUpdate : x).ToArray();
                    break;
                case HyperDriveAvatarGameplayAction.TransferInventoryToClan:
                    if (!command.InventoryItemId.HasValue || command.InventoryItemId == Guid.Empty ||
                        !command.TargetClanId.HasValue || command.TargetClanId == Guid.Empty ||
                        !command.DestinationInventoryItemId.HasValue || command.DestinationInventoryItemId == Guid.Empty ||
                        command.Amount < 1)
                        return Reject("A clan transfer requires source item, target clan, destination item and positive quantity identities.");
                    var clanMatches = detail.Inventory.Where(x => x.Id == command.InventoryItemId.Value).ToArray();
                    if (clanMatches.Length != 1) return Reject("The inventory stack is missing or ambiguous.");
                    if (clanMatches[0].Quantity < command.Amount)
                        return Reject("The inventory stack has insufficient quantity.");
                    clanMatches[0].Quantity -= command.Amount;
                    if (clanMatches[0].Quantity == 0)
                        detail.Inventory = detail.Inventory.Where(x => x.Id != clanMatches[0].Id).ToArray();
                    break;
                case HyperDriveAvatarGameplayAction.SetActiveQuest:
                    if (command.QuestId == Guid.Empty || command.ObjectiveId == Guid.Empty ||
                        (command.ObjectiveId.HasValue && !command.QuestId.HasValue))
                        return Reject("An objective must belong to a selected quest; use null to clear the tracker.");
                    detail.ActiveQuestId = command.QuestId;
                    detail.ActiveObjectiveId = command.ObjectiveId;
                    break;
                case HyperDriveAvatarGameplayAction.AddKarma:
                case HyperDriveAvatarGameplayAction.DeductKarma:
                    if (command.Amount < 1 || string.IsNullOrWhiteSpace(command.KarmaSourceTitle) ||
                        command.KarmaOccurredAtUtc == default || command.KarmaOccurredAtUtc.Kind != DateTimeKind.Utc)
                        return Reject("A Karma mutation requires a positive amount, source title and UTC occurrence time.");
                    if (!string.Equals(command.KarmaPolicyVersion, HyperDriveKarmaPolicy.CurrentVersion,
                        StringComparison.Ordinal))
                        return Reject($"Karma policy version '{command.KarmaPolicyVersion}' is unsupported.");
                    int policyAmount = HyperDriveKarmaPolicy.ResolveAmount(
                        command.Action == HyperDriveAvatarGameplayAction.AddKarma, command.KarmaType);
                    if (policyAmount <= 0 || command.Amount != policyAmount)
                        return Reject("The Karma amount does not match the canonical Karma type weighting.");
                    long delta = command.Action == HyperDriveAvatarGameplayAction.AddKarma
                        ? command.Amount : -command.Amount;
                    if (delta < 0 && detail.Karma < command.Amount)
                        return Reject("The avatar has insufficient Karma.");
                    long newKarma;
                    try { newKarma = checked(detail.Karma + delta); }
                    catch (OverflowException) { return Reject("The Karma total exceeds the supported range."); }
                    detail.Karma = newKarma;
                    detail.KarmaHistory = new[]
                    {
                        new HyperDriveKarmaEntryProjection
                        {
                            Date = command.KarmaOccurredAtUtc,
                            Amount = checked((int)delta),
                            TotalKarma = newKarma,
                            Source = command.KarmaSourceTitle,
                            Reason = command.KarmaSourceDescription,
                            KarmaType = command.KarmaType
                        }
                    }.Concat(detail.KarmaHistory ?? Array.Empty<HyperDriveKarmaEntryProjection>()).ToArray();
                    break;
                default: return Reject("The avatar gameplay action is unsupported.");
            }
            return new OASISResult<HyperDriveAvatarDetailProjection>(detail);
        }

        private static OASISResult<HyperDriveAvatarDetailProjection> Reject(string message) => new OASISResult<HyperDriveAvatarDetailProjection>
        { IsError = true, ErrorCode = "AVATAR_GAMEPLAY_REJECTED", Message = message };
    }
}
