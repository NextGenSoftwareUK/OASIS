using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class ProtectedPreparationJsonTests
{
    private static HolonWritePreparation Candidate()=>new(Guid.NewGuid(),Guid.NewGuid(),ProviderType.OracleDBOASIS,
        new[]{new ProtectedHolonPreparation(Guid.NewGuid(),"oasis-preparation1:opaque-🌳-\"-\\")});
    [TestMethod]
    public void PreservesOpaqueUnicodeAndOwnershipWithoutAuthenticationOrEncryption() {
        var owner=Candidate();var copy=ProtectedPreparationJson.Read(owner,ProtectedPreparationJson.Write(owner));
        Assert.AreEqual(owner.OperationId,copy.OperationId);Assert.AreEqual(owner.ActorId,copy.ActorId);
        Assert.AreEqual(owner.Authority,copy.Authority);Assert.AreEqual(owner.Records[0].HolonId,copy.Records[0].HolonId);
        Assert.AreEqual(owner.Records[0].Envelope,copy.Records[0].Envelope);
    }
    [DataTestMethod]
    [DataRow("{}")][DataRow("[]")][DataRow("[null]")][DataRow("[{}]")]
    [DataRow("[{\"id\":\"00000000-0000-0000-0000-000000000001\",\"id\":\"00000000-0000-0000-0000-000000000001\",\"envelope\":\"oasis-preparation1:x\"}]")]
    [DataRow("[{\"id\":\"00000000-0000-0000-0000-000000000001\",\"envelope\":\"oasis-preparation1:x\",\"extra\":true}]")]
    public void RejectsMalformedOrAmbiguousTransport(string payload) {
        Assert.ThrowsException<InvalidDataException>(()=>ProtectedPreparationJson.Read(Candidate(),payload));
    }
    [TestMethod]
    public void RejectsDuplicateRecordIdentities() {
        var candidate=Candidate();var json=ProtectedPreparationJson.Write(candidate);
        var item=json.Substring(1,json.Length-2);
        Assert.ThrowsException<ArgumentException>(()=>ProtectedPreparationJson.Read(candidate,"["+item+","+item+"]"));
    }
}
