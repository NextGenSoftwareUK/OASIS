using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class AwsS3ProviderIdentityTests
{
    [Theory]
    [InlineData(ProviderType.AWSOASIS, 55)]
    [InlineData(ProviderType.TigrisOASIS, 204)]
    [InlineData(ProviderType.OracleObjectStorageOASIS, 227)]
    [InlineData(ProviderType.AWSS3OASIS, 228)]
    public void S3IdentityDoesNotRenumberExistingStorage(ProviderType provider, int ordinal)
        => Assert.Equal(ordinal, (int)provider);
}
