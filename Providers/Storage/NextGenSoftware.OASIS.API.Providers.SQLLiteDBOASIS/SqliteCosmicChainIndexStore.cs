using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public sealed record CosmicChainIndexEntry(CosmicChainQueryRecordReceipt Receipt,
    CosmicChainQueryRecordReceipt IndependentlyDecodedReceipt,string CanonicalJson);
public sealed record CosmicChainIndexStoredRecord(CosmicChainQueryRecordReceipt Receipt,string CanonicalJson);
public sealed record CosmicChainIndexSnapshot(CosmicChainQueryCheckpoint Checkpoint,
    IReadOnlyList<CosmicChainIndexStoredRecord> History);

/// <summary>
/// Trusted ingestion storage primitive, not a CQL executor or access grant.
/// Source-owner decoding/completeness and actor-authorized query execution remain separate gates.
/// Encrypted immutable history and checkpoint commit in one immediate SQLite transaction.
/// </summary>
public sealed partial class SqliteCosmicChainIndexStore : IDisposable,NextGenSoftware.OASIS.API.Core.Interfaces.ICosmicChainQueryIndexPublisher
{
    private sealed record Envelope(CosmicChainQueryCheckpoint Checkpoint,string BatchHash,int HistoryCount,string ProjectionProfile,string HistoryHash);
    private readonly string connectionString;
    private readonly CosmicChainQuerySource source;
    private readonly string binding;
    private readonly string context;
    private readonly EncryptedRecordCodec codec;
    private bool disposed;
    private readonly bool canonicalRowProjection;

    public SqliteCosmicChainIndexStore(string databasePath,CosmicChainQuerySource source,byte[] storageKey,bool canonicalRowProjection=false)
    {
        if(!Path.IsPathRooted(databasePath))throw new ArgumentException("An explicit absolute index path is required.");
        this.source=source??throw new ArgumentNullException(nameof(source));
        if(source.ExecutionProvider!=ProviderType.SQLLiteDBOASIS)throw new ArgumentException("This execution owner is SQLite.");
        if(source.SchemaVersion!=(canonicalRowProjection?2:1))throw new ArgumentException("Declare schema1 history-only or separately opted-in schema2 canonical rows.");
        this.canonicalRowProjection=canonicalRowProjection;
        codec=new EncryptedRecordCodec(storageKey,compress:true);
        binding=JsonSerializer.Serialize(source);
        context="oasis/cosmic-chain-index/v1/"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(binding)));
        connectionString=new SqliteConnectionStringBuilder{DataSource=databasePath,Pooling=false}.ToString();
    }

    private SqliteConnection Open()
    {
        var connection=new SqliteConnection(connectionString);connection.Open();
        try{
            using var command=connection.CreateCommand();
            command.CommandText="""
CREATE TABLE IF NOT EXISTS CosmicChainIndexState (Id INTEGER PRIMARY KEY CHECK(Id=1),Binding TEXT NOT NULL,Envelope TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS CosmicChainIndexHistory (Id TEXT NOT NULL,Version INTEGER NOT NULL,VersionId TEXT NOT NULL UNIQUE,
CommitReference TEXT NOT NULL UNIQUE,Receipt TEXT NOT NULL,Payload TEXT NOT NULL,PRIMARY KEY(Id,Version));
""";
            command.ExecuteNonQuery();
            if(canonicalRowProjection){
                var existing=ReadState(connection,null);
                if(existing==null)InitializeQueryProjection(connection);
                else{
                    using var schema=connection.CreateCommand();schema.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='CosmicChainIndexRows'";
                    if(Convert.ToInt32(schema.ExecuteScalar())!=1)throw new InvalidDataException("Committed query projection schema is missing; no implicit reconstruction.");
                }
            }
            return connection;
        }catch{connection.Dispose();throw;}
    }

    private Envelope ReadState(SqliteConnection connection,SqliteTransaction transaction)
    {
        using var command=connection.CreateCommand();command.Transaction=transaction;
        command.CommandText="SELECT Binding,Envelope FROM CosmicChainIndexState WHERE Id=1";
        using var reader=command.ExecuteReader();if(!reader.Read())return null;
        if(reader.GetString(0)!=binding)throw new InvalidDataException("Index belongs to another source binding.");
        var envelope=JsonSerializer.Deserialize<Envelope>(codec.Decrypt(context+"/checkpoint",reader.GetString(1)))
            ??throw new InvalidDataException("Index checkpoint is missing.");
        if(JsonSerializer.Serialize(envelope.Checkpoint.Source)!=binding)throw new InvalidDataException("Checkpoint source differs.");
        if(envelope.ProjectionProfile!=(canonicalRowProjection?"canonical-rows-v1":null))
            throw new InvalidDataException("Index projection policy differs; explicit migration/new binding required.");
        return envelope;
    }

    public OASISResult<bool> Publish(CosmicChainQueryReadRequirement requirement,CosmicChainQueryCheckpoint checkpoint,
        CosmicChainQueryFinalizedHead independentlyObservedHead,DateTimeOffset validationTime,
        CosmicChainQueryPosition expectedPrevious,IReadOnlyList<CosmicChainIndexEntry> entries)
    {
        var commitRequested=false;
        try{
            ObjectDisposedException.ThrowIf(disposed,this);
            if(requirement==null||checkpoint==null||JsonSerializer.Serialize(requirement.Source)!=binding)
                return Error<bool>("CQL_SOURCE_BINDING_MISMATCH","An explicitly matching source binding is required.");
            var admission=CosmicChainQueryCheckpointAdmission.Admit(requirement,checkpoint,independentlyObservedHead,validationTime);
            if(admission.IsError)return Error<bool>(admission.ErrorCode,admission.Message);
            if(entries==null||entries.Count>1000)throw new ArgumentException("A bounded complete batch is required.");
            var ordered=entries.OrderBy(e=>e?.Receipt?.Record.Id).ThenBy(e=>e?.Receipt?.Record.Version).ToArray();
            long bytes=0;
            foreach(var entry in ordered){
                if(entry==null||entry.CanonicalJson==null)throw new InvalidDataException("Missing source entry.");
                var recordAdmission=CosmicChainQueryRecordAdmission.Admit(requirement,checkpoint,independentlyObservedHead,
                    validationTime,entry.Receipt,entry.IndependentlyDecodedReceipt);
                if(recordAdmission.IsError)return Error<bool>(recordAdmission.ErrorCode,recordAdmission.Message);
                var payload=Encoding.UTF8.GetBytes(entry.CanonicalJson);bytes+=payload.Length;
                if(payload.Length>4*1024*1024||bytes>16*1024*1024)throw new InvalidDataException("Batch payload budget exceeded.");
                if(Convert.ToHexString(SHA256.HashData(payload))!=entry.Receipt.Record.CanonicalHash)
                    return Error<bool>("CQL_SOURCE_RECORD_MISMATCH","Payload differs from the canonical source hash.");
                var decoded=OasisJson.DeserializeStored<Holon>(entry.CanonicalJson);
                var identity=entry.Receipt.Record;
                if(decoded.Id!=identity.Id||decoded.Version!=identity.Version||decoded.VersionId!=identity.VersionId||decoded.IsDeleted!=identity.IsDeleted)
                    return Error<bool>("CQL_SOURCE_RECORD_MISMATCH","Payload identity differs from the canonical source receipt.");
            }
            if(ordered.Select(e=>e.Receipt.CommitReference).Distinct().Count()!=ordered.Length)
                throw new InvalidDataException("A batch cannot repeat a source commit.");
            var batchHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{
                checkpoint,records=ordered.Select(e=>new{e.Receipt,e.CanonicalJson})}))));

            using var connection=Open();
            using var transaction=connection.BeginTransaction(deferred:false);
            var previous=ReadState(connection,transaction);
            using(var count=connection.CreateCommand()){
                count.Transaction=transaction;count.CommandText="SELECT COUNT(*) FROM CosmicChainIndexHistory";
                if(Convert.ToInt32(count.ExecuteScalar())!=(previous?.HistoryCount??0))throw new InvalidDataException("Index history count differs from its authenticated checkpoint.");
            }
            if(canonicalRowProjection){
                if(previous!=null)ValidateQueryProjection(connection,transaction,previous);
                else{
                    using var empty=connection.CreateCommand();empty.Transaction=transaction;empty.CommandText="SELECT COUNT(*) FROM CosmicChainIndexRows";
                    if(Convert.ToInt32(empty.ExecuteScalar())!=0)throw new InvalidDataException("Query projections have no committed checkpoint.");
                }
            }
            if(previous!=null&&previous.BatchHash==batchHash)
                return new OASISResult<bool>(true); // Exact replay acknowledges no new write.
            if((previous==null)!=(expectedPrevious==null)||previous!=null&&
                (previous.Checkpoint.Position.Slot!=expectedPrevious.Slot||previous.Checkpoint.Position.BlockIdentity!=expectedPrevious.BlockIdentity))
                return Error<bool>("CQL_INDEX_CHECKPOINT_CONFLICT","Another client advanced the index; no batch effects committed.");
            admission=CosmicChainQueryCheckpointAdmission.Admit(requirement,checkpoint,independentlyObservedHead,validationTime,previous?.Checkpoint);
            if(admission.IsError)return Error<bool>(admission.ErrorCode,admission.Message);
            if(previous!=null&&checkpoint.Position.Slot==previous.Checkpoint.Position.Slot)
                return Error<bool>("CQL_INDEX_CHECKPOINT_CONFLICT","An acknowledged checkpoint cannot publish different batch effects.");
            var inserted=0;
            foreach(var entry in ordered){
                var receipt=entry.Receipt;var receiptJson=JsonSerializer.Serialize(receipt);
                using var existing=connection.CreateCommand();existing.Transaction=transaction;
                existing.CommandText="SELECT Receipt,Payload FROM CosmicChainIndexHistory WHERE Id=$id AND Version=$version";
                existing.Parameters.AddWithValue("$id",receipt.Record.Id.ToString("D"));existing.Parameters.AddWithValue("$version",receipt.Record.Version);
                using(var reader=existing.ExecuteReader()){
                    if(reader.Read()){
                        if(reader.GetString(0)!=receiptJson||codec.Decrypt(context+"/record/"+receiptJson,reader.GetString(1))!=entry.CanonicalJson)
                            return Error<bool>("CQL_INDEX_REVISION_CONFLICT","An immutable source revision conflicts with stored history.");
                        continue;
                    }
                }
                if(previous!=null&&receipt.Position.Slot<=previous.Checkpoint.Position.Slot)
                    return Error<bool>("CQL_INDEX_COVERAGE_CONFLICT","A newly discovered old commit contradicts previously complete coverage.");
                using var version=connection.CreateCommand();version.Transaction=transaction;
                version.CommandText="SELECT MAX(Version) FROM CosmicChainIndexHistory WHERE Id=$id";version.Parameters.AddWithValue("$id",receipt.Record.Id.ToString("D"));
                var highest=version.ExecuteScalar();
                if(highest!=null&&highest!=DBNull.Value&&receipt.Record.Version!=Convert.ToInt32(highest)+1)
                    return Error<bool>("CQL_INDEX_REVISION_CONFLICT","Subsequent source revisions must advance the existing history by one.");
                using var insert=connection.CreateCommand();insert.Transaction=transaction;
                insert.CommandText="INSERT INTO CosmicChainIndexHistory VALUES($id,$version,$versionId,$commit,$receipt,$payload)";
                insert.Parameters.AddWithValue("$id",receipt.Record.Id.ToString("D"));insert.Parameters.AddWithValue("$version",receipt.Record.Version);
                insert.Parameters.AddWithValue("$versionId",receipt.Record.VersionId.ToString("D"));insert.Parameters.AddWithValue("$commit",receipt.CommitReference);
                insert.Parameters.AddWithValue("$receipt",receiptJson);insert.Parameters.AddWithValue("$payload",codec.Encrypt(context+"/record/"+receiptJson,entry.CanonicalJson));
                insert.ExecuteNonQuery();
                if(canonicalRowProjection)WriteQueryProjection(connection,transaction,entry,receiptJson);
                inserted++;
            }
            using var update=connection.CreateCommand();update.Transaction=transaction;
            update.CommandText="INSERT INTO CosmicChainIndexState VALUES(1,$binding,$envelope) ON CONFLICT(Id) DO UPDATE SET Envelope=excluded.Envelope";
            update.Parameters.AddWithValue("$binding",binding);update.Parameters.AddWithValue("$envelope",codec.Encrypt(context+"/checkpoint",JsonSerializer.Serialize(new Envelope(checkpoint,batchHash,(previous?.HistoryCount??0)+inserted,canonicalRowProjection?"canonical-rows-v1":null,canonicalRowProjection?HistoryHash(connection,transaction):null))));
            update.ExecuteNonQuery();commitRequested=true;transaction.Commit();
            return new OASISResult<bool>(true){IsSaved=true,SavedCount=inserted};
        }catch(Exception error){return Error<bool>(commitRequested?"CQL_INDEX_PUBLICATION_UNCERTAIN":"CQL_INDEX_PUBLICATION_FAILED",
            commitRequested?"Commit outcome requires checkpoint/history reconciliation before replay.":"Atomic index publication failed; no partial batch acknowledged.",error);}
    }

    public OASISResult<CosmicChainIndexSnapshot> ReadSnapshot(int maximumHistoryRecords=10000)
    {
        try{
            ObjectDisposedException.ThrowIf(disposed,this);
            if(maximumHistoryRecords<1||maximumHistoryRecords>10000)throw new ArgumentOutOfRangeException(nameof(maximumHistoryRecords));
            using var connection=Open();using var transaction=connection.BeginTransaction(deferred:true);
            var state=ReadState(connection,transaction);
            using var command=connection.CreateCommand();command.Transaction=transaction;
            command.CommandText="SELECT Id,Version,VersionId,CommitReference,Receipt,Payload FROM CosmicChainIndexHistory ORDER BY Id,Version LIMIT $limit";
            command.Parameters.AddWithValue("$limit",maximumHistoryRecords+1);
            using var reader=command.ExecuteReader();var history=new List<CosmicChainIndexStoredRecord>();
            while(reader.Read()){
                if(history.Count==maximumHistoryRecords)throw new InvalidDataException("History read budget exceeded; no truncated snapshot returned.");
                var receiptJson=reader.GetString(4);
                var receipt=JsonSerializer.Deserialize<CosmicChainQueryRecordReceipt>(receiptJson)??throw new InvalidDataException("Receipt is missing.");
                if(JsonSerializer.Serialize(receipt.Source)!=binding)throw new InvalidDataException("History source binding differs.");
                if(reader.GetString(0)!=receipt.Record.Id.ToString("D")||reader.GetInt32(1)!=receipt.Record.Version||reader.GetString(2)!=receipt.Record.VersionId.ToString("D")||reader.GetString(3)!=receipt.CommitReference)
                    throw new InvalidDataException("Indexed identity columns differ from their authenticated receipt.");
                var payload=codec.Decrypt(context+"/record/"+receiptJson,reader.GetString(5));
                if(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))!=receipt.Record.CanonicalHash)
                    throw new InvalidDataException("Stored canonical payload hash differs.");
                history.Add(new CosmicChainIndexStoredRecord(receipt,payload));
            }
            if(history.Count!=(state?.HistoryCount??0))throw new InvalidDataException("History has no committed checkpoint.");
            return new OASISResult<CosmicChainIndexSnapshot>(new(state?.Checkpoint,history.AsReadOnly())){IsLoaded=true,LoadedCount=history.Count};
        }catch(Exception error){return Error<CosmicChainIndexSnapshot>("CQL_INDEX_READ_FAILED","Index snapshot validation failed; no partial history returned.",error);}
    }

    OASISResult<bool> NextGenSoftware.OASIS.API.Core.Interfaces.ICosmicChainQueryIndexPublisher.Publish(
        CosmicChainQueryReadRequirement requirement,CosmicChainQueryCheckpoint checkpoint,CosmicChainQueryFinalizedHead head,
        DateTimeOffset validationTime,CosmicChainQueryPosition expectedPrevious,IReadOnlyList<CosmicChainQueryIndexEntry> entries)
        =>Publish(requirement,checkpoint,head,validationTime,expectedPrevious,
            entries?.Take(1001).Select(entry=>entry==null?null:new CosmicChainIndexEntry(entry.Receipt,entry.IndependentlyDecodedReceipt,entry.CanonicalJson)).ToArray());

    private static OASISResult<T> Error<T>(string code,string message,Exception error=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=error};
    public void Dispose(){disposed=true;codec.Dispose();}
}
