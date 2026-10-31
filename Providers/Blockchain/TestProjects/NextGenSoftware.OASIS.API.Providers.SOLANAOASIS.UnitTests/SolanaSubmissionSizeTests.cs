using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaSubmissionSizeTests
{
    [DataTestMethod]
    [DataRow("repository",false,1231)][DataRow("repository",false,1232)][DataRow("repository",false,1233)]
    [DataRow("repository",true,1231)][DataRow("repository",true,1232)][DataRow("repository",true,1233)]
    [DataRow("sdk-bytes",false,1231)][DataRow("sdk-bytes",false,1232)][DataRow("sdk-bytes",false,1233)]
    [DataRow("sdk-bytes",true,1231)][DataRow("sdk-bytes",true,1232)][DataRow("sdk-bytes",true,1233)]
    [DataRow("sdk-base64",false,1231)][DataRow("sdk-base64",false,1232)][DataRow("sdk-base64",false,1233)]
    [DataRow("sdk-base64",true,1231)][DataRow("sdk-base64",true,1232)][DataRow("sdk-base64",true,1233)]
    public async Task SdkBuiltLegacyPacketSizeIsAdmittedBeforeAnySubmission(string route,bool twoSigners,int size)
    {
        var payer=new Account();var second=new Account();
        byte[] Build(int length)
        {
            var builder=new TransactionBuilder().SetRecentBlockHash(Encoders.Base58.EncodeData(new byte[32])).SetFeePayer(payer);
            if(twoSigners)builder.AddInstruction(SystemProgram.CreateAccount(payer.PublicKey,second.PublicKey,1_000_000,82,TokenProgram.ProgramIdKey));
            builder.AddInstruction(MemoProgram.NewMemoV2(new string('x',length)));
            return builder.Build(twoSigners?new List<Account>{payer,second}:new List<Account>{payer});
        }
        var probe=Build(800);
        var packet=Build(800+size-probe.Length);
        Assert.AreEqual(size,packet.Length);
        var signerCount=twoSigners?2:1;
        Assert.AreEqual((byte)signerCount,packet[0]);
        var message=packet.AsSpan(1+64*signerCount).ToArray();
        Assert.IsTrue(message[0]<128,"Actual pinned SDK legacy message, not a synthetic signature header.");
        Assert.IsTrue(payer.Verify(message,packet.AsSpan(1,64).ToArray()));
        if(twoSigners)Assert.IsTrue(second.Verify(message,packet.AsSpan(65,64).ToArray()));
        var original=packet.ToArray();
        var signature=Encoders.Base58.EncodeData(packet.AsSpan(1,64).ToArray());
        var rpc=DispatchProxy.Create<IRpcClient,SolanaFinalityTests.RpcProxy>();
        var observed=(SolanaFinalityTests.RpcProxy)(object)rpc;observed.Signature=signature;
        var repository=new SolanaRepository(payer,rpc);
        var wrapped=FinalizedSolanaRpc.Wrap(rpc,payer);
        async Task<string> Submit()
        {
            if(route=="repository")return await repository.SubmitSignedTransactionAsync(packet);
            var response=route=="sdk-bytes"?await wrapped.Client.SendTransactionAsync(packet)
                :await wrapped.Client.SendTransactionAsync(Convert.ToBase64String(packet));
            return response.Result;
        }
        if(size>1232)
        {
            var cause=await Assert.ThrowsExceptionAsync<InvalidOperationException>(Submit);
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(cause),"An unsubmitted oversized packet is known rejection, not uncertain broadcast.");
            Assert.AreEqual(0,observed.Sends);Assert.AreEqual(0,observed.Reads);
        }
        else
        {
            Assert.AreEqual(signature,await Submit());
            Assert.AreEqual(1,observed.Sends);Assert.AreEqual(1,observed.Reads);
        }
        CollectionAssert.AreEqual(original,packet,"Admission cannot change signed caller bytes.");
    }
}
