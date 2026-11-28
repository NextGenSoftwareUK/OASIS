using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

/// <summary>The parent receipt state a unit of work observed inside its transaction.</summary>
internal enum MongoUnitOfWorkReceipt { Absent, Replay }

public partial class HolonRepository
{
    /// <summary>
    /// Runs both unit-of-work halves in ONE snapshot transaction, then records the parent receipt.
    /// A replay (parent receipt present) re-reads the part receipts and commits nothing.
    /// </summary>
    internal async Task<(bool Committed,string ErrorCode,Exception Cause)> ExecuteUnitOfWorkAsync(ConditionalHolonUnitOfWorkRequest request,
        Func<IClientSessionHandle,MongoUnitOfWorkReceipt,Task<bool>> parts,CancellationToken token)
    {
        using var session=await OpenHeldTransactionAsync(token).ConfigureAwait(false);
        var commitAttempted=false;var abortAttempted=false;Exception primary=null;
        try
        {
            var outcome=await ExecuteUnitOfWorkWithinOwnerAsync(request,parts,session,token).ConfigureAwait(false);
            primary=outcome.Cause;
            if(!outcome.Completed||outcome.Replay)
            {
                if(session.IsInTransaction){abortAttempted=true;await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);}
                return(outcome.Completed,outcome.ErrorCode,outcome.Cause);
            }
            commitAttempted=true;await session.CommitTransactionAsync(token).ConfigureAwait(false);
            return(true,null,null);
        }
        catch(Exception cause)
        {
            if(primary!=null)cause=new AggregateException(primary,cause);
            if(!commitAttempted&&!abortAttempted&&session.IsInTransaction)
                try{await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);}
                catch(Exception abort){cause=new AggregateException(cause,abort);}
            return(false,commitAttempted?"COSMIC_WRITE_RECONCILIATION_REQUIRED":
                MongoTransactionFailure.IsWriteConflict(cause)?"COSMIC_CONCURRENT_WRITE":
                cause is OperationCanceledException?"COSMIC_CONDITIONAL_WRITE_CANCELLED":"COSMIC_CONDITIONAL_WRITE_FAILED",cause);
        }
    }

    /// <summary>Receipt, part and unique-key work under an existing native owner; never commits or aborts it.</summary>
    internal async Task<(bool Completed,bool Replay,string ErrorCode,Exception Cause)> ExecuteUnitOfWorkWithinOwnerAsync(
        ConditionalHolonUnitOfWorkRequest request,Func<IClientSessionHandle,MongoUnitOfWorkReceipt,Task<bool>> parts,
        IClientSessionHandle session,CancellationToken token,bool allowReplay=true)
    {
        try
        {
            if(session==null||!session.IsInTransaction)throw new InvalidOperationException("A held MongoDB transaction is required.");
            var database=_dbContext.MongoDB;var receipts=database.GetCollection<BsonDocument>("CosmicWriteReceipt");
            var parentId=request.Write.OperationId.ToString("D");
            var parent=await receipts.Find(session,new BsonDocument("_id",parentId)).SingleOrDefaultAsync(token).ConfigureAwait(false);
            if(parent!=null&&parent.GetValue("Fingerprint",BsonNull.Value)!=request.ParentFingerprint)
                return(false,false,"COSMIC_OPERATION_ID_REUSED",null);
            if(parent!=null&&!allowReplay)return(false,false,"COSMIC_TRANSACTION_OPERATION_ALREADY_COMMITTED",null);
            if(parent!=null)
            {
                var count=await receipts.CountDocumentsAsync(session,Builders<BsonDocument>.Filter.In("_id",
                    new[]{request.SaveOperationId.ToString("D"),request.DeleteOperationId.ToString("D")}),cancellationToken:token).ConfigureAwait(false);
                var(save,delete)=request.CreatePartRequests();
                if(count!=(save==null?0:1)+(delete==null?0:1))return(false,false,"COSMIC_RECEIPT_INVALID",null);
            }
            if(!await parts(session,parent==null?MongoUnitOfWorkReceipt.Absent:MongoUnitOfWorkReceipt.Replay).ConfigureAwait(false))
                return(false,false,null,null);
            if(parent!=null)return(true,true,null,null);
            if(!await ApplyUniqueKeyClaimsAsync(database,session,request,token).ConfigureAwait(false))
                return(false,false,"COSMIC_UNIQUE_KEY_CONFLICT",null);
            await receipts.InsertOneAsync(session,new BsonDocument{{"_id",parentId},{"Fingerprint",request.ParentFingerprint},
                {"ResultJson",new BsonDocument{{"kind",ConditionalHolonUnitOfWorkRequest.ReceiptKind},
                    {"save",request.SaveOperationId.ToString("D")},{"delete",request.DeleteOperationId.ToString("D")}}.ToJson()},
                {"CommittedAtUtc",DateTime.UtcNow}},cancellationToken:token).ConfigureAwait(false);
            return(true,false,null,null);
        }
        catch(Exception cause)
        {return(false,false,MongoTransactionFailure.IsWriteConflict(cause)?"COSMIC_CONCURRENT_WRITE":
            cause is OperationCanceledException?"COSMIC_CONDITIONAL_WRITE_CANCELLED":"COSMIC_CONDITIONAL_WRITE_FAILED",cause);}
    }

    internal async Task<IClientSessionHandle> OpenHeldTransactionAsync(CancellationToken token)
    {
        var session=await _dbContext.MongoDB.Client.StartSessionAsync(cancellationToken:token).ConfigureAwait(false);
        try
        {
            session.StartTransaction(new TransactionOptions(readConcern:ReadConcern.Snapshot,
                writeConcern:WriteConcern.WMajority.With(journal:true),readPreference:ReadPreference.Primary));
            return session;
        }
        catch{session.Dispose();throw;}
    }

    /// <summary>M10 claims inside the snapshot transaction. _id is the native key, so a concurrent
    /// claim of the same key is a write conflict or duplicate key and aborts one transaction.</summary>
    private static async Task<bool> ApplyUniqueKeyClaimsAsync(IMongoDatabase database, IClientSessionHandle session,
        ConditionalHolonUnitOfWorkRequest request, CancellationToken token)
    {
        if (request.UniqueKeys.Count == 0 && request.Deletes.Count == 0) return true;
        var keys = database.GetCollection<BsonDocument>("CosmicUniqueKey");
        foreach (var deleted in request.Deletes)
            await keys.DeleteManyAsync(session, new BsonDocument("HolonId", deleted.Id.ToString("D")), cancellationToken: token).ConfigureAwait(false);
        foreach (var claim in request.UniqueKeys)
        {
            var holon = claim.HolonId.ToString("D");
            var stale = new BsonDocument { {"HolonId", holon}, {"Scope", claim.Scope} };
            if (claim.Value != null) stale.Add("_id", new BsonDocument("$ne", claim.NativeKey));
            await keys.DeleteManyAsync(session, stale, cancellationToken: token).ConfigureAwait(false);
            if (claim.Value == null) continue;
            var held = await keys.Find(session, new BsonDocument("_id", claim.NativeKey)).SingleOrDefaultAsync(token).ConfigureAwait(false);
            if (held != null && held["HolonId"].AsString != holon) return false;
            if (held == null)
                await keys.InsertOneAsync(session, new BsonDocument { {"_id", claim.NativeKey}, {"Scope", claim.Scope}, {"HolonId", holon} },
                    cancellationToken: token).ConfigureAwait(false);
        }
        return true;
    }
}
