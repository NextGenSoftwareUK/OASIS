using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;
[TestClass]
public sealed class ExecutionStatusTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void StatusRequiresRequestedHashAndCommittedHeight(bool failed)
    {
        var hash = new string('a', 64);
        using var json = System.Text.Json.JsonDocument.Parse($"{{\"hash\":\"{hash.ToUpperInvariant()}\",\"height\":42,\"code\":{(failed ? 5 : 0)}}}");
        var method = typeof(CosmosBlockChainOASIS).GetMethod("MapTransactionStatus", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.AreEqual(failed ? BridgeTransactionStatus.Failed : BridgeTransactionStatus.Completed, method.Invoke(null, new object[] { json.RootElement, hash }));
    }

    [DataTestMethod]
    [DataRow("{}")]
    [DataRow("{\"hash\":\"wrong\",\"height\":42,\"code\":0}")]
    [DataRow("{\"hash\":null,\"height\":42,\"code\":0}")]
    public void MalformedOrMismatchedIdentityCannotReportCompletion(string response)
    {
        using var json = System.Text.Json.JsonDocument.Parse(response);
        var method = typeof(CosmosBlockChainOASIS).GetMethod("MapTransactionStatus", BindingFlags.NonPublic | BindingFlags.Static)!;
        var error = Assert.ThrowsException<TargetInvocationException>(() => method.Invoke(null, new object[] { json.RootElement, new string('a', 64) }));
        Assert.IsInstanceOfType<InvalidOperationException>(error.InnerException);
    }
    [TestMethod]
    public void InvalidCodeCannotBeReportedAsConsensusFailure()
    {
        var method = typeof(CosmosBlockChainOASIS).GetMethod("MapExecutionCode", BindingFlags.NonPublic | BindingFlags.Static)!;
        var error = Assert.ThrowsException<TargetInvocationException>(() => method.Invoke(null, new object[] { -1 }));
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(error.InnerException);
    }
    [TestMethod]
    [DataRow(0, BridgeTransactionStatus.Completed)]
    [DataRow(1, BridgeTransactionStatus.Failed)]
    [DataRow(99, BridgeTransactionStatus.Failed)]
    public void NativeExecutionIsNotCancellation(int code, BridgeTransactionStatus expected)
        => Assert.AreEqual(expected, typeof(CosmosBlockChainOASIS).GetMethod("MapExecutionCode", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { code }));
}
