using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : ICosmicStorageIdentityProvider
{
    public async Task<OASISResult<CosmicStorageIdentity>> CaptureStorageIdentityAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsProviderActivated)
            return new() { IsError = true, ErrorCode = "COSMIC_STORAGE_IDENTITY_NOT_READY", Message = "Activate the existing store before capturing its binding." };
        try
        {
            var settings = new SqliteConnectionStringBuilder(_ownConnectionString);
            if (settings.Mode == SqliteOpenMode.Memory || settings.DataSource == ":memory:" || string.IsNullOrWhiteSpace(settings.DataSource))
                return new() { IsError = true, ErrorCode = "COSMIC_STORAGE_IDENTITY_UNSUPPORTED", Message = "Durable routing identity requires a file-backed SQLite store." };
            if (settings.Mode == SqliteOpenMode.ReadOnly)
                return new() { IsError = true, ErrorCode = "COSMIC_STORAGE_IDENTITY_READ_ONLY", Message = "Identity initialization cannot upgrade a read-only provider to a writable connection." };
            // Never create a missing database merely to obtain a route identity.
            settings.Mode = SqliteOpenMode.ReadWrite;
            settings.Pooling = false;
            using var connection = new SqliteConnection(settings.ToString());
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var location = connection.CreateCommand();
            location.CommandText = "SELECT file FROM pragma_database_list WHERE name='main'";
            var nativePath = (string)await location.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(nativePath))
                return new() { IsError = true, ErrorCode = "COSMIC_STORAGE_IDENTITY_UNSUPPORTED", Message = "The native connection has no durable main database file." };
            // Preserve native path spelling: aliases may conservatively differ;
            // do not merge distinct files in a case-sensitive directory.
            var endpoint = Path.GetFullPath(nativePath);
            using var hash = SHA256.Create();
            var fingerprint = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes("sqlite-file-v1:" + endpoint))).Replace("-", "");

            // An immediate native transaction serializes first initialization by
            // independent clients. Existing identity rows are never replaced.
            using var transaction = connection.BeginTransaction(deferred: false);
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='CosmicReplicationStoreIdentity' COLLATE NOCASE";
            var exists = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) != 0;
            if (!exists)
            {
                command.CommandText = "CREATE TABLE CosmicReplicationStoreIdentity (Id INTEGER PRIMARY KEY CHECK(Id=1), Identity TEXT NOT NULL)";
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                command.CommandText = "INSERT INTO CosmicReplicationStoreIdentity(Id,Identity) VALUES(1,$id)";
                command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("D"));
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                command.Parameters.Clear();
            }
            // A present but empty/corrupt metadata table is not first initialization.
            // Never manufacture a replacement identity for a damaged existing one.
            command.CommandText = "SELECT Id,Identity FROM CosmicReplicationStoreIdentity";
            Guid identity;
            using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false) || reader.GetInt64(0) != 1 ||
                    !Guid.TryParseExact(reader.GetString(1), "D", out identity) || identity == Guid.Empty ||
                    reader.GetString(1) != identity.ToString("D") || await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    throw new InvalidDataException("The durable SQLite storage identity is corrupt; it was not replaced.");
            }
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(new CosmicStorageIdentity(Core.Enums.ProviderType.SQLLiteDBOASIS, fingerprint, identity));
        }
        catch (Exception exception) when (exception is SqliteException || exception is InvalidDataException || exception is IOException || exception is InvalidOperationException || exception is ArgumentException || exception is FormatException || exception is InvalidCastException)
        {
            return new() { IsError = true, ErrorCode = "COSMIC_STORAGE_IDENTITY_CAPTURE_FAILED", ErrorCount = 1,
                Message = "The native SQLite storage identity could not be captured; preserve the original store.", Exception = exception };
        }
    }
}
