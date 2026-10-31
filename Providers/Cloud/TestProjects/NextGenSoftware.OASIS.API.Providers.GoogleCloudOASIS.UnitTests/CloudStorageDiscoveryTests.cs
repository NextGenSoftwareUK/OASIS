using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public class CloudStorageDiscoveryTests
{
    [TestMethod]
    public void CloudDiscoveryPreservesStorageAndDoesNotActivate()
        => CloudStorageDiscoveryAssertions.Verify(new GoogleCloudOASIS(projectId: "owned-test",
            enableStorage: false, enableFirestore: false, enableBigQuery: false), ProviderCategory.StorageAndNetwork);
}
