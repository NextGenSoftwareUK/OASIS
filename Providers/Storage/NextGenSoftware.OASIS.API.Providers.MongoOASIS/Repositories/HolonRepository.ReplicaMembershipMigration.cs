using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Globalization;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    private sealed class ReceiptMembershipAcknowledgementException():IOException("Stored migration acknowledgement requires reconciliation.");
    private string ReceiptMembershipStoreHash()=>MongoReplicaReceiptMembershipAudit.Digest("mongo-receipt-membership-v1\n"
        +string.Join(",",_dbContext.MongoClient.Settings.Servers.OrderBy(server=>server.ToString(),StringComparer.Ordinal).Select(server=>server.ToString()))
        +"\n"+_dbContext.MongoClient.Settings.ReplicaSetName+"\n"+_dbContext.MongoDB.DatabaseNamespace.DatabaseName);
    private static string ReceiptMembershipHash(BsonDocument receipt)=>Convert.ToHexString(SHA256.HashData(receipt.ToBson()));
    private static Guid[] VerifiedReceiptMembership(BsonDocument receipt)
    {
        if(!receipt.TryGetValue("_id",out var id) || !id.IsString || !Guid.TryParseExact(id.AsString,"D",out var operation) || operation==Guid.Empty
            || !receipt.TryGetValue("Fingerprint",out var fingerprint) || !fingerprint.IsString || fingerprint.AsString.Length!=64 || !fingerprint.AsString.All(Uri.IsHexDigit)
            || !receipt.TryGetValue("ResultJson",out var body) || !body.IsString)
            throw new InvalidDataException("Receipt identity or acknowledgement is invalid.");
        var roots=ConditionalHolonReceipt.Read(body.AsString); // Checks canonical envelope, checksum, links and delta binding.
        var document=JObject.Parse(body.AsString);
        if(document.Value<string>("operation")!=operation.ToString("D") || document.Value<string>("intent")!=fingerprint.AsString)
            throw new InvalidDataException("Receipt identity/fingerprint differs from its acknowledgement.");
        return HolonGraphWritePlan.Create(roots,true,true,0,0).Nodes.Select(node=>node.Snapshot.Id)
            .OrderBy(value=>value.ToString("D"),StringComparer.Ordinal).ToArray();
    }

    internal async Task<OASISResult<MongoReplicaReceiptMembershipAudit>> AuditReplicaReceiptMembershipAsync(Guid operatorId,IEnumerable<Guid> operations,CancellationToken token)
    {
        try
        {
            var ids=operations?.Take(129).ToArray() ?? throw new ArgumentNullException(nameof(operations));
            if(operatorId==Guid.Empty || ids.Length is <1 or >128 || ids.Any(id=>id==Guid.Empty) || ids.Distinct().Count()!=ids.Length)
                throw new ArgumentException("Select 1–128 unique receipt identities explicitly.");
            var rows=await _dbContext.MongoDB.GetCollection<BsonDocument>("CosmicWriteReceipt").WithReadConcern(ReadConcern.Majority)
                .Find(new BsonDocument("_id",new BsonDocument("$in",new BsonArray(ids.Select(id=>id.ToString("D"))))))
                .Limit(ids.Length+1).ToListAsync(token).ConfigureAwait(false);
            if(rows.Count!=ids.Length)throw new InvalidDataException("Every selected receipt must exist exactly once.");
            var entries=rows.Select(row=>new MongoReplicaReceiptMembershipAudit.Entry(Guid.Parse(row["_id"].AsString),
                ReceiptMembershipHash(row),VerifiedReceiptMembership(row))).ToArray();
            return new OASISResult<MongoReplicaReceiptMembershipAudit>(new(ReceiptMembershipStoreHash(),operatorId,entries)){IsLoaded=true,LoadedCount=entries.Length};
        }
        catch(Exception cause){return new(){IsError=true,ErrorCount=1,ErrorCode=cause is OperationCanceledException
            ? "COSMIC_RECEIPT_MEMBERSHIP_CANCELLED":"COSMIC_RECEIPT_MEMBERSHIP_INVALID",Message="Receipt audit did not produce a valid migration intent.",Exception=cause};}
    }

    internal async Task<OASISResult<int>> MigrateReplicaReceiptMembershipAsync(Guid migrationId,MongoReplicaReceiptMembershipAudit audit,CancellationToken token)
    {
        IClientSessionHandle session=null;var commitEntered=false;var known=false;
        OASISResult<int> result=null;
        string Acknowledgement(int changed)=>MongoReplicaReceiptMembershipAudit.Digest("receipt-membership-ack-v1|"+migrationId.ToString("D")+"|"+audit.Fingerprint+"|"+audit.Entries.Count.ToString(CultureInfo.InvariantCulture)+"|"+changed.ToString(CultureInfo.InvariantCulture));
        void CleanupFailure(Exception cause)
        {
            result.Exception=result.Exception==null ? cause:new AggregateException(result.Exception,cause);
            if(known){result.IsWarning=true;result.WarningCount++;}
            else{result.IsError=true;result.ErrorCode="COSMIC_RECEIPT_MEMBERSHIP_RECONCILIATION_REQUIRED";result.Message="Reconcile the original migration ID; cleanup outcome is unknown.";}
        }
        try
        {
            if(migrationId==Guid.Empty || audit==null)throw new ArgumentException("A stable migration ID and explicit audit are required.");
            if(audit.StoreHash!=ReceiptMembershipStoreHash())return result=new(){IsError=true,ErrorCount=1,ErrorCode="COSMIC_RECEIPT_MEMBERSHIP_STORE_MISMATCH",Message="The audit belongs to another native store."};
            session=await _dbContext.MongoClient.StartSessionAsync(cancellationToken:token).ConfigureAwait(false);
            session.StartTransaction(new TransactionOptions(readConcern:ReadConcern.Snapshot,writeConcern:WriteConcern.WMajority.With(journal:true)));
            var jobs=_dbContext.MongoDB.GetCollection<BsonDocument>("CosmicReceiptMembershipMigration");
            var jobId=new BsonDocument("_id",migrationId.ToString("D"));
            var previous=await jobs.Find(session,jobId).SingleOrDefaultAsync(token).ConfigureAwait(false);
            if(previous!=null)
            {
                if(!previous.TryGetValue("Fingerprint",out var binding) || !binding.IsString)throw new ReceiptMembershipAcknowledgementException();
                if(binding.AsString!=audit.Fingerprint)
                    return result=new(){IsError=true,ErrorCount=1,ErrorCode="COSMIC_OPERATION_ID_REUSED",Message="Migration ID is bound to another audit."};
                if(!previous.TryGetValue("UpdatedCount",out var updated) || !updated.IsInt32 || updated.AsInt32<0 || updated.AsInt32>audit.Entries.Count
                    || previous.GetValue("OperatorId",BsonNull.Value)!=audit.OperatorId.ToString("D")
                    || previous.GetValue("AuditedCount",BsonNull.Value)!=new BsonInt32(audit.Entries.Count)
                    || previous.GetValue("AcknowledgementHash",BsonNull.Value)!=Acknowledgement(updated.AsInt32))throw new ReceiptMembershipAcknowledgementException();
                known=true;return result=new OASISResult<int>(updated.AsInt32){IsSaved=true,SavedCount=updated.AsInt32};
            }
            // Unique job insertion fences competing claims before any receipt update.
            await jobs.InsertOneAsync(session,new BsonDocument{{"_id",migrationId.ToString("D")},{"Fingerprint",audit.Fingerprint},{"OperatorId",audit.OperatorId.ToString("D")}},cancellationToken:token).ConfigureAwait(false);
            var receipts=_dbContext.MongoDB.GetCollection<BsonDocument>("CosmicWriteReceipt");var changed=0;
            foreach(var entry in audit.Entries)
            {
                var filter=new BsonDocument("_id",entry.OperationId.ToString("D"));
                var row=await receipts.Find(session,filter).SingleOrDefaultAsync(token).ConfigureAwait(false);
                if(row==null || ReceiptMembershipHash(row)!=entry.ReceiptHash)
                    return result=new(){IsError=true,ErrorCount=1,ErrorCode="COSMIC_RECEIPT_MEMBERSHIP_AUDIT_STALE",Message="A selected receipt changed after audit; no batch was committed."};
                var members=VerifiedReceiptMembership(row);
                if(!members.SequenceEqual(entry.HolonIds))throw new InvalidDataException("Audited membership differs from the canonical acknowledgement.");
                var membership=new BsonArray(members.Select(id=>id.ToString("D")));
                var update=new BsonDocument("$set",new BsonDocument("HolonIds",membership));
                var saved=await receipts.UpdateOneAsync(session,filter,update,cancellationToken:token).ConfigureAwait(false);
                if(!saved.IsAcknowledged || saved.MatchedCount!=1)throw new InvalidDataException("Membership update was not acknowledged.");
                changed=checked(changed+(int)saved.ModifiedCount);
            }
            var acknowledgement=await jobs.UpdateOneAsync(session,jobId,new BsonDocument("$set",new BsonDocument{{"UpdatedCount",changed},{"AuditedCount",audit.Entries.Count},{"AcknowledgementHash",Acknowledgement(changed)}}),cancellationToken:token).ConfigureAwait(false);
            if(!acknowledgement.IsAcknowledged || acknowledgement.MatchedCount!=1)throw new InvalidDataException("Migration acknowledgement was not persisted.");
            commitEntered=true;await session.CommitTransactionAsync(token).ConfigureAwait(false);known=true;
            result=new OASISResult<int>(changed){IsSaved=true,SavedCount=changed};return result;
        }
        catch(Exception cause)
        {
            var reconcile=commitEntered || cause is ReceiptMembershipAcknowledgementException;
            result=new(){IsError=true,ErrorCount=1,ErrorCode=reconcile ? "COSMIC_RECEIPT_MEMBERSHIP_RECONCILIATION_REQUIRED"
                :cause is OperationCanceledException ? "COSMIC_RECEIPT_MEMBERSHIP_CANCELLED"
                :MongoTransactionFailure.IsWriteConflict(cause) || cause is MongoWriteException write && write.WriteError?.Category==ServerErrorCategory.DuplicateKey
                    ? "COSMIC_CONCURRENT_WRITE":"COSMIC_RECEIPT_MEMBERSHIP_FAILED",
                Message=reconcile ? "Reconcile the original migration ID before another submission.":"Receipt membership batch did not commit.",Exception=cause};return result;
        }
        finally
        {
            if(session!=null)
            {
                try{if(session.IsInTransaction)await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);}
                catch(Exception cleanup)
                {
                    CleanupFailure(cleanup);
                }
                try{session.Dispose();}catch(Exception cleanup){CleanupFailure(cleanup);}
            }
        }
    }
}
