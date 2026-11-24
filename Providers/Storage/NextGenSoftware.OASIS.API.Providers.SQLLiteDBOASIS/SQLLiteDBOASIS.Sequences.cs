using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : ICosmicSequenceStorageProvider
{
    /// <summary>One atomic UPSERT ... RETURNING statement per block on its own connection.</summary>
    public async Task<OASISResult<CosmicSequenceBlock>> ReserveSequenceBlockAsync(string name, int blockSize = 1,
        CancellationToken cancellationToken = default)
    {
        var invalid = CosmicSequences.Validate(name, blockSize);
        if (invalid != null) return CosmicSequences.Error("COSMIC_SEQUENCE_INVALID", invalid);
        if (!IsProviderActivated || _appDataContext == null)
            return CosmicSequences.Error("COSMIC_AUTHORITY_UNAVAILABLE", "SQLite provider is not activated.");
        try
        {
            await using var connection = new SqliteConnection(_appDataContext.Database.GetConnectionString());
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using (var schema = connection.CreateCommand())
            {
                schema.CommandText = "CREATE TABLE IF NOT EXISTS CosmicSequence (Name TEXT NOT NULL PRIMARY KEY, Value INTEGER NOT NULL)";
                await schema.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            await using var reserve = connection.CreateCommand();
            reserve.CommandText = "INSERT INTO CosmicSequence(Name, Value) VALUES ($name, $size) "
                + "ON CONFLICT(Name) DO UPDATE SET Value = Value + $size RETURNING Value";
            reserve.Parameters.AddWithValue("$name", name);
            reserve.Parameters.AddWithValue("$size", (long)blockSize);
            var last = Convert.ToInt64(await reserve.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
            return new OASISResult<CosmicSequenceBlock>(new CosmicSequenceBlock(name, checked(last - blockSize + 1), last));
        }
        catch (Exception cause)
        {
            return CosmicSequences.Error(cause is OperationCanceledException ? "COSMIC_SEQUENCE_CANCELLED" : "COSMIC_SEQUENCE_FAILED",
                "The SQLite sequence block could not be reserved; its outcome is unknown, so the caller must not assume values.", cause);
        }
    }
}
