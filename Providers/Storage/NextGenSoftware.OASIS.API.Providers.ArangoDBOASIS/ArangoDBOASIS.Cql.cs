using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ArangoDBOASIS;

public partial class ArangoDBOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.ArangoDBOASIS;
    public CosmicQueryCapabilities QueryCapabilities => ArangoDbCqlCompiler.Capabilities;
    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code,string message,Exception cause=null)
            => new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
        try
        {
            if(plan==null || plan.Targets.Count!=1 || plan.Targets[0].Provider!=QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE","One explicit ArangoDB target required.");
            if(maximumResults<1 || maximumResults>10000)
                return Error("CQL_QUERY_LIMIT","Result budget must be between1 and10,000.");
            var admission=QueryCapabilities.AdmitPlan(plan);
            if(admission.IsError)return Error(admission.ErrorCode,admission.Message);
            var selected=ArangoDbCqlCompiler.Compile(plan.Query,QueryProviderType);
            if(selected.IsError)return Error(selected.ErrorCode,selected.Message);
            if(IsVersionControlEnabled)return Error("CQL_CAPABILITY_UNSUPPORTED","Version-control mode requires a verified revision query policy.");
            cancellationToken.ThrowIfCancellationRequested();
            if(!IsProviderActivated)return Error("CQL_PROVIDER_INACTIVE","Activate configured ArangoDB before querying.");
            var rows=new List<IHolon>();long bytesRead=0;
            foreach(var id in selected.Result)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var key=id.ToString("D");
                using var request=new HttpRequestMessage(HttpMethod.Get,ApiUrl($"/_api/document/{ColHolons}/{Uri.EscapeDataString(key)}"));
                using var response=await _http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,cancellationToken);
                if(response.StatusCode!=HttpStatusCode.NotFound)response.EnsureSuccessStatusCode();
                using var stream=await response.Content.ReadAsStreamAsync(cancellationToken);
                using var payload=new MemoryStream();var buffer=new byte[8192];int count;
                while((count=await stream.ReadAsync(buffer,cancellationToken))!=0)
                {
                    bytesRead=checked(bytesRead+count);
                    if(bytesRead>4*1024*1024)return Error("CQL_QUERY_LIMIT","Complete native payload exceeds4MiB.");
                    payload.Write(buffer,0,count);
                }
                using var document=JsonDocument.Parse(payload.ToArray());var native=document.RootElement;
                if(response.StatusCode==HttpStatusCode.NotFound)
                {
                    if(native.TryGetProperty("errorNum",out var missing) && missing.GetInt32()==1202)continue;
                    response.EnsureSuccessStatusCode();
                }
                if(!native.TryGetProperty("_id",out var nativeId) || nativeId.GetString()!=ColHolons+"/"+key ||
                    !native.TryGetProperty("_key",out var nativeKey) || nativeKey.GetString()!=key ||
                    !native.TryGetProperty("_rev",out var revision) || revision.ValueKind!=JsonValueKind.String || string.IsNullOrWhiteSpace(revision.GetString()))
                    throw new InvalidDataException("Native ArangoDB identity/revision acknowledgement differs from selected key.");
                var row=JsonSerializer.Deserialize<Holon>(native.GetRawText(),new JsonSerializerOptions{PropertyNameCaseInsensitive=true});
                if(row==null || row.Id!=id || !plan.Query.ReadAccess.CanRead(row))throw new InvalidDataException("Native holon differs from read authority.");
                if(row.IsDeleted)continue;
                row.Children=Array.Empty<IHolon>();rows.Add(row);
            }
            // Independent native document reads; no database/federated snapshot claim.
            var page=rows.Skip(plan.Query.Offset).Take(plan.Query.Limit??rows.Count).ToArray();
            if(page.Length>maximumResults)return Error("CQL_QUERY_LIMIT","Complete page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)){IsLoaded=true,LoadedCount=page.Length};
        }
        catch(OperationCanceledException cause) when(cancellationToken.IsCancellationRequested)
        {return Error("CQL_QUERY_CANCELLED","ArangoDB query cancelled; no partial rows published.",cause);}
        catch(Exception cause){return Error("CQL_QUERY_FAILED","Native ArangoDB query failed; no partial rows published.",cause);}
    }
}
