using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass]
public class PolkadotTokenEscrowTests
{
    [TestMethod]
    public async Task TokenSubmissionClassificationRequiresOwnerBeforeBridgeProcess()
    {
        using var provider=new PolkadotOASIS();
        var method=typeof(PolkadotOASIS).GetMethod("InvokePolkadotSdkAsync",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var pending=(Task<JsonElement>)method.Invoke(provider,new object?[]{new{operation="submitTokenEscrowPreparation"},CancellationToken.None,null})!;
        var error=await Assert.ThrowsExceptionAsync<InvalidOperationException>(async()=>await pending);
        StringAssert.Contains(error.Message,"owned durable preparation server");
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task PublicCustodyRequiresAdmissionBeforeWalletOrRpc(bool unlock,bool sync)
    {
        using var provider=new PolkadotOASIS();
        var result=unlock
            ?(sync?provider.UnlockToken(new UnlockWeb3TokenRequest()):await provider.UnlockTokenAsync(new UnlockWeb3TokenRequest()))
            :(sync?provider.LockToken(new LockWeb3TokenRequest()):await provider.LockTokenAsync(new LockWeb3TokenRequest()));
        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message,"admitted execution owner");
        Assert.IsTrue(string.IsNullOrEmpty(result.Result?.TransactionResult));
        Assert.IsFalse(result.IsSaved);
    }

    [DataTestMethod][DataRow(null)][DataRow("")][DataRow("123")][DataRow("0x01")]
    public void ConfigurationRejectsAmbiguousLocators(string? invalid)
    {
        var valid="0x"+new string('a',64);
        Assert.ThrowsException<ArgumentException>(()=>new PolkadotTokenEscrowConfiguration(invalid!,valid));
        Assert.ThrowsException<ArgumentException>(()=>new PolkadotTokenEscrowConfiguration(valid,invalid!));
    }

    [DataTestMethod][DataRow("action")][DataRow("contractAddress")][DataRow("depositId")]
    [DataRow("tokenAddress")][DataRow("amountUnits")][DataRow("tokenEscrowEffectMatchesRequest")]
    public void FinalizedEffectMustMatchEveryOriginalCustodyField(string mismatch)
    {
        var original=new Dictionary<string,object>{["action"]="lock",["contractAddress"]="escrow",
            ["depositId"]="original-id",["tokenAddress"]="token",["expectedAmountUnits"]="18446744073709551620"};
        var receipt=new Dictionary<string,object>{["action"]="lock",["contractAddress"]="escrow",
            ["depositId"]="original-id",["tokenAddress"]="token",["amountUnits"]="18446744073709551620",
            ["tokenEscrowEffectMatchesRequest"]=true};
        var method=typeof(PolkadotOASIS).GetMethod("ValidateFinalizedTokenEscrowEffect",BindingFlags.Static|BindingFlags.NonPublic)!;
        method.Invoke(null,new object[]{JsonSerializer.SerializeToElement(original),JsonSerializer.SerializeToElement(receipt)});
        receipt[mismatch]=mismatch=="tokenEscrowEffectMatchesRequest"?false:"different";
        var error=Assert.ThrowsException<TargetInvocationException>(()=>method.Invoke(null,new object[]{
            JsonSerializer.SerializeToElement(original),JsonSerializer.SerializeToElement(receipt)}));
        Assert.IsInstanceOfType(error.InnerException,typeof(InvalidDataException));
    }
}
