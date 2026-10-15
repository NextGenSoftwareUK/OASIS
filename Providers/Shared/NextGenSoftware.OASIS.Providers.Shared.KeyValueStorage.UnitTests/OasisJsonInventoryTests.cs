using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class OasisJsonInventoryTests
{
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
