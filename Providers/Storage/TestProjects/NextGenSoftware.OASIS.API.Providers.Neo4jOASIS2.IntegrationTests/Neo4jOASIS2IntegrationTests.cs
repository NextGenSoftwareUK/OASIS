using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS2.IntegrationTests;

[TestClass]
public class Neo4jV2NativeTests
{
    [TestMethod]
    public async Task V2EntryPointActivatesAgainstDedicatedNeo4j()
    {
        if (Environment.GetEnvironmentVariable("OASIS_NEO4J_V2_CONFORMANCE") != "1")
            Assert.Inconclusive("Enable only for a dedicated Neo4j test endpoint.");

        var endpoint = Environment.GetEnvironmentVariable("OASIS_NEO4J_V2_URI");
        var username = Environment.GetEnvironmentVariable("OASIS_NEO4J_V2_USERNAME");
        var password = Environment.GetEnvironmentVariable("OASIS_NEO4J_V2_PASSWORD");
        Assert.IsFalse(string.IsNullOrWhiteSpace(endpoint));
        Assert.IsFalse(string.IsNullOrWhiteSpace(username));
        Assert.IsFalse(string.IsNullOrWhiteSpace(password));

        var provider = new global::NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS2(endpoint!, username!, password!);
        Assert.AreEqual(ProviderType.Neo4jOASIS, provider.ProviderType.Value);
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Exception?.GetType().FullName);
        Assert.IsTrue(active.Result);
        var inactive = await provider.DeActivateProviderAsync();
        Assert.IsFalse(inactive.IsError, inactive.Exception?.GetType().FullName);
        Assert.IsTrue(inactive.Result);
    }
}
