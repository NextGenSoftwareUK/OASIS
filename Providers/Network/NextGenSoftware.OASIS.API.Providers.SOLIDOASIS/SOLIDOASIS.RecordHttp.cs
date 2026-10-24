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
    private async Task<OASISResult<bool>> DeleteAvatarByIdentityAsync(string identity,bool email,bool softDelete)
    {
        var found=await FindPrivateRecordAsync<IAvatar>(identity,record=>email?record.Email:record.Username,0,includeDeleted:true);
        if(found.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,bool>(found);
        if(found.Result!=null)return await DeletePrivateAvatarAsync(found.Result.Id,softDelete,record=>string.Equals((email?record.Email:record.Username)?.Trim(),identity.Trim(),StringComparison.OrdinalIgnoreCase));
        var result=new OASISResult<bool>();
        OASISErrorHandling.HandleError(ref result,"SOLID avatar lookup returned no record; refusing deletion.",new InvalidDataException("Missing identity result."));
        return result;
    }
    private static Guid ParsePrivateRecordKey(string key)
    {
        if(Guid.TryParse(key,out var id)&&id!=Guid.Empty)return id;
        throw new ArgumentException("SOLID provider key must be a nonempty record GUID.",nameof(key));
    }
    private async Task<OASISResult<T>> LoadPrivateRecordByKeyAsync<T>(string key,int version) where T:class,IHolonBase
    {
        var invalid=new OASISResult<T>();
        try{return await LoadPrivateRecordAsync<T>(ParsePrivateRecordKey(key),version);}
        catch(Exception error){OASISErrorHandling.HandleError(ref invalid,"SOLID provider key read failed.",error);}
        return invalid;
    }
    private async Task<OASISResult<T>> FindPrivateRecordAsync<T>(string query,Func<T,string> selector,int version,bool includeDeleted=false) where T:class,IHolonBase
    {
        return await StoredRecordIdentityLookup.SelectAsync(query,version,async selectedVersion=>{
            var loaded=await LoadPrivateRecordsAsync<T>(selectedVersion);
            if(!includeDeleted&&!loaded.IsError&&loaded.Result!=null)
                loaded.Result=loaded.Result.Where(record=>record==null||record.DeletedDate==DateTime.MinValue).ToArray();
            return loaded;
        },selector);
    }
    private void RequirePrivateStorage()
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        if(_privateRecords==null)throw new InvalidOperationException("An externally persisted storage key is required for private SOLID record I/O.");
        if(!_isActivated)throw new InvalidOperationException("Provider not activated.");
    }
    private async Task<T> ReadPrivateRecordAsync<T>(Guid id) where T:class,IHolonBase
        => (await ReadPrivateRecordStateAsync<T>(id)).Record;
    private async Task<(T Record,System.Net.Http.Headers.EntityTagHeaderValue ETag,string Turtle)> ReadPrivateRecordStateAsync<T>(Guid id) where T:class,IHolonBase
    {
        var resource=_privateRecords.Resource<T>(id);
        using var request=new HttpRequestMessage(HttpMethod.Get,resource);
        request.Headers.Accept.ParseAdd("text/turtle");
        using var response=await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        if(!string.Equals(response.Content.Headers.ContentType?.MediaType,"text/turtle",StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("SOLID private record response must be text/turtle.");
        var turtle=await response.Content.ReadAsStringAsync();
        return (_privateRecords.Decode<T>(turtle,id),response.Headers.ETag,turtle);
    }
    private async Task<OASISResult<IHolon>> DeletePrivateHolonAsync(string key)
    {
        var result=new OASISResult<IHolon>();
        try{return await DeletePrivateHolonAsync(ParsePrivateRecordKey(key));}
        catch(Exception error){OASISErrorHandling.HandleError(ref result,"Invalid SOLID deletion key.",error);return result;}
    }
    private async Task<OASISResult<IHolon>> DeletePrivateHolonAsync(Guid id)
    {
        var result=new OASISResult<IHolon>();
        try{
            RequirePrivateStorage();
            var state=await ReadPrivateRecordStateAsync<IHolon>(id);
            await DeletePrivateResourceAsync(_privateRecords.Resource<IHolon>(id),state.ETag);
            result.Result=state.Record;result.IsDeleted=true;
        }catch(Exception error){
            OASISErrorHandling.HandleError(ref result,"SOLID conditional holon deletion failed.",error);
            if(error is HttpRequestException http&&http.StatusCode==System.Net.HttpStatusCode.NotFound)result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
        }
        return result;
    }
    private static void RequireStrongDeleteTag(System.Net.Http.Headers.EntityTagHeaderValue tag)
    {
        if(tag==null||tag.IsWeak)throw new InvalidDataException("A strong resource ETag is required for conditional SOLID deletion.");
    }
    private async Task DeletePrivateResourceAsync(Uri resource,System.Net.Http.Headers.EntityTagHeaderValue tag)
    {
            RequireStrongDeleteTag(tag);
            bool acknowledged=false;
            try{
                using var request=new HttpRequestMessage(HttpMethod.Delete,resource);
                request.Headers.IfMatch.Add(tag);
                using var response=await _httpClient.SendAsync(request);
                if((int)response.StatusCode>=500||(int)response.StatusCode==408)
                    throw new SolidRecordReconciliationException(resource,false,new HttpRequestException("Uncertain SOLID DELETE status: "+(int)response.StatusCode));
                response.EnsureSuccessStatusCode();acknowledged=true;
                using var verification=await _httpClient.GetAsync(resource);
                if(verification.StatusCode!=System.Net.HttpStatusCode.NotFound)
                    throw new InvalidDataException("SOLID deletion has not been independently confirmed absent.");
            }catch(SolidRecordReconciliationException){throw;}
            catch(HttpRequestException error)when(!acknowledged&&error.StatusCode.HasValue){throw;}
            catch(Exception error){throw new SolidRecordReconciliationException(resource,acknowledged,error);}
    }
    private async Task<OASISResult<bool>> DeletePrivateAvatarAsync(Guid id,bool softDelete,Func<IAvatar,bool> selectedIdentity=null)
    {
        var result=new OASISResult<bool>();var acknowledged=new List<string>();
        try{
            RequirePrivateStorage();
            var avatar=await ReadPrivateRecordStateAsync<IAvatar>(id);
            RequireStrongDeleteTag(avatar.ETag);
            if(selectedIdentity!=null&&!selectedIdentity(avatar.Record))throw new StorageLifecycleConflictException("SOLID avatar identity changed before deletion.");
            (IAvatarDetail Record,System.Net.Http.Headers.EntityTagHeaderValue ETag,string Turtle)? detail=null;
            try{detail=await ReadPrivateRecordStateAsync<IAvatarDetail>(id);}
            // The established provider contract permits an avatar without a detail record.
            catch(HttpRequestException error)when(error.StatusCode==System.Net.HttpStatusCode.NotFound){}
            if(detail.HasValue)RequireStrongDeleteTag(detail.Value.ETag);
            // Preflight both resources before any mutation; two LDP resources are not an atomic transaction.
            if(softDelete){
                var deletedAt=DateTime.UtcNow;
                avatar.Record.IsActive=false;avatar.Record.DeletedDate=deletedAt;
                var saved=await SavePrivateRecordAsync(avatar.Record,avatar.ETag,expectedTurtle:avatar.Turtle);
                if(saved.IsError)throw saved.Exception??new InvalidDataException(saved.Message);
                acknowledged.Add(_privateRecords.Resource<IAvatar>(id).AbsoluteUri);
                if(detail.HasValue){
                    detail.Value.Record.IsActive=false;detail.Value.Record.DeletedDate=deletedAt;
                    var savedDetail=await SavePrivateRecordAsync(detail.Value.Record,detail.Value.ETag,expectedTurtle:detail.Value.Turtle);
                    if(savedDetail.IsError)throw savedDetail.Exception??new InvalidDataException(savedDetail.Message);
                    acknowledged.Add(_privateRecords.Resource<IAvatarDetail>(id).AbsoluteUri);
                }
                result.Result=true;result.IsDeleted=true;return result;
            }
            if(detail.HasValue){
                var resource=_privateRecords.Resource<IAvatarDetail>(id);
                await DeletePrivateResourceAsync(resource,detail.Value.ETag);acknowledged.Add(resource.AbsoluteUri);
            }
            var avatarResource=_privateRecords.Resource<IAvatar>(id);
            await DeletePrivateResourceAsync(avatarResource,avatar.ETag);acknowledged.Add(avatarResource.AbsoluteUri);
            result.Result=true;result.IsDeleted=true;
        }catch(Exception error){
            var cause=acknowledged.Count>0?new KeyValuePartialMutationException(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SOLIDOASIS,acknowledged,error):error;
            OASISErrorHandling.HandleError(ref result,"SOLID avatar deletion failed.",cause);
            if(acknowledged.Count==0&&error is HttpRequestException http&&http.StatusCode==System.Net.HttpStatusCode.NotFound)result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
        }
        return result;
    }
    private async Task<OASISResult<T>> LoadPrivateRecordAsync<T>(Guid id,int version) where T:class,IHolonBase
    {
        var result=new OASISResult<T>();
        try{
            RequirePrivateStorage();
            if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
            var state=await ReadPrivateRecordStateAsync<T>(id);
            result.Result=version==0?state.Record:await _privateRecords.DecodeVersionAsync<T>(state.Turtle,id,version);
            if(result.Result==null){result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Requested SOLID record version was not found.";}
            else{result.IsLoaded=true;result.LoadedCount=1;}
        }catch(Exception error){
            OASISErrorHandling.HandleError(ref result,"SOLID private record read failed.",error);
            if(error is HttpRequestException http&&http.StatusCode==System.Net.HttpStatusCode.NotFound)result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
        }
        return result;
    }
    private async Task<OASISResult<IEnumerable<T>>> LoadPrivateRecordsAsync<T>(int version,bool allVersions=false) where T:class,IHolonBase
    {
        var result=new OASISResult<IEnumerable<T>>();
        try{
            RequirePrivateStorage();
            if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
            var container=_privateRecords.Container<T>();
            using var graph=await ReadPrivateContainerPagesAsync(container);
            var subject=graph.CreateUriNode(container);
            var rdfType=graph.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));
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
            foreach(var id in ids.OrderBy(value=>value)){
                var state=await ReadPrivateRecordStateAsync<T>(id);
                if(allVersions){records.AddRange(_privateRecords.DecodeAllVersions<T>(state.Turtle,id));continue;}
                var record=version==0?state.Record:await _privateRecords.DecodeVersionAsync<T>(state.Turtle,id,version);
                if(record!=null)records.Add(record);
            }
            result.Result=records;result.IsLoaded=true;result.LoadedCount=records.Count;
        }catch(Exception error){OASISErrorHandling.HandleError(ref result,"SOLID private collection read failed.",error);}
        return result;
    }
    private async Task<OASISResult<T>> SavePrivateRecordAsync<T>(T source,System.Net.Http.Headers.EntityTagHeaderValue expectedTag=null,bool createOnly=false,string expectedTurtle=null,
        PlannedHolonWrite plannedHolon=null) where T:class,IHolonBase
    {
        var result=new OASISResult<T>();
        try{
            RequirePrivateStorage();ArgumentNullException.ThrowIfNull(source);
            var historyEnabled=IsVersionControlEnabled;
            var publication=StoredRecordPublicationSnapshot<T>.Capture(source);
            var staged=publication.Proposal;
            if(staged.Id==Guid.Empty){staged.Id=Guid.NewGuid();createOnly=true;}
            staged.ProviderUniqueStorageKey??=new Dictionary<NextGenSoftware.OASIS.API.Core.Enums.ProviderType,string>();
            staged.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SOLIDOASIS]=staged.Id.ToString();
            staged.IsNewHolon=false;
            if(createOnly&&expectedTag!=null)throw new ArgumentException("Create-only SOLID writes cannot also replace an expected revision.");
            if(!createOnly&&expectedTurtle==null){
                try{
                    var observed=await ReadPrivateRecordStateAsync<T>(staged.Id);
                    RequireStrongDeleteTag(observed.ETag);
                    if(expectedTag!=null&&!expectedTag.Equals(observed.ETag))throw new StorageLifecycleConflictException("SOLID resource changed since its original observation.");
                    expectedTag=observed.ETag;expectedTurtle=observed.Turtle;
                }catch(HttpRequestException missing)when(expectedTag==null&&missing.StatusCode==System.Net.HttpStatusCode.NotFound){createOnly=true;}
            }
            var resource=_privateRecords.Resource<T>(staged.Id);
            if(typeof(T)==typeof(IHolon)&&staged is Core.Holons.Holon proposed)
            {
                var observed=expectedTurtle==null ? null:(Core.Holons.Holon)_privateRecords.Decode<IHolon>(expectedTurtle,staged.Id);
                proposed.ChildRoleManifestJson=HolonChildRolePersistence.Resolve(
                    plannedHolon??new PlannedHolonWrite((IHolon)source,proposed),HolonChildRoleWriteKind.SourceSave,
                    expectedTurtle!=null,observed?.ChildRoleManifestJson);
            }
            string turtle;
            if(historyEnabled||(expectedTurtle!=null&&_privateRecords.HasHistory<T>(expectedTurtle,staged.Id))){
                var versioned=await _privateRecords.StageVersionAsync(staged,expectedTurtle);staged=versioned.Record;turtle=versioned.Turtle;
            }else turtle=_privateRecords.Encode(staged);
            using var content=new StringContent(turtle,Encoding.UTF8,"text/turtle");
            publication.ValidateUnchanged();
            bool acknowledged=false;
            try{
                using var request=new HttpRequestMessage(HttpMethod.Put,resource){Content=content};
                if(expectedTag!=null){RequireStrongDeleteTag(expectedTag);request.Headers.IfMatch.Add(expectedTag);}
                if(createOnly)request.Headers.IfNoneMatch.Add(System.Net.Http.Headers.EntityTagHeaderValue.Any);
                using var response=await _httpClient.SendAsync(request);
                if(!response.IsSuccessStatusCode){
                    if((int)response.StatusCode>=500||(int)response.StatusCode==408)
                        throw new SolidRecordReconciliationException(resource,false,new HttpRequestException("Uncertain SOLID PUT status: "+(int)response.StatusCode));
                    response.EnsureSuccessStatusCode();
                }
                acknowledged=true;
                var actual=await ReadPrivateRecordStateAsync<T>(staged.Id);
                if(!_privateRecords.PayloadEquals<T>(turtle,actual.Turtle,staged.Id))
                    throw new InvalidDataException("SOLID readback differs from the complete canonical record written.");
                publication.ValidateUnchanged();
                source.Id=staged.Id;source.ProviderUniqueStorageKey=new Dictionary<NextGenSoftware.OASIS.API.Core.Enums.ProviderType,string>(staged.ProviderUniqueStorageKey);
                source.Version=staged.Version;source.VersionId=staged.VersionId;source.PreviousVersionId=staged.PreviousVersionId;
                if(typeof(T)==typeof(IHolon)&&source is Core.Holons.Holon original&&staged is Core.Holons.Holon committed)
                    original.ChildRoleManifestJson=committed.ChildRoleManifestJson;
                source.IsNewHolon=false;result.Result=source;result.IsSaved=true;result.SavedCount=1;
            }catch(SolidRecordReconciliationException){throw;}
            catch(HttpRequestException error)when(!acknowledged&&error.StatusCode.HasValue){throw;}
            catch(Exception error){throw new SolidRecordReconciliationException(resource,acknowledged,error);}
        }catch(Exception error){OASISErrorHandling.HandleError(ref result,"SOLID private record write failed.",error);}
        return result;
    }
}
