using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaMintReceiptTests
{
    private const string Id="0.0.1001@1700000000.000000001";
    [DataTestMethod]
    [DataRow("[]")]
    [DataRow("[\"1\",\"1\"]")]
    [DataRow("[\"0\",\"2\"]")]
    [DataRow("[\"9223372036854775808\",\"2\"]")]
    public void VerifiedMintIdentitySurvivesInvalidSerialPayload(string serials)
    {
        using var receipt=JsonDocument.Parse("{\"status\":\"SUCCESS\",\"transactionId\":\""+Id+"\",\"serials\":"+serials+"}");
        var response=new Web3NFTTransactionResponse();
        var method=typeof(HashgraphOASIS).GetMethod("ApplyHederaMintReceipt",BindingFlags.Static|BindingFlags.NonPublic)!;
        var error=Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,new object[] {response,receipt.RootElement,Id,2}));
        Assert.AreEqual(Id,response.TransactionResult);StringAssert.Contains(error.InnerException!.Message,"reconcile");
        Assert.IsInstanceOfType<HederaNativeMutationReconciliationException>(error.InnerException);
        var evidence=(HederaNativeMutationReconciliationException)error.InnerException;
        Assert.AreEqual(Id,evidence.OriginalTransactionId);Assert.AreEqual("SUCCESS",evidence.Evidence.NativeStatus);
        Assert.AreEqual("mint-receipt-validation",evidence.Evidence.Stage);Assert.IsNotNull(evidence.InnerException);
        Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(evidence));
    }

    [DataTestMethod]
    [DataRow("{}")] [DataRow("{\"status\":\"FAIL_INVALID\",\"transactionId\":\"foreign\"}")]
    [DataRow("{\"status\":\"SUCCESS\",\"transactionId\":\"foreign\"}")]
    public void UnverifiedMintReceiptFencesOriginalWithoutInventingSuccess(string json)
    {
        using var receipt=JsonDocument.Parse(json);var response=new Web3NFTTransactionResponse();
        var method=typeof(HashgraphOASIS).GetMethod("ApplyHederaMintReceipt",BindingFlags.Static|BindingFlags.NonPublic)!;
        var error=Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,new object[]{response,receipt.RootElement,Id,2}));
        Assert.IsInstanceOfType<HederaNativeMutationReconciliationException>(error.InnerException);
        var evidence=(HederaNativeMutationReconciliationException)error.InnerException!;
        Assert.AreEqual(Id,evidence.OriginalTransactionId);Assert.IsNull(evidence.Evidence.NativeStatus);
        Assert.IsNull(response.TransactionResult);Assert.IsNotNull(evidence.InnerException);
        Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(evidence));
    }

    [TestMethod]
    public void VerifiedMintKeepsExactLargeSerials()
    {
        using var receipt=JsonDocument.Parse("{\"status\":\"SUCCESS\",\"transactionId\":\""+Id+"\",\"serials\":[\"9007199254740993\",\"9223372036854775807\"]}");
        var response=new Web3NFTTransactionResponse();
        var method=typeof(HashgraphOASIS).GetMethod("ApplyHederaMintReceipt",BindingFlags.Static|BindingFlags.NonPublic)!;
        var serials=(string[])method.Invoke(null,new object[] {response,receipt.RootElement,Id,2})!;
        CollectionAssert.AreEqual(new[] {"9007199254740993","9223372036854775807"},serials);
        Assert.AreEqual(Id,response.TransactionResult);
    }
}
