using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;
[TestClass]
public sealed class ExecutionStatusTests
{
    [TestMethod]
    public void CustomBalanceRequiresCanonicalUnitsAndExactDecimalRepresentation()
    {
        var method=typeof(CosmosBlockChainOASIS).GetMethod("CustomTokenBalanceFromUnits",BindingFlags.NonPublic|BindingFlags.Static)!;
        Assert.AreEqual(0m,method.Invoke(null,new object[]{"0"}));
        Assert.AreEqual(0.00000001m,method.Invoke(null,new object[]{"1"}));
        Assert.AreEqual(90071992.54740993m,method.Invoke(null,new object[]{"9007199254740993"}));
        foreach(var invalid in new string?[]{null,"-1","01"," 1","1.5","340282366920938463463374607431768211456",
            "1000000000000000000000000000000000001"}){
            var failure=Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,new object?[]{invalid}));
            Assert.IsInstanceOfType<InvalidOperationException>(failure.InnerException);
        }
    }
    [DataTestMethod]
    [DataRow(9007199254740992L)]
    [DataRow(long.MaxValue)]
    public async Task InexactDeploymentCodeIdIsRejectedBeforeSdkLaunch(long codeId)
    {
        using var provider = new CosmosBlockChainOASIS("http://127.0.0.1:1");
        var result = await provider.InstantiateContractAsync(codeId, new { }, "invalid code ID");
        Assert.IsTrue(result.IsError);
        Assert.IsInstanceOfType<ArgumentException>(result.Exception);
        StringAssert.Contains(result.Message, "SDK-safe exact code ID");
    }
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
    [DataTestMethod]
    [DataRow("2147483648")]
    [DataRow("4294967295")]
    public void FullUnsignedNativeCodeRangeReportsFailedExecution(string code)
    {
        var hash = new string('a', 64);
        using var json = System.Text.Json.JsonDocument.Parse($"{{\"hash\":\"{hash}\",\"height\":42,\"code\":{code}}}");
        var method = typeof(CosmosBlockChainOASIS).GetMethod("MapTransactionStatus", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.AreEqual(BridgeTransactionStatus.Failed, method.Invoke(null, new object[] { json.RootElement, hash }));
    }

    [DataTestMethod]
    [DataRow("-1")]
    [DataRow("4294967296")]
    [DataRow("1.5")]
    [DataRow("null")]
    [DataRow("\"0\"")]
    public void InvalidNativeCodeCannotEstablishExecution(string code)
    {
        var hash = new string('a', 64);
        using var json = System.Text.Json.JsonDocument.Parse($"{{\"hash\":\"{hash}\",\"height\":42,\"code\":{code}}}");
        var method = typeof(CosmosBlockChainOASIS).GetMethod("MapTransactionStatus", BindingFlags.NonPublic | BindingFlags.Static)!;
        var error = Assert.ThrowsException<TargetInvocationException>(() => method.Invoke(null, new object[] { json.RootElement, hash }));
        Assert.IsInstanceOfType<InvalidOperationException>(error.InnerException);
    }
    [TestMethod]
    [DataRow(0, BridgeTransactionStatus.Completed)]
    [DataRow(1, BridgeTransactionStatus.Failed)]
    [DataRow(99, BridgeTransactionStatus.Failed)]
    public void NativeExecutionIsNotCancellation(int code, BridgeTransactionStatus expected)
        => Assert.AreEqual(expected, typeof(CosmosBlockChainOASIS).GetMethod("MapExecutionCode", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { code }));
}
