namespace BuildingBlocks.Enums.Logging;

public enum DatabaseOperationType
{
    Select,
    Insert,
    Update,
    Delete,
    StoredProcedure,
    BulkInsert,
    BulkUpdate,
    BulkDelete,
    CreateIndex,
    DropIndex,
    Backup,
    Restore,
    Migrate
}