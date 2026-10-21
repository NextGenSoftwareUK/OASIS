using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class OasisJsonNullMetadataTests
{
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public void NullMetadataRetainsCanonicalConstructorDefaults(int kind)
    {
        IHolonBase source=kind==0?new Holon():kind==1?new Avatar():new AvatarDetail();
        source.Id=Guid.NewGuid();
        var document=JObject.Parse(OasisJson.Serialize(source));document["MetaData"]=null;
        IHolonBase decoded=kind==0?OasisJson.DeserializeStored<Holon>(document.ToString())
            :kind==1?OasisJson.DeserializeStored<Avatar>(document.ToString()):OasisJson.DeserializeStored<AvatarDetail>(document.ToString());
        Assert.AreEqual(source.Id,decoded.Id);
        Assert.AreEqual(source.MetaData==null,decoded.MetaData==null);
        Assert.AreEqual(source.MetaData?.Count,decoded.MetaData?.Count);
    }

    [TestMethod]
    public void NullMetadataDoesNotPermitCaseAmbiguousSecondOwner()
    {
        var json="{\"Id\":\""+Guid.NewGuid()+"\",\"MetaData\":null,\"metadata\":{}}";
        Assert.ThrowsException<InvalidDataException>(()=>OasisJson.DeserializeStored<Holon>(json));
    }
}
