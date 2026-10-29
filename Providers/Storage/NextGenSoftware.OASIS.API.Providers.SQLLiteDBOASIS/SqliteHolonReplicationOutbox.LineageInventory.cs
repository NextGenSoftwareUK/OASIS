using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public sealed partial class SqliteHolonReplicationOutbox
{
    public Task<OASISResult<HolonReplicationLineagePage>> ReadLineageObligationsAsync(
        HolonReplicationLineageReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null || _expectedSource == null || request.ExpectedSource.BindingFingerprint != _expectedSource.BindingFingerprint)
            return Task.FromResult(new OASISResult<HolonReplicationLineagePage> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_LINEAGE_INVENTORY_OPTIONS_INVALID", Message = "An exact explicitly bound source lineage is required." });
        return Execute(() =>
        {
            var settings = new SqliteConnectionStringBuilder(_connectionString) { Mode = SqliteOpenMode.ReadOnly, Pooling = false };
            using var connection = new SqliteConnection(settings.ToString()); connection.Open();
            using var transaction = connection.BeginTransaction(deferred: true); ValidateSource(connection, transaction);
            var through = request.ThroughSequence;
            if (!through.HasValue)
            {
                using var boundary = connection.CreateCommand(); boundary.Transaction = transaction;
                boundary.CommandText = "SELECT COALESCE(MAX(Sequence),0) FROM CosmicHolonOutbox WHERE SourceId=$source";
                boundary.Parameters.AddWithValue("$source", request.SourceId);
                through = Convert.ToInt64(boundary.ExecuteScalar());
            }
            using var query = connection.CreateCommand(); query.Transaction = transaction;
            query.CommandText = "SELECT " + InspectionColumns + " FROM CosmicHolonOutbox WHERE SourceId=$source "
                + "AND Sequence>$after AND Sequence<=$through ORDER BY Sequence LIMIT $limit";
            query.Parameters.AddWithValue("$source", request.SourceId); query.Parameters.AddWithValue("$after", request.AfterSequence);
            query.Parameters.AddWithValue("$through", through.Value); query.Parameters.AddWithValue("$limit", request.PageSize + 1);
            var rows = new List<HolonReplicationOperationObligation>();
            using var reader = query.ExecuteReader();
            while (reader.Read()) { cancellationToken.ThrowIfCancellationRequested(); rows.Add(ReadOperationObservation(reader)); }
            return new OASISResult<HolonReplicationLineagePage>(new(request, through.Value, rows.Take(request.PageSize),
                rows.Count > request.PageSize, HolonReplicaHeadReadConsistency.NativeSnapshot)) { IsLoaded = true };
        }, cancellationToken);
    }
}
