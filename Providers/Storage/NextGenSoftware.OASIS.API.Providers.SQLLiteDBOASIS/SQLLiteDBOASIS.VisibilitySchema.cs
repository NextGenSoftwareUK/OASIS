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
        await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),"RefreshTokens",
            new Dictionary<string,string> {{"AuthorityProvider","TEXT NULL"}}).ConfigureAwait(false);
        using (var receiptSchema = connection.CreateCommand())
        {
            receiptSchema.Transaction = transaction.GetDbTransaction();
            receiptSchema.CommandText = CosmicSqliteSchema.CreateDestinationBindings+"; "+CosmicSqliteSchema.CreatePreparationTable+"; "+CosmicSqliteSchema.CreateReceiptTable+"; "+CosmicSqliteSchema.CreateReplicaHeadTable+"; "+CosmicSqliteSchema.CreateOutboxTable+"; "+CosmicSqliteSchema.CreateRevisionArchive+"; "+CosmicSqliteSchema.CreateRevisionKeyIndex+"; "+CosmicSqliteSchema.CreateRevisionCollectionIndex+"; "+CosmicSqliteSchema.CreateRevisionParentIndex;
            receiptSchema.CommandText += "; " + CosmicSqliteSchema.CreateMigrationFence;
            receiptSchema.CommandText += "; " + CosmicSqliteSchema.CreateMigrationReplicaAdoption;
            await receiptSchema.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),"CosmicMigrationFence",
            new Dictionary<string,string> {{"MutationDigest",CosmicSqliteSchema.MigrationMutationDigestDefinition}}).ConfigureAwait(false);
        await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),"CosmicHolonOutbox",CosmicSqliteSchema.OutboxDeliveryColumns()).ConfigureAwait(false);
        await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),"CosmicHolonOutbox",CosmicSqliteSchema.OutboxRecoveryColumns()).ConfigureAwait(false);
        using(var completionSchema=connection.CreateCommand())
        {
            completionSchema.Transaction=transaction.GetDbTransaction();
            completionSchema.CommandText=CosmicSqliteSchema.CreateOutboxInitializationCompletionTable;
            await completionSchema.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
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
        foreach(var table in new[]{"Holon","Avatar","AvatarDetail"})
            await EnsureColumnsAsync(connection,transaction.GetDbTransaction(),table,new Dictionary<string,string>{
                {"CreatedDateKind","INTEGER NOT NULL DEFAULT 0"},{"ModifiedDateKind","INTEGER NOT NULL DEFAULT 0"},
                {"DeletedDateKind","INTEGER NOT NULL DEFAULT 0"}}).ConfigureAwait(false);
        using(var profileIdentity=connection.CreateCommand())
        {
            profileIdentity.Transaction=transaction.GetDbTransaction();
            profileIdentity.CommandText=RepairDuplicateAvatarIdentitiesSql+"CREATE UNIQUE INDEX IF NOT EXISTS oasis_avatar_username_unique ON Avatar(Username COLLATE BINARY) WHERE typeof(Username)='text' AND Username<>''; "
                +"CREATE UNIQUE INDEX IF NOT EXISTS oasis_avatar_email_unique ON Avatar(Email COLLATE BINARY) WHERE typeof(Email)='text' AND Email<>'';";
            await profileIdentity.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        await transaction.CommitAsync().ConfigureAwait(false);
    }

    // Databases written before the identity indexes existed can hold several avatars with the same
    // Username or Email. The first registered row (lowest rowid) keeps the value; each later row gets a
    // unique "<value>#legacy-<AvatarId>" value and its original is kept in CosmicAvatarIdentityRepair,
    // so nothing is lost and the repair can be audited or reversed. It is idempotent.
    internal static readonly string RepairDuplicateAvatarIdentitiesSql =
        "CREATE TABLE IF NOT EXISTS CosmicAvatarIdentityRepair(AvatarId TEXT NOT NULL, Field TEXT NOT NULL, OriginalValue TEXT NOT NULL, RepairedValue TEXT NOT NULL, RepairedUtc TEXT NOT NULL, PRIMARY KEY(AvatarId,Field)); "
        + DuplicateIdentityRepair("Username") + DuplicateIdentityRepair("Email");

    private static string DuplicateIdentityRepair(string field) =>
        $"INSERT OR IGNORE INTO CosmicAvatarIdentityRepair(AvatarId,Field,OriginalValue,RepairedValue,RepairedUtc) "
        + $"SELECT a.Id,'{field}',a.{field},a.{field}||'#legacy-'||a.Id,strftime('%Y-%m-%dT%H:%M:%fZ','now') FROM Avatar a "
        + $"WHERE typeof(a.{field})='text' AND a.{field}<>'' AND EXISTS(SELECT 1 FROM Avatar b WHERE b.{field}=a.{field} COLLATE BINARY AND b.rowid<a.rowid); "
        + $"UPDATE Avatar SET {field}=(SELECT r.RepairedValue FROM CosmicAvatarIdentityRepair r WHERE r.AvatarId=Avatar.Id AND r.Field='{field}') "
        + $"WHERE typeof({field})='text' AND {field}<>'' AND EXISTS(SELECT 1 FROM Avatar b WHERE b.{field}=Avatar.{field} COLLATE BINARY AND b.rowid<Avatar.rowid) "
        + $"AND EXISTS(SELECT 1 FROM CosmicAvatarIdentityRepair r WHERE r.AvatarId=Avatar.Id AND r.Field='{field}'); ";

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
