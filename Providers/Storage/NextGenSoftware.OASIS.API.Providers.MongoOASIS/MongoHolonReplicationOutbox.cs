using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

/// <summary>Consumes the enrolled Mongo queue. Native session/transaction ownership and server UTC fence mutations.</summary>
public sealed class MongoHolonReplicationOutbox : IHolonReplicationReadinessOutbox
{
    private readonly IMongoClient _client;
    private readonly IMongoCollection<BsonDocument> _rows;
    public MongoHolonReplicationOutbox(MongoDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _client=context.MongoClient ?? throw new ArgumentException("An initialized Mongo context is required.",nameof(context));
        _rows=(context.MongoDB ?? throw new ArgumentException("A Mongo database is required.",nameof(context)))
            .GetCollection<BsonDocument>("CosmicHolonOutbox");
    }

    public Task<OASISResult<HolonDeliveryClaim>> ClaimNextAsync(string targetId,string workerId,TimeSpan leaseDuration,CancellationToken cancellationToken=default)
        =>Mutate(async session=>{
            var row=await _rows.Find(session,new BsonDocument{{"TargetId",targetId},{"DeliveryState",new BsonDocument("$ne","Completed")}})
                .Sort(new BsonDocument("Sequence",1)).Limit(1).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if(row==null)return (new OASISResult<HolonDeliveryClaim>{Message="No unfinished obligation exists for this target."},false);
            var record=ValidateRow(row);
            if(record.State==HolonDeliveryState.ReconciliationRequired)
                return (new OASISResult<HolonDeliveryClaim>{Message="The oldest target obligation requires reconciliation."},false);
            ValidatePayload(row,record.Operation);
            if(record.Attempts==int.MaxValue)throw new InvalidDataException("Outbox attempt count is exhausted.");
            var lease=Guid.NewGuid();
            var filter=new BsonDocument{{"_id",row["_id"]},{"$expr",new BsonDocument("$and",new BsonArray{
                new BsonDocument("$lte",new BsonArray{"$NextAttemptUtcTicks",NowTicks()}),
                new BsonDocument("$or",new BsonArray{new BsonDocument("$eq",new BsonArray{"$DeliveryState","Pending"}),
                    new BsonDocument("$and",new BsonArray{new BsonDocument("$eq",new BsonArray{"$DeliveryState","InFlight"}),
                        new BsonDocument("$lte",new BsonArray{"$LeaseUntilUtcTicks",NowTicks()})})})})}};
            var changed=await _rows.FindOneAndUpdateAsync(session,filter,Pipeline(new BsonDocument{
                {"DeliveryState","InFlight"},{"LeaseToken",lease.ToString("D")},{"LeaseWorkerId",Literal(workerId)},
                {"LeaseUntilUtcTicks",new BsonDocument("$add",new BsonArray{NowTicks(),leaseDuration.Ticks})},
                {"AttemptCount",new BsonDocument("$add",new BsonArray{"$AttemptCount",1})}}),
                new FindOneAndUpdateOptions<BsonDocument>{ReturnDocument=ReturnDocument.After},cancellationToken).ConfigureAwait(false);
            if(changed==null)return (new OASISResult<HolonDeliveryClaim>{Message="The oldest target obligation is leased or not yet due."},false);
            var until=Ticks(changed,"LeaseUntilUtcTicks");
            return (new OASISResult<HolonDeliveryClaim>(new HolonDeliveryClaim(row["Sequence"].AsInt64,record.Operation,targetId,workerId,lease,
                row["PayloadJson"].AsString,checked(record.Attempts+1),new DateTime(until,DateTimeKind.Utc))),true);
        },cancellationToken,()=>{Identity(targetId);Identity(workerId);Duration(leaseDuration);});

    public Task<OASISResult<bool>> RenewAsync(HolonDeliveryClaim claim,TimeSpan leaseDuration,CancellationToken cancellationToken=default)
        =>Mutate(async session=>{
            var changed=await _rows.UpdateOneAsync(session,Owned(claim),Pipeline(new BsonDocument("LeaseUntilUtcTicks",
                new BsonDocument("$max",new BsonArray{"$LeaseUntilUtcTicks",new BsonDocument("$add",new BsonArray{NowTicks(),leaseDuration.Ticks})}))),
                cancellationToken:cancellationToken).ConfigureAwait(false);
            return (OwnedResult(changed),changed.MatchedCount==1);
        },cancellationToken,()=>{ClaimIdentity(claim);Duration(leaseDuration);});

    public Task<OASISResult<bool>> FinishAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,string errorCode=null,
        string message=null,DateTime? nextAttemptUtc=null,CancellationToken cancellationToken=default)
        =>FinishCore(claim,outcome,null,errorCode,message,nextAttemptUtc,cancellationToken);
    public Task<OASISResult<bool>> FinishWithRecoveryAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,
        HolonDeliveryRecoveryEvidence recoveryEvidence,string errorCode=null,string message=null,DateTime? nextAttemptUtc=null,CancellationToken cancellationToken=default)
        =>FinishCore(claim,outcome,recoveryEvidence ?? throw new ArgumentNullException(nameof(recoveryEvidence)),errorCode,message,nextAttemptUtc,cancellationToken);
    private Task<OASISResult<bool>> FinishCore(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,HolonDeliveryRecoveryEvidence evidence,
        string code,string message,DateTime? retry,CancellationToken token)
        =>Mutate(async session=>{
            var json=evidence==null ? null:HolonRecoveryEvidenceJson.Write(evidence);
            var filter=Owned(claim);
            if(outcome==HolonDeliveryOutcome.Retry)filter["$and"]=new BsonArray{new BsonDocument("$expr",new BsonDocument("$gt",new BsonArray{retry.Value.Ticks,NowTicks()}))};
            var update=new BsonDocument{{"DeliveryState",outcome==HolonDeliveryOutcome.Retry ? "Pending":outcome.ToString()},
                {"LeaseToken",""},{"LeaseWorkerId",""},{"LeaseUntilUtcTicks",0L},{"NextAttemptUtcTicks",retry?.Ticks ?? 0L},
                {"LastErrorCode",Literal(code)},{"LastErrorMessage",Literal(message)},
                {"RecoveryEvidenceJson",Literal(json)},{"RecoveryEvidenceDigest",Literal(json==null ? null:HolonRecoveryEvidenceJson.Digest(json))},
                {"CompletedAtUtcTicks",outcome==HolonDeliveryOutcome.Completed ? NowTicks():new BsonInt64(0L)}};
            var changed=await _rows.UpdateOneAsync(session,filter,Pipeline(update),cancellationToken:token).ConfigureAwait(false);
            return (OwnedResult(changed),changed.MatchedCount==1);
        },token,()=>{
            ClaimIdentity(claim);
            if(!Enum.IsDefined(typeof(HolonDeliveryOutcome),outcome)
                || evidence?.RequiresNativeReconciliation==true && outcome!=HolonDeliveryOutcome.ReconciliationRequired
                || outcome==HolonDeliveryOutcome.Completed && (code!=null || message!=null || retry!=null)
                || outcome!=HolonDeliveryOutcome.Completed && (string.IsNullOrWhiteSpace(code) || code.Length>128 || message?.Length>4096)
                || outcome==HolonDeliveryOutcome.Retry && (!retry.HasValue || retry.Value.Kind!=DateTimeKind.Utc)
                || outcome==HolonDeliveryOutcome.ReconciliationRequired && retry.HasValue)
                throw new ArgumentException("Delivery outcome, diagnostics and recovery evidence disagree.");
        });

    public async Task<OASISResult<HolonDeliveryInspection>> InspectAsync(long sequence,CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if(sequence<=0)throw new ArgumentOutOfRangeException(nameof(sequence));
            var row=await _rows.WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority).Find(new BsonDocument("Sequence",sequence)).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if(row==null)return new OASISResult<HolonDeliveryInspection>{Message="No delivery obligation exists at this sequence."};
            var value=ValidateRow(row);
            return new OASISResult<HolonDeliveryInspection>(new HolonDeliveryInspection(sequence,value.Operation,row["TargetId"].AsString,value.State,value.Attempts,
                NullableString(row,"LastErrorCode"),NullableString(row,"LastErrorMessage"),value.Evidence));
        }
        catch(OperationCanceledException)when(cancellationToken.IsCancellationRequested){throw;}
        catch(Exception cause){return Failure<HolonDeliveryInspection>(cause);}
    }

    public async Task<OASISResult<HolonDeliveryInspection>> PeekNextAsync(string targetId,CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            Identity(targetId);
            var row=await _rows.WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority).Find(new BsonDocument{{"TargetId",targetId},{"DeliveryState",new BsonDocument("$ne","Completed")}})
                .Sort(new BsonDocument("Sequence",1)).Limit(1).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if(row==null)return new OASISResult<HolonDeliveryInspection>{Message="No unfinished obligation exists for this target."};
            var value=ValidateRow(row);ValidatePayload(row,value.Operation);
            return new OASISResult<HolonDeliveryInspection>(new HolonDeliveryInspection(row["Sequence"].AsInt64,value.Operation,targetId,value.State,value.Attempts,
                NullableString(row,"LastErrorCode"),NullableString(row,"LastErrorMessage"),value.Evidence));
        }
        catch(OperationCanceledException)when(cancellationToken.IsCancellationRequested){throw;}
        catch(Exception cause){return Failure<HolonDeliveryInspection>(cause);}
    }

    private async Task<OASISResult<T>> Mutate<T>(Func<IClientSessionHandle,Task<(OASISResult<T> Result,bool Changed)>> action,CancellationToken token,Action validate)
    {
        token.ThrowIfCancellationRequested();OASISResult<T> result=null;var known=false;var commitEntered=false;var readCloseEntered=false;
        IClientSessionHandle session=null;Exception failure=null;var cancelled=false;string sessionId=null;long transaction=0;
        try
        {
            validate();
            session=await _client.StartSessionAsync(cancellationToken:token).ConfigureAwait(false);
            session.StartTransaction(new TransactionOptions(readConcern:ReadConcern.Snapshot,writeConcern:WriteConcern.WMajority.With(journal:true)));
            var outcome=await action(session).ConfigureAwait(false);result=outcome.Result;token.ThrowIfCancellationRequested();
            if(outcome.Changed)
            {
                sessionId=session.ServerSession.Id.ToJson();transaction=session.WrappedCoreSession.CurrentTransaction.TransactionNumber;
                commitEntered=true;await session.CommitTransactionAsync(token).ConfigureAwait(false);known=true;
            }
            else
            {
                // Closing a read-only transaction cannot revoke its observed result or justify another close attempt.
                known=true;readCloseEntered=true;await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch(Exception cause)
        {
            cancelled=!commitEntered && cause is OperationCanceledException && token.IsCancellationRequested;
            failure=commitEntered ? new MongoTransactionReconciliationException(sessionId,transaction,cause):cause;
            if(session!=null && !commitEntered && !readCloseEntered)
            {try{if(session.IsInTransaction)await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);}catch(Exception cleanup){failure=Join(failure,cleanup);}}
        }
        if(session!=null){try{session.Dispose();}catch(Exception cleanup){failure=Join(failure,cleanup);}}
        if(failure!=null)
        {
            if(!known)
            {
                if(cancelled)throw new OperationCanceledException("Delivery operation cancelled before commit; cleanup evidence is retained.",failure,token);
                return Failure<T>(failure);
            }
            result.IsWarning=true;result.WarningCount++;result.Exception=Join(result.Exception,failure);
            result.InnerMessages.Add("Delivery outcome is known, but Mongo session cleanup failed.");
        }
        return result;
    }
    private static Exception Join(Exception first,Exception second)=>first==null ? second:second==null ? first:new AggregateException(first,second);
    private static BsonDocument NowTicks()=>new("$add",new BsonArray{DateTime.UnixEpoch.Ticks,
        new BsonDocument("$multiply",new BsonArray{new BsonDocument("$toLong","$$NOW"),TimeSpan.TicksPerMillisecond})});
    private static BsonDocument Literal(string value)=>new("$literal",value==null ? BsonNull.Value:new BsonString(value));
    private static UpdateDefinition<BsonDocument> Pipeline(BsonDocument values)=>new PipelineUpdateDefinition<BsonDocument>(new[]{new BsonDocument("$set",values)});
    private static BsonDocument Owned(HolonDeliveryClaim claim)=>new(){{"Sequence",claim.Sequence},{"OperationId",claim.OperationId.ToString("D")},
        {"TargetId",claim.TargetId},{"DeliveryState","InFlight"},{"LeaseToken",claim.LeaseToken.ToString("D")},{"LeaseWorkerId",claim.WorkerId},
        {"$expr",new BsonDocument("$gt",new BsonArray{"$LeaseUntilUtcTicks",NowTicks()})}};
    private static OASISResult<bool> OwnedResult(UpdateResult result)=>result.IsAcknowledged && result.MatchedCount==1 ? new OASISResult<bool>(true)
        :new OASISResult<bool>{IsError=true,ErrorCount=1,ErrorCode="COSMIC_OUTBOX_LEASE_LOST",Message="The obligation is no longer owned by this unexpired lease or retry time is not in the future."};
    private static void Identity(string id){if(string.IsNullOrWhiteSpace(id) || id!=id.Trim() || id.Length>256)throw new ArgumentException("A stable identity is required.");}
    private static void Duration(TimeSpan lease){if(lease<=TimeSpan.Zero || lease>TimeSpan.FromHours(1))throw new ArgumentOutOfRangeException(nameof(lease));}
    private static void ClaimIdentity(HolonDeliveryClaim claim)
    {if(claim==null || claim.Sequence<=0 || claim.OperationId==Guid.Empty || claim.LeaseToken==Guid.Empty)throw new ArgumentException("A fenced claim is required.");Identity(claim.TargetId);Identity(claim.WorkerId);}
    private static long Ticks(BsonDocument row,string key)
    {if(!row.TryGetValue(key,out var value) || !value.IsInt64 || value.AsInt64<0 || value.AsInt64>DateTime.MaxValue.Ticks)throw new InvalidDataException("Invalid outbox UTC ticks: "+key);return value.AsInt64;}
    private static string NullableString(BsonDocument row,string key)
    {if(!row.TryGetValue(key,out var value) || value.IsBsonNull)return null;if(!value.IsString)throw new InvalidDataException("Invalid outbox text: "+key);return value.AsString;}
    private static (Guid Operation,HolonDeliveryState State,int Attempts,HolonDeliveryRecoveryEvidence Evidence) ValidateRow(BsonDocument row)
    {
        if(!row.TryGetValue("Format",out var format) || !format.IsInt32 || format.AsInt32!=1
            || !row.TryGetValue("Sequence",out var sequence) || !sequence.IsInt64 || sequence.AsInt64<=0
            || !Guid.TryParseExact(NullableString(row,"OperationId"),"D",out var operation) || operation==Guid.Empty
            || !Enum.TryParse<HolonDeliveryState>(NullableString(row,"DeliveryState"),out var state)
            || !Enum.IsDefined(typeof(HolonDeliveryState),state) || state.ToString()!=NullableString(row,"DeliveryState")
            || !row.TryGetValue("AttemptCount",out var attempts) || !attempts.IsInt32 || attempts.AsInt32<0)
            throw new InvalidDataException("Invalid outbox identity, state or attempt count.");
        try{Identity(NullableString(row,"TargetId"));}catch(ArgumentException error){throw new InvalidDataException("Invalid outbox target.",error);}
        var expiry=Ticks(row,"LeaseUntilUtcTicks");Ticks(row,"NextAttemptUtcTicks");var completed=Ticks(row,"CompletedAtUtcTicks");
        if(state==HolonDeliveryState.InFlight && (expiry==0 || attempts.AsInt32==0)
            || state==HolonDeliveryState.Completed && completed==0 || state!=HolonDeliveryState.Completed && completed!=0)
            throw new InvalidDataException("Outbox lifecycle timestamps disagree with state.");
        if(state==HolonDeliveryState.InFlight && (!Guid.TryParseExact(NullableString(row,"LeaseToken"),"D",out var lease) || lease==Guid.Empty
            || string.IsNullOrWhiteSpace(NullableString(row,"LeaseWorkerId"))))throw new InvalidDataException("Invalid active lease identity.");
        var evidence=HolonRecoveryEvidenceJson.ReadStored(NullableString(row,"RecoveryEvidenceJson"),NullableString(row,"RecoveryEvidenceDigest"),state);
        return (operation,state,attempts.AsInt32,evidence);
    }
    private static void ValidatePayload(BsonDocument row,Guid operation)
    {
        var json=NullableString(row,"PayloadJson");var digest=NullableString(row,"PayloadDigest");
        if(json==null || Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))!=digest)throw new InvalidDataException("Outbox payload checksum differs.");
        var delivery=CommittedHolonReplicaPayload.Read(json);
        if(delivery.OperationId!=operation || delivery.SourceId!=NullableString(row,"SourceId") || delivery.SourceAuthority!=ProviderType.MongoDBOASIS
            || NullableString(row,"SourceAuthority")!=ProviderType.MongoDBOASIS.ToString() || delivery.ActorId.ToString("D")!=NullableString(row,"ActorId"))
            throw new InvalidDataException("Outbox payload and enrollment disagree.");
    }
    private static OASISResult<T> Failure<T>(Exception cause)=>new(){IsError=true,ErrorCount=1,Exception=cause,Message=cause.Message,
        ErrorCode=Core.Helpers.NativeMutationReconciliation.IsRequired(cause) ? "COSMIC_OUTBOX_RECONCILIATION_REQUIRED"
            :cause is InvalidDataException ? "COSMIC_OUTBOX_CORRUPT":MongoTransactionFailure.IsWriteConflict(cause) ? "COSMIC_OUTBOX_CONCURRENT_WRITE":"COSMIC_OUTBOX_OPERATION_FAILED"};
}
