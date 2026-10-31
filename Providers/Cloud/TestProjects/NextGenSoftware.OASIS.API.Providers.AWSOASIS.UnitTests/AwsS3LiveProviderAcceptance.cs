using System;
using System.Threading.Tasks;
using Amazon.S3;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Providers.Shared.S3;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests;

public static class AwsS3LiveProviderAcceptance
{
    public const string Prefix = EncryptedObjectRecordAcceptance.Prefix;
    public static Task RunAsync(IAmazonS3 client, string region, string bucket, string keyVariable, string work)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyVariable);
        return RunAsync(client, region, bucket, () => PersistedPrivateKeyValueBackend.ResolveEnvironmentKey(keyVariable), work);
    }
    internal static Task RunAsync(IAmazonS3 client, string region, string bucket, Func<byte[]> resolver, string work)
    {
        ArgumentNullException.ThrowIfNull(client); ArgumentException.ThrowIfNullOrWhiteSpace(region);
        var configuredRegion = client.Config.RegionEndpoint?.SystemName ?? client.Config.AuthenticationRegion;
        if (!string.Equals(region, configuredRegion, StringComparison.Ordinal))
            throw new ArgumentException("Acceptance journal region must match the configured SDK client.", nameof(region));
        return EncryptedObjectRecordAcceptance.RunAsync(new S3ObjectBackend(client, bucket, Prefix),
            key => new AWSS3OASIS.AWSS3OASIS(client, bucket, key, Prefix), resolver, work,
            "s3-live-records", "S3", bucket, region: region);
    }
}
