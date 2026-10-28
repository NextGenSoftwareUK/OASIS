using Amazon.S3;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.Shared.S3;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AWSS3OASIS;

/// <summary>AWS identity over the canonical encrypted S3 transport and record policy.</summary>
public sealed class AWSS3OASIS : S3StorageProviderBase
{
    public AWSS3OASIS(IAmazonS3 client, string bucketName, Func<byte[]> storageKeyResolver,
        string keyPrefix = "oasis/", bool ownsClient = false)
        : base(client, bucketName, storageKeyResolver, keyPrefix, ownsClient)
    {
        ProviderName = "AWSS3OASIS";
        ProviderDescription = "AWS S3 storage with encrypted OASIS records and opaque keyed indexes";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AWSS3OASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Cloud));
    }
}
