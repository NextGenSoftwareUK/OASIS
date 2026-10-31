using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass]
public sealed class OracleFactoryAdmissionTests
{
    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" \t")]
    public void MissingConfigurationFailsWithoutGuessingAConnection(string? connectionString)
    {
        var error = Assert.ThrowsException<InvalidOperationException>(
            () => OracleDBOASISTestFactory.Create(connectionString));
        StringAssert.Contains(error.Message, "ORACLEDBOASIS_CONNECTIONSTRING");
    }

    [TestMethod]
    public void ExplicitConfigurationConstructsWithoutActivatingOrOpeningConnection()
    {
        var provider = OracleDBOASISTestFactory.Create(
            "Data Source=example.invalid/DO_NOT_CONNECT;User Id=synthetic");
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual("OracleDBOASIS", provider.ProviderName);
    }
}
