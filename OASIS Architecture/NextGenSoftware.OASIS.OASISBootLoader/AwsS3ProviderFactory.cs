using System;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider = NextGenSoftware.OASIS.API.Providers.AWSS3OASIS.AWSS3OASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static class AwsS3ProviderFactory
{
    internal static Provider Create(AWSS3OASISSettings settings, string overrideConnectionString = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (overrideConnectionString != null) throw new ArgumentException("AWS S3 uses structured settings, not a connection string.", nameof(overrideConnectionString));
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Region);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.BucketName);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.KeyPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.StorageEncryptionKeyEnvironmentVariable);
        if (settings.UseDefaultCredentials)
        {
            if (!string.IsNullOrEmpty(settings.AccessKey) || !string.IsNullOrEmpty(settings.SecretKey) || !string.IsNullOrEmpty(settings.SessionToken))
                throw new ArgumentException("Choose SDK default credentials or explicit credentials, not both.", nameof(settings));
        }
        else if (string.IsNullOrWhiteSpace(settings.AccessKey) || string.IsNullOrWhiteSpace(settings.SecretKey))
            throw new ArgumentException("Explicit S3 signing credentials are required unless SDK default credentials are opted in.", nameof(settings));
        var region = settings.Region; var bucket = settings.BucketName; var prefix = settings.KeyPrefix;
        var keyVariable = settings.StorageEncryptionKeyEnvironmentVariable; var history = settings.IsVersionControlEnabled;
        var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(region), MaxErrorRetry = 0 };
        var client = settings.UseDefaultCredentials ? new AmazonS3Client(config) :
            new AmazonS3Client(string.IsNullOrEmpty(settings.SessionToken)
                ? (AWSCredentials)new BasicAWSCredentials(settings.AccessKey, settings.SecretKey)
                : new SessionAWSCredentials(settings.AccessKey, settings.SecretKey, settings.SessionToken), config);
        try
        {
            return new Provider(client, bucket, () => PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(keyVariable), prefix, ownsClient: true)
                { IsVersionControlEnabled = history };
        }
        catch { client.Dispose(); throw; }
    }
}
