using System;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    // The body owns native acknowledgement validation; this owner alone publishes
    // a committed result and classifies rollback/COMMIT/resource failures.
    private async Task<OASISResult<T>> ExecuteOwnedNeoMutationAsync<T>(Func<IAsyncTransaction,Task<(OASISResult<T> Result,bool Commit)>> body)
    {
        var result=new OASISResult<T>();var state=new NeoHolonTransactionState();var knownOutcome=false;
        IAsyncSession session=null;IAsyncTransaction transaction=null;Exception failure=null,operationFailure=null;
        try
        {
            if(!IsProviderActivated||_driver==null)throw new InvalidOperationException("Neo4j provider is not activated.");
            session=_driver.AsyncSession();transaction=await session.BeginTransactionAsync().ConfigureAwait(false);
            var candidate=await body(transaction).ConfigureAwait(false);
            if(candidate.Commit)
            {
                if(candidate.Result.IsError)throw new InvalidOperationException("A failed acknowledgement cannot commit a mutation.");
                state.CommitDispatched=true;await transaction.CommitAsync().ConfigureAwait(false);state.KnownCommitted=true;
            }
            else
            {
                try{await transaction.RollbackAsync().ConfigureAwait(false);}
                catch{state.RollbackFailed=true;throw;}
            }
            result=candidate.Result;knownOutcome=true;
        }
        catch(Exception cause)
        {
            failure=cause;operationFailure=cause;
            if(transaction!=null&&!state.CommitDispatched&&!state.KnownCommitted&&!state.RollbackFailed)
            {
                try{await transaction.RollbackAsync().ConfigureAwait(false);}
                catch(Exception rollback){state.RollbackFailed=true;failure=new AggregateException(cause,rollback);}
            }
        }
        finally
        {
            foreach(var resource in new IDisposable[]{transaction,session})
            {
                try{resource?.Dispose();}
                catch(Exception cleanup){failure=failure==null?cleanup:new AggregateException(failure,cleanup);}
            }
        }
        if(failure==null)return result;
        result.Exception=failure;
        if(knownOutcome)
        {
            result.IsWarning=true;result.WarningCount++;
            result.Message=(string.IsNullOrWhiteSpace(result.Message)?"The native outcome is acknowledged.":result.Message)+" Neo4j resource cleanup failed.";
            return result;
        }
        result.Message=failure.Message;
        var uncertain=state.CommitDispatched||state.RollbackFailed;
        result.IsError=true;result.ErrorCount++;result.Result=default;result.IsSaved=false;result.IsDeleted=false;
        result.SavedCount=0;result.DeletedCount=0;result.HasAnyHolonsChanged=false;
        if(uncertain)result.Exception=new Neo4jMutationReconciliationException(failure);
        var reason=operationFailure??failure;
        result.ErrorCode=uncertain?"COSMIC_WRITE_RECONCILIATION_REQUIRED":reason is StorageLifecycleConflictException?StorageLifecycleConflictException.ErrorCode
            :reason is ArgumentException or System.IO.InvalidDataException?"HYPERDRIVE_PAYLOAD_INVALID":"COSMIC_WRITE_FAILED";
        result.Message=result.Exception.Message;return result;
    }

    private static OASISResult<T> NeoMutationAdmissionFailure<T>(Exception cause)=>new()
    {IsError=true,ErrorCount=1,Exception=cause,Message=cause.Message,
        ErrorCode=cause is ArgumentException or System.IO.InvalidDataException?"HYPERDRIVE_PAYLOAD_INVALID":"COSMIC_WRITE_FAILED"};
}
