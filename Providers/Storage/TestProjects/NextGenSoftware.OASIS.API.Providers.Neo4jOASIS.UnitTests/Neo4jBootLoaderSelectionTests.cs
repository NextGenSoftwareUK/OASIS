using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jBootLoaderSelectionTests
{
    [DataTestMethod]
    [DataRow("bolt")] [DataRow("bolt+s")] [DataRow("bolt+ssc")]
    [DataRow("neo4j")] [DataRow("neo4j+s")] [DataRow("neo4j+ssc")]
    public void ExplicitDriverSecurityAndRoutingSchemesArePreserved(string scheme)
    {
        var uri = Neo4jOASIS.ConnectionUri(scheme + "://owned.invalid:17687");
        Assert.AreEqual(scheme, uri.Scheme); Assert.AreEqual(17687, uri.Port);
        Assert.AreEqual("owned.invalid", uri.Host);
    }

    [DataTestMethod]
    [DataRow("localhost", 7687)] [DataRow("localhost:17687", 17687)] [DataRow("[::1]:17687", 17687)]
    public void BareHostUsesBoltAndPreservesExplicitPort(string host, int port)
    {
        var uri = Neo4jOASIS.ConnectionUri(host);
        Assert.AreEqual("bolt", uri.Scheme); Assert.AreEqual(port, uri.Port);
    }

    [DataTestMethod]
    [DataRow("https://owned.invalid")] [DataRow("bolt://owned.invalid/nested")]
    [DataRow("bolt://user:password@owned.invalid")] [DataRow("")]
    public void InvalidExplicitConfigurationDoesNotBecomeAHostFallback(string host)
        => Assert.ThrowsException<ArgumentException>(() => Neo4jOASIS.ConnectionUri(host));

    [DataTestMethod]
    [DataRow(null)] [DataRow("bolt://127.0.0.1:17687")] [DataRow("neo4j+s://owned.databases.neo4j.io")]
    public async Task ProductionFactorySelectsCanonicalNativeProvider(string? explicitHost)
    {
        var settings = new Neo4jOASISSettings { ConnectionString = "bolt://configured.invalid:7687",
            Username = "owned-test-user", Password = "owned-test-password" };
        var provider = Neo4jProviderFactory.Create(settings, explicitHost);
        try
        {
            Assert.AreEqual(typeof(Neo4jOASIS), provider.GetType());
            Assert.AreEqual(ProviderType.Neo4jOASIS, provider.ProviderType.Value);
            Assert.AreEqual(explicitHost ?? settings.ConnectionString, provider.Host);
            Assert.AreEqual(settings.Username, provider.Username);
            Assert.AreEqual(settings.Password, provider.Password);
            Assert.IsFalse(provider.IsProviderActivated, "Registration must not activate or probe a network.");
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    [DataTestMethod]
    [DataRow("", "user", "password")] [DataRow("bolt://localhost:17687", "", "password")]
    [DataRow("bolt://localhost:17687", "user", "")]
    public void IncompleteConfigurationFailsBeforeProviderConstruction(string host, string username, string password)
        => Assert.ThrowsException<ArgumentException>(() => Neo4jProviderFactory.Create(new Neo4jOASISSettings {
            ConnectionString = host, Username = username, Password = password }));
}
