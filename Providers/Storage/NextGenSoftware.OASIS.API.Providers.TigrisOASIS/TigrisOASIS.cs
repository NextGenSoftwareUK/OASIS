using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests")]

namespace NextGenSoftware.OASIS.API.Providers.TigrisOASIS
{
    /// <summary>
    /// Stores OASIS avatars and holons as objects in a Tigris bucket through Tigris's S3-compatible API (AWS SDK for .NET).
    /// </summary>
    public partial class TigrisOASIS : KeyValueStorageProviderBase, IOASISDBStorageProvider
    {
        private readonly S3ObjectBackend _objectBackend;
        public TigrisOASIS(string accessKey, string secretKey, string bucketName = "oasis-holons", string endpoint = "https://fly.storage.tigris.dev")
            : this(new S3ObjectBackend(CreateClient(accessKey, secretKey, endpoint), bucketName))
        {
        }

        internal TigrisOASIS(S3ObjectBackend backend) : base(backend)
        {
            _objectBackend = backend;
            ProviderName = "TigrisOASIS";
            ProviderDescription = "Tigris provider: OASIS avatars and holons as objects in globally distributed S3-compatible storage.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.TigrisOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        internal static IAmazonS3 CreateClient(string accessKey, string secretKey, string endpoint, bool boundedQuery = false, Amazon.Runtime.HttpClientFactory? httpClientFactory = null)
        {
            if (string.IsNullOrWhiteSpace(accessKey)) throw new ArgumentException("A Tigris access key id is required.", nameof(accessKey));
            if (string.IsNullOrWhiteSpace(secretKey)) throw new ArgumentException("A Tigris secret access key is required.", nameof(secretKey));
            if (boundedQuery && (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != "https" ||
                origin.UserInfo.Length != 0 || origin.AbsolutePath != "/" || origin.Query.Length != 0 || origin.Fragment.Length != 0))
                throw new ArgumentException("Explicit HTTPS Tigris origin required.", nameof(endpoint));
            var config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = false,
                AuthenticationRegion = "auto"
            };
            if (boundedQuery) config.MaxErrorRetry = 0;
            if (httpClientFactory != null) config.HttpClientFactory = httpClientFactory;
            return new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), config);
        }
    }

    internal sealed class S3ObjectBackend : IKeyValueBackend
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;

        public S3ObjectBackend(IAmazonS3 s3, string bucket)
        {
            _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
            if (string.IsNullOrWhiteSpace(bucket)) throw new ArgumentException("A bucket name is required.", nameof(bucket));
            _bucket = bucket;
        }

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            using var response = await OpenObjectAsync(key, cancellationToken);
            if (response == null) return null;
            using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);
            return await reader.ReadToEndAsync(cancellationToken);
        }

        private async Task<GetObjectResponse> OpenObjectAsync(string key, CancellationToken cancellationToken)
        {
            try
            {
                return await _s3.GetObjectAsync(_bucket, key, cancellationToken);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound && ex.ErrorCode == "NoSuchKey")
            {
                return null;
            }
        }

        public async Task<string> GetBoundedAsync(string key, int maximumBytes, Action<int> accountBytes, CancellationToken cancellationToken)
        {
            if (maximumBytes < 1 || accountBytes == null) throw new ArgumentException("Explicit positive native payload budget required.");
            using var response = await OpenObjectAsync(key, cancellationToken);
            if (response == null) return null;
            if (response.HttpStatusCode != HttpStatusCode.OK || response.ResponseStream == null)
                throw new InvalidDataException("Native object response is incomplete.");
            if (response.ContentLength > maximumBytes) throw new ObjectPayloadLimitException();
            using var output = new MemoryStream();
            var buffer = new byte[8192]; int count;
            while ((count = await response.ResponseStream.ReadAsync(buffer, cancellationToken)) != 0)
            {
                if (output.Length + count > maximumBytes) throw new ObjectPayloadLimitException();
                accountBytes(count); output.Write(buffer, 0, count);
            }
            if (response.ContentLength >= 0 && output.Length != response.ContentLength)
                throw new InvalidDataException("Native object response length differs.");
            return new UTF8Encoding(false, true).GetString(output.ToArray());
        }

        internal sealed class ObjectPayloadLimitException : IOException { }

        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => _s3.PutObjectAsync(new PutObjectRequest { BucketName = _bucket, Key = key, ContentBody = value, ContentType = "application/json" }, cancellationToken);

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => _s3.DeleteObjectAsync(_bucket, key, cancellationToken);

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var keys = new List<string>();
            var request = new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix };
            var seenTokens = new HashSet<string>(StringComparer.Ordinal);
            ListObjectsV2Response response;
            do
            {
                response = await _s3.ListObjectsV2Async(request, cancellationToken);
                if (response.S3Objects != null)
                    foreach (var o in response.S3Objects) keys.Add(o.Key);
                if (response.IsTruncated == true)
                {
                    if (string.IsNullOrEmpty(response.NextContinuationToken) || !seenTokens.Add(response.NextContinuationToken))
                        throw new InvalidOperationException("S3 returned a truncated listing without a new continuation token.");
                    request.ContinuationToken = response.NextContinuationToken;
                }
            } while (response.IsTruncated == true);
            return keys;
        }

        // Bucket provisioning is a separate, explicitly authorized operation.
        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            await _s3.HeadBucketAsync(new HeadBucketRequest { BucketName = _bucket }, cancellationToken);
        }
    }
}
