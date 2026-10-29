using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public sealed partial class SqliteHolonReplicationOutbox
{
    public Task<OASISResult<HolonReplicationOperationPage>> ReadOperationObligationsAsync(
        HolonReplicationOperationReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null || _expectedSource == null || request.ExpectedSource.BindingFingerprint != _expectedSource.BindingFingerprint)
            return Task.FromResult(new OASISResult<HolonReplicationOperationPage> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_OPERATION_INVENTORY_OPTIONS_INVALID", Message = "An exact explicitly bound source operation is required." });
        return Execute(() =>
        {
            var settings = new SqliteConnectionStringBuilder(_connectionString) { Mode = SqliteOpenMode.ReadOnly, Pooling = false };
            using var connection = new SqliteConnection(settings.ToString()); connection.Open();
            using var transaction = connection.BeginTransaction(deferred: true); ValidateSource(connection, transaction);
            using var query = connection.CreateCommand(); query.Transaction = transaction;
            query.CommandText = "SELECT " + InspectionColumns + " FROM CosmicHolonOutbox WHERE OperationId=$operation "
                + "AND ($after IS NULL OR TargetId COLLATE BINARY > $after) ORDER BY TargetId COLLATE BINARY LIMIT $limit";
            query.Parameters.AddWithValue("$operation", request.OperationId.ToString("D"));
            query.Parameters.AddWithValue("$after", (object)request.AfterTargetId ?? DBNull.Value);
            query.Parameters.AddWithValue("$limit", request.PageSize + 1);
            var rows = new List<HolonReplicationOperationObligation>();
            using var reader = query.ExecuteReader();
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var state = ReadCurrentInspection(reader, true).Result;
                rows.Add(HolonReplicationOperationObservation.Capture(state, reader.GetString(11), ProviderType.SQLLiteDBOASIS,
                    Guid.Parse(reader.GetString(13)), reader.GetString(10), reader.IsDBNull(17) ? null : reader.GetString(17),
                    reader.IsDBNull(18) ? null : reader.GetString(18), reader.GetInt64(14), reader.GetInt64(15), reader.GetInt64(16),
                    reader.IsDBNull(8) ? null : reader.GetString(8)));
            }
            return new OASISResult<HolonReplicationOperationPage>(new(request, rows.Take(request.PageSize),
                rows.Count > request.PageSize, HolonReplicaHeadReadConsistency.NativeSnapshot)) { IsLoaded = true };
        }, cancellationToken);
    }
}
