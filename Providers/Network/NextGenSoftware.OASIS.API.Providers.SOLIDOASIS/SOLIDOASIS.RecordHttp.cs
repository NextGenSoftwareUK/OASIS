using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

public sealed class SolidRecordReconciliationException : IOException
{
    public Uri Resource {get;}
    public bool WriteAcknowledged {get;}
    public SolidRecordReconciliationException(Uri resource,bool acknowledged,Exception cause)
        :base("SOLID record write may have applied; reconcile the original resource before retrying.",cause)
        {Resource=resource;WriteAcknowledged=acknowledged;}
}

public partial class SOLIDOASIS
{
    private readonly SolidPrivateRecordCodec _privateRecords;
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
