using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;
[TestClass]
public sealed class ExecutionStatusTests
{
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
