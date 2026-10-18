using System.Globalization;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

[TestClass]
public sealed class HederaIdBoundIntentTests
{
    [TestMethod]
    public void AssociationIsClassifiedAsNativeMutation()
        =>Assert.IsTrue(HederaSdkClient.IsNativeMutationAction("tokenEscrowAssociate"));
    private static readonly Guid Id=Guid.Parse("5f31afde-1229-4e99-8088-a0034d33a557");
    private const string NativeId="0x99c50e74aceedb30b6b1e3810ec571884c5ac132c94a49d34ee6d092f9c41ae1";
    private static HederaTokenEscrowConfiguration Config()=>new(){ContractId="0.0.2001",RuntimeCodeHash="0x"+new string('1',64),
        GasLimit=400000,MaxFeeTinybars=100000000,Protocol=HederaTokenEscrowProtocol.IdBoundV1};

    [DataTestMethod]
    [DataRow("aggregate")] [DataRow("lock")] [DataRow("release")]
    public void IntentSelectsExplicitAbiAndCanonicalIdentity(string mode)
    {
        var config=Config();if(mode=="aggregate")config.Protocol=HederaTokenEscrowProtocol.AggregateV1;
        var intent=HederaTokenEscrowIntent.Capture(config,mode!="release",mode=="aggregate"?Guid.Empty:Id,mode=="lock"?1.23m:0,config.ContractId);
        var payload=new Dictionary<string,object>();intent.AppendTo(payload);
        Assert.AreEqual(mode=="aggregate"?"aggregate-v1":"id-bound-v1",payload["escrowProtocol"]);
        if(mode=="aggregate"){Assert.IsFalse(payload.ContainsKey("depositId"));Assert.IsFalse(payload.ContainsKey("amount"));}
        else{Assert.AreEqual(NativeId,payload["depositId"]);Assert.AreEqual(mode=="lock"?"1.23":"0",payload["amount"]);Assert.AreEqual(Id,intent.LockId);}
    }

    [TestMethod]
    public void SmallestDecimalUsesExactInvariantNonScientificNotation()
    {
        var text="0."+new string('0',27)+"1";
        var intent=HederaTokenEscrowIntent.Capture(Config(),true,Id,decimal.Parse(text,CultureInfo.InvariantCulture),"0.0.2001");
        Assert.AreEqual(text,intent.Amount);
    }

    [DataTestMethod]
    [DataRow("id")] [DataRow("zero")] [DataRow("negative")] [DataRow("escrow")] [DataRow("protocol")]
    public void InvalidIntentDoesNotBecomeAggregate(string mode)
    {
        var config=Config();if(mode=="protocol")config.Protocol=(HederaTokenEscrowProtocol)99;
        void Capture()=>HederaTokenEscrowIntent.Capture(config,true,mode=="id"?Guid.Empty:Id,
            mode=="zero"?0:mode=="negative"?-1:1.23m,mode=="escrow"?"0.0.9999":config.ContractId);
        if(mode=="protocol")Assert.ThrowsException<InvalidOperationException>(Capture);
        else Assert.ThrowsException<ArgumentException>(Capture);
    }

    [DataTestMethod]
    [DataRow("valid")] [DataRow("identity")] [DataRow("zero")] [DataRow("overflow")]
    public void ReplyMustRetainDepositAndExactInt64Units(string mode)
    {
        var intent=HederaTokenEscrowIntent.Capture(Config(),true,Id,1.23m,"0.0.2001");
        using var document=JsonDocument.Parse(JsonSerializer.Serialize(new {depositId=mode=="identity"?"foreign":NativeId,
            amountUnits=mode=="zero"?"0":mode=="overflow"?"9223372036854775808":"123"}));
        if(mode=="valid")intent.ValidateReply(document.RootElement);
        else Assert.ThrowsException<System.IO.InvalidDataException>(()=>intent.ValidateReply(document.RootElement));
    }

    [DataTestMethod]
    [DataRow("id",true)] [DataRow("id",false)] [DataRow("escrow",true)] [DataRow("escrow",false)]
    [DataRow("owner",true)] [DataRow("owner",false)]
    public async Task PublicReleaseRejectsIncompleteIntentBeforeWalletLookup(string mode,bool synchronous)
    {
        var config=Config();var provider=new HashgraphOASIS(tokenEscrowConfiguration:config);
        config.Protocol=HederaTokenEscrowProtocol.AggregateV1; // Construction must retain its captured selection.
        var request=new Core.Objects.Wallet.Requests.UnlockWeb3TokenRequest{LockId=mode=="id"?Guid.Empty:Id,
            UnlockedByAvatarId=mode=="owner"?Guid.Empty:Guid.NewGuid(),TokenAddress="not-a-token",
            EscrowContractAddress=mode=="escrow"?"0.0.9999":"0.0.2001"};
        var result=synchronous?provider.UnlockToken(request):await provider.UnlockTokenAsync(request);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
        StringAssert.Contains(result.Message,mode=="owner"?"requesting avatar":"Per-deposit escrow");
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
        Assert.IsNull(result.Result);Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public void ResponseRetainsOriginalOrderedNativeIds()
    {
        var result=new HederaIdBoundEscrowResponse(Id,"mutation","approval");
        Assert.AreEqual(Id,result.LockId);Assert.AreEqual("mutation",result.TransactionResult);
        CollectionAssert.AreEqual(new[]{"approval","mutation"},result.NativeTransactions.ToArray());
    }
}
