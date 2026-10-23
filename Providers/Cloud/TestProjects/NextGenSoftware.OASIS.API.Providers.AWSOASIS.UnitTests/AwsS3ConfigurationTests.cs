using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests;

[TestClass]
public class AwsS3ConfigurationTests
{
    private static AWSS3OASISSettings Settings() => new()
    {
        Region = "us-east-1", BucketName = "synthetic-existing-bucket", AccessKey = "synthetic-key", SecretKey = "synthetic-secret"
    };

    [DataTestMethod]
    [DataRow("region")]
    [DataRow("bucket")]
    [DataRow("prefix")]
    [DataRow("encryption")]
    [DataRow("access")]
    [DataRow("secret")]
    public void Required_configuration_is_validated_before_sdk_use(string missing)
    {
        var settings = Settings();
        switch (missing)
        {
            case "region": settings.Region = " "; break;
            case "bucket": settings.BucketName = " "; break;
            case "prefix": settings.KeyPrefix = " "; break;
            case "encryption": settings.StorageEncryptionKeyEnvironmentVariable = " "; break;
            case "access": settings.AccessKey = " "; break;
            case "secret": settings.SecretKey = " "; break;
        }
        Assert.ThrowsException<ArgumentException>(() => AwsS3ProviderFactory.Create(settings));
    }
    [TestMethod]
    public void Credential_sources_cannot_be_mixed()
    {
        var settings = Settings(); settings.UseDefaultCredentials = true;
        Assert.ThrowsException<ArgumentException>(() => AwsS3ProviderFactory.Create(settings));
    }
    [TestMethod]
    public void Sql_connection_override_is_rejected()
        => Assert.ThrowsException<ArgumentException>(() => AwsS3ProviderFactory.Create(Settings(), "not-a-s3-configuration"));
    [DataTestMethod]
    [DataRow(null)]
    [DataRow("synthetic-session-token")]
    public void Explicit_sdk_credentials_create_distinct_s3_provider_without_activation(string token)
    {
        var settings = Settings(); settings.SessionToken = token; settings.IsVersionControlEnabled = true;
        using var provider = AwsS3ProviderFactory.Create(settings);
        Assert.AreEqual(ProviderType.AWSS3OASIS, provider.ProviderType.Value);
        Assert.AreEqual("AWSS3OASIS", provider.ProviderName);
        Assert.IsTrue(provider.IsVersionControlEnabled);
        Assert.IsFalse(provider.IsProviderActivated);
    }
    [TestMethod]
    public void Null_configuration_is_rejected()
        => Assert.ThrowsException<ArgumentNullException>(() => AwsS3ProviderFactory.Create(null!));
}
