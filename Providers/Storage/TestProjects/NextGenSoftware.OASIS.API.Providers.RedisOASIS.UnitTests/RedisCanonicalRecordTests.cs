using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.RedisOASIS;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.UnitTests;

[TestClass]
public class RedisCanonicalRecordTests
{
    private static string Encode(object value)=>Core.Serialization.StoredRecordJson.Serialize(value);
    private static T? Decode<T>(string value)=>Core.Serialization.StoredRecordJson.DeserializeStored<T>(value)
        ??throw new System.IO.InvalidDataException("Redis stored record JSON must contain a record, not null.");

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public void ExistingCamelCaseScalarRecordsRemainReadable(int kind)
    {
        var id=Guid.NewGuid();
        var json=$"{{\"id\":\"{id:D}\",\"name\":\"existing scalar\",\"username\":\"existing-user\",\"metaData\":null}}";
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase? loaded=kind switch{
            0=>Decode<Avatar>(json),1=>Decode<AvatarDetail>(json),_=>Decode<Holon>(json)};
        Assert.IsNotNull(loaded);Assert.AreEqual(id,loaded.Id);
        // Avatar display names are computed domain properties, not persisted scalar Name inputs.
        if(loaded is Holon && loaded is not Avatar && loaded is not AvatarDetail)Assert.AreEqual("existing scalar",loaded.Name);
        if(loaded is Avatar avatar)Assert.AreEqual("existing-user",avatar.Username);
        if(loaded is AvatarDetail detail)Assert.AreEqual("existing-user",detail.Username);
    }

    [TestMethod]
    public void PopulatedDetailPreservesOwnedInventoryAndCanonicalInterfaces()
    {
        var detail=new AvatarDetail{Id=Guid.NewGuid(),Username="owned-codec",Karma=321,XP=456};
        detail.Inventory.Add(new InventoryItem{Id=Guid.NewGuid(),Name="owned seed",Quantity=7});
        var loaded=Decode<AvatarDetail>(Encode(detail));
        Assert.IsNotNull(loaded);Assert.AreEqual(detail.Id,loaded.Id);Assert.AreEqual(321L,loaded.Karma);Assert.AreEqual(456L,Convert.ToInt64(loaded.XP));
        Assert.AreEqual(detail.Inventory.Single().Id,loaded.Inventory.Single().Id);Assert.AreEqual(7,loaded.Inventory.Single().Quantity);
        Assert.IsNotNull(loaded.Chakras);Assert.IsNotNull(loaded.Stats);Assert.IsNotNull(loaded.Skills);
    }

    [TestMethod]
    public void StoredHolonContainsRelationshipIdsNotHydratedNavigation()
    {
        var parent=Guid.NewGuid();var holon=new Holon{Id=Guid.NewGuid(),ParentHolonId=parent};
        holon.Children.Add(new Holon{Id=Guid.NewGuid(),Name="navigation must not embed"});
        var json=Encode(holon);var document=Newtonsoft.Json.Linq.JObject.Parse(json);
        Assert.IsNull(document.Property("Children",StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(parent,Decode<Holon>(json)!.ParentHolonId);
    }

    [DataTestMethod][DataRow("null")][DataRow("{broken")][DataRow("{\"$type\":\"System.Version, System.Private.CoreLib\"}")]
    public void CorruptStoredRecordCannotDecodeAsSuccess(string json)
    {
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>Decode<Holon>(json));
    }
}
