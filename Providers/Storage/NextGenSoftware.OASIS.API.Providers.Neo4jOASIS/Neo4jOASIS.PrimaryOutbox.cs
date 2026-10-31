using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static async Task WritePrimaryHolonObligationsAsync(IAsyncTransaction transaction,ConditionalHolonPlan plan,
        Guid actor,List<IHolon> acknowledgedRoots)
    {
        // Freeze only the verified native acknowledgement, inside its existing graph transaction.
        var payload=CommittedHolonReplicaPayload.Capture(new CommittedHolonReplicaRequest(plan.OperationId,
            plan.ReplicationEnrollment.SourceId,plan.Authority,actor,plan.Operation,acknowledgedRoots,
            plan.SaveChildren,plan.Recursive,plan.MaxChildDepth));
        var digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        var count=plan.ReplicationEnrollment.TargetIds.Count;
        var row=await(await transaction.RunAsync(
            "MERGE (s:CosmicHolonOutboxSequence {Name:'primary-holon-outbox-v1'}) ON CREATE SET s.Value=0 "
            +"SET s.__cosmicLock=true REMOVE s.__cosmicLock "
            +"WITH s OPTIONAL MATCH (o:CosmicHolonOutbox) RETURN s.Value AS value,max(o.Sequence) AS highest")
            .ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
        if(row["value"] is not long previous || previous<0 ||
            (row["highest"]!=null && (row["highest"] is not long highest || highest>previous)))
            throw new InvalidDataException("The primary outbox sequence is invalid or behind retained obligations.");
        var last=checked(previous+count);
        var advanced=await(await transaction.RunAsync(
            "MATCH (s:CosmicHolonOutboxSequence {Name:'primary-holon-outbox-v1'}) WHERE s.Value=$previous "
            +"SET s.Value=$last RETURN count(s) AS count",new{previous,last}).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
        if(advanced["count"].As<long>()!=1)throw new InvalidDataException("The primary outbox counter acknowledgement is incomplete.");
        var sequence=previous;
        foreach(var target in plan.ReplicationEnrollment.TargetIds)
        {
            var written=await(await transaction.RunAsync(
                "CREATE (o:CosmicHolonOutbox) SET o=$record RETURN count(o) AS count",
                new{record=new Dictionary<string,object>{
                    ["Format"]=1L,["Sequence"]=checked(++sequence),["OperationId"]=plan.OperationId.ToString("D"),
                    ["TargetId"]=target,["SourceId"]=plan.ReplicationEnrollment.SourceId,["SourceAuthority"]=plan.Authority.ToString(),
                    ["ActorId"]=actor.ToString("D"),["PayloadJson"]=payload,["PayloadDigest"]=digest,
                    ["CreatedAtUtcTicks"]=DateTime.UtcNow.Ticks,["DeliveryState"]="Pending",["AttemptCount"]=0L,
                    ["LeaseToken"]="",["LeaseWorkerId"]="",["LeaseUntilUtcTicks"]=0L,["NextAttemptUtcTicks"]=0L,["CompletedAtUtcTicks"]=0L
                }}).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
            if(written["count"].As<long>()!=1)throw new InvalidDataException("The primary outbox obligation acknowledgement is incomplete.");
        }
    }
}
