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
using INode=Neo4j.Driver.INode;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public sealed partial class NeoHolonReplicationOutbox
{
    /// <summary>Trusted host retirement of one reviewed held lineage conflict whose
    /// target already holds a newer owner-approved head. Nothing is delivered: the
    /// obligation is completed with an exact retirement acknowledgement under the same
    /// native node lock and attempt/operation/payload fence used by baseline completion.
    /// The caller must have verified the superseding target head first.</summary>
    public Task<OASISResult<bool>> RetireSupersededHeldHeadAsync(Guid operatorId,long sequence,Guid sourceOperationId,
        int attemptCount,string sourcePayloadDigest,string supersedingEvidenceJson,CancellationToken cancellationToken=default)
        =>Execute(async tx=>{
            var rows=await(await tx.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) "
                +"SET o.__deliveryLock=true REMOVE o.__deliveryLock RETURN o,"+Now+" AS now",
                new{sequence}).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            if(rows.Count!=1)throw new InvalidDataException("Reviewed source obligation is missing or ambiguous.");
            var row=rows[0]["o"].As<INode>();var observed=ValidateRow(row);ValidatePayload(row,observed.Operation);
            if(observed.Operation!=sourceOperationId || observed.Attempts!=attemptCount || Text(row,"PayloadDigest")!=sourcePayloadDigest)
                throw new InvalidDataException("Source obligation differs from the reviewed retirement.");
            var evidence=new JObject{["format"]=1,["kind"]="SupersededByTargetHead",["operatorId"]=operatorId.ToString("D"),
                ["supersedingHead"]=JObject.Parse(supersedingEvidenceJson)}.ToString(Formatting.None);
            var digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(evidence)));
            if(observed.State==HolonDeliveryState.Completed)
            {
                if(Text(row,"RetirementJson")!=evidence || Text(row,"RetirementDigest")!=digest)
                    throw new InvalidDataException("Completion does not acknowledge this exact retirement.");
                return (new OASISResult<bool>(true),false);
            }
            if(Text(row,"RetirementJson")!=null || Text(row,"InitializationCompletionJson")!=null)
                throw new InvalidDataException("Unfinished obligation already carries an acknowledgement.");
            if(observed.State!=HolonDeliveryState.ReconciliationRequired || Text(row,"LastErrorCode")!="COSMIC_REPLICA_LINEAGE_CONFLICT"
                || observed.Evidence==null || observed.Evidence.RequiresNativeReconciliation)
                throw new InvalidDataException("Only a reviewed lineage conflict without unresolved execution can be retired.");
            var heads=await(await tx.RunAsync("MATCH (o:CosmicHolonOutbox {TargetId:$target}) WHERE o.DeliveryState<>'Completed' "
                +"RETURN o.Sequence AS sequence ORDER BY sequence LIMIT 1",new{target=Text(row,"TargetId")})
                .ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            if(heads.Count!=1 || heads[0]["sequence"].As<long>()!=sequence)
                throw new InvalidDataException("Reviewed obligation is not the current held target head.");
            var changed=await(await tx.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) "
                +"WHERE o.DeliveryState='ReconciliationRequired' AND o.AttemptCount=$attempt "
                +"AND o.OperationId=$operation AND o.PayloadDigest=$payload "
                +"SET o.DeliveryState='Completed',o.CompletedAtUtcTicks=$at,o.RetirementJson=$json,o.RetirementDigest=$digest "
                +"RETURN count(o) AS count",
                new{sequence,attempt=(long)attemptCount,operation=sourceOperationId.ToString("D"),payload=sourcePayloadDigest,
                    at=rows[0]["now"].As<long>(),json=evidence,digest})
                .ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
            if(changed["count"].As<long>()!=1)throw new InvalidDataException("Reviewed source retirement fence changed.");
            return (new OASISResult<bool>(true),true);
        },()=>{
            if(operatorId==Guid.Empty || sequence<=0 || sourceOperationId==Guid.Empty || attemptCount<=0
                || sourcePayloadDigest?.Length!=64 || string.IsNullOrWhiteSpace(supersedingEvidenceJson) || supersedingEvidenceJson.Length>2048)
                throw new ArgumentException("A complete reviewed retirement is required.");
        },cancellationToken);
}
