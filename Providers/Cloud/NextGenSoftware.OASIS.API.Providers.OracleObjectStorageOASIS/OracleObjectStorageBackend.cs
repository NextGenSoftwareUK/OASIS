using System.Net;
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
public sealed class OracleObjectStorageBackend : IKeyValueBackend
{
    private readonly ObjectStorageClient _client;
    private readonly string _namespace;
    private readonly string _bucket;
    private static readonly UTF8Encoding Utf8=new(false,true);
    private static RetryConfiguration Once()=>new(){MaxAttempts=1,RetryableErrors=new(),RetryableStatusCodeFamilies=new()};

    public OracleObjectStorageBackend(ObjectStorageClient client,string namespaceName,string bucketName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(namespaceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucketName);
        _client=client;_namespace=namespaceName;_bucket=bucketName;
    }

    public async Task<string> GetAsync(string key,CancellationToken cancellationToken=default)
    {
        ValidateKey(key);cancellationToken.ThrowIfCancellationRequested();
        try {
            var response=await _client.GetObject(new(){NamespaceName=_namespace,BucketName=_bucket,ObjectName=key},Once(),cancellationToken);
            try {
                using var stream=response.InputStream??throw new InvalidDataException("OCI object response has no content stream.");
                using var reader=new StreamReader(stream,Utf8,detectEncodingFromByteOrderMarks:false);
                return await reader.ReadToEndAsync(cancellationToken);
            }finally{response.httpResponseMessage?.Dispose();}
        }catch(OciException cause)when(IsMissingObject(cause)){return null!;}
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
        var request=new PutObjectRequest {NamespaceName=_namespace,BucketName=_bucket,ObjectName=key,PutObjectBody=stream,
            ContentLength=bytes.LongLength,ContentType="application/octet-stream",ContentMD5=Convert.ToBase64String(MD5.HashData(bytes)),
            IfMatch=etag,IfNoneMatch=createOnly?"*":null};
        try {
            var response=await _client.PutObject(request,Once(),token);
            response.httpResponseMessage?.Dispose();
        }catch(OciException cause)when(cause.StatusCode==HttpStatusCode.PreconditionFailed){
            var conflict=new StorageLifecycleConflictException("OCI object write precondition failed.");
            conflict.Data["NativeCause"]=cause;throw conflict;
        }catch(OciException cause)when((int)cause.StatusCode>=400&&(int)cause.StatusCode<500&&cause.StatusCode!=HttpStatusCode.RequestTimeout){throw;}
        catch(Exception cause){throw new OracleObjectMutationReconciliationException(key,cause);}
    }

    public async Task DeleteAsync(string key,CancellationToken cancellationToken=default)
    {
        ValidateKey(key);cancellationToken.ThrowIfCancellationRequested();
        try {
            var response=await _client.DeleteObject(new(){NamespaceName=_namespace,BucketName=_bucket,ObjectName=key},Once(),cancellationToken);
            response.httpResponseMessage?.Dispose();
        }catch(OciException cause)when(IsMissingObject(cause)){}
        catch(OciException cause)when((int)cause.StatusCode>=400&&(int)cause.StatusCode<500&&cause.StatusCode!=HttpStatusCode.RequestTimeout){throw;}
        catch(Exception cause){throw new OracleObjectMutationReconciliationException(key,cause);}
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(prefix);cancellationToken.ThrowIfCancellationRequested();
        var keys=new List<string>();var seen=new HashSet<string>(StringComparer.Ordinal);
        var cursors=new HashSet<string>(StringComparer.Ordinal);string? start=null;
        do {
            cancellationToken.ThrowIfCancellationRequested();
            var response=await _client.ListObjects(new(){NamespaceName=_namespace,BucketName=_bucket,Prefix=prefix,Start=start},Once(),cancellationToken);
            try {
                var page=response.ListObjects??throw new InvalidDataException("OCI object listing is missing its payload.");
                foreach(var item in page.Objects??throw new InvalidDataException("OCI object listing is missing its objects.")) {
                    if(string.IsNullOrWhiteSpace(item?.Name)||!item.Name.StartsWith(prefix,StringComparison.Ordinal)||!seen.Add(item.Name))
                        throw new InvalidDataException("OCI listing contains an invalid, duplicate or out-of-prefix object.");
                    keys.Add(item.Name);
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
        response.httpResponseMessage?.Dispose();
    }

    private static bool IsMissingObject(OciException cause)=>cause.StatusCode==HttpStatusCode.NotFound&&cause.ServiceCode=="ObjectNotFound";
    private static void ValidateKey(string key)=>ArgumentException.ThrowIfNullOrWhiteSpace(key);
}

public sealed class OracleObjectMutationReconciliationException : Exception,INativeMutationReconciliationRequired
{
    public string ObjectName{get;}
    public string OriginalTransactionId=>string.Empty;
    public OracleObjectMutationReconciliationException(string objectName,Exception cause)
        :base("OCI object mutation outcome is uncertain; reconcile the original object before replay.",cause)=>ObjectName=objectName;
}
