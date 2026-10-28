using System.Net;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
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
    private readonly string[] _legacyPrefixes;
    private readonly object _layoutLock = new();
    private Task? _layoutCheck;

    // AWSSDK.S3 3.7.502 keeps service presence separately from its public bool.
    // Core's IsPropertySet explicitly excludes S3. Bind to the SDK's own exact
    // presence method, not an inferred default or a second XML parser. This
    // version-coupled contract must fail visibly if a future SDK removes it.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "IsSetIsTruncated")]
    private static extern bool HasNativeCompletionFlag(ListObjectsV2Response response);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "IsSetKeyCount")]
    private static extern bool HasNativeKeyCount(ListObjectsV2Response response);

    public S3ObjectBackend(IAmazonS3 client, string bucket, string objectPrefix = "")
        : this(client, bucket, objectPrefix, Array.Empty<string>()) { }

    public S3ObjectBackend(IAmazonS3 client, string bucket, string objectPrefix, IReadOnlyList<string> legacyPrefixes)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentNullException.ThrowIfNull(objectPrefix);
        ArgumentNullException.ThrowIfNull(legacyPrefixes);
        if (legacyPrefixes.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Legacy layout prefixes must be explicit nonempty keys.", nameof(legacyPrefixes));
        if (client.Config.MaxErrorRetry != 0)
            throw new ArgumentException("S3 storage requires MaxErrorRetry=0; uncertain mutations must not be replayed by the SDK.", nameof(client));
        if (objectPrefix.Length > 0 && (!objectPrefix.EndsWith('/') || objectPrefix.Split('/').Any(p => p is "." or "..")))
            throw new ArgumentException("Object prefix must end in '/' and cannot contain navigation segments.", nameof(objectPrefix));
        _client = client; _bucket = bucket; _prefix = objectPrefix;
        _legacyPrefixes = legacyPrefixes.ToArray();
    }

    // Every transport entry point shares this read-only check, including callers
    // that dispatch a storage operation without first activating the provider.
    // A failed/uncertain check is never retried within this provider instance.
    private Task CheckLayoutAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (_legacyPrefixes.Length == 0) return Task.CompletedTask;
        lock (_layoutLock) return _layoutCheck ??= CheckLegacyPrefixesAsync(token);
    }

    private async Task CheckLegacyPrefixesAsync(CancellationToken token)
    {
        foreach (var prefix in _legacyPrefixes)
        {
            var response = await _client.ListObjectsV2Async(new ListObjectsV2Request
                { BucketName = _bucket, Prefix = prefix, MaxKeys = 1 }, token);
            Complete(response.HttpStatusCode, HttpStatusCode.OK, "legacy layout inspection");
            if (response.S3Objects == null || !HasNativeCompletionFlag(response) || !HasNativeKeyCount(response)
                || response.KeyCount != response.S3Objects.Count
                || response.Prefix != null && response.Prefix != prefix
                || response.CommonPrefixes is { Count: > 0 }
                || !response.IsTruncated && !string.IsNullOrEmpty(response.NextContinuationToken))
                throw new InvalidDataException("S3 legacy layout inspection omitted its completion state.");
            if (response.S3Objects.Count != 0)
                throw new InvalidDataException("Legacy plaintext OASIS objects exist; preserve them and review migration before selecting the encrypted layout.");
            if (response.IsTruncated)
                throw new InvalidDataException("S3 legacy layout inspection returned an incomplete empty page.");
        }
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
        await CheckLayoutAsync(cancellationToken);
        try
        {
            using var response = await _client.GetObjectAsync(new GetObjectRequest { BucketName = _bucket, Key = physical, ChecksumMode = ChecksumMode.ENABLED }, cancellationToken);
            Complete(response.HttpStatusCode, HttpStatusCode.OK, "GET");
            if (!string.IsNullOrEmpty(response.ContentRange) || string.IsNullOrWhiteSpace(response.ETag))
                throw new InvalidDataException("S3 GET must return a whole object and its native ETag.");
            // AmazonWebServiceResponse.ContentLength defaults to zero even when
            // the service omitted the header. The S3 header collection preserves
            // absence as -1; do not invent a complete empty object from that default.
            var nativeLength = response.Headers.ContentLength;
            if (nativeLength < 0)
                throw new InvalidDataException("S3 GET omitted a valid native object length.");
            using var reader = new StreamReader(response.ResponseStream ?? throw new InvalidDataException("S3 GET omitted its stream."), Utf8, false);
            var value = await reader.ReadToEndAsync(cancellationToken);
            if (Utf8.GetByteCount(value) != nativeLength)
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
        await CheckLayoutAsync(token);
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
        await CheckLayoutAsync(token);
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
        await CheckLayoutAsync(cancellationToken);
        var physical = _prefix + prefix;
        // Request the documented XML-safe format. The pinned v3 SDK leaves these
        // fields encoded; normalize exactly once using the response's format tag.
        var request = new ListObjectsV2Request { BucketName = _bucket, Prefix = physical, Encoding = EncodingType.Url };
        var keys = new List<string>(); var seen = new HashSet<string>(StringComparer.Ordinal); var tokens = new HashSet<string>(StringComparer.Ordinal);
        while (true)
        {
            var response = await _client.ListObjectsV2Async(request, cancellationToken);
            Complete(response.HttpStatusCode, HttpStatusCode.OK, "LIST");
            // The v3 SDK exposes omitted nullable service fields as CLR defaults.
            // Only an explicitly returned false establishes a complete final page.
            if (!HasNativeCompletionFlag(response))
                throw new InvalidDataException("S3 listing omitted its native completion flag.");
            if (!response.IsTruncated && !string.IsNullOrEmpty(response.NextContinuationToken))
                throw new InvalidDataException("S3 listing claims completion but also supplies a next continuation token.");
            if (response.Prefix != null && DecodeListingValue(response.Prefix,response.Encoding) != physical)
                throw new InvalidDataException("S3 listing prefix differs from its original requested selection.");
            // Continuation tokens are opaque and are not among the URL-encoded
            // fields. StartAfter/delimiter were never requested by this backend.
            if (response.ContinuationToken != null && response.ContinuationToken != request.ContinuationToken
                || !string.IsNullOrEmpty(response.StartAfter) || !string.IsNullOrEmpty(response.Delimiter))
                throw new InvalidDataException("S3 listing cursor or delimiter differs from its original request.");
            if (response.Encoding != null && response.Encoding != EncodingType.Url)
                throw new InvalidDataException("S3 listing uses an unsupported response encoding.");
            var objects=response.S3Objects ?? throw new InvalidDataException("S3 listing omitted its object collection.");
            if(response.CommonPrefixes is { Count: > 0 })
                throw new InvalidDataException("S3 delimiter-free listing cannot publish grouped prefixes as a complete object collection.");
            if(HasNativeKeyCount(response) && response.KeyCount!=objects.Count)
                throw new InvalidDataException("S3 listing's declared key count does not match its returned object collection.");
            foreach (var item in objects)
            {
                var key=DecodeListingValue(item?.Key,response.Encoding);
                if (string.IsNullOrWhiteSpace(key) || !key.StartsWith(physical, StringComparison.Ordinal) || !seen.Add(key))
                    throw new InvalidDataException("S3 listing contains an invalid, duplicate or out-of-prefix key.");
                keys.Add(key[_prefix.Length..]);
            }
            if (!response.IsTruncated) return keys;
            if (string.IsNullOrEmpty(response.NextContinuationToken) || !tokens.Add(response.NextContinuationToken))
                throw new InvalidDataException("S3 truncated listing requires a new continuation token.");
            request.ContinuationToken = response.NextContinuationToken;
        }
    }

    private static string? DecodeListingValue(string? value, EncodingType? encoding)
    {
        if (value == null || encoding == null) return value;
        if (encoding != EncodingType.Url) throw new InvalidDataException("S3 listing uses an unsupported response encoding.");
        try
        {
            Utf8.GetByteCount(value);
            for (var index=value.IndexOf('%');index>=0;index=value.IndexOf('%',index+3))
                if (index+2>=value.Length || !Uri.IsHexDigit(value[index+1]) || !Uri.IsHexDigit(value[index+2]))
                    throw new InvalidDataException("S3 listing contains a malformed percent-encoded value.");
            // S3 uses URI encoding, not HTML form encoding: a literal '+' stays '+'.
            return Utf8.GetString(System.Web.HttpUtility.UrlDecodeToBytes(value.Replace("+","%2B",StringComparison.Ordinal)));
        }
        catch (ArgumentException cause) { throw new InvalidDataException("S3 listing contains invalid encoded UTF-8.",cause); }
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await CheckLayoutAsync(cancellationToken);
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
