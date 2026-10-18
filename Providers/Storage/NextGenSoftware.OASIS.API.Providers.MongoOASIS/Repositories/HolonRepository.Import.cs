using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal async Task<OASISResult<bool>> ImportAtomicAsync(IEnumerable<Holon> holons,Action<Holon,Holon> preserveAudit,Action prepareResponse=null)
    {
        var result=new OASISResult<bool>();
        var validatingPayload=false;
        try
        {
            var entries=holons.ToList();
            if(entries.Any(row=>row==null || row.HolonId==Guid.Empty) || entries.GroupBy(row=>row.HolonId).Any(group=>group.Count()>1))
                throw new ArgumentException("Import requires distinct nonempty holon identities.");
            var typed=_dbContext.Holon;
            var raw=_dbContext.MongoDB.GetCollection<BsonDocument>("Holon",typed.Settings);
            using var session=await _dbContext.MongoClient.StartSessionAsync().ConfigureAwait(false);
            session.StartTransaction(new TransactionOptions(readConcern:ReadConcern.Snapshot,writeConcern:WriteConcern.WMajority.With(journal:true)));
            var commitAttempted=false;var mutationsStarted=false;
            string sessionId=null;long transactionNumber=0;
            try
            {
                var staged=new List<(BsonDocument Body,bool New)>();
                foreach(var holon in entries)
                {
                    var lookup=Builders<Holon>.Filter.Eq(row=>row.HolonId,holon.HolonId)
                        .Render(new RenderArgs<Holon>(typed.DocumentSerializer,typed.Settings.SerializerRegistry));
                    var matches=await raw.Find(session,lookup).Limit(2).ToListAsync().ConfigureAwait(false);
                    if(matches.Count>1) throw new StorageIdentityAmbiguousException();
                    var observed=matches.SingleOrDefault();
                    var existing=observed==null ? null:BsonSerializer.Deserialize<Holon>(observed);
                    preserveAudit(holon,existing);
                    if(existing!=null)
                    {
                        holon.CreatedProviderType=existing.CreatedProviderType;
                        holon.CreatedOASISType=existing.CreatedOASISType;
                    }
                    if(existing==null && holon.CreatedProviderType==null)
                        holon.CreatedProviderType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);
                    PrepareNativeHolonIdentity(holon);
                    validatingPayload=true;
                    var body=MongoNativeRecordWriter.SerializePayload(typed,holon);
                    validatingPayload=false;
                    if(observed!=null) MongoNativeRecordWriter.RetainUnknownFields<Holon>(body,observed);
                    staged.Add((body,observed==null));
                }
                // Every payload and response must prepare before the FIRST native write.
                prepareResponse?.Invoke();
                foreach(var entry in staged)
                {
                    mutationsStarted=true;
                    if(entry.New) await raw.InsertOneAsync(session,entry.Body).ConfigureAwait(false);
                    else
                    {
                        var replaced=await raw.ReplaceOneAsync(session,new BsonDocument("_id",entry.Body["_id"]),entry.Body).ConfigureAwait(false);
                        if(!replaced.IsAcknowledged || replaced.MatchedCount!=1) throw new InvalidOperationException("Imported replacement was not acknowledged.");
                    }
                }
                // Capture identity before dispatch; cleanup must not erase the original commit evidence.
                sessionId=session.ServerSession.Id.ToJson();
                transactionNumber=session.WrappedCoreSession.CurrentTransaction.TransactionNumber;
                commitAttempted=true;
                await session.CommitTransactionAsync().ConfigureAwait(false);
                result.Result=true;result.IsSaved=true;result.SavedCount=entries.Count;
            }
            catch(Exception cause)
            {
                result.IsError=true;
                result.Exception=commitAttempted ? new MongoTransactionReconciliationException(sessionId,transactionNumber,cause):cause;
                result.Message="Mongo holon import transaction failed.";
                if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
                else if(!commitAttempted && MongoTransactionFailure.IsWriteConflict(cause)) result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
                else if(!mutationsStarted && (cause is ArgumentException || validatingPayload && cause is BsonSerializationException))
                    result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
                try {if(session.IsInTransaction) await session.AbortTransactionAsync().ConfigureAwait(false);}
                catch(Exception abort) {result.Exception=new AggregateException(result.Exception,abort);}
            }
        }
        catch(Exception cause)
        {
            if(result.IsSaved && !result.IsError)
            {
                result.IsWarning=true;result.WarningCount++;result.Exception=cause;
                result.Message="Mongo holon import committed, but session cleanup failed.";
            }
            else
            {
                result.IsError=true;
                result.Exception=result.Exception==null ? cause:new AggregateException(result.Exception,cause);
                result.Message="Mongo holon import preparation/session failed.";
                if(result.ErrorCode==null && cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
            }
        }
        return result;
    }
}
