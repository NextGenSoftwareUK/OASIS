using System.Reflection;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Rpc;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaFinalityTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SubmissionReturnsOnlyAfterFinalizedStatus(bool firstPending)
    {
        var bytes = new byte[70]; bytes[0] = 1; bytes[1] = 42;
        var signature = Encoders.Base58.EncodeData(bytes.AsSpan(1,64).ToArray());
        var rpc = DispatchProxy.Create<IRpcClient, RpcProxy>();
        var proxy = (RpcProxy)(object)rpc;
        proxy.Signature = signature;
        proxy.FirstPending = firstPending;
        var repository = new SolanaRepository(null!, rpc);
        var result = await Submit(repository, bytes);
        Assert.AreEqual(signature, result);
        Assert.AreEqual(1, proxy.Sends);
        Assert.AreEqual(firstPending ? 2 : 1, proxy.Reads);
    }

    [TestMethod]
    public async Task LostBroadcastReplyPreservesSignedIdentityAndFencesReplay()
    {
        var bytes = new byte[70]; bytes[0] = 1; bytes[1] = 99;
        var rpc = DispatchProxy.Create<IRpcClient, RpcProxy>();
        var proxy = (RpcProxy)(object)rpc;
        proxy.LostReply = true;
        var error = await Assert.ThrowsExceptionAsync<SolanaTransactionReconciliationException>(() => Submit(new SolanaRepository(null!, rpc), bytes));
        Assert.AreEqual(Encoders.Base58.EncodeData(bytes.AsSpan(1,64).ToArray()), error.OriginalTransactionId);
        StringAssert.Contains(error.Message, error.OriginalTransactionId);
        Assert.IsInstanceOfType(error.InnerException, typeof(IOException));
        Assert.IsTrue(NativeMutationReconciliation.IsRequired(error));
        Assert.AreEqual(1, proxy.Sends);
        Assert.AreEqual(0, proxy.Reads);
    }

    private static Task<string> Submit(SolanaRepository repository, byte[] bytes) => (Task<string>)typeof(SolanaRepository)
        .GetMethod("SubmitAndConfirmAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(repository,new object[] { bytes })!;

    public class RpcProxy : DispatchProxy
    {
        public string Signature = "";
        public bool FirstPending, LostReply;
        public int Sends, Reads;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            var resultType = method!.ReturnType.GetGenericArguments()[0];
            if (method.Name == "SendTransactionAsync")
            {
                Sends++;
                if (LostReply) return typeof(Task).GetMethods().Single(m => m.Name == "FromException" && m.IsGenericMethod)
                    .MakeGenericMethod(resultType).Invoke(null,new object[] { new IOException("Broadcast reply lost") });
            }
            else if (method.Name == "GetSignatureStatusesAsync") Reads++;
            else throw new InvalidOperationException("Unexpected RPC " + method.Name);
            var valueType = resultType.GetGenericArguments()[0];
            var json = method.Name == "SendTransactionAsync" ? JsonConvert.SerializeObject(Signature)
                : JsonConvert.SerializeObject(new { Value = new[] { new {
                    confirmationStatus = FirstPending && Reads == 1 ? "confirmed" : "finalized", err = (object?)null } } });
            var value = JsonConvert.DeserializeObject(json, valueType);
            var response = Activator.CreateInstance(resultType, new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK), value);
            resultType.GetProperty("WasRequestSuccessfullyHandled")!.SetValue(response,true);
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType).Invoke(null,new[] { response });
        }
    }
}
