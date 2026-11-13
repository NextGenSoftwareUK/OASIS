using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class NativeHostedReplicaRestartTests
{
    [DataTestMethod]
    [DataRow("denied")]
    [DataRow("expired")]
    [DataRow("legacy-route")]
    [DataRow("policy-changed")]
    public Task ShippingWorkerRejectsUnadmittedOrExpiredActorBeforeNativeReplicaWrite(string admission)
        => VerifyShippingWorkerRestart(false, admission);
    [TestMethod]
    public Task ShippingWorkerRecoversOlderBoundTransportUsingFreshNativeProof()
        => VerifyShippingWorkerRestart(false,olderBoundTransport:true);

}
