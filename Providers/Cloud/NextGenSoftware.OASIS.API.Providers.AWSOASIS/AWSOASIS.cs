using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS;

/// <summary>
/// AWS storage provider backed by the official AWSSDK.DynamoDBv2 client.
/// The optional service URL supports DynamoDB Local and LocalStack without changing production behavior.
/// </summary>
public sealed class AWSOASIS : NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.DynamoDBOASIS
{
    protected override ProviderType StorageProviderType => Core.Enums.ProviderType.AWSOASIS;

    public AWSOASIS(
        string region = "us-east-1",
        string accessKey = "",
        string secretKey = "",
        string? serviceUrl = null)
        : base(accessKey, secretKey, region, serviceUrl)
    {
        ProviderName = "AWSOASIS";
        ProviderDescription = "AWS Provider - official AWSSDK.DynamoDBv2 storage integration";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AWSOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
    }
}
