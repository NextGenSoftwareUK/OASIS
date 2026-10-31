using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public sealed partial class SqliteHolonReplicationOutbox
{
    /// <summary>Trusted host capability, not an HTTP grant. Reauthorize the intent
    /// and obtain the exact immutable receipt directly from its configured target.
    /// Source state and acknowledgement commit together under an IMMEDIATE writer fence.</summary>
    public Task<OASISResult<bool>> CompleteInitializedBaselineAsync(HolonReplicaInitializationIntent intent,
        HolonReplicaInitializationOutcome outcome,CancellationToken cancellationToken=default)
        =>Execute(()=>{
            ArgumentNullException.ThrowIfNull(intent);ArgumentNullException.ThrowIfNull(outcome);
            if(outcome.ReceiptState!=ConditionalHolonReceiptState.Committed || outcome.OperationId!=intent.InitializationOperationId
                || outcome.BaselineFingerprint!=intent.BaselineFingerprint)
                throw new ArgumentException("An exact committed target initialization receipt is required.");
            using var connection=Open();using var transaction=connection.BeginTransaction(deferred:false);
            HolonDeliveryInspection observed;string payload,digest;
            using(var query=connection.CreateCommand())
            {
                query.Transaction=transaction;query.CommandText="SELECT "+InspectionColumns+" FROM CosmicHolonOutbox WHERE Sequence=$sequence";
                query.Parameters.AddWithValue("$sequence",intent.Sequence);
                using var reader=query.ExecuteReader();var inspected=ReadInspection(reader,true);
                observed=inspected.Result ?? throw new InvalidDataException("Reviewed source obligation no longer exists.");
                payload=reader.GetString(9);digest=reader.GetString(10);
            }
            var original=CommittedHolonReplicaPayload.Read(payload);
            var baseline=new CommittedHolonReplicaRequest(intent.InitializationOperationId,original.SourceId,original.SourceAuthority,
                original.ActorId,original.Operation,original.Roots,original.SaveChildren,original.Recursive,original.MaxChildDepth);
            var plan=CommittedHolonReplicaPlan.CreateInitialBaseline(baseline,outcome.TargetAuthority);
            if(observed.OperationId!=intent.SourceOperationId || observed.AttemptCount!=intent.AttemptCount
                || digest!=intent.SourcePayloadDigest || plan.Fingerprint!=intent.BaselineFingerprint)
                throw new InvalidDataException("Source obligation differs from reviewed baseline intent.");
            var evidence=new JObject { ["format"]=1,["intentFingerprint"]=intent.Fingerprint,
                ["operatorId"]=intent.OperatorId.ToString("D"),["initializationOperationId"]=outcome.OperationId.ToString("D"),
                ["targetAuthority"]=outcome.TargetAuthority.ToString(),["baselineFingerprint"]=outcome.BaselineFingerprint,
                ["receiptDigest"]=outcome.ReceiptDigest }.ToString(Formatting.None);
            var evidenceDigest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(evidence)));
            using(var recorded=connection.CreateCommand())
            {
                recorded.Transaction=transaction;
                recorded.CommandText="SELECT EvidenceJson,EvidenceDigest FROM CosmicHolonOutboxInitializationCompletion WHERE Sequence=$sequence";
                recorded.Parameters.AddWithValue("$sequence",intent.Sequence);
                using var reader=recorded.ExecuteReader();
                var exists=reader.Read();
                if(observed.State==HolonDeliveryState.Completed)
                {
                    if(!exists || reader.GetString(0)!=evidence || reader.GetString(1)!=evidenceDigest)
                        throw new InvalidDataException("Completion does not acknowledge this exact initialization.");
                    cancellationToken.ThrowIfCancellationRequested();return new OASISResult<bool>(true);
                }
                if(exists)throw new InvalidDataException("Unfinished obligation has an initialization acknowledgement.");
            }
            if(observed.State!=HolonDeliveryState.ReconciliationRequired || observed.ErrorCode!="COSMIC_REPLICA_LINEAGE_CONFLICT"
                || observed.RecoveryEvidence==null || observed.RecoveryEvidence.RequiresNativeReconciliation)
                throw new InvalidDataException("Only a reviewed lineage conflict without unresolved execution can complete.");
            using(var head=connection.CreateCommand())
            {
                head.Transaction=transaction;
                head.CommandText="SELECT Sequence FROM CosmicHolonOutbox WHERE TargetId=$target AND DeliveryState<>'Completed' ORDER BY Sequence LIMIT 1";
                head.Parameters.AddWithValue("$target",observed.TargetId);
                if(Convert.ToInt64(head.ExecuteScalar())!=intent.Sequence)
                    throw new InvalidDataException("Reviewed obligation is not the current held target head.");
            }
            using(var complete=connection.CreateCommand())
            {
                complete.Transaction=transaction;
                complete.CommandText="UPDATE CosmicHolonOutbox SET DeliveryState='Completed',CompletedAtUtcTicks=$at WHERE Sequence=$sequence AND DeliveryState='ReconciliationRequired' AND AttemptCount=$attempt AND OperationId=$operation AND PayloadDigest=$digest";
                complete.Parameters.AddWithValue("$at",Now().Ticks);complete.Parameters.AddWithValue("$sequence",intent.Sequence);
                complete.Parameters.AddWithValue("$attempt",intent.AttemptCount);complete.Parameters.AddWithValue("$operation",intent.SourceOperationId.ToString("D"));
                complete.Parameters.AddWithValue("$digest",intent.SourcePayloadDigest);
                if(complete.ExecuteNonQuery()!=1)throw new InvalidDataException("Reviewed source completion fence changed.");
            }
            using(var acknowledge=connection.CreateCommand())
            {
                acknowledge.Transaction=transaction;
                acknowledge.CommandText="INSERT INTO CosmicHolonOutboxInitializationCompletion(Sequence,EvidenceJson,EvidenceDigest) VALUES($sequence,$json,$digest)";
                acknowledge.Parameters.AddWithValue("$sequence",intent.Sequence);acknowledge.Parameters.AddWithValue("$json",evidence);
                acknowledge.Parameters.AddWithValue("$digest",evidenceDigest);acknowledge.ExecuteNonQuery();
            }
            cancellationToken.ThrowIfCancellationRequested();transaction.Commit();return new OASISResult<bool>(true);
        },cancellationToken);
}
