using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public sealed class DynamoConfigurationTests
{
    [DataTestMethod]
    [DataRow("", "owned-secret", "us-east-1", null)]
    [DataRow(" ", "owned-secret", "us-east-1", null)]
    [DataRow("owned-access", "", "us-east-1", null)]
    [DataRow("owned-access", " ", "us-east-1", null)]
    [DataRow("owned-access", "owned-secret", "", null)]
    [DataRow("owned-access", "owned-secret", " ", null)]
    [DataRow("owned-access", "owned-secret", "us-east-1", " ")]
    public void DirectProviderRejectsIncompleteSigningConfigurationBeforeCreatingClient(
        string access, string secret, string region, string? token)
    {
        Assert.ThrowsException<ArgumentException>(() =>
        {
            var provider = new DynamoDBOASIS(access, secret, region, "http://127.0.0.1:18001", token, false);
            provider.DeActivateProvider();
        });
    }
}
