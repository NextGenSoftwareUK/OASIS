using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public sealed partial class SqliteHolonReplicationOutbox : IHolonReplicationPayloadAuditOutbox
{
    public Task<OASISResult<string>> ReadFrozenPayloadAsync(CosmicStorageIdentity expectedSource,
        HolonReplicationOperationObligation expected,
        HolonReplicaHeadReadConsistency requiredConsistency = HolonReplicaHeadReadConsistency.NativeSnapshot,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (expectedSource == null || expected == null || _expectedSource == null
            || expectedSource.BindingFingerprint != _expectedSource.BindingFingerprint
            || !Enum.IsDefined(typeof(HolonReplicaHeadReadConsistency), requiredConsistency))
            return Task.FromResult(new OASISResult<string> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_PAYLOAD_AUDIT_OPTIONS_INVALID", Message = "An exact bound native source and observation are required." });
        return Execute(() =>
        {
            var settings = new SqliteConnectionStringBuilder(_connectionString) { Mode = SqliteOpenMode.ReadOnly, Pooling = false };
            using var connection = new SqliteConnection(settings.ToString()); connection.Open();
            using var transaction = connection.BeginTransaction(deferred: true); ValidateSource(connection, transaction);
            using var query = connection.CreateCommand(); query.Transaction = transaction;
            query.CommandText = "SELECT " + InspectionColumns + " FROM CosmicHolonOutbox WHERE Sequence=$sequence LIMIT 2";
            query.Parameters.AddWithValue("$sequence", expected.Sequence);
            using var reader = query.ExecuteReader();
            if (!reader.Read()) throw new System.IO.InvalidDataException("The exact original obligation is absent.");
            HolonReplicationPayloadReceiptVerifier.ValidateObservation(expected, ReadOperationObservation(reader));
            var payload = reader.GetString(9);
            if (reader.Read()) throw new System.IO.InvalidDataException("Original obligation sequence is not unique.");
            cancellationToken.ThrowIfCancellationRequested();
            return new OASISResult<string>(payload) { IsLoaded = true };
        }, cancellationToken);
    }
}
