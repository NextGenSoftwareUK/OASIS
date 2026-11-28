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
        =>await ExecuteConditionalUnitOfWorkWithinOwnerAsync(request,cancellationToken).ConfigureAwait(false);

    private async Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalUnitOfWorkWithinOwnerAsync(
        ConditionalHolonUnitOfWorkRequest request,CancellationToken cancellationToken,MongoHeldTransactionCommands held=null)
    {
        static OASISResult<IEnumerable<IHolon>> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Write.Authority != Core.Enums.ProviderType.MongoDBOASIS)
                return Error("COSMIC_AUTHORITY_MISMATCH", "The unit of work belongs to another provider authority.");
            if(held!=null&&(!ReferenceEquals(_holonRepository,held.Repository)||request.Write.ActorId!=held.Access.ActorId))
                return Error("COSMIC_TRANSACTION_SCOPE_INVALID","The staged write changed the held actor or storage instance.");
            var (save, delete) = request.CreatePartRequests();
            if (save == null && delete == null)
                return Error("COSMIC_UNIT_OF_WORK_INVALID", "A unit of work needs at least one entry.");
            IAtomicHolonWritePlan Plan(ConditionalHolonWriteRequest part) => part.Write.ReplicationEnrollment == null
                ? ConditionalHolonPlan.Create(part, Core.Enums.ProviderType.MongoDBOASIS)
                : ConditionalHolonPlan.CreateReplicating(part, Core.Enums.ProviderType.MongoDBOASIS, part.Write.ReplicationEnrollment);
            OASISResult<IEnumerable<IHolon>> saved = null, deleted = null, failed = null;
            async Task<bool> Parts(MongoDB.Driver.IClientSessionHandle session,Repositories.MongoUnitOfWorkReceipt receipt)
            {
                foreach (var part in new[] { save, delete }.Where(part => part != null))
                {
                    var written = await SaveGraphsAtomicAsync(part.Roots, false, false, 0, 0, Plan(part), part.Write.ActorId, cancellationToken,
                        expectedStorage: part.Write.ReplicationEnrollment?.SourceStorageIdentity, ambient: session).ConfigureAwait(false);
                    if (written == null || written.IsError) { failed = written; return false; }
                    if (ReferenceEquals(part, save)) saved = written; else deleted = written;
                }
                return true;
            }
            (bool Completed,string ErrorCode,Exception Cause) outcome;
            if(held==null)
            {
                var committed=await _holonRepository.ExecuteUnitOfWorkAsync(request,Parts,cancellationToken).ConfigureAwait(false);
                outcome=(committed.Committed,committed.ErrorCode,committed.Cause);
            }
            else
            {
                var staged=await held.Repository.ExecuteUnitOfWorkWithinOwnerAsync(request,Parts,held.Session,cancellationToken,allowReplay:false).ConfigureAwait(false);
                outcome=(staged.Completed,staged.ErrorCode,staged.Cause);
            }
            saved ??= new OASISResult<IEnumerable<IHolon>>(Array.Empty<IHolon>());
            deleted ??= new OASISResult<IEnumerable<IHolon>>(Array.Empty<IHolon>());
            if (!outcome.Completed && outcome.ErrorCode == "COSMIC_UNIQUE_KEY_CONFLICT")
                return Error(outcome.ErrorCode, "A unique key in this unit of work is already held by another record.");
            if (!outcome.Completed)
                return failed ?? Error(outcome.ErrorCode ?? "COSMIC_CONDITIONAL_WRITE_FAILED", "The MongoDB unit of work did not commit.", outcome.Cause);
            var combined = new OASISResult<IEnumerable<IHolon>>(saved.Result.Concat(deleted.Result).ToList()) {
                IsSaved = true, IsDeleted = deleted.Result.Any(), HasAnyHolonsChanged = true, SavedCount = saved.SavedCount, DeletedCount = deleted.DeletedCount,
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
