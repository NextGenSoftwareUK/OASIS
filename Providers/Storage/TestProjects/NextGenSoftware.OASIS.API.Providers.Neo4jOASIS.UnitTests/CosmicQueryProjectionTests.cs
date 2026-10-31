using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class CosmicQueryProjectionTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void DeclaredEmptyMembershipIsDistinctFromParentLinkRepresentation(bool declared)
    {
        var owner=new Core.Holons.Holon{Id=Guid.NewGuid()};
        if(declared)owner.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(
            new Core.Holons.HolonChildRoleManifest(1,owner.Id,new[]{new Core.Holons.HolonChildRole("Children",Array.Empty<Guid>())}));
        var map=NeoCqlProjection.Build(Core.Serialization.StoredRecordJson.Serialize(owner));
        Assert.AreEqual(3L,map["SchemaVersion"]);Assert.AreEqual(declared,map["f_HasChildManifest"]);
        Assert.AreEqual(0,((string[])map["f_ChildIds"]).Length);
    }

    [TestMethod]
    public void MembershipProjectionUsesCanonicalRolesRatherThanMetadataLabels()
    {
        var owner=new Core.Holons.Holon{Id=Guid.NewGuid()};var member=Guid.NewGuid();
        owner.MetaData["f_ChildIds"]=new[]{Guid.NewGuid().ToString()};
        owner.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(
            new Core.Holons.HolonChildRoleManifest(1,owner.Id,new[]{new Core.Holons.HolonChildRole("Extras",new[]{member})}));
        var map=NeoCqlProjection.Build(Core.Serialization.StoredRecordJson.Serialize(owner));
        CollectionAssert.AreEqual(new[]{member.ToString("D")},(string[])map["f_ChildIds"]);
    }

    [TestMethod]
    public void AForeignRoleObservationCannotBecomeANativeMembershipProjection()
    {
        var owner=new Core.Holons.Holon{Id=Guid.NewGuid()};
        owner.ChildRoleManifestJson=Core.Serialization.HolonChildRoleManifestJson.Serialize(
            new Core.Holons.HolonChildRoleManifest(1,Guid.NewGuid(),new[]{new Core.Holons.HolonChildRole("Children",Array.Empty<Guid>())}));
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>NeoCqlProjection.Build(Core.Serialization.StoredRecordJson.Serialize(owner)));
    }

    [TestMethod]
    public void HistoricalDefaultsAndGuidCasingMatchCanonicalMaterialization()
    {
        var id=Guid.NewGuid();
        var map=NeoCqlProjection.Build("{\"Id\":\""+id.ToString("D").ToUpperInvariant()+"\"}");
        Assert.AreEqual(id.ToString("D"),map["f_Id"]);
        Assert.AreEqual(true,map["f_IsActive"]);
        Assert.AreEqual(false,map["f_IsPublic"]);
        Assert.AreEqual(0L,map["f_Version"]);
        Assert.AreEqual(Guid.Empty.ToString("D"),map["f_CreatedByAvatarId"]);
    }

    [TestMethod]
    public void CanonicalQueryFieldsComeFromTheFinalSnapshotWithoutNumericCoercion()
    {
        var map = NeoCqlProjection.Build("{\"Version\":7,\"Name\":\"stamped\",\"IsActive\":false,\"IsPublic\":true,\"Description\":null}");
        Assert.AreEqual(7L, map["f_Version"]);
        Assert.AreEqual("stamped", map["f_Name"]);
        Assert.AreEqual(false, map["f_IsActive"]);
        Assert.AreEqual(true, map["f_IsPublic"]);
        Assert.IsFalse(map.ContainsKey("f_Description"));
    }

    [TestMethod]
    public void NestedPathsAndLiteralKeysCannotCollideAndNullIsPresent()
    {
        var map = NeoCqlProjection.Build("{\"MetaData\":{\"a.b\":1,\"a\":{\"b\":null},\"\":false,\"array\":[1]}}");
        var literal = NeoCqlProjection.PathKey(new[] { "a.b" });
        var nested = NeoCqlProjection.PathKey(new[] { "a", "b" });
        Assert.AreNotEqual(literal, nested);
        Assert.AreEqual(1L, map[literal + "_value"]);
        Assert.AreEqual("null", map[nested + "_kind"]);
        Assert.IsFalse(map.ContainsKey(nested + "_value"));
        Assert.IsFalse(map.ContainsKey(NeoCqlProjection.PathKey(new[] { "missing" }) + "_kind"));
        Assert.AreEqual(false, map[NeoCqlProjection.PathKey(new[] { "" }) + "_value"]);
        Assert.AreEqual("array", map[NeoCqlProjection.PathKey(new[] { "array" }) + "_kind"]);
    }

    [TestMethod]
    public void ExactNumericDomainsDoNotNarrowOrStringifyIntoIntegerMatches()
    {
        var map = NeoCqlProjection.Build("{\"MetaData\":{\"max\":9223372036854775807,\"wide\":18446744073709551615,\"fraction\":1.000000000000000000000000001,\"decimal\":1.0,\"text\":\"1\"}}");
        Assert.AreEqual(long.MaxValue, map[NeoCqlProjection.PathKey(new[] { "max" }) + "_value"]);
        foreach (var key in new[] { "wide", "fraction", "decimal" })
            Assert.AreEqual("number", map[NeoCqlProjection.PathKey(new[] { key }) + "_kind"]);
        Assert.AreEqual("1.000000000000000000000000001", map[NeoCqlProjection.PathKey(new[] { "fraction" }) + "_value"]);
        Assert.AreEqual("string", map[NeoCqlProjection.PathKey(new[] { "text" }) + "_kind"]);
    }

    [TestMethod]
    public void CorruptOrDuplicateMetadataIsRejectedRatherThanPartiallyProjected()
    {
        foreach (var json in new[] { "[]", "{\"MetaData\":null}", "{\"MetaData\":[]}", "{\"MetaData\":{\"x\":1,\"x\":2}}",
            "{\"MetaData\":{},\"metadata\":{}}" })
            Assert.ThrowsException<System.IO.InvalidDataException>(() => NeoCqlProjection.Build(json));
    }
}
