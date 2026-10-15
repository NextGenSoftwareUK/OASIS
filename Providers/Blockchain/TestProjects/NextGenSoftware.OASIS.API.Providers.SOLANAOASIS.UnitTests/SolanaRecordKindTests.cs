using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaRecordKindTests
{
    private static T Read<T>(string json) where T:SolanaBaseDto,new()
        => (T)typeof(SolanaRepository).GetMethod("DeserializeStoredRecord",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T)).Invoke(null,new object[] {json})!;

    [TestMethod]
    public void EachCanonicalTypeRetainsItsIdentity()
    {
        var id=Guid.NewGuid();
        Assert.AreEqual(id,Read<SolanaAvatarDto>(JsonConvert.SerializeObject(new SolanaAvatarDto {Id=id})).Id);
        Assert.AreEqual(id,Read<SolanaAvatarDetailDto>(JsonConvert.SerializeObject(new SolanaAvatarDetailDto {Id=id})).Id);
        Assert.AreEqual(id,Read<SolanaHolonDto>(JsonConvert.SerializeObject(new SolanaHolonDto {Id=id})).Id);
    }

    [DataTestMethod]
    [DataRow("wrong-kind")]
    [DataRow("missing-kind")]
    [DataRow("empty-id")]
    [DataRow("negative-version")]
    public void InvalidOrWrongTypedRecordsCannotBecomeFabricatedAvatars(string scenario)
    {
        var json=JObject.FromObject(new SolanaAvatarDto {Id=Guid.NewGuid()});
        if(scenario=="wrong-kind") json["RecordKind"]="oasis.holon.v1";
        if(scenario=="missing-kind") json.Remove("RecordKind");
        if(scenario=="empty-id") json["Id"]=Guid.Empty;
        if(scenario=="negative-version") json["Version"]=-1;
        var error=Assert.ThrowsException<TargetInvocationException>(()=>Read<SolanaAvatarDto>(json.ToString()));
        Assert.IsInstanceOfType(error.InnerException,typeof(InvalidOperationException));
    }
}
