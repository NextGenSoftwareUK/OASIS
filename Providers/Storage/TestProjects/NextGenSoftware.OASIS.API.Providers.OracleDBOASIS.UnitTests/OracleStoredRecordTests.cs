using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleStoredRecordTests
{
    [TestMethod]
    public void OwnedInventoryRoundTripsThroughActualOracleCodec()
    {
        var source = new AvatarDetail { Id = Guid.NewGuid(), Karma = 543, XP = 123 };
        source.Inventory.Add(new InventoryItem { Id = Guid.NewGuid(), Name = "owned oak seed", Quantity = 7, GameSource = "Our World" });
        var loaded = Read<AvatarDetail>(Write(source));
        Assert.AreEqual(source.Id, loaded.Id);
        Assert.AreEqual(source.Karma, loaded.Karma);
        Assert.AreEqual(source.XP, loaded.XP);
        Assert.AreEqual(1, loaded.Inventory.Count);
        Assert.IsInstanceOfType<InventoryItem>(loaded.Inventory[0]);
        Assert.AreEqual(source.Inventory[0].Id, loaded.Inventory[0].Id);
        Assert.AreEqual(7, loaded.Inventory[0].Quantity);
        Assert.AreEqual("Our World", loaded.Inventory[0].GameSource);
    }

    [TestMethod]
    public void HydratedNavigationIsNotEmbeddedButRelationshipIdentitySurvives()
    {
        var source = new Holon { Id = Guid.NewGuid(), ParentHolonId = Guid.NewGuid() };
        source.Children.Add(new Holon { Id = Guid.NewGuid(), Name = "owned-hydrated-edge-must-not-be-stored" });
        var json = Write(source);
        Assert.IsFalse(json.Contains("owned-hydrated-edge-must-not-be-stored", StringComparison.Ordinal));
        var loaded = Read<Holon>(json);
        Assert.AreEqual(source.ParentHolonId, loaded.ParentHolonId);
        Assert.AreEqual(0, loaded.Children.Count);
    }

    [DataTestMethod]
    [DataRow("{broken")]
    [DataRow("{\"$type\":\"System.IO.FileInfo, System.Private.CoreLib\",\"OriginalPath\":\"owned-unused\"}")]
    [DataRow("null")]
    public void InvalidStoredRecordIsReportedAsDataCorruption(string json)
    {
        var error = Assert.ThrowsException<TargetInvocationException>(() => Read<Holon>(json));
        Assert.IsInstanceOfType<System.IO.InvalidDataException>(error.InnerException);
    }

    private static string Write(object row)
        => (string)typeof(OracleDBOASIS).GetMethod("Serialize", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new[] { row })!;

    private static T Read<T>(string json)
        => (T)typeof(OracleDBOASIS).GetMethod("Deserialize", BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(typeof(T)).Invoke(null, new object[] { json })!;
}
