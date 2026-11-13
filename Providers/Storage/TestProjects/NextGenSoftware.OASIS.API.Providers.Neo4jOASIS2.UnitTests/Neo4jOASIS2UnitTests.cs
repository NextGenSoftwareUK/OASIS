using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using V2Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS2;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS2.UnitTests;

[TestClass]
public class Neo4jOASIS2UnitTests
{
    [TestMethod]
    public async Task V2EntryPointUsesCanonicalProviderIdentityBeforeActivation()
    {
        var provider = new V2Provider("bolt://127.0.0.1:7687", "test-user", "test-password");
        try
        {
            Assert.AreEqual(ProviderType.Neo4jOASIS, provider.ProviderType.Value);
            Assert.AreEqual("Neo4jOASIS", provider.ProviderName);
            Assert.IsFalse(provider.IsProviderActivated);
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }
}
