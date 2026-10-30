using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS:ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType=>Core.Enums.ProviderType.OracleDBOASIS;
    public CosmicQueryCapabilities QueryCapabilities=>OracleCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
    {
        try
        {
            if(plan==null||plan.Targets.Count!=1||plan.Targets[0].Provider!=QueryProviderType)
                return CqlError("CQL_PLAN_TARGET_UNAVAILABLE","One explicitly admitted Oracle target is required.");
            if(maximumResults<1||maximumResults>10000)return CqlError("CQL_QUERY_LIMIT","A result budget between1and10000 is required.");
            var admitted=QueryCapabilities.AdmitPlan(plan);
            if(admitted.IsError)return CqlError(admitted.ErrorCode,admitted.Message);
            var compiled=OracleCqlCompiler.Compile(plan.Query);
            if(compiled.IsError)return CqlError(compiled.ErrorCode,compiled.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if(!IsProviderActivated)return CqlError("CQL_PROVIDER_INACTIVE","Explicitly activate the configured Oracle provider before querying.");
            await using var connection=new OracleConnection(_connectionString);await connection.OpenAsync(cancellationToken);
            await using var command=CreateCommand(compiled.Result.Sql,connection);
            compiled.Result.BindParameters(command);
            await using var reader=await command.ExecuteReaderAsync(cancellationToken);
            var rows=new List<IHolon>();var seen=new HashSet<Guid>();var characters=0;
            while(await reader.ReadAsync(cancellationToken))
            {
                if(seen.Count>=128)return CqlError("CQL_QUERY_LIMIT","The native authorized record set exceeds128 rows.");
                var id=new Guid((byte[])reader.GetValue(0));
                if(!seen.Add(id))return CqlError("CQL_PROVIDER_RESULT_INVALID","Native source returned duplicate identities.");
                using var text=reader.GetTextReader(1);var body=new StringBuilder();var buffer=new char[8192];int count;
                while((count=await text.ReadAsync(buffer.AsMemory(),cancellationToken))!=0)
                {
                    characters+=count;
                    if(characters>4*1024*1024||body.Length+count>1024*1024)
                        return CqlError("CQL_QUERY_LIMIT","Oracle canonical records exceed the bounded query envelope budget.");
                    body.Append(buffer,0,count);
                }
                // Reuse the one canonical identity/history/native-column validation owner.
                var row=await ReadRecordVersionAsync<Holon>(id,body.ToString(),0,nativeHolon:ReadNativeHolonProjection(reader,2));
                if(row==null||row.IsDeleted||!plan.Query.ReadAccess.CanRead(row))
                    return CqlError("CQL_VISIBILITY_VIOLATION","Native source differs from its admitted canonical visibility.");
                row.Children=Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey??=new Dictionary<ProviderType,string>();
                row.ProviderUniqueStorageKey[QueryProviderType]=id.ToString("D");rows.Add(row);
            }
            // Native ID selection and authorization precede this bounded page. Validate
            // every selected canonical envelope before publishing even an empty window.
            var page=rows.Skip(plan.Query.Offset).Take(plan.Query.Limit??rows.Count).ToArray();
            if(page.Length>maximumResults)return CqlError("CQL_QUERY_LIMIT","The complete source page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(page)){IsLoaded=true,LoadedCount=page.Length};
        }
        catch(OperationCanceledException error){return CqlError("CQL_QUERY_CANCELLED","Oracle CQL query cancelled; no partial rows.",error);}
        catch(Exception error){return CqlError("CQL_QUERY_FAILED","Oracle native CQL query failed; no partial rows.",error);}
    }
    private static OASISResult<IReadOnlyList<IHolon>> CqlError(string code,string message,Exception? error=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=error};
}
