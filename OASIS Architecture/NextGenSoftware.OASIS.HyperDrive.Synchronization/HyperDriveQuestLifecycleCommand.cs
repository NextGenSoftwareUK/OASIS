using System;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization
{
    public enum HyperDriveQuestLifecycleAction { Start = 1, Complete = 2, CompleteObjective = 3 }
    public sealed class HyperDriveQuestLifecycleCommand
    {
        public HyperDriveQuestLifecycleAction Action { get; set; }
        public string Notes { get; set; }
        public Guid? ObjectiveId { get; set; }
        public string GameSource { get; set; }
        public const string ReceiptMetadataKey = "Quest.LifecycleOperationLedger.v1";
    }
}
