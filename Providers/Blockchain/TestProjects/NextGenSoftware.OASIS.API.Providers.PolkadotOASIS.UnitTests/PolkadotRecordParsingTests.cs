using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass]
public class PolkadotRecordParsingTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void CanonicalRecordsRetainStoredIdentityAndMetadata(bool avatar)
    {
        using var provider=new PolkadotOASIS("http://127.0.0.1:1","fixture");
        IHolonBase original=avatar?new Avatar{Id=Guid.NewGuid(),Username="owned",Email="owned@example.invalid"}:new Holon{Id=Guid.NewGuid(),Name="owned"};
        original.MetaData=new Dictionary<string,object>{{"owned:enabled",true}};
        var parser=typeof(PolkadotOASIS).GetMethod(avatar?"ParsePolkadotToAvatar":"ParsePolkadotToHolon",BindingFlags.NonPublic|BindingFlags.Instance)!;
        var parsed=(IHolonBase)parser.Invoke(provider,new object[]{OasisJson.Serialize(original)})!;
        Assert.AreEqual(original.Id,parsed.Id);Assert.AreEqual(true,parsed.MetaData["owned:enabled"]);
        if(avatar){Assert.AreEqual("owned",((IAvatar)parsed).Username);Assert.AreEqual("owned@example.invalid",((IAvatar)parsed).Email);}
    }

    [DataTestMethod]
    [DataRow("ParsePolkadotToAvatar","{bad")]
    [DataRow("ParsePolkadotToAvatar","{}")]
    [DataRow("ParsePolkadotToHolon","{bad")]
    [DataRow("ParsePolkadotToHolon","{}")]
    [DataRow("ParsePolkadotToAvatars","{bad")]
    [DataRow("ParsePolkadotToAvatars","{}")]
    [DataRow("ParsePolkadotToHolons","{bad")]
    [DataRow("ParsePolkadotToHolons","{}")]
    public void InvalidRecordsCannotFabricateIdentityOrSuccessfulEmptyCollection(string method,string json)
    {
        using var provider=new PolkadotOASIS("http://127.0.0.1:1","fixture");
        var parser=typeof(PolkadotOASIS).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance)!;
        var failure=Assert.ThrowsException<TargetInvocationException>(()=>parser.Invoke(provider,new object[]{json}));
        Assert.IsNotNull(failure.InnerException);
    }
}
