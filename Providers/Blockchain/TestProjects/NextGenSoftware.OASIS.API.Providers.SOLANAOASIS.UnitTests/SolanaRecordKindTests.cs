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
    [TestMethod]
    public void EveryStoredHolonGuidSurvivesSerializedAdapterRoundTrip()
    {
        var holon=new NextGenSoftware.OASIS.API.Core.Holons.Holon();
        var identities=typeof(SolanaHolonDto).GetProperties()
            .Where(p=>p.PropertyType==typeof(Guid) && p.CanWrite).ToArray();
        foreach(var property in identities)
            typeof(NextGenSoftware.OASIS.API.Core.Holons.Holon).GetProperty(property.Name)!.SetValue(holon,Guid.NewGuid());
        var dto=NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions.OasisEntitiesExtensions.GetSolanaHolonDto(holon);
        var stored=Read<SolanaHolonDto>(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(dto));
        var loaded=NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions.SolanaEntitiesExtensions.GetHolon(stored);
        foreach(var property in identities) {
            var source=typeof(NextGenSoftware.OASIS.API.Core.Holons.Holon).GetProperty(property.Name)!;
            Assert.AreEqual(source.GetValue(holon),source.GetValue(loaded),property.Name);
        }
    }

    [TestMethod]
    public void EncryptedDetailMemoReconstructsOwnedInventoryAndStats()
    {
        var detail = new NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail {Id=Guid.NewGuid(),XP=321,Karma=29};
        var item = new NextGenSoftware.OASIS.API.Core.Objects.InventoryItem {Id=Guid.NewGuid(),Name="owned seed"};
        detail.Inventory.Add(item);
        var dto=NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions.OasisEntitiesExtensions.GetSolanaAvatarDetailDto(detail);
        var repository=new SolanaRepository(new Solnet.Wallet.Account(),null!,()=>Enumerable.Repeat((byte)23,32).ToArray());
        var encoded=(string)typeof(SolanaRepository).GetMethod("EncodeEntity",BindingFlags.Instance|BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(SolanaAvatarDetailDto)).Invoke(repository,new object[] {dto})!;
        Assert.IsFalse(encoded.Contains("owned seed"));
        var decoded=(string)typeof(SolanaRepository).GetMethod("DecodeEntity",BindingFlags.Instance|BindingFlags.NonPublic)!
            .Invoke(repository,new object[] {System.Text.Encoding.UTF8.GetBytes(encoded)})!;
        var loaded=Read<SolanaAvatarDetailDto>(decoded);
        Assert.AreEqual(item.Id,loaded.Inventory.Single().Id);
        Assert.AreEqual("owned seed",loaded.Inventory.Single().Name);
        Assert.IsInstanceOfType(loaded.Inventory.Single(),typeof(NextGenSoftware.OASIS.API.Core.Objects.InventoryItem));
        Assert.IsNotNull(loaded.Stats); Assert.IsNotNull(loaded.Skills);
        Assert.AreEqual(321,loaded.XP); Assert.AreEqual(29L,loaded.Karma);
    }

    [TestMethod]
    public void StoredDetailCannotInstantiateNonOasisInventoryType()
    {
        var document=JObject.FromObject(new SolanaAvatarDetailDto {Id=Guid.NewGuid()});
        document["Inventory"]=new JArray(new JObject {
            ["$type"]="System.IO.FileInfo, System.Private.CoreLib",
            ["OriginalPath"]="must-not-be-opened"
        });
        var error=Assert.ThrowsException<TargetInvocationException>(()=>Read<SolanaAvatarDetailDto>(document.ToString()));
        Assert.IsInstanceOfType(error.InnerException,typeof(JsonSerializationException));
    }

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
