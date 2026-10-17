using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class NativePreparedAttemptSnapshotTests
{
    private static UsageProviderAttempt Attempt()=>new() {Provider="provider",ProviderRequestId="original",
        NativePreparation=new() {ChainId="chain",Sender="sender",SignedTransactionBase64="AQ==",Signature="signature",SignDocBase64="Ag=="}};

    [TestMethod]
    public void SnapshotCannotBeChangedByCallerMutatingOriginalAttempt()
    {
        var attempt=Attempt();var original=attempt.NativePreparation;
        var snapshot=NativePreparedAttemptSnapshot.Capture(attempt,"provider","chain");
        attempt.ProviderRequestId="replacement";original.Sender="other";original.ChainId="other";
        original.SignedTransactionBase64="Aw==";original.Signature="other";original.SignDocBase64="BA==";
        attempt.NativePreparation=new();
        Assert.AreEqual(new NativePreparedAttemptSnapshot("original","chain","sender","AQ==","signature","Ag=="),snapshot);
    }

    [TestMethod]
    public void WrongProviderNetworkOrMissingIdentityIsRejectedBeforeRecovery()
    {
        Assert.ThrowsException<InvalidDataException>(()=>NativePreparedAttemptSnapshot.Capture(Attempt(),"other","chain"));
        Assert.ThrowsException<InvalidDataException>(()=>NativePreparedAttemptSnapshot.Capture(Attempt(),"provider","other"));
        foreach(var id in new[]{"",new string('x',257)}){
            var attempt=Attempt();attempt.ProviderRequestId=id;
            Assert.ThrowsException<InvalidDataException>(()=>NativePreparedAttemptSnapshot.Capture(attempt,"provider","chain"));
        }
        var missing=Attempt();missing.NativePreparation=null;
        Assert.ThrowsException<InvalidDataException>(()=>NativePreparedAttemptSnapshot.Capture(missing,"provider","chain"));
    }

    [TestMethod]
    public void ExistingCanonicalPreparationValidationRemainsAuthoritative()
    {
        var attempt=Attempt();attempt.NativePreparation.SignedTransactionBase64="not base64";
        Assert.ThrowsException<UsageProtocolException>(()=>NativePreparedAttemptSnapshot.Capture(attempt,"provider","chain"));
        attempt=Attempt();attempt.NativePreparation.SignDocBase64="";
        Assert.ThrowsException<UsageProtocolException>(()=>NativePreparedAttemptSnapshot.Capture(attempt,"provider","chain"));
        Assert.ThrowsException<ArgumentException>(()=>NativePreparedAttemptSnapshot.Capture(Attempt(),"provider",""));
    }
}
