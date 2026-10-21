using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class CosmicQueryProviderContractTests
{
    [TestMethod]
    public void BootLoaderSelectedProviderExposesSharedCqlExecutionContract()
    {
        Assert.IsTrue(typeof(ICosmicQueryStorageProvider).IsAssignableFrom(typeof(Neo4jOASIS)),
            "The selected Neo4j provider must execute admitted CQL plans through the shared WEB4 contract.");
    }
}
