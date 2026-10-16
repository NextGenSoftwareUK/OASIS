using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS
{
    // EnsureCreated databases have no migration history. Upgrade the same database,
    // atomically and fail closed: old records must never become public by default.
    private async Task EnsureVisibilitySchemaAsync()
    {
        using var transaction=await _appDataContext.Database.BeginTransactionAsync().ConfigureAwait(false);
        var connection=_appDataContext.Database.GetDbConnection();
        foreach(var table in new[] {"Holon","Avatar","AvatarDetail"})
        {
            using var command=connection.CreateCommand();
            command.Transaction=transaction.GetDbTransaction();
            command.CommandText=$"PRAGMA table_info(\"{table}\")";
            var found=false;var exists=false;
            using(var reader=await command.ExecuteReaderAsync().ConfigureAwait(false))
                while(await reader.ReadAsync().ConfigureAwait(false))
                {
                    exists=true;
                    if(string.Equals(reader.GetString(1),"IsPublic",StringComparison.Ordinal)) found=true;
                }
            if(!exists) throw new InvalidOperationException($"Required SQLite table {table} is missing.");
            if(!found)
            {
                command.CommandText=$"ALTER TABLE \"{table}\" ADD COLUMN \"IsPublic\" INTEGER NOT NULL DEFAULT 0";
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
        await transaction.CommitAsync().ConfigureAwait(false);
    }
}
