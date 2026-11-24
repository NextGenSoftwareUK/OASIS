using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using INode = Neo4j.Driver.INode;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : IConditionalHolonUnitOfWorkStorageProvider
{
    /// <summary>Mixed saves and deletions in one Neo4j transaction; each half uses the normal atomic write.</summary>
    public async Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonUnitOfWorkAsync(ConditionalHolonUnitOfWorkRequest request,
        CancellationToken cancellationToken = default)
    {
        static OASISResult<IEnumerable<IHolon>> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        if (request == null) return Error("COSMIC_UNIT_OF_WORK_INVALID", "A unit of work is required.");
        if (request.Write.Authority != Core.Enums.ProviderType.Neo4jOASIS)
            return Error("COSMIC_AUTHORITY_MISMATCH", "The unit of work belongs to another provider authority.");
        if (!IsProviderActivated || _driver == null) return Error("COSMIC_AUTHORITY_UNAVAILABLE", "Neo4j provider is not activated.");
        var (save, delete) = request.CreatePartRequests();
        if (save == null || delete == null) return Error("COSMIC_UNIT_OF_WORK_INVALID", "A unit of work needs at least one save and one deletion.");
        var expected = request.Write.ReplicationEnrollment?.SourceStorageIdentity;
        if (expected != null) await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var commitDispatched = false;
        try
        {
            await using var session = _driver.AsyncSession();
            var transaction = await session.BeginTransactionAsync(config => config.WithMetadata(new Dictionary<string, object> {
                ["COSMIC.OperationId"] = request.Write.OperationId.ToString("D") })).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var parentId = request.Write.OperationId.ToString("D");
                var parentRows = await (await transaction.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN r",
                    new { operation = parentId }).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
                var replay = parentRows.Count == 1;
                if (replay)
                {
                    var stored = parentRows[0]["r"].As<INode>();
                    if (!stored.Properties.TryGetValue("Fingerprint", out var fingerprint) || (fingerprint as string) != request.ParentFingerprint)
                    { await transaction.RollbackAsync().ConfigureAwait(false); return Error("COSMIC_OPERATION_ID_REUSED", "This operation ID is bound to different write intent or actor."); }
                }
                var results = new List<OASISResult<IEnumerable<IHolon>>>();
                foreach (var part in new[] { save, delete })
                {
                    var plan = part.Write.ReplicationEnrollment == null ? ConditionalHolonPlan.Create(part, Core.Enums.ProviderType.Neo4jOASIS)
                        : ConditionalHolonPlan.CreateReplicating(part, Core.Enums.ProviderType.Neo4jOASIS, part.Write.ReplicationEnrollment);
                    var write = new NeoAtomicHolonWrite(plan, part.Write.ActorId, cancellationToken) {
                        ExpectedStorage = expected, StorageBindingCodePrefix = "COSMIC_SOURCE_STORAGE_BINDING", Ambient = transaction };
                    var written = await SaveHolonSnapshotsAsync(part.Roots, false, false, 0, 0, write).ConfigureAwait(false);
                    if (written == null || written.IsError)
                    { await transaction.RollbackAsync().ConfigureAwait(false); return written ?? Error("COSMIC_CONDITIONAL_WRITE_FAILED", "A unit-of-work part returned no outcome."); }
                    if (replay != write.AmbientReplayed)
                    { await transaction.RollbackAsync().ConfigureAwait(false); return Error("COSMIC_RECEIPT_INVALID", "The unit-of-work receipt and its part receipts disagree."); }
                    results.Add(written);
                }
                if (replay) await transaction.RollbackAsync().ConfigureAwait(false);
                else
                {
                    await (await transaction.RunAsync("CREATE (r:CosmicWriteReceipt {OperationId:$operation,Fingerprint:$fingerprint,ResultJson:$json,CommittedAtUtc:$now})",
                        new { operation = parentId, fingerprint = request.ParentFingerprint, now = DateTime.UtcNow.ToString("O"),
                            json = "{\"kind\":\"" + ConditionalHolonUnitOfWorkRequest.ReceiptKind + "\",\"save\":\"" + request.SaveOperationId.ToString("D")
                                + "\",\"delete\":\"" + request.DeleteOperationId.ToString("D") + "\"}" }).ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    commitDispatched = true;
                    await transaction.CommitAsync().ConfigureAwait(false);
                }
                var combined = new OASISResult<IEnumerable<IHolon>>(results.SelectMany(row => row.Result).ToList()) {
                    IsSaved = true, IsDeleted = true, HasAnyHolonsChanged = true, SavedCount = results[0].SavedCount, DeletedCount = results[1].DeletedCount,
                    MetaData = new Dictionary<string, string> { ["COSMIC.OperationId"] = parentId,
                        ["COSMIC.SaveOperationId"] = request.SaveOperationId.ToString("D"), ["COSMIC.DeleteOperationId"] = request.DeleteOperationId.ToString("D") } };
                foreach (var (part, name) in new[] { (results[0], "Save"), (results[1], "Delete") })
                    if (part.MetaData != null)
                        foreach (var pair in part.MetaData.Where(pair => pair.Key != "COSMIC.OperationId")) combined.MetaData[pair.Key + "." + name] = pair.Value;
                return combined;
            }
            catch (Exception cause) when (!commitDispatched)
            {
                try { await transaction.RollbackAsync().ConfigureAwait(false); }
                catch (Exception rollback) { throw new AggregateException(cause, rollback); }
                throw;
            }
            finally { await transaction.DisposeAsync().ConfigureAwait(false); }
        }
        catch (Exception cause)
        {
            return Error(commitDispatched ? "COSMIC_WRITE_RECONCILIATION_REQUIRED"
                : cause is OperationCanceledException ? "COSMIC_CONDITIONAL_WRITE_CANCELLED" : "COSMIC_CONDITIONAL_WRITE_FAILED",
                commitDispatched ? "Reconcile the original unit-of-work receipt before another mutation." : "The Neo4j unit of work did not commit.", cause);
        }
        finally { if (expected != null) _lifecycleGate.Release(); }
    }
}
