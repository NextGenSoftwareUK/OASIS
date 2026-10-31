using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CloudStorageDiscoveryTests
{
    [TestMethod]
    public void CloudDiscoveryPreservesStorageAndDoesNotActivate()
        => CloudStorageDiscoveryAssertions.Verify(new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),
            "unused-test-key", "owned-test", new List<string>()), ProviderCategory.StorageAndNetwork);
}
