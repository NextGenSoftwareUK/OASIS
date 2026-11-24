using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    /// <summary>
    /// Mixed saves and deletions in ONE IMMEDIATE transaction. Each half runs through the normal
    /// atomic write (revisions, receipts, delta, outbox) under its derived operation ID; the parent
    /// receipt binds both under the caller's operation ID. Nothing commits unless both halves do.
    /// </summary>
    public OASISResult<IEnumerable<IHolon>> ExecuteConditionalHolonUnitOfWork(ConditionalHolonUnitOfWorkRequest request, CancellationToken token)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request.Write.Authority != Core.Enums.ProviderType.SQLLiteDBOASIS)
            return ConditionalError("COSMIC_AUTHORITY_MISMATCH", "The unit of work belongs to another provider authority.");
        var (save, delete) = request.CreatePartRequests();
        if (save == null && delete == null)
            return ConditionalError("COSMIC_UNIT_OF_WORK_INVALID", "A unit of work needs at least one entry.");
        if (_dbContext.ChangeTracker.HasChanges())
            return ConditionalError("COSMIC_CONTEXT_HAS_PENDING_CHANGES", "A conditional transaction cannot commit unrelated pending context changes.");
        SqliteTransaction native = null; Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction enlisted = null;
        var commitEntered = false; OASISResult<IEnumerable<IHolon>> result;
        _dbContext.Database.OpenConnection();
        try
        {
            token.ThrowIfCancellationRequested();
            var connection = (SqliteConnection)_dbContext.Database.GetDbConnection();
            native = connection.BeginTransaction(deferred: false);
            enlisted = _dbContext.Database.UseTransaction(native);
            var parentId = request.Write.OperationId.ToString("D");
            string storedParent = null;
            using (var read = connection.CreateCommand())
            {
                read.Transaction = native;
                read.CommandText = "SELECT RequestHash FROM CosmicWriteReceipt WHERE OperationId = $id";
                read.Parameters.AddWithValue("$id", parentId);
                storedParent = read.ExecuteScalar() as string;
            }
            if (storedParent != null && storedParent != request.ParentFingerprint)
                return Rollback(ConditionalError("COSMIC_OPERATION_ID_REUSED", "This operation ID is already bound to different write content or actor."));
            if (storedParent != null && !PartReceiptsExist(connection, native, request))
                return Rollback(ConditionalError("COSMIC_RECEIPT_INVALID", "The unit-of-work receipt is missing a committed part."));
            var saved = save == null ? Empty() : ExecuteAtomicHolonWrite(save, null, token, ambient: native);
            if (saved == null || saved.IsError) return Rollback(saved);
            var deleted = delete == null ? Empty() : ExecuteAtomicHolonWrite(delete, null, token, ambient: native);
            if (deleted == null || deleted.IsError) return Rollback(deleted);
            if (storedParent == null)
            {
                var conflict = ApplyUniqueKeyClaims(connection, native, request);
                if (conflict != null) return Rollback(conflict);
            }
            if (storedParent == null)
                using (var receipt = connection.CreateCommand())
                {
                    receipt.Transaction = native;
                    receipt.CommandText = "INSERT INTO CosmicWriteReceipt(OperationId, RequestHash, ResultJson, CommittedAtUtc) VALUES ($id, $hash, $json, $at)";
                    receipt.Parameters.AddWithValue("$id", parentId);
                    receipt.Parameters.AddWithValue("$hash", request.ParentFingerprint);
                    receipt.Parameters.AddWithValue("$json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                        kind = ConditionalHolonUnitOfWorkRequest.ReceiptKind, save = request.SaveOperationId, delete = request.DeleteOperationId }));
                    receipt.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("O"));
                    receipt.ExecuteNonQuery();
                }
            token.ThrowIfCancellationRequested();
            commitEntered = true;
            if (storedParent == null) enlisted.Commit(); else enlisted.Rollback();
            result = Combine(request, saved, deleted);
        }
        catch (Exception cause)
        {
            if (!commitEntered) { try { native?.Rollback(); } catch (Exception rollback) { cause = new AggregateException(cause, rollback); } }
            DetachAll();
            result = ConditionalError(commitEntered ? "COSMIC_WRITE_RECONCILIATION_REQUIRED"
                : cause is OperationCanceledException ? "COSMIC_CONDITIONAL_WRITE_CANCELLED" : "COSMIC_CONDITIONAL_WRITE_FAILED",
                commitEntered ? "Reconcile the original unit-of-work receipt before another mutation." : "The unit of work did not commit.", cause);
        }
        finally
        {
            enlisted?.Dispose(); native?.Dispose();
            _dbContext.Database.CloseConnection();
        }
        result.MetaData ??= new Dictionary<string, string>();
        result.MetaData["COSMIC.OperationId"] = request.Write.OperationId.ToString("D");
        return result;

        static OASISResult<IEnumerable<IHolon>> Empty() => new(Array.Empty<IHolon>());

        OASISResult<IEnumerable<IHolon>> Rollback(OASISResult<IEnumerable<IHolon>> failure)
        {
            native.Rollback(); DetachAll();
            return failure ?? ConditionalError("COSMIC_CONDITIONAL_WRITE_FAILED", "A unit-of-work part returned no outcome.");
        }
    }

    private void DetachAll()
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries<HolonModel>().ToArray()) entry.State = EntityState.Detached;
    }

    private static bool PartReceiptsExist(SqliteConnection connection, SqliteTransaction native, ConditionalHolonUnitOfWorkRequest request)
    {
        using var command = connection.CreateCommand();
        command.Transaction = native;
        var (save, delete) = request.CreatePartRequests();
        command.CommandText = "SELECT COUNT(*) FROM CosmicWriteReceipt WHERE OperationId IN ($save, $delete)";
        command.Parameters.AddWithValue("$save", request.SaveOperationId.ToString("D"));
        command.Parameters.AddWithValue("$delete", request.DeleteOperationId.ToString("D"));
        return Convert.ToInt64(command.ExecuteScalar()) == (save == null ? 0 : 1) + (delete == null ? 0 : 1);
    }

    /// <summary>M10 claims inside the unit-of-work transaction; returns a conflict or null.</summary>
    private static OASISResult<IEnumerable<IHolon>> ApplyUniqueKeyClaims(SqliteConnection connection, SqliteTransaction native,
        ConditionalHolonUnitOfWorkRequest request)
    {
        void Run(string sql, params (string Name, object Value)[] parameters)
        {
            using var command = connection.CreateCommand();
            command.Transaction = native; command.CommandText = sql;
            foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
            command.ExecuteNonQuery();
        }
        Run("CREATE TABLE IF NOT EXISTS CosmicUniqueKey (KeyId TEXT NOT NULL PRIMARY KEY, Scope TEXT NOT NULL, HolonId TEXT NOT NULL)");
        Run("CREATE UNIQUE INDEX IF NOT EXISTS IX_CosmicUniqueKey_Holon ON CosmicUniqueKey(HolonId, Scope)");
        foreach (var deleted in request.Deletes)
            Run("DELETE FROM CosmicUniqueKey WHERE HolonId = $holon", ("$holon", deleted.Id.ToString("D")));
        foreach (var claim in request.UniqueKeys)
        {
            var holon = claim.HolonId.ToString("D");
            Run("DELETE FROM CosmicUniqueKey WHERE HolonId = $holon AND Scope = $scope AND ($key IS NULL OR KeyId <> $key)",
                ("$holon", holon), ("$scope", claim.Scope), ("$key", claim.Value == null ? DBNull.Value : claim.NativeKey));
            if (claim.Value == null) continue;
            using var owner = connection.CreateCommand();
            owner.Transaction = native;
            owner.CommandText = "SELECT HolonId FROM CosmicUniqueKey WHERE KeyId = $key";
            owner.Parameters.AddWithValue("$key", claim.NativeKey);
            var held = owner.ExecuteScalar() as string;
            if (held != null && held != holon)
                return ConditionalError("COSMIC_UNIQUE_KEY_CONFLICT", "The '" + claim.Scope + "' key is already held by another record.");
            if (held == null)
                Run("INSERT INTO CosmicUniqueKey(KeyId, Scope, HolonId) VALUES ($key, $scope, $holon)",
                    ("$key", claim.NativeKey), ("$scope", claim.Scope), ("$holon", holon));
        }
        return null;
    }

    internal static OASISResult<IEnumerable<IHolon>> Combine(ConditionalHolonUnitOfWorkRequest request,
        OASISResult<IEnumerable<IHolon>> saved, OASISResult<IEnumerable<IHolon>> deleted)
    {
        var combined = new OASISResult<IEnumerable<IHolon>>(saved.Result.Concat(deleted.Result).ToList()) {
            IsSaved = true, IsDeleted = deleted.Result.Any(), HasAnyHolonsChanged = true,
            SavedCount = saved.SavedCount, DeletedCount = deleted.DeletedCount,
            IsWarning = saved.IsWarning || deleted.IsWarning, WarningCount = saved.WarningCount + deleted.WarningCount };
        combined.MetaData = new Dictionary<string, string>();
        foreach (var (part, id) in new[] { (saved, request.SaveOperationId), (deleted, request.DeleteOperationId) })
            if (part.MetaData != null)
                foreach (var pair in part.MetaData.Where(pair => pair.Key != "COSMIC.OperationId"))
                    combined.MetaData[pair.Key + "." + (id == request.SaveOperationId ? "Save" : "Delete")] = pair.Value;
        combined.MetaData["COSMIC.SaveOperationId"] = request.SaveOperationId.ToString("D");
        combined.MetaData["COSMIC.DeleteOperationId"] = request.DeleteOperationId.ToString("D");
        return combined;
    }
}
