using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Oci.Common.Model;
using Oci.Common.Retry;
using Oci.ObjectstorageService;
using Oci.ObjectstorageService.Requests;

namespace NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS;

/// <summary>OCI's native, authenticated SDK adapter. The caller owns its client and credentials.
/// Record/query/encryption policy belongs to the shared provider layer, not this object transport.</summary>
public sealed class OracleObjectStorageBackend : IConditionalKeyValueBackend
{
    private readonly ObjectStorageClient _client;
    private readonly string _namespace;
    private readonly string _bucket;
    private readonly string _objectPrefix;
    private static readonly UTF8Encoding Utf8=new(false,true);
    private static RetryConfiguration Once()=>new(){MaxAttempts=1,RetryableErrors=new(),RetryableStatusCodeFamilies=new()};

    private static void RequireCompletedResponse(HttpResponseMessage? response,HttpStatusCode expected,string operation)
    {
        if(response?.StatusCode!=expected)
            throw new InvalidDataException($"OCI {operation} requires native HTTP {(int)expected} completion, not an asynchronous or partial acknowledgement.");
    }

    public OracleObjectStorageBackend(ObjectStorageClient client,string namespaceName,string bucketName,string objectPrefix="")
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(namespaceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
        ArgumentNullException.ThrowIfNull(objectPrefix);
        if(objectPrefix.Length>0&&(!objectPrefix.EndsWith("/",StringComparison.Ordinal)||objectPrefix.Split('/').Any(part=>part=="."||part=="..")))
            throw new ArgumentException("OCI object namespace prefix must end in '/' and cannot contain dot-navigation segments.",nameof(objectPrefix));
        _client=client;_namespace=namespaceName;_bucket=bucketName;
        _objectPrefix=objectPrefix;
    }

    public async Task<string> GetAsync(string key,CancellationToken cancellationToken=default)
        =>(await GetWithEntityTagAsync(key,cancellationToken))?.Value!;

    public async Task<KeyValueObservation?> ReadObservedAsync(string key,CancellationToken cancellationToken=default)
    {
        var observed=await GetWithEntityTagAsync(key,cancellationToken);
        return observed==null?null:new KeyValueObservation(observed.Value,observed.EntityTag);
    }

    /// <summary>Value and opaque native ETag from the SAME GET, not a later HEAD/read.</summary>
    public async Task<OracleObjectSnapshot?> GetWithEntityTagAsync(string key,CancellationToken cancellationToken=default)
    {
        ValidateKey(key);cancellationToken.ThrowIfCancellationRequested();
        try {
            var response=await _client.GetObject(new(){NamespaceName=_namespace,BucketName=_bucket,ObjectName=_objectPrefix+key},Once(),cancellationToken);
            try {
                RequireCompletedResponse(response.httpResponseMessage,HttpStatusCode.OK,"object GET");
                if(response.ContentRange is not null||response.IsNotModified)
                    throw new InvalidDataException("OCI whole-record GET cannot publish a ranged or not-modified response.");
                if(response.ContentLength is not long expectedLength||expectedLength<0)
                    throw new InvalidDataException("OCI whole-record GET requires its native object byte length.");
                using var stream=response.InputStream??throw new InvalidDataException("OCI object response has no content stream.");
                using var reader=new StreamReader(stream,Utf8,detectEncodingFromByteOrderMarks:false);
                var value=await reader.ReadToEndAsync(cancellationToken);
                if(Utf8.GetByteCount(value)!=expectedLength)
                    throw new InvalidDataException("OCI GET body length does not match its complete object acknowledgement.");
                // Multipart objects may omit Content-MD5. When supplied, it binds these exact bytes,
                // unlike the opaque ETag which must not be interpreted as a checksum.
                if(response.ContentMd5 is not null&&!string.Equals(response.ContentMd5,Convert.ToBase64String(MD5.HashData(Utf8.GetBytes(value))),StringComparison.Ordinal))
                    throw new InvalidDataException("OCI GET body does not match its declared native MD5 checksum.");
                if(string.IsNullOrWhiteSpace(response.ETag))
                    throw new InvalidDataException("OCI object GET must return its observed ETag and complete value.");
                return new OracleObjectSnapshot(value,response.ETag);
            }finally{response.httpResponseMessage?.Dispose();}
        }catch(OciException cause)when(IsMissingObject(cause)){return null;}
    }

    public Task PutAsync(string key,string value,CancellationToken cancellationToken=default)
        =>WriteAsync(key,value,null,false,cancellationToken);

    /// <summary>Native conditional writes; preconditions are never replaced by a read-then-upsert.</summary>
    public Task CreateOnlyAsync(string key,string value,CancellationToken cancellationToken=default)
        =>WriteAsync(key,value,null,true,cancellationToken);

    public Task ReplaceUnchangedAsync(string key,string value,string etag,CancellationToken cancellationToken=default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(etag);
        return WriteAsync(key,value,etag,false,cancellationToken);
    }

    private async Task WriteAsync(string key,string value,string? etag,bool createOnly,CancellationToken token)
    {
        ValidateKey(key);ArgumentNullException.ThrowIfNull(value);token.ThrowIfCancellationRequested();
        var bytes=Utf8.GetBytes(value);
        using var stream=new MemoryStream(bytes,writable:false);
        var request=new PutObjectRequest {NamespaceName=_namespace,BucketName=_bucket,ObjectName=_objectPrefix+key,PutObjectBody=stream,
            ContentLength=bytes.LongLength,ContentType="application/octet-stream",ContentMD5=Convert.ToBase64String(MD5.HashData(bytes)),
            IfMatch=etag,IfNoneMatch=createOnly?"*":null};
        try {
            var response=await _client.PutObject(request,Once(),token);
            try{
                // A 2xx with a malformed acknowledgement may already have stored the object.
                // ETag is an opaque revision, not an MD5; compare only the server's MD5 header.
                RequireCompletedResponse(response.httpResponseMessage,HttpStatusCode.OK,"object PUT");
                if(string.IsNullOrWhiteSpace(response.ETag)||!string.Equals(response.OpcContentMd5,request.ContentMD5,StringComparison.Ordinal))
                    throw new InvalidDataException("Oracle upload acknowledgement omitted its revision or did not match the request checksum.");
            }finally{response.httpResponseMessage?.Dispose();}
        }catch(OciException cause)when(cause.StatusCode==HttpStatusCode.PreconditionFailed||(etag!=null&&IsMissingObject(cause))){
            throw PreconditionConflict(cause);
        }catch(OciException cause)when((int)cause.StatusCode>=400&&(int)cause.StatusCode<500&&cause.StatusCode!=HttpStatusCode.RequestTimeout){throw;}
        catch(Exception cause){throw new OracleObjectMutationReconciliationException(_objectPrefix+key,cause);}
    }

    public Task DeleteAsync(string key,CancellationToken cancellationToken=default)
        =>DeleteObservedAsync(key,null,cancellationToken);

    public Task DeleteUnchangedAsync(string key,string etag,CancellationToken cancellationToken=default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(etag);
        return DeleteObservedAsync(key,etag,cancellationToken);
    }

    private async Task DeleteObservedAsync(string key,string? etag,CancellationToken cancellationToken)
    {
        ValidateKey(key);cancellationToken.ThrowIfCancellationRequested();
        try {
            var response=await _client.DeleteObject(new(){NamespaceName=_namespace,BucketName=_bucket,ObjectName=_objectPrefix+key,IfMatch=etag},Once(),cancellationToken);
            try{RequireCompletedResponse(response.httpResponseMessage,HttpStatusCode.NoContent,"object DELETE");}
            finally{response.httpResponseMessage?.Dispose();}
        }catch(OciException cause)when(cause.StatusCode==HttpStatusCode.PreconditionFailed||(etag!=null&&IsMissingObject(cause))){
            throw PreconditionConflict(cause);
        }catch(OciException cause)when(etag==null&&IsMissingObject(cause)){}
        catch(OciException cause)when((int)cause.StatusCode>=400&&(int)cause.StatusCode<500&&cause.StatusCode!=HttpStatusCode.RequestTimeout){throw;}
        catch(Exception cause){throw new OracleObjectMutationReconciliationException(_objectPrefix+key,cause);}
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(prefix);cancellationToken.ThrowIfCancellationRequested();
        var keys=new List<string>();var seen=new HashSet<string>(StringComparer.Ordinal);
        var cursors=new HashSet<string>(StringComparer.Ordinal);string? start=null;
        var physicalPrefix=_objectPrefix+prefix;
        do {
            cancellationToken.ThrowIfCancellationRequested();
            var response=await _client.ListObjects(new(){NamespaceName=_namespace,BucketName=_bucket,Prefix=physicalPrefix,Start=start},Once(),cancellationToken);
            try {
                RequireCompletedResponse(response.httpResponseMessage,HttpStatusCode.OK,"object LIST");
                var page=response.ListObjects??throw new InvalidDataException("OCI object listing is missing its payload.");
                foreach(var item in page.Objects??throw new InvalidDataException("OCI object listing is missing its objects.")) {
                    if(string.IsNullOrWhiteSpace(item?.Name)||!item.Name.StartsWith(physicalPrefix,StringComparison.Ordinal)||!seen.Add(item.Name))
                        throw new InvalidDataException("OCI listing contains an invalid, duplicate or out-of-prefix object.");
                    keys.Add(item.Name[_objectPrefix.Length..]);
                }
                start=page.NextStartWith;
                if(!string.IsNullOrEmpty(start)&&!cursors.Add(start))throw new InvalidDataException("OCI object listing repeated its continuation cursor.");
            }finally{response.httpResponseMessage?.Dispose();}
        }while(!string.IsNullOrEmpty(start));
        return keys;
    }

    public async Task VerifyAsync(CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response=await _client.HeadBucket(new(){NamespaceName=_namespace,BucketName=_bucket},Once(),cancellationToken);
        try{RequireCompletedResponse(response.httpResponseMessage,HttpStatusCode.OK,"bucket HEAD");}
        finally{response.httpResponseMessage?.Dispose();}
    }

    private static bool IsMissingObject(OciException cause)=>cause.StatusCode==HttpStatusCode.NotFound&&cause.ServiceCode=="ObjectNotFound";
    private static StorageLifecycleConflictException PreconditionConflict(OciException cause)
    {
        var conflict=new StorageLifecycleConflictException("OCI object mutation precondition failed.");
        conflict.Data["NativeCause"]=cause;return conflict;
    }
    private static void ValidateKey(string key)=>ArgumentException.ThrowIfNullOrWhiteSpace(key);
}

public sealed record OracleObjectSnapshot(string Value,string EntityTag);

public sealed class OracleObjectMutationReconciliationException : Exception,INativeMutationReconciliationRequired
{
    public string ObjectName{get;}
    public string OriginalTransactionId=>string.Empty;
    public OracleObjectMutationReconciliationException(string objectName,Exception cause)
        :base("OCI object mutation outcome is uncertain; reconcile the original object before replay.",cause)=>ObjectName=objectName;
}
