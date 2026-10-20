using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private static void AddRevisionCandidate(Dictionary<Guid,IHolon> records,IHolon record)
    {
        if(records.TryAdd(record.Id,record))return;
        var previous=records[record.Id];
        if(Core.Holons.CosmicConcurrency.GetProviderRevisionId(previous,Core.Enums.ProviderType.SQLLiteDBOASIS)
            !=Core.Holons.CosmicConcurrency.GetProviderRevisionId(record,Core.Enums.ProviderType.SQLLiteDBOASIS))
            throw new InvalidDataException("The logical version identifies multiple native revisions.");
        // A concurrent writer can archive the current observation before our archive query.
        // Only identical observations of that same physical revision may collapse.
        if(!NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordPayloadEquality.Equivalent(
            StoredRecordJson.CaptureRuntimeToken(previous), StoredRecordJson.CaptureRuntimeToken(record)))
            throw new InvalidDataException("The same native revision has different canonical content.");
    }

    private IHolon DecodeArchivedRevision(Guid id,int version,string json,string digest,string logical,string native)
    {
        if(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))!=digest)
            throw new InvalidDataException("Archived holon checksum differs from stored content.");
        var record=StoredRecordJson.DeserializeStoredHolon(json);
        if(record==null || record.Id!=id || record.Version!=version || record.VersionId.ToString("D")!=logical
            || Core.Holons.CosmicConcurrency.GetProviderRevisionId(record,Core.Enums.ProviderType.SQLLiteDBOASIS)?.ToString("D")!=native)
            throw new InvalidDataException("Archived holon identity or revision differs from its index.");
        if(record is Core.Holons.Holon holon && holon.ChildRoleManifestJson!=null
            && HolonChildRoleManifestJson.Deserialize(holon.ChildRoleManifestJson).ParentId!=id)
            throw new InvalidDataException("Archived relationship parent differs from record identity.");
        record.IsNewHolon=false;return record;
    }

    private List<IHolon> ReadHolonRevisions(int version,Core.Enums.HolonType type,Guid? parentId=null)
    {
        var records=_dbContext.Holons.AsNoTracking().Where(row=>row.Version==version && (type==Core.Enums.HolonType.All || row.HolonType==type))
            .Where(row=>!parentId.HasValue || row.ParentHolonId==parentId.Value.ToString("D"))
            .ToList().Select(GetHolonFromEntity).ToDictionary(record=>record.Id);
        var connection=_dbContext.Database.GetDbConnection();var opened=connection.State!=System.Data.ConnectionState.Open;
        if(opened)_dbContext.Database.OpenConnection();
        try
        {
            using var command=connection.CreateCommand();command.Transaction=_dbContext.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText="SELECT HolonId,RecordJson,RecordDigest,VersionId,ProviderRevisionId FROM CosmicHolonRevision WHERE Version=$version AND ($all=1 OR json_extract(RecordJson,'$.HolonType')=$type)";
            if(parentId.HasValue)
            {
                command.CommandText+=" AND json_extract(RecordJson,'$.ParentHolonId')=$parent";
                var parent=command.CreateParameter();parent.ParameterName="$parent";parent.Value=parentId.Value.ToString("D");command.Parameters.Add(parent);
            }
            foreach(var pair in new Dictionary<string,object>{{"$version",version},{"$all",type==Core.Enums.HolonType.All ? 1:0},{"$type",(int)type}})
            {var parameter=command.CreateParameter();parameter.ParameterName=pair.Key;parameter.Value=pair.Value;command.Parameters.Add(parameter);}
            using var rows=command.ExecuteReader();
            while(rows.Read())
            {
                var id=Guid.Parse(rows.GetString(0));
                var record=DecodeArchivedRevision(id,version,rows.GetString(1),rows.GetString(2),rows.GetString(3),rows.GetString(4));
                if(parentId.HasValue && record.ParentHolonId!=parentId.Value)
                    throw new InvalidDataException("Archived parent differs from the selected parent identity.");
                AddRevisionCandidate(records,record);
            }
        }
        finally{if(opened)_dbContext.Database.CloseConnection();}
        return records.Values.OrderBy(record=>record.Id).ToList();
    }

    private List<IHolon> ReadHistoricalChildren(string key,int version,Core.Enums.HolonType type)
    {
        // Parent identity is independent of the requested child revision, as with search ancestor scope.
        var parent=_dbContext.Holons.AsNoTracking().SingleOrDefault(row=>row.LocalProviderKey==key);
        return parent==null ? new List<IHolon>():ReadHolonRevisions(version,type,Guid.Parse(parent.Id));
    }

    private IHolon ReadHolonRevision(string key,int version)
    {
        var ids=_dbContext.Holons.AsNoTracking().Where(row=>row.LocalProviderKey==key && row.Version==version)
            .Select(row=>row.Id).ToHashSet(StringComparer.Ordinal);
        var connection=_dbContext.Database.GetDbConnection();var opened=connection.State!=System.Data.ConnectionState.Open;
        if(opened)_dbContext.Database.OpenConnection();
        try
        {
            using var command=connection.CreateCommand();command.Transaction=_dbContext.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText="SELECT DISTINCT HolonId FROM CosmicHolonRevision WHERE json_extract(RecordJson,'$.ProviderUniqueStorageKey.SQLLiteDBOASIS')=$key AND Version=$version";
            var selected=command.CreateParameter();selected.ParameterName="$key";selected.Value=key;command.Parameters.Add(selected);
            var number=command.CreateParameter();number.ParameterName="$version";number.Value=version;command.Parameters.Add(number);
            using var rows=command.ExecuteReader();while(rows.Read())ids.Add(rows.GetString(0));
        }
        finally{if(opened)_dbContext.Database.CloseConnection();}
        var matches=ids.Select(id=>ReadHolonRevision(Guid.Parse(id),version)).Where(record=>record!=null
            && record.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SQLLiteDBOASIS,out var observed) && observed==key).ToArray();
        if(matches.Length>1)throw new InvalidDataException("The historical provider key identifies multiple public records.");
        return matches.SingleOrDefault();
    }

    private IHolon ReadHolonRevision(Guid id,int version)
    {
        var matches=new Dictionary<Guid,IHolon>();
        var current=_dbContext.Holons.AsNoTracking().SingleOrDefault(row=>row.Id==id.ToString() && row.Version==version);
        if(current!=null)AddRevisionCandidate(matches,GetHolonFromEntity(current));
        var connection=_dbContext.Database.GetDbConnection();
        var opened=connection.State!=System.Data.ConnectionState.Open;
        if(opened)_dbContext.Database.OpenConnection();
        try
        {
            using var command=connection.CreateCommand();
            command.Transaction=_dbContext.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText="SELECT RecordJson,RecordDigest,VersionId,ProviderRevisionId FROM CosmicHolonRevision WHERE HolonId=$id AND Version=$version";
            var identity=command.CreateParameter();identity.ParameterName="$id";identity.Value=id.ToString("D");command.Parameters.Add(identity);
            var number=command.CreateParameter();number.ParameterName="$version";number.Value=version;command.Parameters.Add(number);
            using var rows=command.ExecuteReader();
            while(rows.Read())
            {
                AddRevisionCandidate(matches,DecodeArchivedRevision(id,version,rows.GetString(0),rows.GetString(1),rows.GetString(2),rows.GetString(3)));
            }
        }
        finally { if(opened)_dbContext.Database.CloseConnection(); }
        return matches.Values.SingleOrDefault();
    }

    private void ArchiveHolonRevision(SqliteConnection connection,SqliteTransaction transaction,HolonModel previous)
    {
        var record=GetHolonFromEntity(previous);
        var json=StoredRecordJson.Serialize<IHolon>(record);
        var digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        using(var existing=connection.CreateCommand())
        {
            existing.Transaction=transaction;
            existing.CommandText="SELECT RecordJson, RecordDigest FROM CosmicHolonRevision WHERE HolonId=$id AND ProviderRevisionId=$revision";
            existing.Parameters.AddWithValue("$id",previous.Id);
            existing.Parameters.AddWithValue("$revision",previous.ProviderRevisionId.ToString("D"));
            using var row=existing.ExecuteReader();
            if(row.Read())
            {
                if(row.GetString(0)!=json || row.GetString(1)!=digest)
                    throw new InvalidDataException("A native revision is already archived with different canonical content.");
                return;
            }
        }
        using var insert=connection.CreateCommand();
        insert.Transaction=transaction;
        insert.CommandText="INSERT INTO CosmicHolonRevision(HolonId,ProviderRevisionId,Version,VersionId,RecordJson,RecordDigest,ArchivedAtUtc) VALUES($id,$revision,$version,$logical,$json,$digest,$at)";
        insert.Parameters.AddWithValue("$id",previous.Id);
        insert.Parameters.AddWithValue("$revision",previous.ProviderRevisionId.ToString("D"));
        insert.Parameters.AddWithValue("$version",previous.Version);
        insert.Parameters.AddWithValue("$logical",previous.VersionId.ToString("D"));
        insert.Parameters.AddWithValue("$json",json);
        insert.Parameters.AddWithValue("$digest",digest);
        insert.Parameters.AddWithValue("$at",DateTime.UtcNow.ToString("O"));
        insert.ExecuteNonQuery();
    }
}
