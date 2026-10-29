using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
[TestClass]
public class Neo4jGraphAdoptionFailureRecovery
{
    [TestMethod]
    public async Task ResumeKnownGraphReceiptCountAssertionWithoutReplayingSource()
        => await new Neo4jNativeAvatarTests().RunNativeBoundSourceReceipt(true, true, true, true);
}
