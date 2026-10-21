using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS
{
    // EnsureCreated databases have no migration history. Upgrade the same database,
    // atomically and fail closed: old records stay private; no version IDs invented.
    private async Task EnsureRecordSchemaAsync()
    {
        using var transaction=await _appDataContext.Database.BeginTransactionAsync().ConfigureAwait(false);
        var connection=_appDataContext.Database.GetDbConnection();
        using (var receiptSchema = connection.CreateCommand())
        {
            receiptSchema.Transaction = transaction.GetDbTransaction();
            receiptSchema.CommandText = CosmicSqliteSchema.CreatePreparationTable+"; "+CosmicSqliteSchema.CreateReceiptTable+"; "+CosmicSqliteSchema.CreateReplicaHeadTable+"; "+CosmicSqliteSchema.CreateOutboxTable+"; "+CosmicSqliteSchema.CreateRevisionArchive+"; "+CosmicSqliteSchema.CreateRevisionKeyIndex+"; "+CosmicSqliteSchema.CreateRevisionCollectionIndex+"; "+CosmicSqliteSchema.CreateRevisionParentIndex;
            await receiptSchema.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),"CosmicHolonOutbox",CosmicSqliteSchema.OutboxDeliveryColumns()).ConfigureAwait(false);
        await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),"CosmicHolonOutbox",CosmicSqliteSchema.OutboxRecoveryColumns()).ConfigureAwait(false);
        foreach(var table in new[] {"Holon","Avatar","AvatarDetail"})
            await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),table,new Dictionary<string,string> {
                {"IsPublic","INTEGER NOT NULL DEFAULT 0"},
                {"VersionId","TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'"},
                {"PreviousVersionId","TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'"},
                {"CustomHolonType","TEXT NULL"}}).ConfigureAwait(false);
        await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),"Holon",
            new Dictionary<string,string> {{"ProviderRevisionId","TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'"},
                {"PersistentValueJson","TEXT NOT NULL DEFAULT '{}'"}}).ConfigureAwait(false);
        await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),"Holon",new Dictionary<string,string>{
            {"HasCreatedProviderType","INTEGER NOT NULL DEFAULT 1"},{"HasCreatedOASISType","INTEGER NOT NULL DEFAULT 1"}}).ConfigureAwait(false);
        await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),"AvatarDetail",
            new Dictionary<string,string> {{"ActiveQuestId","TEXT NULL"},{"ActiveObjectiveId","TEXT NULL"},
                {"ProviderLinksJson","TEXT NOT NULL DEFAULT '{}'"},
                {"DimensionLevelIdsJson","TEXT NOT NULL DEFAULT '{}'"},
                {"CelestialParentIdsJson","TEXT NOT NULL DEFAULT '{}'"},
                {"Portrait","TEXT NULL"},{"UmaJson","TEXT NULL"},
                {"ParentHolonId","TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'"},
                {"ParentHolonId2","TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'"},
                {"ParentHolonId3","TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'"}}).ConfigureAwait(false);
        await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),"Avatar",
            new Dictionary<string,string> {{"HasAvatarType","INTEGER NOT NULL DEFAULT 1"},
                {"HasCreatedProviderType","INTEGER NOT NULL DEFAULT 1"},{"HasCreatedOASISType","INTEGER NOT NULL DEFAULT 1"},
                {"ProviderWalletsJson","TEXT NOT NULL DEFAULT '{}'"},
                {"ParentHolonId","TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'"}}).ConfigureAwait(false);
        await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),"AvatarInventory",
            new Dictionary<string,string> {{"Image2D","BLOB NULL"},{"Object3D","BLOB NULL"},
                {"Image2DURI","TEXT NULL"},{"Object3DURI","TEXT NULL"},{"PublishedSTARNETHolon","BLOB NULL"},
                {"ExtendedValueJson","TEXT NOT NULL DEFAULT '{}'"}}).ConfigureAwait(false);
        await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),"HeartRateEntry",
            new Dictionary<string,string> {{"SequenceIndex","INTEGER NOT NULL DEFAULT 0"}}).ConfigureAwait(false);
        await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),"KarmaAkashicRecord",
            new Dictionary<string,string> {{"SequenceIndex","INTEGER NOT NULL DEFAULT 0"},{"DateKind","INTEGER NOT NULL DEFAULT 0"},
                {"HasKarmaSource","INTEGER NOT NULL DEFAULT 1"},{"HasKarmaEarntOrLost","INTEGER NOT NULL DEFAULT 1"},
                {"HasKarmaTypePositive","INTEGER NOT NULL DEFAULT 1"},{"HasKarmaTypeNegative","INTEGER NOT NULL DEFAULT 1"},
                {"HasProvider","INTEGER NOT NULL DEFAULT 1"}}).ConfigureAwait(false);
        await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),"AvatarGifts",
            new Dictionary<string,string> {{"SequenceIndex","INTEGER NOT NULL DEFAULT 0"}}).ConfigureAwait(false);
        await EnsureOptionalGiftChakraAsync(connection,transaction.GetDbTransaction()).ConfigureAwait(false);
        await transaction.CommitAsync().ConfigureAwait(false);
    }

    private static async Task EnsureColumnsAsync(DbConnection connection,DbTransaction transaction,string table,Dictionary<string,string> columns)
    {
        {
            using var command=connection.CreateCommand();
            command.Transaction=transaction;
            command.CommandText=$"PRAGMA table_info(\"{table}\")";
            var found=new HashSet<string>(StringComparer.Ordinal);var exists=false;
            using(var reader=await command.ExecuteReaderAsync().ConfigureAwait(false))
                while(await reader.ReadAsync().ConfigureAwait(false))
                {
                    exists=true;
                    found.Add(reader.GetString(1));
                }
            if(!exists) throw new InvalidOperationException($"Required SQLite table {table} is missing.");
            foreach(var column in columns)
            {
                if(!found.Contains(column.Key))
                {
                    command.CommandText=$"ALTER TABLE \"{table}\" ADD COLUMN \"{column.Key}\" {column.Value}";
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
