using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Rpc.Models;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaStoredTransactionTests
{
    [DataTestMethod]
    [DataRow("valid")]
    [DataRow("missing-meta")]
    [DataRow("failed-execution")]
    [DataRow("wrong-signature")]
    [DataRow("wrong-writer")]
    [DataRow("wrong-program")]
    [DataRow("duplicate-memo")]
    [DataRow("invalid-index")]
    public void ReadsValidateNativeRecordBeforeDecoding(string scenario)
    {
        var memo = new { ProgramIdIndex = scenario == "invalid-index" ? 7 : 1, Data = "2" };
        var json = JsonConvert.SerializeObject(new { Meta = new { }, Transaction = new {
            Signatures = new[] { scenario == "wrong-signature" ? "other" : "signature" }, Message = new {
                AccountKeys = new[] { scenario == "wrong-writer" ? "other" : "writer",
                    scenario == "wrong-program" ? "other-program" : "MemoSq4gqABAXKb96qnH8TysNcWxMyWCqXgDLGmfcHr" },
                Instructions = scenario == "duplicate-memo" ? new[] { memo, memo } : new[] { memo }
            } } });
        var native = JsonConvert.DeserializeObject<TransactionMetaSlotInfo>(json)!;
        if (scenario == "missing-meta") native.Meta = null;
        if (scenario == "failed-execution") native.Meta.Error = new TransactionError();
        var method = typeof(SolanaRepository).GetMethod("ValidateStoredTransaction", BindingFlags.Static | BindingFlags.NonPublic)!;
        if (scenario == "valid")
            CollectionAssert.AreEqual(new byte[] { 1 }, (byte[])method.Invoke(null,new object[] { native, "signature", "writer" })!);
        else
            Assert.IsInstanceOfType(Assert.ThrowsException<TargetInvocationException>(() =>
                method.Invoke(null,new object[] { native, "signature", "writer" })).InnerException, typeof(InvalidOperationException));
    }
}
