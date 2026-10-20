using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed class SqliteReplicaConflictException(IEnumerable<string> ids)
        :InvalidOperationException("Replica source lineage, local revision or enrollment differs for: "+string.Join(",",ids));

    public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request,CancellationToken token)
        =>request==null ? ConditionalError("COSMIC_REPLICA_WRITE_INVALID","A committed replica request is required.")
            :ExecuteAtomicHolonWrite(null,request,token);

    private void PrepareReplicaModels(SqliteConnection connection,SqliteTransaction native,PreparedGraph graph,
        Dictionary<string,HolonModel> persisted,CommittedHolonReplicaPlan plan)
    {
        var prior=new Dictionary<string,IHolon>();var conflicts=new List<string>();
        foreach(var model in graph.Models)
        {
            persisted.TryGetValue(model.Id,out var current);
            using var command=connection.CreateCommand();command.Transaction=native;
            command.CommandText="SELECT SourceId,SourceAuthority,SnapshotJson,SnapshotDigest,ProviderRevisionId,HardDeleted FROM CosmicReplicaHead WHERE HolonId=$id";
            command.Parameters.AddWithValue("$id",model.Id);
            using var reader=command.ExecuteReader();
            if(!reader.Read())
            {
                if(current!=null || model.Version!=1 || model.PreviousVersionId!=Guid.Empty)conflicts.Add(model.Id);
                prior.Add(model.Id,null);continue;
            }
            var json=reader.GetString(2);
            if(reader.GetString(3)!=ReplicaSnapshotDigest(json))throw new InvalidDataException("Committed replica head checksum is invalid.");
            var stored=StoredRecordJson.DeserializeStored<Holon>(json);
            var revisionText=reader.GetString(4);var hardDeleted=reader.GetInt64(5);
            if(stored==null || stored.Id.ToString()!=model.Id || stored.Version<1 || stored.VersionId==Guid.Empty
                || !Guid.TryParse(revisionText,out var revision) || revision==Guid.Empty
                || CosmicConcurrency.GetProviderRevisionId(stored,ProviderType.SQLLiteDBOASIS)!=revision
                || hardDeleted is not 0 and not 1)
                throw new InvalidDataException("Committed replica head snapshot/revision is inconsistent.");
            prior.Add(model.Id,current==null ? stored:GetHolonFromEntity(current));
            if(reader.GetString(0)!=plan.SourceId || reader.GetString(1)!=plan.SourceAuthority.ToString()
                || model.PreviousVersionId!=stored.VersionId || model.Version-1!=stored.Version
                || (hardDeleted==1 ? current!=null : current==null || current.VersionId!=stored.VersionId
                    || current.Version!=stored.Version || current.ProviderRevisionId!=revision))conflicts.Add(model.Id);
        }
        if(conflicts.Count!=0)throw new SqliteReplicaConflictException(conflicts);
        for(var index=0;index<graph.Models.Count;index++)
        {
            var model=graph.Models[index];var previous=prior[model.Id];
            var record=StoredRecordJson.Project<Holon>(plan.Nodes.Single(node=>node.Snapshot.Id.ToString()==model.Id).Snapshot);
            record.ProviderMetaData??=new Dictionary<ProviderType,Dictionary<string,string>>();
            record.ProviderMetaData[ProviderType.SQLLiteDBOASIS]=previous?.ProviderMetaData!=null
                && previous.ProviderMetaData.TryGetValue(ProviderType.SQLLiteDBOASIS,out var local) && local!=null
                ?new Dictionary<string,string>(local):new Dictionary<string,string>();
            record.ProviderUniqueStorageKey??=new Dictionary<ProviderType,string>();
            record.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS]=previous?.ProviderUniqueStorageKey!=null
                && previous.ProviderUniqueStorageKey.TryGetValue(ProviderType.SQLLiteDBOASIS,out var key) ? key:model.Id;
            var prepared=CreateHolonModel(record,record.Id);prepared.ProviderRevisionId=Guid.NewGuid();
            graph.Models[index]=prepared;
        }
    }

    private static void WriteReplicaHeads(SqliteConnection connection,SqliteTransaction native,IEnumerable<IHolon> roots,CommittedHolonReplicaPlan plan)
    {
        foreach(var node in HolonGraphWritePlan.Create(roots,true,true,0,0).Nodes)
        {
            var record=node.Snapshot;var json=StoredRecordJson.Serialize(record);
            using var command=connection.CreateCommand();command.Transaction=native;
            command.CommandText="INSERT INTO CosmicReplicaHead(HolonId,SourceId,SourceAuthority,SnapshotJson,SnapshotDigest,ProviderRevisionId,HardDeleted) "
                +"VALUES($id,$source,$authority,$json,$digest,$revision,$deleted) ON CONFLICT(HolonId) DO UPDATE SET "
                +"SourceId=excluded.SourceId,SourceAuthority=excluded.SourceAuthority,SnapshotJson=excluded.SnapshotJson,SnapshotDigest=excluded.SnapshotDigest,"
                +"ProviderRevisionId=excluded.ProviderRevisionId,HardDeleted=excluded.HardDeleted";
            command.Parameters.AddWithValue("$id",record.Id.ToString());command.Parameters.AddWithValue("$source",plan.SourceId);
            command.Parameters.AddWithValue("$authority",plan.SourceAuthority.ToString());command.Parameters.AddWithValue("$json",json);
            command.Parameters.AddWithValue("$digest",ReplicaSnapshotDigest(json));
            command.Parameters.AddWithValue("$revision",CosmicConcurrency.GetProviderRevisionId(record,ProviderType.SQLLiteDBOASIS)!.Value.ToString("D"));
            command.Parameters.AddWithValue("$deleted",plan.Operation==ConditionalHolonOperation.HardDelete ? 1:0);
            if(command.ExecuteNonQuery()!=1)throw new InvalidDataException("Committed replica head acknowledgement is incomplete.");
        }
    }
    private static string ReplicaSnapshotDigest(string json)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
}
