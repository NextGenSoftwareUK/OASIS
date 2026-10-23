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

    [DataTestMethod]
    [DataRow("avatar","date",0)][DataRow("avatar","date",1)][DataRow("avatar","number",0)][DataRow("avatar","number",1)]
    [DataRow("detail","date",0)][DataRow("detail","date",1)][DataRow("detail","number",0)][DataRow("detail","number",1)]
    [DataRow("holon","date",0)][DataRow("holon","date",1)][DataRow("holon","number",0)][DataRow("holon","number",1)]
    public async Task OracleHistoryPreservesExactUntypedScalarMetadata(string kind,string scalar,int version)
    {
        if(kind=="avatar")await Check<Avatar>();else if(kind=="detail")await Check<AvatarDetail>();else await Check<Holon>();
        async Task Check<T>() where T:class,NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase,new()
        {
            using var number=System.Text.Json.JsonDocument.Parse("9007199254740993.125");
            var row=new T{Id=Guid.NewGuid(),MetaData=new(){{"literal",scalar=="date"?(object)"2030-01-02T03:04:05.0000000+05:30":number.RootElement.Clone()}}};
            using var expected=System.Text.Json.JsonDocument.Parse(Write(row));
            var owner=new OracleDBOASIS(""){IsVersionControlEnabled=true};
            var stage=typeof(OracleDBOASIS).GetMethod("StageRecordHistoryAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.MakeGenericMethod(typeof(T));
            var first=await (Task<string>)stage.Invoke(owner,new object?[]{row,null,null,null})!;
            var second=await (Task<string>)stage.Invoke(owner,new object?[]{Read<T>(first),first,null,null})!;
            var reader=typeof(OracleDBOASIS).GetMethod("ReadRecordVersionAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.MakeGenericMethod(typeof(T));
            var loaded=await (Task<T>)reader.Invoke(owner,new object?[]{row.Id,second,version,null,null,null})!;
            using var actual=System.Text.Json.JsonDocument.Parse(Write(loaded));
            Assert.AreEqual(expected.RootElement.GetProperty("MetaData").GetProperty("literal").GetRawText(),
                actual.RootElement.GetProperty("MetaData").GetProperty("literal").GetRawText(),"History must not parse metadata strings as dates or narrow untyped JSON numbers.");
        }
    }

    private static string Write(object row)
        => (string)typeof(OracleDBOASIS).GetMethod("Serialize", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new[] { row })!;

    private static T Read<T>(string json)
        => (T)typeof(OracleDBOASIS).GetMethod("Deserialize", BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(typeof(T)).Invoke(null, new object[] { json })!;
}
