using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    public OASISResult<IEnumerable<IHolon>> ExecuteReplicatingHolonWrite(ConditionalHolonWriteRequest request,
        HolonReplicationEnrollment enrollment, CancellationToken cancellationToken)
    {
        if(enrollment==null)return ConditionalError("COSMIC_REPLICATION_ENROLLMENT_REQUIRED", "Atomic replication requires explicit source and target enrollment.");
        return ExecuteAtomicHolonWrite(request,null,cancellationToken,enrollment);
    }

    private static string ReplicationFingerprint(string mutationFingerprint, HolonReplicationEnrollment enrollment)
    {
        var intent=new JObject { ["format"]=1, ["kind"]="atomic-holon-replication",
            ["mutation"]=mutationFingerprint, ["source"]=enrollment.SourceId, ["targets"]=new JArray(enrollment.TargetIds) };
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(intent.ToString(Formatting.None))));
    }

    private static void WriteReplicationObligations(SqliteConnection connection,SqliteTransaction transaction,
        ConditionalHolonWriteRequest request,HolonReplicationEnrollment enrollment,IEnumerable<IHolon> acknowledged,DateTime now)
    {
        var payload=CommittedHolonReplicaPayload.Capture(new CommittedHolonReplicaRequest(request.Write.OperationId,
            enrollment.SourceId,ProviderType.SQLLiteDBOASIS,request.Write.ActorId,request.Operation,acknowledged,
            request.SaveChildren,request.Recursive,request.MaxChildDepth));
        var digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        foreach(var target in enrollment.TargetIds)
        {
            using var command=connection.CreateCommand();command.Transaction=transaction;
            command.CommandText="INSERT INTO CosmicHolonOutbox(OperationId,TargetId,SourceId,SourceAuthority,ActorId,PayloadJson,PayloadDigest,CreatedAtUtc) VALUES ($operation,$target,$source,$authority,$actor,$payload,$digest,$at)";
            command.Parameters.AddWithValue("$operation",request.Write.OperationId.ToString("D"));
            command.Parameters.AddWithValue("$target",target);command.Parameters.AddWithValue("$source",enrollment.SourceId);
            command.Parameters.AddWithValue("$authority",ProviderType.SQLLiteDBOASIS.ToString());
            command.Parameters.AddWithValue("$actor",request.Write.ActorId.ToString("D"));
            command.Parameters.AddWithValue("$payload",payload);command.Parameters.AddWithValue("$digest",digest);
            command.Parameters.AddWithValue("$at",now.ToString("O"));command.ExecuteNonQuery();
        }
    }
}
