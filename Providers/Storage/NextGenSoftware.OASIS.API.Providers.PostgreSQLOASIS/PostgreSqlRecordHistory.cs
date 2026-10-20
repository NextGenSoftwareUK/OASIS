using System;
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;

public partial class PostgreSQLOASIS
{
    private static readonly EmbeddedRecordHistory RecordHistory=new();

    private async Task<OASISResult<IEnumerable<IHolon>>> QueryStoredMetadataAsync(string key,string value,HolonType type,
        bool children,bool recursive,int depth,int initial,bool fromProvider,int version)
    {
        try{
            if(string.IsNullOrWhiteSpace(key))throw new ArgumentException("A nonblank metadata key is required.",nameof(key));
            return await QueryStoredMetadataAsync(new Dictionary<string,string>{{key,value}},MetaKeyValuePairMatchMode.All,type,children,recursive,depth,initial,fromProvider,version);
        }catch(Exception cause){var result=new OASISResult<IEnumerable<IHolon>>();OASISErrorHandling.HandleError(ref result,"Invalid PostgreSQL metadata query.",cause);return result;}
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> QueryStoredMetadataAsync(Dictionary<string,string> pairs,MetaKeyValuePairMatchMode mode,
        HolonType type,bool children,bool recursive,int depth,int initial,bool fromProvider,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try{
            if(pairs==null||pairs.Count==0||pairs.Keys.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Nonempty metadata filters are required.");
            var search=new SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new Dictionary<string,string>(pairs),MetaKeyValuePairMatchMode=mode};
            StoredRecordSearch.Validate(search,version,depth,supportsHistory:true);
            return await QueryStoredHolonsAsync(type,row=>StoredRecordSearch.Matches(row,false,search,Array.Empty<IHolon>()),children,recursive,depth,initial,fromProvider,version);
        }catch(Exception cause){OASISErrorHandling.HandleError(ref result,"Invalid PostgreSQL metadata query.",cause);return result;}
    }

    private async Task<OASISResult<ISearchResults>> SearchStoredRecordsAsync(ISearchParams request,bool children,bool recursive,int depth,int version)
    {
        var result=new OASISResult<ISearchResults>();
        try{
            StoredRecordSearch.Validate(request,version,depth,supportsHistory:true);
            var search=StoredRecordJson.Project<SearchParams>(request);
            StoredRecordSearch.Validate(search,version,depth,supportsHistory:true);
            var groups=search.SearchGroups??new List<ISearchGroupBase>();
            var wantsAvatars=groups.Count==0||groups.Any(group=>group.SearchAvatars||!group.SearchHolons);
            var wantsHolons=groups.Count==0||groups.Any(group=>group.SearchHolons||!group.SearchAvatars);
            var avatars=Array.Empty<IAvatar>();var holons=Array.Empty<IHolon>();
            if(wantsAvatars){var loaded=await LoadAllAvatarsAsync(version);if(loaded.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IAvatar>,ISearchResults>(loaded);avatars=loaded.Result.ToArray();}
            if(wantsHolons||search.ParentId!=Guid.Empty){var loaded=await LoadAllHolonsAsync(HolonType.All,children,recursive,depth,loadChildrenFromProvider:true,version:version);if(loaded.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,ISearchResults>(loaded);holons=loaded.Result.ToArray();}
            var current=version>0&&(wantsHolons||search.ParentId!=Guid.Empty)?await ReadHolonRowsAsync(0):holons.AsEnumerable();
            var found=new SearchResults();
            found.SearchResultAvatars.AddRange(avatars.Where(row=>StoredRecordSearch.Matches(row,true,search,current)));
            if(wantsHolons)found.SearchResultHolons.AddRange(holons.Where(row=>StoredRecordSearch.Matches(row,false,search,current)));
            found.NumberOfResults=found.SearchResultAvatars.Count+found.SearchResultHolons.Count;
            result.Result=found;result.IsLoaded=true;
        }catch(Exception cause){OASISErrorHandling.HandleError(ref result,"PostgreSQL versioned search failed.",cause);}
        return result;
    }

    private async Task<List<IHolon>> ReadHolonRowsAsync(int version)
    {
        if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
        var rows=new List<IHolon>();
        await using var connection=new NpgsqlConnection(_connectionString);await connection.OpenAsync();
        await using var command=new NpgsqlCommand("SELECT id,data_json FROM oasis_holons WHERE is_deleted=FALSE",connection);
        await using var reader=await command.ExecuteReaderAsync();
        while(await reader.ReadAsync()){var row=await ReadKeyedStoredRecordAsync<Holon>(reader.GetGuid(0),reader.GetString(1),version);if(row!=null)rows.Add(row);}
        return rows;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> QueryStoredHolonsAsync(HolonType type,Func<IHolon,bool> filter,
        bool children,bool recursive,int depth,int initial,bool fromProvider,int version,bool rootsAreChildren=false)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try{
            if(!Enum.IsDefined(typeof(HolonType),type))throw new ArgumentOutOfRangeException(nameof(type));
            if(version<0||depth<0||initial<0)throw new ArgumentOutOfRangeException(nameof(version));
            var roots=await ReadHolonRowsAsync(version);
            var pool=version>0&&children&&fromProvider?await ReadHolonRowsAsync(0):roots;
            var project=HolonGraphProjection.Create(pool,children,recursive,depth,initial,fromProvider,
                row=>StoredRecordJson.DeserializeStored<Holon>(StoredRecordJson.Serialize(row)),rootsAreChildren:rootsAreChildren);
            result.Result=roots.Where(row=>(type==HolonType.All||row.HolonType==type)&&(filter==null||filter(row))).Select(project).ToArray();
            result.IsLoaded=true;
        }catch(Exception cause){OASISErrorHandling.HandleError(ref result,"PostgreSQL versioned holon query failed.",cause);}
        return result;
    }

    private static JObject StoredDocument(string json)
    {
        var document=OasisJson.ParseStoredObject(json);
        // Native SQL keys are separate columns. The shared embedded codec also requires
        // an explicit envelope key; older canonical rows already carry the same Id.
        if(document["id"]==null)document["id"]=document["Id"]?.DeepClone();
        return document;
    }

    private static Task<T> ReadStoredRecordAsync<T>(string json,int version) where T:class,IHolonBase
    {
        var document=StoredDocument(json);
        if(document["Id"]?.Type!=JTokenType.String||!Guid.TryParse(document["Id"]!.Value<string>(),out var id)||id==Guid.Empty)
            throw new InvalidDataException("PostgreSQL stored record requires a nonempty canonical identity.");
        return RecordHistory.ReadAsync(id,document,version,StoredRecordJson.DeserializeStored<T>);
    }

    private static Task<T> ReadKeyedStoredRecordAsync<T>(Guid identity,string json,int version) where T:class,IHolonBase
        =>RecordHistory.ReadAsync(identity,StoredDocument(json),version,StoredRecordJson.DeserializeStored<T>);

    private async Task<(string Json,T Record)> StageStoredRecordAsync<T>(T proposal,string previous) where T:class,IHolonBase
    {
        var observed=previous==null?null:StoredDocument(previous);
        if(!IsVersionControlEnabled&&observed?[EmbeddedRecordHistory.PropertyName]==null)
            return (StoredRecordJson.Serialize(proposal),proposal);
        var document=await RecordHistory.StageAsync(proposal,observed,StoredRecordJson.DeserializeStored<T>,StoredRecordJson.Serialize);
        var committed=await RecordHistory.ReadAsync(proposal.Id,document,0,StoredRecordJson.DeserializeStored<T>);
        return (document.ToString(Formatting.None),committed);
    }

    private async Task<T> ExecuteRecordWriteAsync<T>(NpgsqlCommand command,string table,T proposal,Action validateCaller) where T:class,IHolonBase
    {
        // One serializable transaction owns observation, archive, current payload and
        // indexed columns. Serialization conflicts surface; no implicit write retry.
        var committing=false;var committed=false;var mutationStarted=false;
        try{
            await using var transaction=await command.Connection!.BeginTransactionAsync(IsolationLevel.Serializable);
            command.Transaction=transaction;
            var stored=await WriteRecordInTransactionAsync(command,table,proposal,validateCaller,mutationStarting:()=>mutationStarted=true);
            validateCaller();
            committing=true;await transaction.CommitAsync();committed=true;
            return stored;
        }catch(Exception cause)when(committed||mutationStarted&&cause is NpgsqlException and not PostgresException||committing&&cause is not PostgresException){
            throw new PostgreSqlWriteReconciliationException(proposal.Id,cause);
        }
    }

    private async Task<T> WriteRecordInTransactionAsync<T>(NpgsqlCommand command,string table,T proposal,Action validateCaller,PlannedHolonWrite plan=null,Action mutationStarting=null) where T:class,IHolonBase
    {
        string previous;
        await using(var read=new NpgsqlCommand($"SELECT data_json FROM {table} WHERE id=@id FOR UPDATE",command.Connection,command.Transaction)){
            read.Parameters.AddWithValue("id",proposal.Id);previous=(string)await read.ExecuteScalarAsync();
        }
        if(plan!=null&&proposal is Holon holon){
            var observed=previous==null?null:await ReadKeyedStoredRecordAsync<Holon>(proposal.Id,previous,0);
            holon.ChildRoleManifestJson=HolonChildRolePersistence.Resolve(plan,HolonChildRoleWriteKind.SourceSave,observed!=null,observed?.ChildRoleManifestJson);
        }
        var staged=await StageStoredRecordAsync(proposal,previous);
        command.Parameters["@data_json"].Value=staged.Json;
        command.CommandText+=" RETURNING data_json";
        validateCaller();
        mutationStarting?.Invoke();
        var acknowledged=await command.ExecuteScalarAsync();
        if(acknowledged is not string json||!StoredRecordPayloadEquality.Equivalent(OasisJson.ParseStoredObject(staged.Json),OasisJson.ParseStoredObject(json)))
            throw new InvalidDataException("PostgreSQL RETURNING payload did not acknowledge the exact staged record.");
        validateCaller();
        return staged.Record;
    }
}
