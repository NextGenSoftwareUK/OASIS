using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class OasisJsonInventoryTests
{
    private sealed class InventoryExtension:InventoryItem { public string ExtensionOnly {get;set;} }

    [TestMethod]
    public void CanonicalNavigationRuleKeepsOwnedInventoryButNotHydratedEdges()
    {
        Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.IsNavigationMember(typeof(NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar)));
        Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.IsNavigationMember(typeof(System.Collections.Generic.IList<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>)));
        Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.IsNavigationMember(typeof(System.Collections.Generic.IList<NextGenSoftware.OASIS.API.Core.Interfaces.IInventoryItem>)));
        Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.IsNavigationMember(typeof(Guid)));
    }

    [TestMethod]
    public void ConcreteProjectionPreservesInheritedOwnedValuesAndSkipsNavigation()
    {
        NextGenSoftware.OASIS.API.Core.Interfaces.IInventoryItem source=new InventoryExtension{
            Id=Guid.NewGuid(),Version=17,VersionId=Guid.NewGuid(),IsPublic=true,Image2D=new byte[]{1,2,3},
            ExtensionOnly="not-a-storage-type"};
        source.Children.Add(new Holon{Id=Guid.NewGuid(),Name="navigation-must-not-be-embedded"});
        var projected=OasisJson.Project<InventoryItem>(source);
        Assert.AreEqual(typeof(InventoryItem),projected.GetType());
        Assert.AreEqual(source.Id,projected.Id);Assert.AreEqual(source.Version,projected.Version);
        Assert.AreEqual(source.VersionId,projected.VersionId);Assert.AreEqual(source.IsPublic,projected.IsPublic);
        CollectionAssert.AreEqual(source.Image2D,projected.Image2D);Assert.AreNotSame(source.Image2D,projected.Image2D);
        Assert.AreEqual(0,projected.Children.Count);
        Assert.IsNull(OasisJson.Project<InventoryItem>(null));
    }

    [TestMethod]
    public void OwnedInventoryValuesSurviveWithoutHolonNavigation()
    {
        var avatar=new AvatarDetail {Id=Guid.NewGuid()};
        var item=new InventoryItem {Id=Guid.NewGuid(),Name="owned seed",Quantity=7,GameSource="Our World"};
        item.Children.Add(new Holon {Id=Guid.NewGuid(),Name="navigation-must-not-be-embedded"});
        avatar.Inventory.Add(item);
        var json=OasisJson.Serialize(avatar);
        StringAssert.Contains(json,"owned seed");
        Assert.IsFalse(json.Contains("navigation-must-not-be-embedded",StringComparison.Ordinal));
        var loaded=OasisJson.Deserialize<AvatarDetail>(json);
        Assert.AreEqual(1,loaded.Inventory.Count);
        Assert.IsInstanceOfType<InventoryItem>(loaded.Inventory[0]);
        Assert.AreEqual(item.Id,loaded.Inventory[0].Id);
        Assert.AreEqual(7,loaded.Inventory[0].Quantity);
        Assert.AreEqual("Our World",loaded.Inventory[0].GameSource);
    }
}
