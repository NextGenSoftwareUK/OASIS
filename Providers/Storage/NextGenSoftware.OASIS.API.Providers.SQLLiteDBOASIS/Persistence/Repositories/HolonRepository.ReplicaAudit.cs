using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    public Task<OASISResult<HolonReplicaHeadReadResult>> ReadHolonReplicaHeadsAsync(
        HolonReplicaHeadReadRequest request, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (request == null || request.ExpectedStorage.Authority != ProviderType.SQLLiteDBOASIS)
            return Task.FromResult(new OASISResult<HolonReplicaHeadReadResult> {
                IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_HEAD_AUDIT_OPTIONS_INVALID",
                Message = "An exact SQLite physical-store binding and bounded read intent are required." });
        try
        {
            var settings = new SqliteConnectionStringBuilder(_dbContext.Database.GetDbConnection().ConnectionString);
            if (settings.Mode == SqliteOpenMode.Memory || settings.DataSource == ":memory:")
                throw new InvalidOperationException("A durable existing file-backed replica is required.");
            settings.Mode = SqliteOpenMode.ReadOnly; settings.Pooling = false;
            using var connection = new SqliteConnection(settings.ToString());
            connection.Open();
            using var transaction = connection.BeginTransaction(deferred: true);
            var actual = SqliteStorageIdentity.Read(connection, transaction);
            if (actual.BindingFingerprint != request.ExpectedStorage.BindingFingerprint)
                return Task.FromResult(new OASISResult<HolonReplicaHeadReadResult> {
                    IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_HEAD_AUDIT_STORAGE_MISMATCH",
                    Message = "The native replica does not match the expected physical binding." });
            var rows = new List<HolonReplicaHeadObservation>();
            foreach (var id in request.HolonIds)
            {
                token.ThrowIfCancellationRequested();
                HolonReplicaHeadEvidence head = null;
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "SELECT SourceId,SourceAuthority,SnapshotJson,SnapshotDigest,ProviderRevisionId,HardDeleted FROM CosmicReplicaHead WHERE HolonId=$id";
                    command.Parameters.AddWithValue("$id", id.ToString("D"));
                    using var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        var deleted = reader.GetInt64(5);
                        if (deleted is not 0 and not 1) throw new InvalidDataException("Invalid replica deletion state.");
                        head = HolonReplicaHeadRecord.Read(id, ProviderType.SQLLiteDBOASIS, reader.GetString(0),
                            reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), deleted == 1).Evidence;
                        if (reader.Read()) throw new InvalidDataException("Replica head identity is not unique.");
                    }
                }
                bool exists;
                using (var current = connection.CreateCommand())
                {
                    current.Transaction = transaction;
                    current.CommandText = "SELECT Version,VersionId,ProviderRevisionId FROM Holon WHERE Id=$id";
                    current.Parameters.AddWithValue("$id", id.ToString("D"));
                    using var reader = current.ExecuteReader();
                    exists = reader.Read();
                    if (head != null && (head.HardDeleted ? exists : !exists
                        || reader.GetInt32(0) != head.Version
                        || !Guid.TryParse(reader.GetString(1), out var version) || version != head.VersionId
                        || reader.IsDBNull(2) || !Guid.TryParse(reader.GetString(2), out var revision) || revision != head.ProviderRevisionId))
                        throw new InvalidDataException("The current native record and protected replica head diverge.");
                    if (exists && reader.Read()) throw new InvalidDataException("Current record identity is not unique.");
                }
                rows.Add(new(id, exists, head));
            }
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new OASISResult<HolonReplicaHeadReadResult>(new(actual, rows)) { IsLoaded = true });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception cause)
        {
            return Task.FromResult(new OASISResult<HolonReplicaHeadReadResult> {
                IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_HEAD_AUDIT_UNVERIFIED",
                Message = "Protected target evidence could not be verified; no partial observations are returned.", Exception = cause });
        }
    }
}
