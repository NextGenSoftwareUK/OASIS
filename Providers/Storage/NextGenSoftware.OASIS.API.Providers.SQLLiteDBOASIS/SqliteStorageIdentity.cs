using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

/// <summary>Reads only, using the caller's actual native connection and transaction.</summary>
internal static class SqliteStorageIdentity
{
    internal static CosmicStorageIdentity Read(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT file FROM pragma_database_list WHERE name='main'";
        var path = command.ExecuteScalar() as string;
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidDataException("A durable main database file is required.");
        var endpoint = Path.GetFullPath(path);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("sqlite-file-v1:" + endpoint)));

        command.CommandText = "SELECT Id,Identity FROM CosmicReplicationStoreIdentity";
        using var reader = command.ExecuteReader();
        if (!reader.Read() || reader.GetInt64(0) != 1 ||
            !Guid.TryParseExact(reader.GetString(1), "D", out var identity) || identity == Guid.Empty ||
            reader.GetString(1) != identity.ToString("D") || reader.Read())
            throw new InvalidDataException("The durable SQLite storage identity is corrupt; it was not replaced.");
        return new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS, fingerprint, identity);
    }
}
