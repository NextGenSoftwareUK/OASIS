using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

/// <summary>Deterministic SDK-response controls, never a native S3 service.</summary>
public sealed class ControlledS3Client : AmazonS3Client
{
    public sealed record Entry(byte[] Bytes, string Revision);
    public ConcurrentDictionary<string, Entry> Objects { get; } = new(StringComparer.Ordinal);
    public AmazonS3Exception? ReadError { get; set; }
    public AmazonS3Exception? WriteError { get; set; }
    public AmazonS3Exception? ListError { get; set; }
    public Action<ListObjectsV2Response>? AlterListing { get; set; }
    public int PutCalls { get; private set; }
    public int ListCalls { get; private set; }
    public bool WasDisposed { get; private set; }
    private readonly object _mutationLock = new();

    public ControlledS3Client() : base(new AnonymousAWSCredentials(), new AmazonS3Config
        { ServiceURL = "http://127.0.0.1:1", ForcePathStyle = true, MaxErrorRetry = 0 }) { }

    public static AmazonS3Exception Failure(string code, HttpStatusCode status)
        => new(code) { ErrorCode = code, StatusCode = status };

    public override Task<GetObjectResponse> GetObjectAsync(GetObjectRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ReadError != null) return Task.FromException<GetObjectResponse>(ReadError);
        if (!Objects.TryGetValue(request.Key, out var entry))
            return Task.FromException<GetObjectResponse>(Failure("NoSuchKey", HttpStatusCode.NotFound));
        var response = new GetObjectResponse
        {
            HttpStatusCode = HttpStatusCode.OK, ETag = entry.Revision,
            ResponseStream = new MemoryStream(entry.Bytes, writable: false)
        };
        response.Headers.ContentLength = entry.Bytes.Length;
        return Task.FromResult(response);
    }

    public override async Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PutCalls++;
        if (WriteError != null) throw WriteError;
        using var bytes = new MemoryStream();
        await request.InputStream.CopyToAsync(bytes, cancellationToken);
        var payload = bytes.ToArray();
        var revision = '"' + Convert.ToHexString(SHA256.HashData(payload)) + '"';
        lock (_mutationLock)
        {
            Objects.TryGetValue(request.Key, out var previous);
            if (request.IfNoneMatch == "*" && previous != null || request.IfMatch != null && previous?.Revision != request.IfMatch)
                throw Failure("PreconditionFailed", HttpStatusCode.PreconditionFailed);
            Objects[request.Key] = new Entry(payload, revision);
        }
        return new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK, ETag = revision,
            ChecksumSHA256 = Convert.ToBase64String(SHA256.HashData(payload)) };
    }

    public override Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (WriteError != null) return Task.FromException<DeleteObjectResponse>(WriteError);
        lock (_mutationLock)
        {
            Objects.TryGetValue(request.Key, out var previous);
            if (request.IfMatch != null && previous?.Revision != request.IfMatch)
                return Task.FromException<DeleteObjectResponse>(Failure("PreconditionFailed", HttpStatusCode.PreconditionFailed));
            Objects.TryRemove(request.Key, out _);
        }
        return Task.FromResult(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent });
    }

    public override Task<ListObjectsV2Response> ListObjectsV2Async(ListObjectsV2Request request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ListCalls++;
        if (ListError != null) return Task.FromException<ListObjectsV2Response>(ListError);
        var all = Objects.Keys.Where(k => k.StartsWith(request.Prefix ?? "", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
        var offset = request.ContinuationToken == null ? 0 : int.Parse(request.ContinuationToken, System.Globalization.CultureInfo.InvariantCulture);
        var page = all.Skip(offset).Take(request.MaxKeys > 0 ? request.MaxKeys : 1000).ToArray();
        var truncated = offset + page.Length < all.Length;
        var response = new ListObjectsV2Response
        {
            HttpStatusCode = HttpStatusCode.OK, Encoding = request.Encoding, IsTruncated = truncated, KeyCount = page.Length,
            NextContinuationToken = truncated ? (offset + page.Length).ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
            S3Objects = page.Select(k => new S3Object { Key = request.Encoding == EncodingType.Url ? Uri.EscapeDataString(k) : k }).ToList()
        };
        AlterListing?.Invoke(response);
        return Task.FromResult(response);
    }

    public override Task<HeadBucketResponse> HeadBucketAsync(HeadBucketRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new HeadBucketResponse { HttpStatusCode = HttpStatusCode.OK });

    protected override void Dispose(bool disposing) { WasDisposed = true; base.Dispose(disposing); }
}
