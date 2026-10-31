using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

[TestClass]
public sealed class HederaAssociationIntentTests
{
    private static HederaTokenEscrowConfiguration Config()=>new(){Protocol=HederaTokenEscrowProtocol.IdBoundV1,
        ContractId="0.0.2001",RuntimeCodeHash="0x"+new string('1',64),GasLimit=400000,MaxFeeTinybars=100000000};
    [TestMethod]
    public void AssociationPayloadHasNoDepositAmountOrApproval()
    {
        var intent=HederaTokenEscrowIntent.CaptureAssociation(Config(),"0.0.3001","0.0.2001");
        var payload=new Dictionary<string,object>();intent.AppendTo(payload);
        Assert.AreEqual("id-bound-v1",payload["escrowProtocol"]);Assert.AreEqual(1,payload.Count);Assert.AreEqual(Guid.Empty,intent.LockId);
    }
    [DataTestMethod]
    [DataRow("protocol")] [DataRow("escrow")] [DataRow("token")]
    public void IncompleteAssociationIntentIsRejected(string field)
    {
        var config=Config();if(field=="protocol")config.Protocol=HederaTokenEscrowProtocol.AggregateV1;
        Assert.ThrowsException<ArgumentException>(()=>HederaTokenEscrowIntent.CaptureAssociation(config,field=="token"?"":"0.0.3001",field=="escrow"?"0.0.9999":"0.0.2001"));
    }
    [DataTestMethod]
    [DataRow("valid")] [DataRow("false")] [DataRow("token")]
    public void AssociationReplyRequiresSuccessForOriginalToken(string field)
    {
        var intent=HederaTokenEscrowIntent.CaptureAssociation(Config(),"0.0.3001","0.0.2001");
        using var document=JsonDocument.Parse(JsonSerializer.Serialize(new {associated=field!="false",tokenId=field=="token"?"0.0.9999":"0.0.3001"}));
        if(field=="valid")intent.ValidateReply(document.RootElement);
        else Assert.ThrowsException<System.IO.InvalidDataException>(()=>intent.ValidateReply(document.RootElement));
    }
    [DataTestMethod]
    [DataRow("owner",true)] [DataRow("owner",false)] [DataRow("token",true)] [DataRow("token",false)]
    [DataRow("escrow",true)] [DataRow("escrow",false)] [DataRow("protocol",true)] [DataRow("protocol",false)]
    public async Task PublicAssociationRejectsBeforeWalletLookup(string field,bool synchronous)
    {
        var config=Config();if(field=="protocol")config.Protocol=HederaTokenEscrowProtocol.AggregateV1;
        var provider=new HashgraphOASIS(tokenEscrowConfiguration:config);
        var avatar=field=="owner"?Guid.Empty:Guid.NewGuid();var token=field=="token"?"invalid":"0.0.3001";var escrow=field=="escrow"?"0.0.9999":"0.0.2001";
        var result=synchronous?provider.AssociateTokenWithEscrow(avatar,token,escrow):await provider.AssociateTokenWithEscrowAsync(avatar,token,escrow);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));Assert.IsNull(result.Result);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.IsFalse(provider.IsProviderActivated);
    }
}
