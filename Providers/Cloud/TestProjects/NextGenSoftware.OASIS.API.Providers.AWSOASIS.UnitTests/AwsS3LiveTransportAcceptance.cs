using System;
using System.Threading.Tasks;
using Amazon.S3;
using NextGenSoftware.OASIS.Providers.Shared.S3;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests;

/// <summary>Explicit runner entry point, never dispatched by ordinary unit discovery.</summary>
public static class AwsS3LiveTransportAcceptance
{
    public static Task RunAsync(IAmazonS3 client, string region, string bucket, string prefix, string work)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        var configuredRegion = client.Config.RegionEndpoint?.SystemName ?? client.Config.AuthenticationRegion;
        if (!string.Equals(region, configuredRegion, StringComparison.Ordinal))
            throw new ArgumentException("Acceptance journal region must match the configured SDK client.", nameof(region));
        return ConditionalObjectTransportAcceptance.RunAsync(new S3ObjectBackend(client, bucket, prefix),
            bucket, prefix, work, "s3-live-transport", region: region);
    }
}
