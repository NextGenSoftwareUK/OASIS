using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MotherDuckOASIS.UnitTests;

[TestClass]
public class MotherDuckFamilyContractTests
{
    [TestMethod]
    public void CloudFacadeReusesRecordPolicyAndRetainsItsIdentity()
    {
        var provider = new MotherDuckOASIS("md:synthetic");
        Assert.IsInstanceOfType(provider, typeof(NextGenSoftware.OASIS.API.Providers.DuckDBOASIS.DuckDBOASIS));
        Assert.AreEqual("MotherDuckOASIS", provider.ProviderName);
        Assert.AreEqual("MotherDuckOASIS", provider.ProviderType.Value.ToString());
        Assert.AreEqual(typeof(KeyValueStorageProviderBase), provider.GetType().GetMethod("SaveHolonAsync")!.DeclaringType);
    }

    [DataTestMethod, DataRow("local.duckdb"), DataRow(":memory:"), DataRow("MD:synthetic")]
    public void CloudProviderCannotSilentlySelectLocalStorage(string database)
        => Assert.ThrowsException<ArgumentException>(() => new MotherDuckOASIS(database));

    [TestMethod]
    public void TokenRemainsOneEscapedUriValueAndOneAdoParameter()
    {
        const string token = "synthetic/quote\";?&=+☃";
        var connection = new DbConnectionStringBuilder { ConnectionString = MotherDuckOASIS.CreateConnectionString("md:synthetic", token) };
        Assert.AreEqual(1, connection.Count);
        var source = (string)connection["Data Source"];
        Assert.IsTrue(source.StartsWith("md:synthetic?motherduck_token=", StringComparison.Ordinal));
        Assert.AreEqual(token, Uri.UnescapeDataString(source.Split('=', 2)[1]));
    }

    [TestMethod]
    public void ExistingOptionsSurviveAndConflictingTokensAreRejected()
    {
        var connection = new DbConnectionStringBuilder { ConnectionString = MotherDuckOASIS.CreateConnectionString("md:synthetic?some_option=1", "synthetic") };
        Assert.AreEqual("md:synthetic?some_option=1&motherduck_token=synthetic", connection["Data Source"]);
        Assert.ThrowsException<ArgumentException>(() => new MotherDuckOASIS("md:synthetic?motherduck_token=old", "new"));
    }
}
