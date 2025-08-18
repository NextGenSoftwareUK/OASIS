using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class InventoryItemManager : STARNETManagerBase<InventoryItem, DownloadedInventoryItem, InstalledInventoryItem, InventoryItemDNA>
    public class InventoryItemManager : STARNETManagerBase<InventoryItem, DownloadedInventoryItem, InstalledInventoryItem, STARNETDNA>
    {
        public InventoryItemManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId,
            OASISDNA,
            typeof(InventoryItemType),
            HolonType.InventoryItem,
            HolonType.InstalledInventoryItem,
            "Inventory Item",
            //"InventoryItemId",
            "STARNETHolonId",
            "InventoryItemName",
            "InventoryItemType",
            "oinventoryitem",
            "oasis_inventoryitems",
            "InventoryItemDNA.json",
            "InventoryItemDNAJSON")
        { }

        public InventoryItemManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(InventoryItemType),
            HolonType.InventoryItem,
            HolonType.InstalledInventoryItem,
            "Inventory Item",
            //"InventoryItemId",
            "STARNETHolonId",
            "InventoryItemName",
            "InventoryItemType",
            "oinventoryitem",
            "oasis_inventoryitems",
            "InventoryItemDNA.json",
            "InventoryItemDNAJSON")
        { }
    }
}