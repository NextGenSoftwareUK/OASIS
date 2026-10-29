namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

internal static class CosmicSqliteSchema
{
    internal const string CreateMigrationReplicaAdoption = "CREATE TABLE IF NOT EXISTS CosmicMigrationReplicaAdoption (MigrationId TEXT PRIMARY KEY NOT NULL, RequestJson TEXT NOT NULL, RequestHash TEXT NOT NULL CHECK(length(RequestHash)=64), PreparationHash TEXT NOT NULL CHECK(length(PreparationHash)=64), RecordCount INTEGER NOT NULL CHECK(RecordCount BETWEEN 1 AND 4096))";
    internal const string MigrationMutationDigestDefinition = "TEXT NULL CHECK(MutationDigest IS NULL OR (FenceState='Held' AND length(MutationDigest)=64 AND MutationDigest NOT GLOB '*[^0-9A-F]*'))";
    internal const string AddMigrationMutationDigest = "ALTER TABLE CosmicMigrationFence ADD COLUMN MutationDigest " + MigrationMutationDigestDefinition;
    internal const string CreateMigrationFence = "CREATE TABLE IF NOT EXISTS CosmicMigrationFence (MigrationId TEXT PRIMARY KEY NOT NULL, RequestJson TEXT NOT NULL, RequestHash TEXT NOT NULL CHECK(length(RequestHash)=64), OwnerToken TEXT NOT NULL, FenceState TEXT NOT NULL CHECK(FenceState IN ('Held','Cancelled')))";
    internal const string CreateDestinationBindings = "CREATE TABLE IF NOT EXISTS CosmicDestinationBinding (SourceBinding TEXT NOT NULL CHECK(length(SourceBinding)=64), TargetAuthority TEXT NOT NULL, RecordJson TEXT NOT NULL, RecordDigest TEXT NOT NULL CHECK(length(RecordDigest)=64), PRIMARY KEY(SourceBinding,TargetAuthority))";
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
