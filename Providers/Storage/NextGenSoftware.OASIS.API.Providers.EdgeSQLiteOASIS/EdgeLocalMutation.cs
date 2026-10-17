using System;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;

namespace NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS
{
    public sealed class EdgeLocalMutation
    {
        public Guid OperationId { get; set; }
        public Guid DeviceId { get; set; }
        public Guid AvatarId { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; }
        public SyncOperationKind Kind { get; set; }
        public Guid BaseVersionId { get; set; }
        public Guid LocalVersionId { get; set; }
        public string PayloadJson { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public sealed class EdgeStoredEntity
    {
        public Guid EntityId { get; set; }
        public string EntityType { get; set; }
        public Guid VersionId { get; set; }
        public string PayloadJson { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime ChangedUtc { get; set; }
    }
}
