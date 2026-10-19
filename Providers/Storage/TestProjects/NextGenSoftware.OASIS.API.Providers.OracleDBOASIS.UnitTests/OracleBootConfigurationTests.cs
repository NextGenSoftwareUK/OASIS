using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleBootConfigurationTests
{
    [TestMethod]
    public void DnaRoundTripConstructsUnactivatedProviderWithoutDatabaseAccess()
    {
        var settings = new StorageProviderSettings
        {
            OracleDBOASIS = new OracleDBOASISSettings { ConnectionString = "Data Source=owned-not-connected;User Id=owned" }
        };
        var restored = JsonConvert.DeserializeObject<StorageProviderSettings>(JsonConvert.SerializeObject(settings))!;
        var provider = OracleProviderFactory.Create(restored.OracleDBOASIS);
        Assert.AreEqual(settings.OracleDBOASIS.ConnectionString, ConnectionString(provider));
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public void ExplicitOverrideDoesNotRequireDnaSection()
    {
        var provider = OracleProviderFactory.Create(null!, "Data Source=owned-override");
        Assert.AreEqual("Data Source=owned-override", ConnectionString(provider));
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public void MissingOrBlankConfigurationIsRejected(string? value)
        => Assert.ThrowsException<ArgumentException>(() => OracleProviderFactory.Create(
            value == null ? null! : new OracleDBOASISSettings { ConnectionString = value }));

    [TestMethod]
    public void BlankExplicitOverrideCannotSilentlyUseConfiguredAccount()
        => Assert.ThrowsException<ArgumentException>(() => OracleProviderFactory.Create(
            new OracleDBOASISSettings { ConnectionString = "Data Source=owned-config" }, ""));

    private static string ConnectionString(OracleDBOASIS provider)
        => (string)typeof(OracleDBOASIS).GetField("_connectionString", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(provider)!;
}
