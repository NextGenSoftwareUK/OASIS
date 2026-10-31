using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

/// <summary>A caller-supplied Neo4j connection is checked and refused before any driver is created.</summary>
[TestClass]
public class Neo4jNativeConnectionTests
{
    private static Neo4jOASIS Provider() => new("bolt://oasis.invalid:7687", "oasis", "oasis", () => null!);

    private static NativeConnection Connection(string uri, string password = "pw") =>
        new() { ["uri"] = uri, ["username"] = "reader", ["password"] = password };

    [DataTestMethod]
    [DataRow("bolt://localhost:7687")]
    [DataRow("neo4j://10.1.2.3:7687")]
    [DataRow("bolt://neo4j.railway.internal:7687")]
    [DataRow("neo4j+s://169.254.169.254")]
    [DataRow("bolt://[::1]:7687")]
    public async Task InternalHostsAreRefusedWithoutConnecting(string uri)
    {
        var result = await Provider().ListNativeDataSourcesAsync(Connection(uri));

        Assert.IsTrue(result.IsError);
        Assert.AreEqual("NATIVE_IMPORT_CONNECTION_REFUSED", result.ErrorCode);
    }

    [DataTestMethod]
    [DataRow("http://db.example.com:7474")]
    [DataRow("mongodb://db.example.com")]
    [DataRow("neo4j+s://user:secret@db.example.com")]
    [DataRow("not a uri")]
    public async Task OnlyBoltSchemesWithoutEmbeddedCredentialsAreAccepted(string uri)
    {
        var result = await Provider().ReadNativeRecordsAsync("Person", null, 5, Connection(uri));
        Assert.AreEqual("NATIVE_IMPORT_CONNECTION_REFUSED", result.ErrorCode);
    }

    [TestMethod]
    public async Task CredentialsAreRequiredAndTyposReported()
    {
        var noPassword = new NativeConnection { ["uri"] = "neo4j+s://db.example.com", ["username"] = "reader" };
        StringAssert.Contains((await Provider().ListNativeDataSourcesAsync(noPassword)).Message, "password");

        var typo = Connection("neo4j+s://db.example.com");
        typo["pasword"] = "x";
        StringAssert.Contains((await Provider().ListNativeDataSourcesAsync(typo)).Message, "pasword");
    }

    [TestMethod]
    public void IdentityNamesTheHostAndDatabaseButNeverCredentials()
    {
        var connection = Connection("neo4j+s://DB.Example.com:7687", "hunter22");
        connection["database"] = "shop";

        var identity = Provider().DescribeNativeConnection(connection);

        Assert.AreEqual("neo4j+s://db.example.com:7687/shop", identity);
        Assert.IsFalse(identity.Contains("hunter22"));
        Assert.AreEqual("neo4j+s://db.example.com:7687/default", Provider().DescribeNativeConnection(Connection("neo4j+s://db.example.com")));
        Assert.IsNull(Provider().DescribeNativeConnection(null));
    }
}
