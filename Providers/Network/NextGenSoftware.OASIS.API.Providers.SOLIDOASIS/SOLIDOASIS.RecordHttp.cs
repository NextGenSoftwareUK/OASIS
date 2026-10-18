using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

public sealed class SolidRecordReconciliationException : IOException,INativeMutationReconciliationRequired
{
    public NextGenSoftware.OASIS.API.Core.Enums.ProviderType ProviderType=>NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SOLIDOASIS;
    public string OriginalTransactionId=>string.Empty;
    public Uri Resource {get;}
    public bool WriteAcknowledged {get;}
    public SolidRecordReconciliationException(Uri resource,bool acknowledged,Exception cause)
        :base("SOLID record write may have applied; reconcile the original resource before retrying.",cause)
        {Resource=resource;WriteAcknowledged=acknowledged;}
}

public partial class SOLIDOASIS
{
    private readonly SolidPrivateRecordCodec _privateRecords;
    private async Task<OASISResult<T>> LoadPrivateRecordByKeyAsync<T>(string key,int version) where T:class,IHolonBase
    {
        if(Guid.TryParse(key,out var id)&&id!=Guid.Empty)return await LoadPrivateRecordAsync<T>(id,version);
        var invalid=new OASISResult<T>();
        OASISErrorHandling.HandleError(ref invalid,"SOLID provider key must be a nonempty record GUID.",new ArgumentException("Invalid provider key.",nameof(key)));
        return invalid;
    }
    private async Task<OASISResult<T>> FindPrivateRecordAsync<T>(string query,Func<T,string> selector,int version) where T:class,IHolonBase
    {
        var result=new OASISResult<T>();
        try{
            if(string.IsNullOrWhiteSpace(query))throw new ArgumentException("A nonblank private identity lookup is required.",nameof(query));
            var selected=query.Trim();
            var records=await LoadPrivateRecordsAsync<T>(version);
            if(records.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>,T>(records);
            var matches=records.Result.Where(record=>string.Equals(selector(record)?.Trim(),selected,StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
            if(matches.Length==0){
                OASISErrorHandling.HandleError(ref result,"No matching private record.",new KeyNotFoundException("Private identity not found."));
                result.ErrorCode=OASISResultErrorCodes.RecordNotFound;return result;
            }
            if(matches.Length!=1)throw new InvalidDataException("Multiple private records match the identity; refusing arbitrary selection.");
            result.Result=matches[0];
        }catch(Exception error){OASISErrorHandling.HandleError(ref result,"SOLID private identity lookup failed.",error);}
        return result;
    }
    private void RequirePrivateStorage()
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        if(_privateRecords==null)throw new InvalidOperationException("An externally persisted storage key is required for private SOLID record I/O.");
        if(!_isActivated)throw new InvalidOperationException("Provider not activated.");
    }
    private async Task<T> ReadPrivateRecordAsync<T>(Guid id) where T:class,IHolonBase
    {
        var resource=_privateRecords.Resource<T>(id);
        using var request=new HttpRequestMessage(HttpMethod.Get,resource);
        request.Headers.Accept.ParseAdd("text/turtle");
        using var response=await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        if(!string.Equals(response.Content.Headers.ContentType?.MediaType,"text/turtle",StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("SOLID private record response must be text/turtle.");
        return _privateRecords.Decode<T>(await response.Content.ReadAsStringAsync(),id);
    }
    private async Task<OASISResult<T>> LoadPrivateRecordAsync<T>(Guid id,int version) where T:class,IHolonBase
    {
        var result=new OASISResult<T>();
        try{
            RequirePrivateStorage();
            if(version!=0)throw new NotSupportedException("Explicit SOLID record versions require a verified history implementation; current-only reads must request version zero.");
            result.Result=await ReadPrivateRecordAsync<T>(id);
        }catch(Exception error){OASISErrorHandling.HandleError(ref result,"SOLID private record read failed.",error);}
        return result;
    }
    private async Task<OASISResult<IEnumerable<T>>> LoadPrivateRecordsAsync<T>(int version) where T:class,IHolonBase
    {
        var result=new OASISResult<IEnumerable<T>>();
        try{
            RequirePrivateStorage();
            if(version!=0)throw new NotSupportedException("Explicit SOLID collection versions require verified record history.");
            var container=_privateRecords.Container<T>();
            using var request=new HttpRequestMessage(HttpMethod.Get,container);
            request.Headers.Accept.ParseAdd("text/turtle");
            using var response=await _httpClient.SendAsync(request);response.EnsureSuccessStatusCode();
            if(!string.Equals(response.Content.Headers.ContentType?.MediaType,"text/turtle",StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("SOLID container response must be text/turtle.");
            // Paging is not silently interpreted as an exhaustive collection.
            if(response.Headers.TryGetValues("Link",out var links)&&links.Any(link=>System.Text.RegularExpressions.Regex.IsMatch(link,@"\brel\s*=\s*(?:""[^""]*\bnext\b[^""]*""|'[^']*\bnext\b[^']*'|next\b)",System.Text.RegularExpressions.RegexOptions.IgnoreCase)))
                throw new NotSupportedException("Paged SOLID containers require verified pagination before exhaustive reads.");
            using var graph=new Graph{BaseUri=container};
            using var input=new StringReader(await response.Content.ReadAsStringAsync());new TurtleParser().Load(graph,input);
            var subject=graph.CreateUriNode(container);
            var rdfType=graph.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));
            if(graph.Triples.Any(triple=>triple.Object is IUriNode node&&node.Uri.AbsoluteUri=="http://www.w3.org/ns/ldp#Page"))
                throw new NotSupportedException("LDP page documents cannot be treated as exhaustive containers.");
            if(!graph.GetTriplesWithSubjectPredicate(subject,rdfType).Any(triple=>triple.Object is IUriNode node&&
                node.Uri.AbsoluteUri is "http://www.w3.org/ns/ldp#BasicContainer" or "http://www.w3.org/ns/ldp#Container"))
                throw new InvalidDataException("Expected native LDP container type at the requested resource.");
            var ids=new HashSet<Guid>();
            foreach(var triple in graph.GetTriplesWithSubjectPredicate(subject,graph.CreateUriNode(new Uri("http://www.w3.org/ns/ldp#contains")))){
                if(triple.Object is not IUriNode member)throw new InvalidDataException("LDP containment must reference a resource URI.");
                var filename=Path.GetFileNameWithoutExtension(member.Uri.AbsolutePath);
                if(!Guid.TryParseExact(filename,"D",out var id)||id==Guid.Empty||!member.Uri.Equals(_privateRecords.Resource<T>(id)))
                    throw new InvalidDataException("LDP member is outside the exact canonical record namespace; no request sent to that member.");
                ids.Add(id);if(ids.Count>10000)throw new InvalidDataException("SOLID collection exceeds the 10000-record safety bound; no partial result returned.");
            }
            var records=new List<T>();
            foreach(var id in ids.OrderBy(value=>value))records.Add(await ReadPrivateRecordAsync<T>(id));
            result.Result=records;
        }catch(Exception error){OASISErrorHandling.HandleError(ref result,"SOLID private collection read failed.",error);}
        return result;
    }
    private async Task<OASISResult<T>> SavePrivateRecordAsync<T>(T source) where T:class,IHolonBase
    {
        var result=new OASISResult<T>();
        try{
            RequirePrivateStorage();ArgumentNullException.ThrowIfNull(source);
            var staged=StoredRecordJson.DeserializeStored<T>(StoredRecordJson.Serialize(source));
            if(staged.Id==Guid.Empty)staged.Id=Guid.NewGuid();
            var resource=_privateRecords.Resource<T>(staged.Id);
            using var content=new StringContent(_privateRecords.Encode(staged),Encoding.UTF8,"text/turtle");
            bool acknowledged=false;
            try{
                using var response=await _httpClient.PutAsync(resource,content);
                if(!response.IsSuccessStatusCode){
                    if((int)response.StatusCode>=500||(int)response.StatusCode==408)
                        throw new SolidRecordReconciliationException(resource,false,new HttpRequestException("Uncertain SOLID PUT status: "+(int)response.StatusCode));
                    response.EnsureSuccessStatusCode();
                }
                acknowledged=true;
                var actual=await ReadPrivateRecordAsync<T>(staged.Id);
                if(!StoredRecordPayloadEquality.Equivalent(JToken.Parse(StoredRecordJson.Serialize(staged)),JToken.Parse(StoredRecordJson.Serialize(actual))))
                    throw new InvalidDataException("SOLID readback differs from the complete canonical record written.");
                source.Id=staged.Id;result.Result=source;
            }catch(SolidRecordReconciliationException){throw;}
            catch(HttpRequestException error)when(!acknowledged&&error.StatusCode.HasValue){throw;}
            catch(Exception error){throw new SolidRecordReconciliationException(resource,acknowledged,error);}
        }catch(Exception error){OASISErrorHandling.HandleError(ref result,"SOLID private record write failed.",error);}
        return result;
    }
}
