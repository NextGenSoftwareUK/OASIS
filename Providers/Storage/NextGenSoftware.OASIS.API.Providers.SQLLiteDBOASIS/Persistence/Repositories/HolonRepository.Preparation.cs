using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    public OASISResult<HolonWritePreparation> ClaimHolonWritePreparation(HolonWritePreparation candidate, CancellationToken token)
    {
        SqliteTransaction transaction = null; var opened = false; var commitEntered = false; var known = false; var rollbackFailed = false;
        Exception failure = null; OASISResult<HolonWritePreparation> result = null;
        void Fail(Exception error) => failure = failure == null ? error : new AggregateException(failure, error);
        try { result = Claim(); } catch (Exception error) { Fail(error); }
        if (transaction != null && !commitEntered)
            try { transaction.Rollback(); } catch (Exception error) { rollbackFailed = true; Fail(error); }
        try { transaction?.Dispose(); } catch (Exception error) { Fail(error); }
        if (opened) try { _dbContext.Database.CloseConnection(); } catch (Exception error) { Fail(error); }
        if (failure != null)
        {
            if (known) { result.IsWarning = true; result.WarningCount++; result.Exception = failure; result.Message = "The durable preparation is known, but SQLite cleanup failed."; }
            else result = PreparationError(commitEntered || rollbackFailed ? "COSMIC_PREPARATION_RECONCILIATION_REQUIRED"
                : failure is OperationCanceledException ? "COSMIC_PREPARATION_CANCELLED" : "COSMIC_PREPARATION_FAILED",
                commitEntered || rollbackFailed ? "Recover the original preparation before dispatching a holon mutation." : "The preparation claim did not commit.",
                commitEntered || rollbackFailed ? new SqlitePreparationReconciliationException(candidate?.OperationId ?? Guid.Empty, failure) : failure);
        }
        if (result != null && candidate != null) { result.MetaData ??= new Dictionary<string, string>(); result.MetaData["COSMIC.OperationId"] = candidate.OperationId.ToString("D"); }
        return result;

        OASISResult<HolonWritePreparation> Claim()
        {
            token.ThrowIfCancellationRequested();
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            if (candidate.Authority != ProviderType.SQLLiteDBOASIS)
                return PreparationError("COSMIC_AUTHORITY_MISMATCH", "The preparation belongs to another provider authority.");
            if (_dbContext.Database.CurrentTransaction != null)
                return PreparationError("COSMIC_PREPARATION_CONTEXT_BUSY", "The context already owns a transaction.");
            // The transport contains opaque per-record ciphertext only. Authorization/decryption belongs to Core.
            var payload = JsonSerializer.Serialize(candidate.Records.Select(record => new { id = record.HolonId.ToString("D"), envelope = record.Envelope }));
            _dbContext.Database.OpenConnection(); opened = true;
            var connection = (SqliteConnection)_dbContext.Database.GetDbConnection();
            transaction = connection.BeginTransaction(deferred: false);
            token.ThrowIfCancellationRequested();
            using (var prior = connection.CreateCommand())
            {
                prior.Transaction = transaction;
                prior.CommandText = "SELECT EXISTS(SELECT 1 FROM CosmicWriteReceipt WHERE OperationId=$op), EXISTS(SELECT 1 FROM CosmicWritePreparation WHERE OperationId=$op)";
                prior.Parameters.AddWithValue("$op", candidate.OperationId.ToString("D"));
                using var reader = prior.ExecuteReader();
                if (!reader.Read()) throw new InvalidDataException("Preparation receipt admission returned no state.");
                if (reader.GetInt64(0) != 0 && reader.GetInt64(1) == 0)
                    return PreparationError("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",
                        "This operation already has a write receipt but no original protected preparation. Reconcile the existing operation before another mutation.",
                        new SqlitePreparationReconciliationException(candidate.OperationId, new InvalidDataException("Original protected preparation is unavailable.")));
            }
            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO CosmicWritePreparation(OperationId,ActorId,PayloadJson,CreatedAtUtc) VALUES ($op,$actor,$payload,$at) ON CONFLICT(OperationId) DO NOTHING";
                insert.Parameters.AddWithValue("$op", candidate.OperationId.ToString("D"));
                insert.Parameters.AddWithValue("$actor", candidate.ActorId.ToString("D"));
                insert.Parameters.AddWithValue("$payload", payload); insert.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("O"));
                insert.ExecuteNonQuery();
            }
            HolonWritePreparation winner;
            using (var read = connection.CreateCommand())
            {
                read.Transaction = transaction;
                read.CommandText = "SELECT ActorId,length(PayloadJson),PayloadJson FROM CosmicWritePreparation WHERE OperationId=$op";
                read.Parameters.AddWithValue("$op", candidate.OperationId.ToString("D"));
                using var reader = read.ExecuteReader();
                if (!reader.Read()) throw new InvalidDataException("The atomic preparation claim has no winning record.");
                if (reader.GetString(0) != candidate.ActorId.ToString("D"))
                    return PreparationError("COSMIC_PREPARATION_ACTOR_MISMATCH", "The operation is owned by another actor.");
                // Check native length before requesting the complete string from the provider.
                var length = reader.GetInt64(1);
                if (length > HolonWritePreparation.MaximumTotalEnvelopeCharacters * 6L + 1024L * HolonWritePreparation.MaximumRecords)
                    throw new InvalidDataException("The stored preparation exceeds its transport limit.");
                winner = ReadPreparation(candidate, reader.GetString(2));
            }
            token.ThrowIfCancellationRequested();
            commitEntered = true; transaction.Commit(); known = true;
            return new OASISResult<HolonWritePreparation>(winner);
        }
    }

    private static HolonWritePreparation ReadPreparation(HolonWritePreparation candidate, string payload)
    {
        using var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 1 || root.GetArrayLength() > HolonWritePreparation.MaximumRecords)
            throw new InvalidDataException("Stored preparation record count is invalid.");
        var records = new List<ProtectedHolonPreparation>();
        foreach (var item in root.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Stored preparation record must be an object.");
            var fields = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in item.EnumerateObject()) if (!fields.Add(field.Name)) throw new InvalidDataException("Stored preparation has duplicate fields.");
            if (!fields.SetEquals(new[] { "id", "envelope" }) || item.GetProperty("id").ValueKind != JsonValueKind.String
                || item.GetProperty("envelope").ValueKind != JsonValueKind.String)
                throw new InvalidDataException("Stored preparation schema is invalid.");
            var text = item.GetProperty("id").GetString();
            if (!Guid.TryParseExact(text, "D", out var id) || text != id.ToString("D")) throw new InvalidDataException("Stored preparation identity is invalid.");
            records.Add(new ProtectedHolonPreparation(id, item.GetProperty("envelope").GetString()));
        }
        return new HolonWritePreparation(candidate.OperationId, candidate.ActorId, candidate.Authority, records);
    }
    private static OASISResult<HolonWritePreparation> PreparationError(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };
}
