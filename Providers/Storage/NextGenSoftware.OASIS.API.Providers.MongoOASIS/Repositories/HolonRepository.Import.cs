using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal async Task<OASISResult<bool>> ImportAtomicAsync(IEnumerable<Holon> holons,Action<Holon,Holon> preserveAudit,Action<IReadOnlyList<Holon>> prepareResponse=null,
        Action<IReadOnlyDictionary<Guid,BsonDocument>,IReadOnlyDictionary<Guid,BsonDocument>> prepareBodies=null,
        MongoAtomicHolonWrite conditional=null,Action<Holon,Holon> prepareMembership=null)
    {
        var result=new OASISResult<bool>();
        var validatingPayload=false;
        try
        {
            var cancellation = conditional?.CancellationToken ?? CancellationToken.None;
            var replica = conditional?.Plan as CommittedHolonReplicaPlan;
            var edit = conditional?.Plan as ConditionalHolonPlan;
            cancellation.ThrowIfCancellationRequested();
            var entries=holons.ToList();
            if(entries.Any(row=>row==null || row.HolonId==Guid.Empty) || entries.GroupBy(row=>row.HolonId).Any(group=>group.Count()>1))
                throw new ArgumentException("Import requires distinct nonempty holon identities.");
            var typed=_dbContext.Holon;
            var raw=_dbContext.MongoDB.GetCollection<BsonDocument>("Holon",typed.Settings);
            using var session=await _dbContext.MongoClient.StartSessionAsync(cancellationToken:cancellation).ConfigureAwait(false);
            session.StartTransaction(new TransactionOptions(readConcern:ReadConcern.Snapshot,writeConcern:WriteConcern.WMajority.With(journal:true)));
            var commitAttempted=false;var mutationsStarted=false;
            string sessionId=null;long transactionNumber=0;
            try
            {
                var receipts = _dbContext.MongoDB.GetCollection<BsonDocument>("CosmicWriteReceipt");
                if (conditional != null)
                {
                    var receipt = await receipts.Find(session, new BsonDocument("_id", conditional.Plan.OperationId.ToString("D")))
                        .SingleOrDefaultAsync(cancellation).ConfigureAwait(false);
                    if (receipt != null)
                    {
                        if (receipt.GetValue("Fingerprint", BsonNull.Value) != conditional.Plan.Fingerprint)
                            throw new MongoConditionalIntentException();
                        try { conditional.ReplayReceipt(receipt["ResultJson"].AsString); }
                        catch (Exception invalid) { throw new MongoConditionalReceiptException("Stored conditional acknowledgement is invalid.", invalid); }
                        // The validated receipt proves the original durable outcome. Closing this
                        // read-only transaction cannot revoke it, or trigger a second abort attempt.
                        result.Result=true; result.IsSaved=true; result.SavedCount=entries.Count;
                        try { await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false); }
                        catch(Exception cleanup)
                        {
                            result.IsWarning=true; result.WarningCount++; result.Exception=cleanup;
                            result.Message="Mongo committed acknowledgement replay succeeded, but read-only transaction cleanup failed.";
                        }
                        return result;
                    }
                    // Positive receipt replay stays read-only. For an absent receipt, a
                    // committed fence write makes a stale snapshot conflict before mutation.
                    await TouchOperationFenceAsync(session, conditional.Plan.OperationId, cancellation).ConfigureAwait(false);
                }
                var staged=new List<(BsonDocument Body,bool New)>();
                var bodies=new Dictionary<Guid,BsonDocument>();
                var observations=new Dictionary<Guid,BsonDocument>();
                var conflicts = new List<CosmicConcurrencyConflict>();
                foreach(var holon in entries)
                {
                    var lookup=Builders<Holon>.Filter.Eq(row=>row.HolonId,holon.HolonId)
                        .Render(new RenderArgs<Holon>(typed.DocumentSerializer,typed.Settings.SerializerRegistry));
                    var matches=await raw.Find(session,lookup).Limit(2).ToListAsync(cancellation).ConfigureAwait(false);
                    if(matches.Count>1) throw new StorageIdentityAmbiguousException();
                    var observed=matches.SingleOrDefault();
                    var existing=observed==null ? null:BsonSerializer.Deserialize<Holon>(observed);
                    if(observed!=null) observations.Add(holon.HolonId,observed);
                    if(edit!=null)
                    {
                        var expected=edit.ExpectedRevisions[holon.HolonId];
                        if(expected.HasValue ? existing==null || existing.ProviderRevisionId!=expected.Value : existing!=null)
                            conflicts.Add(new CosmicConcurrencyConflict { HolonId=holon.HolonId, ExpectedRevisionId=expected,
                                ActualRevisionId=existing?.ProviderRevisionId, ActualVersionId=existing?.VersionId, ActualVersion=existing?.Version });
                    }
                }
                if(conflicts.Count!=0) throw new CosmicConcurrencyException(conflicts);
                if(replica!=null)
                    await PrepareCommittedReplicaEntitiesAsync(session, entries, observations, replica, cancellation).ConfigureAwait(false);
                var now=DateTime.UtcNow;
                for(var index=0;index<entries.Count;index++)
                {
                    var holon=entries[index];
                    var existing=observations.TryGetValue(holon.HolonId,out var original) ? BsonSerializer.Deserialize<Holon>(original) : null;
                    if(edit!=null && edit.Operation!=ConditionalHolonOperation.Save)
                        entries[index]=holon=existing;
                    prepareMembership?.Invoke(holon,existing);
                    if(replica==null) preserveAudit(holon,existing);
                    if(replica==null && existing!=null)
                    {
                        holon.CreatedProviderType=existing.CreatedProviderType;
                        holon.CreatedOASISType=existing.CreatedOASISType;
                    }
                    if(replica==null && existing==null && holon.CreatedProviderType==null)
                        holon.CreatedProviderType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);
                    if(edit!=null)
                    {
                        holon.PreviousVersionId=existing?.VersionId ?? Guid.Empty;
                        holon.VersionId=Guid.NewGuid(); holon.Version=checked((existing?.Version ?? 0)+1);
                        holon.ModifiedDate=existing==null ? DateTime.MinValue : now;
                        holon.ModifiedByAvatarId=existing==null ? Guid.Empty.ToString() : conditional.ActorId.ToString();
                        if(existing==null) {holon.CreatedDate=now;holon.CreatedByAvatarId=conditional.ActorId.ToString();}
                        if(conditional.Plan.Operation!=ConditionalHolonOperation.Save)
                        {holon.DeletedDate=now;holon.DeletedByAvatarId=conditional.ActorId.ToString();holon.IsActive=false;}
                    }
                    PrepareNativeHolonIdentity(holon);
                    MongoNativeRecordWriter.RotateHolonRevision(holon);
                }
                // Finish every provider-owned identity/revision before graph response materialization.
                prepareResponse?.Invoke(entries);
                foreach(var holon in entries)
                {
                    validatingPayload=true;
                    var body=MongoNativeRecordWriter.SerializePayload(typed,holon);
                    validatingPayload=false;
                    if(observations.TryGetValue(holon.HolonId,out var observed))
                        MongoNativeRecordWriter.RetainUnknownFields<Holon>(body,observed);
                    staged.Add((body,observed==null));
                    bodies.Add(holon.HolonId,body);
                }
                // Native frontier preparation remains inside the snapshot, before the first write.
                prepareBodies?.Invoke(bodies,observations);
                // Validate the actual BSON representation before any native mutation or receipt.
                if(replica!=null)
                    foreach(var body in bodies.Values)
                        replica.ValidateAcknowledgedRecord(Helpers.DataHelper.ConvertMongoEntityToOASISHolon(BsonSerializer.Deserialize<Holon>(body),false));
                var receiptJson=conditional?.CaptureReceipt();
                string replicationPayload=null;
                if(edit?.ReplicationEnrollment!=null)
                {
                    replicationPayload=conditional.CaptureReplicationPayload?.Invoke()
                        ?? throw new System.IO.InvalidDataException("An enrolled write requires its frozen committed payload before mutation.");
                    var delivery=CommittedHolonReplicaPayload.Read(replicationPayload);
                    if(delivery.OperationId!=edit.OperationId || delivery.ActorId!=conditional.ActorId
                        || delivery.SourceAuthority!=edit.Authority || delivery.SourceId!=edit.ReplicationEnrollment.SourceId)
                        throw new System.IO.InvalidDataException("The replication payload differs from its enrolled source operation.");
                    var nativePlan=CommittedHolonReplicaPlan.Create(delivery,edit.Authority);
                    foreach(var body in bodies.Values)
                        nativePlan.ValidateAcknowledgedRecord(Helpers.DataHelper.ConvertMongoEntityToOASISHolon(BsonSerializer.Deserialize<Holon>(body),false));
                }
                cancellation.ThrowIfCancellationRequested();
                foreach(var entry in staged)
                {
                    mutationsStarted=true;
                    if(conditional?.Plan.Operation==ConditionalHolonOperation.HardDelete)
                    {
                        var deleted=await raw.DeleteOneAsync(session,new BsonDocument("_id",entry.Body["_id"]),cancellationToken:cancellation).ConfigureAwait(false);
                        if(!deleted.IsAcknowledged || deleted.DeletedCount!=1) throw new InvalidOperationException("Conditional deletion was not acknowledged.");
                    }
                    else if(entry.New) await raw.InsertOneAsync(session,entry.Body,cancellationToken:cancellation).ConfigureAwait(false);
                    else
                    {
                        var replaced=await raw.ReplaceOneAsync(session,new BsonDocument("_id",entry.Body["_id"]),entry.Body,cancellationToken:cancellation).ConfigureAwait(false);
                        if(!replaced.IsAcknowledged || replaced.MatchedCount!=1) throw new InvalidOperationException("Imported replacement was not acknowledged.");
                    }
                }
                if(replica!=null)
                    await WriteCommittedReplicaHeadsAsync(session,bodies,replica,cancellation).ConfigureAwait(false);
                if(replicationPayload!=null)
                    await WritePrimaryHolonObligationsAsync(session,edit,conditional.ActorId,replicationPayload,now,cancellation).ConfigureAwait(false);
                if(conditional!=null)
                    await receipts.InsertOneAsync(session,new BsonDocument { {"_id",conditional.Plan.OperationId.ToString("D")},
                        {"Fingerprint",conditional.Plan.Fingerprint},{"ResultJson",receiptJson},{"CommittedAtUtc",now} },cancellationToken:cancellation).ConfigureAwait(false);
                // Capture identity before dispatch; cleanup must not erase the original commit evidence.
                sessionId=session.ServerSession.Id.ToJson();
                transactionNumber=session.WrappedCoreSession.CurrentTransaction.TransactionNumber;
                commitAttempted=true;
                await session.CommitTransactionAsync(cancellation).ConfigureAwait(false);
                result.Result=true;result.IsSaved=true;result.SavedCount=entries.Count;
            }
            catch(Exception cause)
            {
                result.IsError=true;
                result.Exception=commitAttempted ? new MongoTransactionReconciliationException(sessionId,transactionNumber,cause):cause;
                result.Message="Mongo holon import transaction failed.";
                if(cause is CosmicConcurrencyException) result.ErrorCode=CosmicConcurrencyException.Code;
                else if(cause is MongoConditionalIntentException) result.ErrorCode="COSMIC_OPERATION_ID_REUSED";
                else if(cause is MongoReplicaConflictException) result.ErrorCode="COSMIC_REPLICA_LINEAGE_CONFLICT";
                else if(cause is MongoReplicaHeadException) result.ErrorCode="COSMIC_REPLICA_HEAD_INVALID";
                else if(cause is MongoConditionalReceiptException) result.ErrorCode="COSMIC_RECEIPT_INVALID";
                else if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
                else if(!commitAttempted && (MongoTransactionFailure.IsWriteConflict(cause)
                    || conditional!=null && cause is MongoWriteException write && write.WriteError?.Category==ServerErrorCategory.DuplicateKey))
                    result.ErrorCode=conditional==null ? StorageLifecycleConflictException.ErrorCode : "COSMIC_CONCURRENT_WRITE";
                else if(!mutationsStarted && (cause is ArgumentException || validatingPayload && cause is BsonSerializationException))
                    result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
                if(conditional!=null && result.ErrorCode==null)
                    result.ErrorCode=commitAttempted ? "COSMIC_WRITE_RECONCILIATION_REQUIRED"
                        : cause is OperationCanceledException ? replica==null ? "COSMIC_CONDITIONAL_WRITE_CANCELLED" : "COSMIC_REPLICA_WRITE_CANCELLED"
                        : replica==null ? "COSMIC_CONDITIONAL_WRITE_FAILED" : "COSMIC_REPLICA_WRITE_FAILED";
                try {if(!commitAttempted && session.IsInTransaction) await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);}
                catch(Exception abort) {result.Exception=new AggregateException(result.Exception,abort);}
            }
        }
        catch(Exception cause)
        {
            if(result.IsSaved && !result.IsError)
            {
                result.IsWarning=true;result.WarningCount++;result.Exception=result.Exception==null ? cause:new AggregateException(result.Exception,cause);
                result.Message="Mongo committed acknowledgement succeeded, but session cleanup failed.";
            }
            else
            {
                result.IsError=true;
                result.Exception=result.Exception==null ? cause:new AggregateException(result.Exception,cause);
                result.Message="Mongo holon import preparation/session failed.";
                if(result.ErrorCode==null && cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
                if(conditional!=null && result.ErrorCode==null) result.ErrorCode="COSMIC_CONDITIONAL_WRITE_FAILED";
            }
        }
        return result;
    }
}
