using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS.UnitTests;

[TestClass]
public class CloudStorageDiscoveryTests
{
    [TestMethod]
    public void CloudDiscoveryPreservesStorageAndDoesNotActivate()
        => CloudStorageDiscoveryAssertions.Verify(new BigQueryOASIS("owned-test"), ProviderCategory.StorageLocalAndNetwork);
}
