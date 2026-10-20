namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

internal static class CosmicSqliteSchema
{
    internal const string CreatePreparationTable = "CREATE TABLE IF NOT EXISTS CosmicWritePreparation (OperationId TEXT PRIMARY KEY NOT NULL, ActorId TEXT NOT NULL, PayloadJson TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL)";
    internal const string CreateRevisionCollectionIndex = "CREATE INDEX IF NOT EXISTS IX_CosmicHolonRevision_CollectionVersion ON CosmicHolonRevision(Version, HolonId)";
    internal const string CreateRevisionParentIndex = "CREATE INDEX IF NOT EXISTS IX_CosmicHolonRevision_ParentVersion ON CosmicHolonRevision(json_extract(RecordJson,'$.ParentHolonId'), Version)";
    internal const string CreateRevisionKeyIndex = "CREATE INDEX IF NOT EXISTS IX_CosmicHolonRevision_ProviderKeyVersion ON CosmicHolonRevision(json_extract(RecordJson,'$.ProviderUniqueStorageKey.SQLLiteDBOASIS'), Version)";
    internal const string CreateRevisionArchive = "CREATE TABLE IF NOT EXISTS CosmicHolonRevision (HolonId TEXT NOT NULL, ProviderRevisionId TEXT NOT NULL, Version INTEGER NOT NULL, VersionId TEXT NOT NULL, RecordJson TEXT NOT NULL, RecordDigest TEXT NOT NULL CHECK(length(RecordDigest)=64), ArchivedAtUtc TEXT NOT NULL, PRIMARY KEY(HolonId, ProviderRevisionId)); CREATE INDEX IF NOT EXISTS IX_CosmicHolonRevision_LogicalVersion ON CosmicHolonRevision(HolonId, Version)";
    internal static System.Collections.Generic.Dictionary<string,string> OutboxRecoveryColumns()=>new() {
        ["RecoveryEvidenceJson"]="TEXT NULL",["RecoveryEvidenceDigest"]="TEXT NULL"
    };
    internal static System.Collections.Generic.Dictionary<string,string> OutboxDeliveryColumns()=>new() {
        ["DeliveryState"]="TEXT NOT NULL DEFAULT 'Pending' CHECK(DeliveryState IN ('Pending','InFlight','Completed','ReconciliationRequired'))",
        ["LeaseToken"]="TEXT NOT NULL DEFAULT ''", ["LeaseWorkerId"]="TEXT NOT NULL DEFAULT ''",
        ["LeaseUntilUtcTicks"]="INTEGER NOT NULL DEFAULT 0", ["AttemptCount"]="INTEGER NOT NULL DEFAULT 0",
        ["NextAttemptUtcTicks"]="INTEGER NOT NULL DEFAULT 0", ["LastErrorCode"]="TEXT NULL", ["LastErrorMessage"]="TEXT NULL",
        ["CompletedAtUtcTicks"]="INTEGER NOT NULL DEFAULT 0"
    };
    internal const string CreateOutboxTable = "CREATE TABLE IF NOT EXISTS CosmicHolonOutbox (Sequence INTEGER PRIMARY KEY AUTOINCREMENT, OperationId TEXT NOT NULL, TargetId TEXT NOT NULL, SourceId TEXT NOT NULL, SourceAuthority TEXT NOT NULL, ActorId TEXT NOT NULL, PayloadJson TEXT NOT NULL, PayloadDigest TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL, UNIQUE(OperationId, TargetId))";
    internal const string CreateReceiptTable = "CREATE TABLE IF NOT EXISTS CosmicWriteReceipt (OperationId TEXT PRIMARY KEY NOT NULL, RequestHash TEXT NOT NULL, ResultJson TEXT NOT NULL, CommittedAtUtc TEXT NOT NULL)";
    internal const string CreateReplicaHeadTable = "CREATE TABLE IF NOT EXISTS CosmicReplicaHead (HolonId TEXT PRIMARY KEY NOT NULL, SourceId TEXT NOT NULL, SourceAuthority TEXT NOT NULL, SnapshotJson TEXT NOT NULL, SnapshotDigest TEXT NOT NULL, ProviderRevisionId TEXT NOT NULL, HardDeleted INTEGER NOT NULL CHECK(HardDeleted IN (0,1)))";
}
