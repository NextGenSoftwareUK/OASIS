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
    private async Task<OASISResult<IEnumerable<IHolon>>> WriteHolonGraphAsync(IEnumerable<IHolon> roots,bool children,bool recursive,int maxDepth,int initialDepth,bool continueOnError,bool onProvider)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();var saved=new List<IHolon>();var failures=new List<Exception>();
        try{
            var plan=HolonGraphWritePlan.Create(roots,children,recursive,maxDepth,initialDepth);var failedParents=new HashSet<Guid>();
            foreach(var node in plan.Nodes){
                if(failedParents.Contains(node.Snapshot.ParentHolonId)){
                    failedParents.Add(node.Snapshot.Id);failures.Add(new InvalidOperationException("Oracle descendant skipped because its parent failed."));continue;
                }
                var written=await SaveHolonAsync(node.Snapshot,false,recursive,maxDepth,continueOnError,onProvider);
                if(written.IsError||written.Result==null){
                    var cause=written.Exception??new InvalidOperationException(written.Message);failures.Add(cause);failedParents.Add(node.Snapshot.Id);
                    if(!continueOnError||NativeMutationReconciliation.IsRequired(cause))break;
                }else{
                    PublishSavedIdentity(node.Source,written.Result);node.Source.ParentHolonId=written.Result.ParentHolonId;saved.Add(node.Source);
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
