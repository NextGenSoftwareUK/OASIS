using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class OutboxTargetOrderingSchemaTests
{
    [DataTestMethod]
    [DataRow("ready")]
    [DataRow("missing")]
    [DataRow("equivalent-populating")]
    [DataRow("wrong-shape")]
    [DataRow("denied")]
    [DataRow("wait-failure")]
    public async Task ActivationRequiresAnOnlineNativeTargetOrderIndexWithoutTouchingBusinessData(string mode)
    {
        var driver = DispatchProxy.Create<IDriver, Neo4jLifecycleTests.DriverProbe>();
        var probe = (Neo4jLifecycleTests.DriverProbe)driver;
        probe.TargetIndexExists = mode != "missing";
        probe.TargetIndexOnline = mode == "ready";
        if (mode == "equivalent-populating") probe.TargetIndexName = "existing-equivalent-index";
        if (mode == "wrong-shape") { probe.TargetIndexShapeValid = false; probe.InstallTargetIndex = false; }
        if (mode == "denied") probe.SchemaWriteFailure = new ClientException("denied");
        var cause = new InvalidOperationException("native population wait failed");
        if (mode == "wait-failure") probe.TargetIndexWaitFailure = cause;
        var provider = new Provider("bolt://owned.invalid", "owned", "owned", () => driver);
        var result = await provider.ActivateProviderAsync();
        var rejected = mode is "wrong-shape" or "denied" or "wait-failure";
        Assert.AreEqual(rejected, result.IsError, result.Message + result.Exception);
        Assert.AreEqual(!rejected, provider.IsProviderActivated);
        Assert.AreEqual(0, probe.SchemaCreates, "Existing native constraints must not be recreated to repair an ordering index.");
        Assert.AreEqual(mode == "ready" || mode == "denied" ? 0 : 1, probe.TargetIndexCreates);
        if (mode == "ready") Assert.AreEqual(0, probe.TargetIndexWaits);
        if (mode == "equivalent-populating") Assert.AreEqual(1, probe.TargetIndexWaits);
        if (mode == "wait-failure") Assert.IsTrue(result.Exception!.ToString().Contains(cause.Message, StringComparison.Ordinal));
        if (!rejected) Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
