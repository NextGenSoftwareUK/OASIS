using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

/// <summary>Native encrypted LDP record point read; no general SPARQL/index claim.</summary>
public partial class SOLIDOASIS : ICosmicQueryStorageProvider
{
    public const string CqlRecordIdKey="SolidRecordId";
    private const int CqlMaximumRdfBytes=4*1024*1024;
    private static readonly CosmicQueryCapabilities RecordCapabilities=
        CosmicQueryCapabilities.ForStringMetadataPointLookup(CqlRecordIdKey,36);
    public ProviderType QueryProviderType=>Core.Enums.ProviderType.SOLIDOASIS;
    public CosmicQueryCapabilities QueryCapabilities=>RecordCapabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
    {
        try{
            if(plan==null||plan.Targets.Count!=1||plan.Targets[0].Provider!=QueryProviderType)
                return CqlFailure("CQL_PLAN_TARGET_UNAVAILABLE","One admitted SOLID target is required.");
            if(maximumResults<1||maximumResults>10000)
                return CqlFailure("CQL_QUERY_LIMIT","The result budget must be between one and 10,000.");
            var admitted=QueryCapabilities.AdmitPlan(plan);
            if(admitted.IsError)return CqlFailure(admitted.ErrorCode,admitted.Message);
            var key=QueryCapabilities.StringPointLookup.SelectKey(plan.Query).Result;
            if(!Guid.TryParseExact(key,"D",out var id)||id==Guid.Empty||id.ToString("D")!=key)
                return CqlFailure("CQL_INVALID_SOURCE_KEY","A canonical lowercase nonempty record GUID is required.");
            cancellationToken.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(_disposed,this);
            if(!_isActivated)return CqlFailure("CQL_PROVIDER_INACTIVE","Activate the configured SOLID provider before querying.");
            RequirePrivateStorage();
            if(plan.Query.Offset>0||plan.Query.Limit==0)return CqlRows();

            // One canonical resource GET. Never enumerate the Pod or substitute an index.
            var resource=_privateRecords.Resource<IHolon>(id);
            using var request=new HttpRequestMessage(HttpMethod.Get,resource);
            request.Headers.Accept.ParseAdd("text/turtle");
            using var response=await _httpClient.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,cancellationToken);
            if(response.StatusCode==HttpStatusCode.NotFound)return CqlRows();
            response.EnsureSuccessStatusCode();
            if(!string.Equals(response.Content.Headers.ContentType?.MediaType,"text/turtle",StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The encrypted SOLID record must be text/turtle.");
            if(response.Content.Headers.ContentLength>CqlMaximumRdfBytes)
                return CqlFailure("CQL_QUERY_LIMIT","The RDF envelope exceeds its byte budget.");
            using var source=await response.Content.ReadAsStreamAsync(cancellationToken);
            using var body=new MemoryStream();var buffer=new byte[16384];int count;
            while((count=await source.ReadAsync(buffer,0,buffer.Length,cancellationToken))!=0){
                if(body.Length+count>CqlMaximumRdfBytes)return CqlFailure("CQL_QUERY_LIMIT","The RDF envelope exceeds its byte budget.");
                body.Write(buffer,0,count);
            }
            var holon=_privateRecords.Decode<IHolon>(new UTF8Encoding(false,true).GetString(body.ToArray()),id);
            cancellationToken.ThrowIfCancellationRequested();
            if(holon.IsDeleted||(plan.Query.ReadAccess!=null&&!plan.Query.ReadAccess.CanRead(holon)))return CqlRows();
            holon.Children=Array.Empty<IHolon>();
            holon.ProviderUniqueStorageKey??=new Dictionary<ProviderType,string>();
            holon.ProviderUniqueStorageKey[QueryProviderType]=key;
            holon.MetaData??=new Dictionary<string,object>();
            holon.MetaData[CqlRecordIdKey]=key;
            holon.MetaData["SolidRecordResource"]=resource.AbsoluteUri;
            if(response.Headers.ETag!=null)holon.MetaData["SolidSourceETag"]=response.Headers.ETag.ToString();
            else holon.MetaData.Remove("SolidSourceETag");
            return CqlRows(holon);
        }
        catch(OperationCanceledException error){return CqlFailure("CQL_QUERY_CANCELLED","The SOLID query was cancelled.",error);}
        catch(Exception error){return CqlFailure("CQL_QUERY_FAILED","The SOLID query failed; no partial rows returned.",error);}
    }

    private static OASISResult<IReadOnlyList<IHolon>> CqlRows(params IHolon[] rows)
        =>new(Array.AsReadOnly(rows)){IsLoaded=true,LoadedCount=rows.Length};
    private static OASISResult<IReadOnlyList<IHolon>> CqlFailure(string code,string message,Exception error=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=error};
}
