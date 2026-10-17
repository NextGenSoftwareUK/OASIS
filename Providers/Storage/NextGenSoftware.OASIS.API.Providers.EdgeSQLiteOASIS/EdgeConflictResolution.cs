using System;

namespace NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS
{
    public enum EdgeConflictResolutionKind
    {
        ServerWins = 0,
        RetryLocal = 1,
        ManualMerge = 2
    }

    public sealed class EdgeSyncConflict
    {
        public Guid OperationId { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; }
        public Guid ServerVersionId { get; set; }
        public Guid LocalVersionId { get; set; }
        public string LocalPayloadJson { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public DateTime RecordedUtc { get; set; }
    }

    public sealed class EdgeConflictResolution
    {
        public Guid OperationId { get; set; }
        public EdgeConflictResolutionKind Kind { get; set; }
        public Guid ResolutionOperationId { get; set; }
        public Guid ResolutionVersionId { get; set; }
        public string MergedPayloadJson { get; set; }
    }
}
