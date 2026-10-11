using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;
[TestClass]
public sealed class ExecutionStatusTests
{
    [TestMethod]
    [DataRow("{}")]
    [DataRow("{\"success\":null}")]
    [DataRow("{\"success\":\"false\"}")]
    public void MissingOrMalformedSuccessCannotEstablishFailure(string json)
    {
        using var document = JsonDocument.Parse(json);
        var method = typeof(SuiOASIS).GetMethod("MapExecutionStatus", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.ThrowsException<TargetInvocationException>(() => method.Invoke(null, new object[] { document.RootElement }));
    }
    [TestMethod]
    [DataRow("{\"success\":true}", BridgeTransactionStatus.Completed)]
    [DataRow("{\"success\":false,\"error\":\"abort\"}", BridgeTransactionStatus.Failed)]
    public void ConfirmedFailureIsAStatus(string json, BridgeTransactionStatus expected)
    {
        using var document = JsonDocument.Parse(json);
        Assert.AreEqual(expected, typeof(SuiOASIS).GetMethod("MapExecutionStatus", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { document.RootElement }));
    }
}
