using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using INode=Neo4j.Driver.INode;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public sealed partial class NeoHolonReplicationOutbox
{
    /// <summary>Trusted host completion, not an HTTP grant. The application must
    /// reauthorize its reviewed intent and read the original receipt from the bound
    /// target. A native node lock fences source state and acknowledgement together.</summary>
    public Task<OASISResult<bool>> CompleteInitializedBaselineAsync(HolonReplicaInitializationIntent intent,
        HolonReplicaInitializationOutcome outcome,CancellationToken cancellationToken=default)
        =>Execute(async tx=>{
            var rows=await(await tx.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) "
                +"SET o.__deliveryLock=true REMOVE o.__deliveryLock RETURN o,"+Now+" AS now",
                new{sequence=intent.Sequence}).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            if(rows.Count!=1)throw new InvalidDataException("Reviewed source obligation is missing or ambiguous.");
            var row=rows[0]["o"].As<INode>();var observed=ValidateRow(row);ValidatePayload(row,observed.Operation);
            var original=CommittedHolonReplicaPayload.Read(Text(row,"PayloadJson"));
            var baseline=new CommittedHolonReplicaRequest(intent.InitializationOperationId,original.SourceId,original.SourceAuthority,
                original.ActorId,original.Operation,original.Roots,original.SaveChildren,original.Recursive,original.MaxChildDepth);
            var plan=CommittedHolonReplicaPlan.CreateInitialBaseline(baseline,outcome.TargetAuthority);
            if(observed.Operation!=intent.SourceOperationId || observed.Attempts!=intent.AttemptCount
                || Text(row,"PayloadDigest")!=intent.SourcePayloadDigest || plan.Fingerprint!=intent.BaselineFingerprint)
                throw new InvalidDataException("Source obligation differs from reviewed baseline intent.");
            var evidence=new JObject { ["format"]=1,["intentFingerprint"]=intent.Fingerprint,
                ["operatorId"]=intent.OperatorId.ToString("D"),["initializationOperationId"]=outcome.OperationId.ToString("D"),
                ["targetAuthority"]=outcome.TargetAuthority.ToString(),["baselineFingerprint"]=outcome.BaselineFingerprint,
                ["receiptDigest"]=outcome.ReceiptDigest }.ToString(Formatting.None);
            var evidenceDigest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(evidence)));
            if(observed.State==HolonDeliveryState.Completed)
            {
                if(Text(row,"InitializationCompletionJson")!=evidence || Text(row,"InitializationCompletionDigest")!=evidenceDigest)
                    throw new InvalidDataException("Completion does not acknowledge this exact initialization.");
                return (new OASISResult<bool>(true),false);
            }
            if(Text(row,"InitializationCompletionJson")!=null || Text(row,"InitializationCompletionDigest")!=null)
                throw new InvalidDataException("Unfinished obligation has an initialization acknowledgement.");
            if(observed.State!=HolonDeliveryState.ReconciliationRequired || Text(row,"LastErrorCode")!="COSMIC_REPLICA_LINEAGE_CONFLICT"
                || observed.Evidence==null || observed.Evidence.RequiresNativeReconciliation)
                throw new InvalidDataException("Only a reviewed lineage conflict without unresolved execution can complete.");
            var heads=await(await tx.RunAsync("MATCH (o:CosmicHolonOutbox {TargetId:$target}) WHERE o.DeliveryState<>'Completed' "
                +"RETURN o.Sequence AS sequence ORDER BY sequence LIMIT 1",new{target=Text(row,"TargetId")})
                .ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            if(heads.Count!=1 || heads[0]["sequence"].As<long>()!=intent.Sequence)
                throw new InvalidDataException("Reviewed obligation is not the current held target head.");
            var changed=await(await tx.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) "
                +"WHERE o.DeliveryState='ReconciliationRequired' AND o.AttemptCount=$attempt "
                +"AND o.OperationId=$operation AND o.PayloadDigest=$payload "
                +"SET o.DeliveryState='Completed',o.CompletedAtUtcTicks=$at,"
                +"o.InitializationCompletionJson=$json,o.InitializationCompletionDigest=$digest RETURN count(o) AS count",
                new{sequence=intent.Sequence,attempt=(long)intent.AttemptCount,operation=intent.SourceOperationId.ToString("D"),
                    payload=intent.SourcePayloadDigest,at=rows[0]["now"].As<long>(),json=evidence,digest=evidenceDigest})
                .ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
            if(changed["count"].As<long>()!=1)throw new InvalidDataException("Reviewed source completion fence changed.");
            return (new OASISResult<bool>(true),true);
        },()=>{
            ArgumentNullException.ThrowIfNull(intent);ArgumentNullException.ThrowIfNull(outcome);
            if(outcome.ReceiptState!=ConditionalHolonReceiptState.Committed || outcome.OperationId!=intent.InitializationOperationId
                || outcome.BaselineFingerprint!=intent.BaselineFingerprint)
                throw new ArgumentException("An exact committed target initialization receipt is required.");
        },cancellationToken);
}
