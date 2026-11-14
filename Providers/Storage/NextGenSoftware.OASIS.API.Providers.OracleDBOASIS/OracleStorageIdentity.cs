using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS : ICosmicStorageIdentityProvider
{
    public async Task<OASISResult<CosmicStorageIdentity>> CaptureStorageIdentityAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsProviderActivated)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_NOT_READY",
                Message = "Activate the Oracle store before enrolling it for replication." };

        try
        {
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await EnsureOracleReplicationTableAsync(connection, "OASIS_STORAGE_IDENTITY", @"
    SLOT NUMBER(1) NOT NULL PRIMARY KEY CHECK (SLOT = 1),
    STORE_ID RAW(16) NOT NULL", cancellationToken).ConfigureAwait(false);
            await EnsureOracleReplicationTableAsync(connection, "OASIS_REPLICA_RECEIPTS", @"
    OPERATION_ID RAW(16) NOT NULL PRIMARY KEY,
    REQUEST_HASH VARCHAR2(64) NOT NULL,
    RESULT_JSON CLOB NOT NULL", cancellationToken).ConfigureAwait(false);

            await EnsureOracleReplicationTableAsync(connection, "OASIS_REPLICA_HEADS", @"
    ID RAW(16) NOT NULL PRIMARY KEY,
    SOURCE_ID VARCHAR2(256 CHAR) NOT NULL,
    SOURCE_AUTHORITY VARCHAR2(64) NOT NULL,
    SNAPSHOT_JSON CLOB NOT NULL,
    SNAPSHOT_DIGEST VARCHAR2(64) NOT NULL,
    REVISION_ID VARCHAR2(36) NOT NULL,
    HARD_DELETED NUMBER(1) NOT NULL CHECK (HARD_DELETED IN (0,1))", cancellationToken).ConfigureAwait(false);

            await EnsureOracleReplicationTableAsync(connection, "OASIS_OUTBOUND_STATE", @"
    SLOT NUMBER(1) NOT NULL PRIMARY KEY CHECK (SLOT = 1),
    LAST_SEQUENCE NUMBER(19) NOT NULL CHECK (LAST_SEQUENCE >= 0)", cancellationToken).ConfigureAwait(false);
            await EnsureOracleReplicationTableAsync(connection, "OASIS_OUTBOUND_DELIVERIES", @"
    DELIVERY_SEQUENCE NUMBER(19) NOT NULL PRIMARY KEY,
    OPERATION_ID RAW(16) NOT NULL,
    TARGET_ID VARCHAR2(256 CHAR) NOT NULL,
    PAYLOAD_JSON CLOB NOT NULL,
    PAYLOAD_DIGEST VARCHAR2(64) NOT NULL,
    DELIVERY_STATE VARCHAR2(32) DEFAULT ''Pending'' NOT NULL,
    ATTEMPT_COUNT NUMBER(10) DEFAULT 0 NOT NULL,
    UNIQUE (OPERATION_ID, TARGET_ID)", cancellationToken).ConfigureAwait(false);

            await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
            var identity = await ReadOracleStorageIdentityAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            if (identity != null)
                return new(identity);

            await using var insert = CreateCommand(
                "INSERT INTO OASIS_STORAGE_IDENTITY (SLOT, STORE_ID) VALUES (1, :StoreId)", connection);
            insert.Transaction = transaction;
            insert.Parameters.Add("StoreId", OracleDbType.Raw).Value = Guid.NewGuid().ToByteArray();
            try
            {
                if (await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
                    throw new InvalidDataException("Oracle did not initialize exactly one store identity.");
            }
            catch (OracleException cause) when (cause.Number == 1)
            {
                // Concurrent enrollment won the singleton key. Read the identity it committed.
            }

            identity = await ReadOracleStorageIdentityAsync(connection, transaction, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidDataException("The Oracle store identity is absent after enrollment.");
            cancellationToken.ThrowIfCancellationRequested();
            await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
            return new(identity);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OracleException cause) when (cause.Number == 1013 && cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Oracle cancelled the requested storage operation.", cause, cancellationToken);
        }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_CAPTURE_FAILED",
                Exception = cause, Message = "The Oracle store identity could not be captured." };
        }
    }

    // Table names and definitions are fixed provider schema, never supplied by a caller.
    private static async Task EnsureOracleReplicationTableAsync(OracleConnection connection, string table,
        string columns, CancellationToken cancellationToken)
    {
        await using var exists = CreateCommand("SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME=:TableName AND TEMPORARY='N'", connection);
        exists.Parameters.Add("TableName", OracleDbType.Varchar2).Value = table;
        if (Convert.ToInt32(await exists.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) != 0)
            return;

        await using var provision = CreateCommand($@"BEGIN
  EXECUTE IMMEDIATE 'CREATE TABLE {table} ({columns})';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;", connection);
        await provision.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<CosmicStorageIdentity?> ReadOracleStorageIdentityAsync(
        OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(@"SELECT i.STORE_ID,
    SYS_CONTEXT('USERENV','DB_UNIQUE_NAME'), SYS_CONTEXT('USERENV','CON_NAME'),
    SYS_CONTEXT('USERENV','CURRENT_SCHEMA'), i.SLOT, t.TEMPORARY
FROM OASIS_STORAGE_IDENTITY i
LEFT JOIN USER_TABLES t ON t.TABLE_NAME='OASIS_STORAGE_IDENTITY'", connection);
        command.Transaction = transaction;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return null;
        var raw = reader.GetValue(0) as byte[];
        if (raw?.Length != 16 || new Guid(raw) == Guid.Empty
            || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3)
            || reader.IsDBNull(4) || reader.GetDecimal(4) != 1
            || reader.IsDBNull(5) || reader.GetString(5) != "N")
            throw new InvalidDataException("The Oracle store identity or native endpoint is invalid.");

        var resource = $"oracle:{reader.GetString(1)}:{reader.GetString(2)}:{reader.GetString(3)}";
        var identity = new CosmicStorageIdentity(Core.Enums.ProviderType.OracleDBOASIS,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resource))), new Guid(raw));
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidDataException("The Oracle store has multiple routing identities.");
        return identity;
    }
}
