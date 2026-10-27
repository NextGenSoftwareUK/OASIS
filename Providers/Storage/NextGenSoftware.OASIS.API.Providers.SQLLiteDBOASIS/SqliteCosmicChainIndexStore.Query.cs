using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public sealed record CosmicChainIndexRows(CosmicChainQueryCheckpoint Checkpoint,CosmicChainQueryFinalizedHead Head,
    IReadOnlyList<IHolon> Rows,IReadOnlyList<CosmicChainQueryRecordReceipt> Receipts);

public sealed partial class SqliteCosmicChainIndexStore
{
    private sealed record Projection(string Id,int Version,string VersionId,bool IsDeleted,string CreatedByAvatarId,bool IsPublic,string Name,string Receipt);
    private static readonly CosmicQueryComparisonOperator[] Equality={CosmicQueryComparisonOperator.Equal,CosmicQueryComparisonOperator.NotEqual};
    private static CosmicQueryFieldCapability Field(string name,Type type,CosmicQueryComparisonOperator[] comparisons,bool order=false)
        =>new(name,type,comparisons,Array.Empty<CosmicQueryStringOperator>(),order);
    public static CosmicQueryCapabilities CanonicalRowCapabilities {get;}=new(
        CosmicQueryFeatures.And|CosmicQueryFeatures.Or|CosmicQueryFeatures.Not|CosmicQueryFeatures.BooleanConstant|
        CosmicQueryFeatures.Ordering|CosmicQueryFeatures.Paging|CosmicQueryFeatures.Membership,
        new[]{Field("Id",typeof(Guid),Equality,true),Field("Name",typeof(string),Equality,true),
            Field("Version",typeof(int),(CosmicQueryComparisonOperator[])Enum.GetValues(typeof(CosmicQueryComparisonOperator)),true),
            Field("IsPublic",typeof(bool),Equality),Field("CreatedByAvatarId",typeof(Guid),Equality)},Array.Empty<CosmicQueryMetadataCapability>());

    private void InitializeQueryProjection(SqliteConnection connection)
    {
        using var command=connection.CreateCommand();command.CommandText="""
CREATE TABLE IF NOT EXISTS CosmicChainIndexRows(Id TEXT PRIMARY KEY,Version INTEGER NOT NULL,VersionId TEXT NOT NULL,
IsDeleted INTEGER NOT NULL CHECK(IsDeleted IN(0,1)),CreatedByAvatarId TEXT NOT NULL,IsPublic INTEGER NOT NULL CHECK(IsPublic IN(0,1)),Name TEXT,Receipt TEXT NOT NULL,Proof TEXT NOT NULL);
""";command.ExecuteNonQuery();
    }
    private void WriteQueryProjection(SqliteConnection connection,SqliteTransaction transaction,CosmicChainIndexEntry entry,string receipt)
    {
        var row=OasisJson.DeserializeStored<Holon>(entry.CanonicalJson);
        var projection=new Projection(row.Id.ToString("D"),row.Version,row.VersionId.ToString("D"),row.IsDeleted,row.CreatedByAvatarId.ToString("D"),row.IsPublic,row.Name,receipt);
        var projectionJson=JsonSerializer.Serialize(projection);
        if(row.Name!=null)_=new UTF8Encoding(false,true).GetByteCount(row.Name);
        if(Encoding.UTF8.GetByteCount(projectionJson)>4*1024*1024)throw new InvalidDataException("Authenticated query projection exceeds its codec budget.");
        using var command=connection.CreateCommand();command.Transaction=transaction;
        command.CommandText="""
INSERT INTO CosmicChainIndexRows VALUES($id,$version,$versionId,$deleted,$owner,$public,$name,$receipt,$proof)
ON CONFLICT(Id) DO UPDATE SET Version=excluded.Version,VersionId=excluded.VersionId,IsDeleted=excluded.IsDeleted,
CreatedByAvatarId=excluded.CreatedByAvatarId,IsPublic=excluded.IsPublic,Name=excluded.Name,Receipt=excluded.Receipt,Proof=excluded.Proof;
""";
        command.Parameters.AddWithValue("$id",projection.Id);command.Parameters.AddWithValue("$version",projection.Version);
        command.Parameters.AddWithValue("$versionId",projection.VersionId);command.Parameters.AddWithValue("$deleted",projection.IsDeleted);
        command.Parameters.AddWithValue("$owner",projection.CreatedByAvatarId);command.Parameters.AddWithValue("$public",projection.IsPublic);
        command.Parameters.AddWithValue("$name",(object)projection.Name??DBNull.Value);command.Parameters.AddWithValue("$receipt",receipt);
        command.Parameters.AddWithValue("$proof",codec.Encrypt(context+"/projection/"+projection.Id,projectionJson));command.ExecuteNonQuery();
    }
    private string HistoryHash(SqliteConnection connection,SqliteTransaction transaction)
    {
        using var command=connection.CreateCommand();command.Transaction=transaction;
        command.CommandText="SELECT Id,Version,VersionId,CommitReference,Receipt FROM CosmicChainIndexHistory ORDER BY Id,Version";
        using var reader=command.ExecuteReader();using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);var count=0;
        while(reader.Read()){
            if(++count>10000)throw new InvalidDataException("Indexed history metadata integrity budget exceeded.");
            if(reader.GetFieldType(1)!=typeof(long)||new[]{0,2,3,4}.Any(index=>reader.GetFieldType(index)!=typeof(string)))
                throw new InvalidDataException("History metadata has noncanonical SQLite storage types.");
            var tuple=JsonSerializer.Serialize(new object[]{reader.GetString(0),reader.GetInt32(1),reader.GetString(2),reader.GetString(3),reader.GetString(4)});
            hash.AppendData(Encoding.UTF8.GetBytes(tuple+"\n"));
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    private void ValidateQueryProjection(SqliteConnection connection,SqliteTransaction transaction,Envelope state)
    {
        if(state.HistoryHash!=HistoryHash(connection,transaction))throw new InvalidDataException("History identity manifest differs from the authenticated checkpoint.");
        using(var count=connection.CreateCommand()){
            count.Transaction=transaction;count.CommandText="SELECT (SELECT COUNT(*) FROM CosmicChainIndexRows)-(SELECT COUNT(DISTINCT Id) FROM CosmicChainIndexHistory)";
            if(Convert.ToInt64(count.ExecuteScalar())!=0)throw new InvalidDataException("Current row projection is incomplete.");
        }
        using var command=connection.CreateCommand();command.Transaction=transaction;
        command.CommandText="""
SELECT r.Id,r.Version,r.VersionId,r.IsDeleted,r.CreatedByAvatarId,r.IsPublic,r.Name,r.Receipt,r.Proof,
(SELECT MAX(h.Version) FROM CosmicChainIndexHistory h WHERE h.Id=r.Id),
(SELECT h.Receipt FROM CosmicChainIndexHistory h WHERE h.Id=r.Id AND h.Version=r.Version) FROM CosmicChainIndexRows r;
""";
        using var reader=command.ExecuteReader();while(reader.Read()){
            if(new[]{1,3,5,9}.Any(index=>reader.GetFieldType(index)!=typeof(long))
                ||new[]{0,2,4,7,8,10}.Any(index=>reader.GetFieldType(index)!=typeof(string))
                ||!reader.IsDBNull(6)&&reader.GetFieldType(6)!=typeof(string)
                ||reader.GetInt64(3) is not (0 or 1)||reader.GetInt64(5) is not (0 or 1))
                throw new InvalidDataException("Query projection has noncanonical SQLite storage types/flags.");
            var projection=new Projection(reader.GetString(0),reader.GetInt32(1),reader.GetString(2),reader.GetBoolean(3),reader.GetString(4),reader.GetBoolean(5),reader.IsDBNull(6)?null:reader.GetString(6),reader.GetString(7));
            if(reader.IsDBNull(9)||reader.IsDBNull(10)||projection.Version!=reader.GetInt32(9)||projection.Receipt!=reader.GetString(10)
                ||codec.Decrypt(context+"/projection/"+projection.Id,reader.GetString(8))!=JsonSerializer.Serialize(projection))
                throw new InvalidDataException("Current query projection differs from its authenticated source history.");
        }
    }

    /// <summary>Native SQL canonical leaf rows over an explicitly opted-in schema2 index, with independent source evidence.</summary>
    public async Task<OASISResult<CosmicChainIndexRows>> QueryRowsAsync<T>(CosmicQueryPlan<T> plan,
        ICosmicChainQuerySourceObserver observer,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
    {
        try{
            ObjectDisposedException.ThrowIf(disposed,this);
            if(!canonicalRowProjection)return Error<CosmicChainIndexRows>("CQL_CAPABILITY_UNSUPPORTED","This store has no opted-in canonical query projection.");
            if(observer==null||maximumResults<1||maximumResults>10000)return Error<CosmicChainIndexRows>("CQL_QUERY_LIMIT","A source observer and bounded result limit are required.");
            var admitted=CanonicalRowCapabilities.AdmitIndexedRows(plan,source);if(admitted.IsError)return Error<CosmicChainIndexRows>(admitted.ErrorCode,admitted.Message);
            if(plan.Query.ReadAccess==null||plan.Query.ReadAccess.Authority.HasValue&&plan.Query.ReadAccess.Authority!=source.SourceProvider)
                return Error<CosmicChainIndexRows>("CQL_ACCESS_INVALID","A server-bound matching actor read policy is required.");
            var sql=HolonRepository.CompileChainIndexRows(plan.Query); // Full translation precedes storage/source I/O.
            cancellationToken.ThrowIfCancellationRequested();
            var raw=new List<CosmicChainQueryObservedRecord>();CosmicChainQueryCheckpoint checkpoint;
            using(var connection=Open())using(var transaction=connection.BeginTransaction(deferred:true)){
                var state=ReadState(connection,transaction);if(state==null)return Error<CosmicChainIndexRows>("CQL_SOURCE_EVIDENCE_MISSING","Index has no complete checkpoint.");
                ValidateQueryProjection(connection,transaction,state);checkpoint=state.Checkpoint;
                using var command=connection.CreateCommand();command.Transaction=transaction;
                command.CommandText="SELECT h.Receipt,h.Payload FROM CosmicChainIndexRows AS Holon JOIN CosmicChainIndexHistory h ON h.Id=Holon.Id AND h.Version=Holon.Version WHERE Holon.IsDeleted=0 AND ("+sql.Predicate+") ORDER BY "+sql.Ordering+" LIMIT $rowLimit OFFSET $rowOffset";
                command.Parameters.AddRange(sql.Parameters.ToArray());command.Parameters.AddWithValue("$rowLimit",plan.Query.Limit.HasValue?Math.Min(plan.Query.Limit.Value,maximumResults+1):maximumResults+1);
                command.Parameters.AddWithValue("$rowOffset",plan.Query.Offset);
                using var cancellation=cancellationToken.Register(command.Cancel);using var reader=command.ExecuteReader();
                while(reader.Read()){
                    cancellationToken.ThrowIfCancellationRequested();if(raw.Count==maximumResults)return Error<CosmicChainIndexRows>("CQL_QUERY_LIMIT","No partial indexed page is returned beyond its budget.");
                    var receiptJson=reader.GetString(0);var receipt=JsonSerializer.Deserialize<CosmicChainQueryRecordReceipt>(receiptJson);
                    raw.Add(new(receipt,codec.Decrypt(context+"/record/"+receiptJson,reader.GetString(1))));
                }
            }
            // The captured SQL page/checkpoint is immutable while source proofs are refreshed.
            var independentlyDecoded=new List<CosmicChainQueryObservedRecord>();
            foreach(var row in raw){
                cancellationToken.ThrowIfCancellationRequested();var independent=await observer.ObserveRecordAsync(source,row.Receipt.CommitReference,cancellationToken);
                if(independent?.IsError!=false||independent.Result==null)return Error<CosmicChainIndexRows>(independent?.ErrorCode??"CQL_SOURCE_EVIDENCE_MISSING","Independent source record is unavailable.",independent?.Exception);
                if(row.CanonicalJson!=independent.Result.CanonicalJson)return Error<CosmicChainIndexRows>("CQL_SOURCE_RECORD_MISMATCH","Indexed payload differs from the canonical source.");
                independentlyDecoded.Add(independent.Result);
            }
            cancellationToken.ThrowIfCancellationRequested();var head=await observer.ObserveHeadAsync(source,cancellationToken);
            if(head?.IsError!=false||head.Result==null)return Error<CosmicChainIndexRows>(head?.ErrorCode??"CQL_SOURCE_EVIDENCE_MISSING","Independent finalized head is unavailable.",head?.Exception);
            var requirement=plan.Query.IndexedSource;var now=DateTimeOffset.UtcNow;
            var freshness=CosmicChainQueryCheckpointAdmission.Admit(requirement,checkpoint,head.Result,now);if(freshness.IsError)return Error<CosmicChainIndexRows>(freshness.ErrorCode,freshness.Message);
            for(var index=0;index<raw.Count;index++){
                var receipt=CosmicChainQueryRecordAdmission.Admit(requirement,checkpoint,head.Result,now,raw[index].Receipt,independentlyDecoded[index].Receipt);
                if(receipt.IsError)return Error<CosmicChainIndexRows>(receipt.ErrorCode,receipt.Message);
            }
            cancellationToken.ThrowIfCancellationRequested();var rows=raw.Select(row=>(IHolon)OasisJson.DeserializeStored<Holon>(row.CanonicalJson)).ToArray();
            if(rows.Any(row=>row.IsDeleted||!plan.Query.ReadAccess.CanRead(row)))return Error<CosmicChainIndexRows>("CQL_VISIBILITY_VIOLATION","Indexed rows violated the mandatory read policy.");
            foreach(var row in rows)row.Children=Array.Empty<IHolon>();
            return new OASISResult<CosmicChainIndexRows>(new(checkpoint,head.Result,Array.AsReadOnly(rows),Array.AsReadOnly(raw.Select(row=>row.Receipt).ToArray()))){IsLoaded=true,LoadedCount=rows.Length};
        }catch(OperationCanceledException error){return Error<CosmicChainIndexRows>("CQL_QUERY_CANCELLED","Indexed read cancelled; no partial rows returned.",error);}
        catch(Exception error){return Error<CosmicChainIndexRows>("CQL_INDEX_READ_FAILED","Indexed SQL read/evidence validation failed; no partial rows returned.",error);}
    }
}
