using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS
{
    // Selected role intent travels explicitly to the native locked writer; never recapture
    // flattened children or publish relationship metadata in a later corrective write.
    protected virtual Task<OASISResult<IHolon>> SavePlannedHolonAsync(PlannedHolonWrite write)
        =>SaveHolonRecordAsync(write.Snapshot,write);

    private async Task<OASISResult<IHolon>> SaveHolonRecordAsync(IHolon source,PlannedHolonWrite? write)
    {
        var result=new OASISResult<IHolon>();
        try{
            var publication=StoredRecordPublicationSnapshot<Core.Holons.Holon>.Capture(source);
            var proposal=publication.Proposal;
            if(proposal.Id==Guid.Empty)proposal.Id=Guid.NewGuid();
            proposal.ProviderUniqueStorageKey??=new Dictionary<Core.Enums.ProviderType,string>();
            proposal.ProviderUniqueStorageKey[Core.Enums.ProviderType.OracleDBOASIS]=proposal.Id.ToString();
            var stored=await WriteCanonicalRecordAsync(proposal,"OASIS_HOLONS",write);
            PublishSavedIdentity(source,stored,publication.ValidateUnchanged);
            result.Result=source;result.IsSaved=true;result.SavedCount=1;result.Message=$"OracleDBOASIS: Holon '{proposal.Name}' saved.";
        }catch(Exception cause){result.Exception=cause;OASISErrorHandling.HandleError(ref result,$"OracleDBOASIS: Error saving holon: {cause.Message}");if(cause is StorageLifecycleConflictException)result.ErrorCode=StorageLifecycleConflictException.ErrorCode;}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> WriteHolonGraphAsync(IEnumerable<IHolon> roots,bool children,bool recursive,int maxDepth,int initialDepth,bool continueOnError,bool onProvider)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();var saved=new List<IHolon>();var failures=new List<Exception>();
        try{
            var plan=HolonGraphWritePlan.Create(roots,children,recursive,maxDepth,initialDepth);var failedParents=new HashSet<Guid>();
            var publications=new Dictionary<IHolon,StoredRecordPublicationSnapshot<Core.Holons.Holon>>(ReferenceEqualityComparer.Instance);
            foreach(var node in plan.Nodes)publications.Add(node.Source,StoredRecordPublicationSnapshot<Core.Holons.Holon>.Capture(node.Source));
            foreach(var node in plan.Nodes){
                if(failedParents.Contains(node.Snapshot.ParentHolonId)){
                    failedParents.Add(node.Snapshot.Id);failures.Add(new InvalidOperationException("Oracle descendant skipped because its parent failed."));continue;
                }
                publications[node.Source].ValidateUnchanged();
                var written=await SavePlannedHolonAsync(node);
                if(written.IsError||written.Result==null){
                    var cause=written.Exception??new InvalidOperationException(written.Message);failures.Add(cause);failedParents.Add(node.Snapshot.Id);
                    if(!continueOnError||NativeMutationReconciliation.IsRequired(cause))break;
                }else{
                    try{PublishSavedIdentity(node.Source,written.Result,publications[node.Source].ValidateUnchanged);node.Source.ParentHolonId=written.Result.ParentHolonId;saved.Add(node.Source);}
                    catch(Exception cause){failures.Add(cause);break;}
                }
            }
            result.Result=failures.Count==0?plan.Roots:saved;result.IsSaved=failures.Count==0;
            if(failures.Count>0){
                var cause=failures.Count==1?failures[0]:new AggregateException(failures);
                result.Exception=saved.Count>0?new OraclePartialMutationException(saved.Select(row=>row.Id),cause):cause;
                OASISErrorHandling.HandleError(ref result,"Oracle graph save failed; inspect acknowledged records and original exception.");
            }
        }catch(Exception cause){
            result.Exception=saved.Count>0?new OraclePartialMutationException(saved.Select(row=>row.Id),cause):cause;
            OASISErrorHandling.HandleError(ref result,"Oracle graph admission or write failed.");
        }
        result.SavedCount=saved.Count;
        if(result.Exception is StorageLifecycleConflictException)result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
        return result;
    }
}

public sealed class OraclePartialMutationException:Exception,INativeMutationReconciliationRequired
{
    public IReadOnlyList<Guid> AcknowledgedRecordIds{get;}
    public string OriginalTransactionId=>string.Empty;
    public OraclePartialMutationException(IEnumerable<Guid> acknowledged,Exception cause):base("Oracle graph has acknowledged partial writes; reconcile before replay.",cause)
        =>AcknowledgedRecordIds=Array.AsReadOnly(acknowledged.ToArray());
}
