using System;
using System.Collections.Generic;
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
        foreach(var table in new[] {"Holon","Avatar","AvatarDetail"})
        {
            using var command=connection.CreateCommand();
            command.Transaction=transaction.GetDbTransaction();
            command.CommandText=$"PRAGMA table_info(\"{table}\")";
            var found=new HashSet<string>(StringComparer.Ordinal);var exists=false;
            using(var reader=await command.ExecuteReaderAsync().ConfigureAwait(false))
                while(await reader.ReadAsync().ConfigureAwait(false))
                {
                    exists=true;
                    found.Add(reader.GetString(1));
                }
            if(!exists) throw new InvalidOperationException($"Required SQLite table {table} is missing.");
            foreach(var column in new Dictionary<string,string> {
                {"IsPublic","INTEGER NOT NULL DEFAULT 0"},
                {"VersionId","TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'"},
                {"PreviousVersionId","TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'"},
                {"CustomHolonType","TEXT NULL"}})
            {
                if(!found.Contains(column.Key))
                {
                    command.CommandText=$"ALTER TABLE \"{table}\" ADD COLUMN \"{column.Key}\" {column.Value}";
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
        }
        await transaction.CommitAsync().ConfigureAwait(false);
    }
}
