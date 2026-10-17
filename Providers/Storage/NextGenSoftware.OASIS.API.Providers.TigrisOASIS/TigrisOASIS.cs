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
using Amazon.S3.Util;
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
    public class TigrisOASIS : KeyValueStorageProviderBase, IOASISDBStorageProvider
    {
        public TigrisOASIS(string accessKey, string secretKey, string bucketName = "oasis-holons", string endpoint = "https://fly.storage.tigris.dev")
            : this(new S3ObjectBackend(CreateClient(accessKey, secretKey, endpoint), bucketName))
        {
        }

        internal TigrisOASIS(S3ObjectBackend backend) : base(backend)
        {
            ProviderName = "TigrisOASIS";
            ProviderDescription = "Tigris provider: OASIS avatars and holons as objects in globally distributed S3-compatible storage.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.TigrisOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private static IAmazonS3 CreateClient(string accessKey, string secretKey, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(accessKey)) throw new ArgumentException("A Tigris access key id is required.", nameof(accessKey));
            if (string.IsNullOrWhiteSpace(secretKey)) throw new ArgumentException("A Tigris secret access key is required.", nameof(secretKey));
            return new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = "auto"
            });
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
            try
            {
                using var response = await _s3.GetObjectAsync(_bucket, key, cancellationToken);
                using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);
                return await reader.ReadToEndAsync(cancellationToken);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => _s3.PutObjectAsync(new PutObjectRequest { BucketName = _bucket, Key = key, ContentBody = value, ContentType = "application/json" }, cancellationToken);

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => _s3.DeleteObjectAsync(_bucket, key, cancellationToken);

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var keys = new List<string>();
            var request = new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix };
            ListObjectsV2Response response;
            do
            {
                response = await _s3.ListObjectsV2Async(request, cancellationToken);
                if (response.S3Objects != null)
                    foreach (var o in response.S3Objects) keys.Add(o.Key);
                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated == true);
            return keys;
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(_s3, _bucket))
                await _s3.PutBucketAsync(new PutBucketRequest { BucketName = _bucket }, cancellationToken);
        }
    }
}
