using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

/// <summary>The Cosmos reader reading the caller's own account. Checked and refused before any client is created.</summary>
[TestClass]
public class CosmosNativeConnectionTests
{
    private static AzureCosmosDBOASIS Provider() =>
        new(new Uri("https://localhost:8081"), "configured-key", "oasis", new List<string> { "avatarItems", "avatarDetailItems", "holonItems" });

    private static NativeConnection Connection(string endpoint = "https://mine.documents.azure.com:443/", string key = "account-key-123", string database = "shop") =>
        new() { ["endpoint"] = endpoint, ["key"] = key, ["database"] = database };

    [TestMethod]
    public void IdentityIsTheAccountAndDatabaseNeverTheKey()
    {
        var identity = Provider().DescribeNativeConnection(Connection("https://Mine.Documents.Azure.com:443/", "account-key-123"));

        Assert.AreEqual("cosmos://mine.documents.azure.com/shop", identity);
        Assert.IsFalse(identity.Contains("account-key-123"));
        Assert.IsNull(Provider().DescribeNativeConnection(null));
    }

    [TestMethod]
    public async Task BadConnectionsAreRefusedBeforeAnyClientIsCreated()
    {
        var provider = Provider();

        StringAssert.Contains((await provider.ListNativeDataSourcesAsync(Connection("http://mine.documents.azure.com"))).Message, "https");
        StringAssert.Contains((await provider.ListNativeDataSourcesAsync(Connection("https://user:pw@mine.documents.azure.com"))).Message, "https");
        StringAssert.Contains((await provider.ListNativeDataSourcesAsync(new NativeConnection { ["endpoint"] = "https://mine.documents.azure.com", ["database"] = "shop" })).Message, "key");
        StringAssert.Contains((await provider.ListNativeDataSourcesAsync(new NativeConnection { ["endpoint"] = "https://mine.documents.azure.com", ["key"] = "k" })).Message, "database");
        var typo = Connection(); typo["contaners"] = "x";
        StringAssert.Contains((await provider.ListNativeDataSourcesAsync(typo)).Message, "contaners");
    }

    [DataTestMethod]
    [DataRow("https://localhost:8081/")]
    [DataRow("https://127.0.0.1:8081/")]
    [DataRow("https://10.0.0.5/")]
    [DataRow("https://cosmos.railway.internal/")]
    public async Task InternalAccountsAreRefusedWithTheRealReason(string endpoint)
    {
        var result = await Provider().ListNativeDataSourcesAsync(Connection(endpoint));

        Assert.IsTrue(result.IsError);
        Assert.AreEqual("NATIVE_IMPORT_UNAVAILABLE", result.ErrorCode);
        Assert.IsTrue(result.Message.Contains("internal name") || result.Message.Contains("not a public address"), result.Message);
        Assert.IsFalse(result.Message.Contains("account-key-123"));
    }

    [TestMethod]
    public async Task ReadingAnUnlistedContainerNeverReachesTheNetwork()
    {
        var connection = Connection("https://localhost/");
        connection["containers"] = "customers";

        var result = await Provider().ReadNativeRecordsAsync("orders", null, 5, connection);

        Assert.IsTrue(result.IsError);
    }
}
