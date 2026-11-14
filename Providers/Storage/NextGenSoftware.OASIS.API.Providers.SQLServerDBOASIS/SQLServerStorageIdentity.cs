using System;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS;

public partial class SQLServerDBOASIS : ICosmicStorageIdentityProvider
{
    public async Task<OASISResult<CosmicStorageIdentity>> CaptureStorageIdentityAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsProviderActivated)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_NOT_READY",
                Message = "Activate SQL Server before enrolling its database for replication." };

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
            await using var provision = new SqlCommand(@"
DECLARE @lockResult int;
EXEC @lockResult = sys.sp_getapplock @Resource=N'OASIS.StorageIdentity',
    @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000, @DbPrincipal='public';
IF @lockResult < 0 THROW 51000, 'Could not acquire the OASIS storage identity lock.', 1;

IF OBJECT_ID(N'dbo.OASISStorageIdentity', N'U') IS NULL
    CREATE TABLE dbo.OASISStorageIdentity (
        Slot tinyint NOT NULL PRIMARY KEY CHECK (Slot = 1),
        StoreId uniqueidentifier NOT NULL
    );
IF NOT EXISTS (SELECT 1 FROM dbo.OASISStorageIdentity WHERE Slot=1)
    INSERT INTO dbo.OASISStorageIdentity (Slot, StoreId) VALUES (1, NEWID());
", connection, transaction);
            await provision.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            var identity = await ReadSqlServerStorageIdentityAsync(connection, transaction, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
            return new(identity);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_CAPTURE_FAILED",
                Exception = cause, Message = "The SQL Server store identity could not be captured." };
        }
    }

    private static async Task<CosmicStorageIdentity> ReadSqlServerStorageIdentityAsync(SqlConnection connection,
        SqlTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(@"
SELECT StoreId, CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')), DB_NAME(),
       OBJECT_SCHEMA_NAME(OBJECT_ID(N'OASISHolons', N'U'))
FROM dbo.OASISStorageIdentity
WHERE Slot=1;", connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3))
            throw new InvalidDataException("SQL Server did not return its enrolled database and holon schema.");

        var storeId = reader.GetGuid(0);
        if (storeId == Guid.Empty)
            throw new InvalidDataException("The SQL Server store identity is empty.");
        var resource = string.Concat(reader.GetString(1).Length, ":", reader.GetString(1),
            reader.GetString(2).Length, ":", reader.GetString(2), reader.GetString(3));
        return new CosmicStorageIdentity(ProviderType.SQLServerDBOASIS,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resource))), storeId);
    }
}
