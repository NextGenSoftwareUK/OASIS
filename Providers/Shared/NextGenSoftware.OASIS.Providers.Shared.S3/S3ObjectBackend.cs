using System.Net;
using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.Providers.Shared.S3;

/// <summary>Official SDK transport for existing general-purpose S3 buckets.
/// The caller owns the client; record policy, encryption and history belong above this layer.
/// Conditional support on a compatible service must be verified before advertising it.</summary>
public sealed class S3ObjectBackend : IConditionalKeyValueBackend
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly IAmazonS3 _client;
    private readonly string _bucket, _prefix;

    public S3ObjectBackend(IAmazonS3 client, string bucket, string objectPrefix = "")
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentNullException.ThrowIfNull(objectPrefix);
        if (client.Config.MaxErrorRetry != 0)
            throw new ArgumentException("S3 storage requires MaxErrorRetry=0; uncertain mutations must not be replayed by the SDK.", nameof(client));
        if (objectPrefix.Length > 0 && (!objectPrefix.EndsWith('/') || objectPrefix.Split('/').Any(p => p is "." or "..")))
            throw new ArgumentException("Object prefix must end in '/' and cannot contain navigation segments.", nameof(objectPrefix));
        _client = client; _bucket = bucket; _prefix = objectPrefix;
    }

    private string Key(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var physical = _prefix + key;
        // S3's key limit includes the configured prefix and counts UTF-8 bytes,
        // not UTF-16 characters. Reject before any SDK read or mutation dispatch.
        if (Utf8.GetByteCount(physical) > 1024)
            throw new ArgumentException("S3 object key including its prefix exceeds 1024 UTF-8 bytes.", nameof(key));
        return physical;
    }
    private static void Complete(HttpStatusCode actual, HttpStatusCode expected, string operation)
    {
        if (actual != expected) throw new InvalidDataException($"S3 {operation} requires completed HTTP {(int)expected} acknowledgement.");
    }
    private static bool Missing(AmazonS3Exception e) => e.StatusCode == HttpStatusCode.NotFound && e.ErrorCode == "NoSuchKey";
    private static bool Conflict(AmazonS3Exception e, bool conditional)
        => conditional && (e.StatusCode == HttpStatusCode.PreconditionFailed ||
            Missing(e) || e.StatusCode == HttpStatusCode.Conflict && e.ErrorCode == "ConditionalRequestConflict");
    private static StorageLifecycleConflictException ConflictError(AmazonS3Exception e)
    {
        var error = new StorageLifecycleConflictException("S3 native object precondition failed.");
        error.Data["NativeCause"] = e;
        return error;
    }
    private static bool Rejected(AmazonS3Exception e) => (int)e.StatusCode >= 400 && (int)e.StatusCode < 500 && e.StatusCode != HttpStatusCode.RequestTimeout;

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        => (await ReadObservedAsync(key, cancellationToken))?.Value!;

    public async Task<KeyValueObservation?> ReadObservedAsync(string key, CancellationToken cancellationToken = default)
    {
        var physical = Key(key); cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var response = await _client.GetObjectAsync(new GetObjectRequest { BucketName = _bucket, Key = physical, ChecksumMode = ChecksumMode.ENABLED }, cancellationToken);
            Complete(response.HttpStatusCode, HttpStatusCode.OK, "GET");
            if (!string.IsNullOrEmpty(response.ContentRange) || string.IsNullOrWhiteSpace(response.ETag))
                throw new InvalidDataException("S3 GET must return a whole object and its native ETag.");
            using var reader = new StreamReader(response.ResponseStream ?? throw new InvalidDataException("S3 GET omitted its stream."), Utf8, false);
            var value = await reader.ReadToEndAsync(cancellationToken);
            if (Utf8.GetByteCount(value) != response.ContentLength)
                throw new InvalidDataException("S3 GET body length does not match the complete object acknowledgement.");
            return new KeyValueObservation(value, response.ETag);
        }
        catch (AmazonS3Exception e) when (Missing(e)) { return null; }
    }

    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        => WriteAsync(key, value, null, false, cancellationToken);
    public Task CreateOnlyAsync(string key, string value, CancellationToken cancellationToken = default)
        => WriteAsync(key, value, null, true, cancellationToken);
    public Task ReplaceUnchangedAsync(string key, string value, string revision, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);
        return WriteAsync(key, value, revision, false, cancellationToken);
    }
    private async Task WriteAsync(string key, string value, string? revision, bool create, CancellationToken token)
    {
        var physical = Key(key); ArgumentNullException.ThrowIfNull(value); token.ThrowIfCancellationRequested();
        var bytes = Utf8.GetBytes(value);
        using var stream = new MemoryStream(bytes, false);
        var checksum = Convert.ToBase64String(SHA256.HashData(bytes));
        var request = new PutObjectRequest { BucketName = _bucket, Key = physical, InputStream = stream,
            ContentType = "application/octet-stream", ChecksumSHA256 = checksum, IfMatch = revision, IfNoneMatch = create ? "*" : null };
        try
        {
            var response = await _client.PutObjectAsync(request, token);
            Complete(response.HttpStatusCode, HttpStatusCode.OK, "PUT");
            if (string.IsNullOrWhiteSpace(response.ETag) || response.ChecksumSHA256 != checksum)
                throw new InvalidDataException("S3 PUT omitted its revision or matching SHA256 acknowledgement.");
        }
        catch (AmazonS3Exception e) when (Conflict(e, create || revision != null)) { throw ConflictError(e); }
        catch (AmazonS3Exception e) when (Rejected(e)) { throw; }
        catch (Exception e) { throw new S3MutationReconciliationException(_bucket, physical, e); }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => DeleteAsync(key, null, cancellationToken);
    public Task DeleteUnchangedAsync(string key, string revision, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);
        return DeleteAsync(key, revision, cancellationToken);
    }
    private async Task DeleteAsync(string key, string? revision, CancellationToken token)
    {
        var physical = Key(key); token.ThrowIfCancellationRequested();
        try
        {
            var response = await _client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = physical, IfMatch = revision }, token);
            Complete(response.HttpStatusCode, HttpStatusCode.NoContent, "DELETE");
        }
        catch (AmazonS3Exception e) when (Conflict(e, revision != null)) { throw ConflictError(e); }
        catch (AmazonS3Exception e) when (revision == null && Missing(e)) { }
        catch (AmazonS3Exception e) when (Rejected(e)) { throw; }
        catch (Exception e) { throw new S3MutationReconciliationException(_bucket, physical, e); }
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prefix); cancellationToken.ThrowIfCancellationRequested();
        var physical = _prefix + prefix;
        var request = new ListObjectsV2Request { BucketName = _bucket, Prefix = physical };
        var keys = new List<string>(); var seen = new HashSet<string>(StringComparer.Ordinal); var tokens = new HashSet<string>(StringComparer.Ordinal);
        while (true)
        {
            var response = await _client.ListObjectsV2Async(request, cancellationToken);
            Complete(response.HttpStatusCode, HttpStatusCode.OK, "LIST");
            foreach (var item in response.S3Objects ?? throw new InvalidDataException("S3 listing omitted its object collection."))
            {
                if (string.IsNullOrWhiteSpace(item?.Key) || !item.Key.StartsWith(physical, StringComparison.Ordinal) || !seen.Add(item.Key))
                    throw new InvalidDataException("S3 listing contains an invalid, duplicate or out-of-prefix key.");
                keys.Add(item.Key[_prefix.Length..]);
            }
            if (!response.IsTruncated) return keys;
            if (string.IsNullOrEmpty(response.NextContinuationToken) || !tokens.Add(response.NextContinuationToken))
                throw new InvalidDataException("S3 truncated listing requires a new continuation token.");
            request.ContinuationToken = response.NextContinuationToken;
        }
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await _client.HeadBucketAsync(new HeadBucketRequest { BucketName = _bucket }, cancellationToken);
        Complete(response.HttpStatusCode, HttpStatusCode.OK, "HEAD bucket");
    }
}

public sealed class S3MutationReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string BucketName { get; }
    public string ObjectName { get; }
    public string OriginalTransactionId => string.Empty;
    public S3MutationReconciliationException(string bucketName, string objectName, Exception cause)
        : base("S3 mutation outcome is uncertain; reconcile the original object before replay.", cause)
    { BucketName = bucketName; ObjectName = objectName; }
}
