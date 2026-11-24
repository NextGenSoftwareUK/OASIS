using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : IConditionalHolonUnitOfWorkStorageProvider
{
    /// <summary>Mixed saves and deletions in one MongoDB transaction; each half uses the normal atomic write.</summary>
    public async Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonUnitOfWorkAsync(ConditionalHolonUnitOfWorkRequest request,
        CancellationToken cancellationToken = default)
    {
        static OASISResult<IEnumerable<IHolon>> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Write.Authority != Core.Enums.ProviderType.MongoDBOASIS)
                return Error("COSMIC_AUTHORITY_MISMATCH", "The unit of work belongs to another provider authority.");
            var (save, delete) = request.CreatePartRequests();
            if (save == null || delete == null)
                return Error("COSMIC_UNIT_OF_WORK_INVALID", "A unit of work needs at least one save and one deletion.");
            IAtomicHolonWritePlan Plan(ConditionalHolonWriteRequest part) => part.Write.ReplicationEnrollment == null
                ? ConditionalHolonPlan.Create(part, Core.Enums.ProviderType.MongoDBOASIS)
                : ConditionalHolonPlan.CreateReplicating(part, Core.Enums.ProviderType.MongoDBOASIS, part.Write.ReplicationEnrollment);
            OASISResult<IEnumerable<IHolon>> saved = null, deleted = null, failed = null;
            var outcome = await _holonRepository.ExecuteUnitOfWorkAsync(request, async (session, receipt) =>
            {
                foreach (var part in new[] { save, delete })
                {
                    var written = await SaveGraphsAtomicAsync(part.Roots, false, false, 0, 0, Plan(part), part.Write.ActorId, cancellationToken,
                        expectedStorage: part.Write.ReplicationEnrollment?.SourceStorageIdentity, ambient: session).ConfigureAwait(false);
                    if (written == null || written.IsError) { failed = written; return false; }
                    if (ReferenceEquals(part, save)) saved = written; else deleted = written;
                }
                return true;
            }, cancellationToken).ConfigureAwait(false);
            if (!outcome.Committed)
                return failed ?? Error(outcome.ErrorCode ?? "COSMIC_CONDITIONAL_WRITE_FAILED", "The MongoDB unit of work did not commit.", outcome.Cause);
            var combined = new OASISResult<IEnumerable<IHolon>>(saved.Result.Concat(deleted.Result).ToList()) {
                IsSaved = true, IsDeleted = true, HasAnyHolonsChanged = true, SavedCount = saved.SavedCount, DeletedCount = deleted.DeletedCount,
                MetaData = new Dictionary<string, string> { ["COSMIC.OperationId"] = request.Write.OperationId.ToString("D"),
                    ["COSMIC.SaveOperationId"] = request.SaveOperationId.ToString("D"), ["COSMIC.DeleteOperationId"] = request.DeleteOperationId.ToString("D") } };
            foreach (var (part, name) in new[] { (saved, "Save"), (deleted, "Delete") })
                if (part.MetaData != null)
                    foreach (var pair in part.MetaData.Where(pair => pair.Key != "COSMIC.OperationId")) combined.MetaData[pair.Key + "." + name] = pair.Value;
            return combined;
        }
        catch (Exception cause)
        {
            return Error(cause is OperationCanceledException ? "COSMIC_CONDITIONAL_WRITE_CANCELLED" : "COSMIC_CONDITIONAL_WRITE_INVALID",
                "MongoDB unit-of-work admission failed.", cause);
        }
    }
}
