using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

/// <summary>A caller-supplied MongoDB connection is checked and refused before any connection is opened.</summary>
[TestClass]
public class MongoNativeConnectionTests
{
    private static MongoDBOASIS Provider() => new("mongodb://127.0.0.1:27028/?serverSelectionTimeoutMS=100", "oasis-native-connection-unit");

    private static NativeConnection Connection(string uri, string? database = "shop")
    {
        var connection = new NativeConnection { ["uri"] = uri };
        if (database != null) connection["database"] = database;
        return connection;
    }

    [DataTestMethod]
    [DataRow("mongodb://localhost:27017")]
    [DataRow("mongodb://127.0.0.1")]
    [DataRow("mongodb://10.0.0.5:27017")]
    [DataRow("mongodb://mongo.railway.internal:27017")]
    [DataRow("mongodb://169.254.169.254")]
    public async Task InternalHostsAreRefusedWithoutConnecting(string uri)
    {
        var result = await Provider().ListNativeDataSourcesAsync(Connection(uri));

        Assert.IsTrue(result.IsError);
        Assert.AreEqual("NATIVE_IMPORT_CONNECTION_REFUSED", result.ErrorCode);
    }

    [TestMethod]
    public async Task OnlyKnownSettingsAreAcceptedAndADatabaseIsRequired()
    {
        var typo = new NativeConnection { ["uri"] = "mongodb://db.example.com", ["databse"] = "shop" };
        StringAssert.Contains((await Provider().ListNativeDataSourcesAsync(typo)).Message, "databse");

        var noDatabase = await Provider().ReadNativeRecordsAsync("people", null, 5, Connection("mongodb://db.example.com", null));
        Assert.AreEqual("NATIVE_IMPORT_CONNECTION_REFUSED", noDatabase.ErrorCode);
        StringAssert.Contains(noDatabase.Message, "database");

        var notAUri = await Provider().ListNativeDataSourcesAsync(Connection("not a uri"));
        Assert.AreEqual("NATIVE_IMPORT_CONNECTION_REFUSED", notAUri.ErrorCode);
    }

    [DataTestMethod]
    [DataRow("admin")]
    [DataRow("local")]
    [DataRow("CONFIG")]
    public async Task MongoSystemDatabasesCannotBeImported(string database)
    {
        var result = await Provider().ListNativeDataSourcesAsync(Connection("mongodb://db.example.com", database));
        Assert.AreEqual("NATIVE_IMPORT_CONNECTION_REFUSED", result.ErrorCode);
    }

    [TestMethod]
    public async Task ErrorsNeverContainTheConnectionSecrets()
    {
        var result = await Provider().ListNativeDataSourcesAsync(Connection("mongodb://alice:hunter22@localhost:27017/shop"));

        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.Message.Contains("hunter22"));
        Assert.IsFalse(result.Message.Contains("alice:"));
    }

    [TestMethod]
    public void IdentityNamesHostsAndDatabaseButNeverCredentials()
    {
        var identity = Provider().DescribeNativeConnection(Connection("mongodb://alice:hunter22@B.example.com:27017,a.example.com:27017/shop?replicaSet=rs"));

        Assert.AreEqual("mongodb://a.example.com:27017,b.example.com:27017/shop", identity);
        Assert.IsFalse(identity.Contains("hunter22"));
        Assert.IsNull(Provider().DescribeNativeConnection(null));
        Assert.AreEqual(identity, Provider().DescribeNativeConnection(Connection("mongodb://x:y@a.example.com,b.example.com", "shop")));
    }
}
