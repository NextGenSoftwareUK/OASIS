using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : ICosmicUniqueKeyStorageProvider
{
    public async Task<OASISResult<Guid>> FindUniqueKeyHolderAsync(string scope, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = new CosmicUniqueKeyClaim(Guid.NewGuid(), scope, value).NativeKey;
            await using var connection = new SqliteConnection(_appDataContext.Database.GetConnectionString());
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var exists = connection.CreateCommand();
            exists.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='CosmicUniqueKey'";
            if (Convert.ToInt64(await exists.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) == 0)
                return new OASISResult<Guid>(Guid.Empty) { IsLoaded = true };
            await using var read = connection.CreateCommand();
            read.CommandText = "SELECT HolonId FROM CosmicUniqueKey WHERE KeyId = $key";
            read.Parameters.AddWithValue("$key", key);
            var held = await read.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
            return new OASISResult<Guid>(held == null ? Guid.Empty : Guid.Parse(held)) { IsLoaded = true };
        }
        catch (Exception cause)
        {
            return new OASISResult<Guid> { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_UNIQUE_KEY_LOOKUP_FAILED", Message = "The SQLite unique key could not be read.", Exception = cause };
        }
    }
}
