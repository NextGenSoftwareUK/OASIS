using System;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS;

public partial class SQLServerDBOASIS : ICosmicStorageIdentityProvider, ICommittedHolonReplicaReceiptStorageProvider
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
IF OBJECT_ID(N'dbo.OASISReplicaReceipts', N'U') IS NULL
    CREATE TABLE dbo.OASISReplicaReceipts (
        OperationId uniqueidentifier NOT NULL PRIMARY KEY,
        RequestHash varchar(64) NOT NULL,
        ResultJson nvarchar(max) NOT NULL
    );
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

    public async Task<OASISResult<CommittedHolonReplicaReceiptEvidence>> ReadCommittedHolonReplicaReceiptAsync(
        CommittedHolonReplicaReceiptReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null || request.ExpectedStorage.Authority != Core.Enums.ProviderType.SQLServerDBOASIS)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_RECEIPT_OPTIONS_INVALID",
                Message = "An exact SQL Server replica receipt intent and binding are required." };

        try
        {
            if (!IsProviderActivated)
                throw new InvalidOperationException("Activate SQL Server before reading replica receipts.");
            var plan = CommittedHolonReplicaReceiptProjection.Prepare(request);
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
            var identity = await ReadSqlServerStorageIdentityAsync(connection, transaction, cancellationToken)
                .ConfigureAwait(false);
            if (identity.BindingFingerprint != request.ExpectedStorage.BindingFingerprint)
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_RECEIPT_STORAGE_MISMATCH",
                    Message = "The physical SQL Server database differs from the reviewed replica store." };

            await using var command = new SqlCommand(
                "SELECT RequestHash, ResultJson FROM dbo.OASISReplicaReceipts WHERE OperationId=@Id;",
                connection, transaction);
            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = plan.OperationId;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var found = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var evidence = CommittedHolonReplicaReceiptProjection.Project(request, plan,
                found ? reader.GetString(0) : null, found ? reader.GetString(1) : null,
                HolonReplicaHeadReadConsistency.NativeSnapshot);
            return new(evidence) { IsLoaded = true };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception cause)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_RECEIPT_UNVERIFIED",
                Exception = cause, Message = "The SQL Server replica receipt could not be verified." };
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
        return new CosmicStorageIdentity(Core.Enums.ProviderType.SQLServerDBOASIS,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resource))), storeId);
    }
}
