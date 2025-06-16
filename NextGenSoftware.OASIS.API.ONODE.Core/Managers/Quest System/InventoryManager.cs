using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class InventoryManager : STARNETManagerBase<InventoryItem, DownloadedInventoryItem, InstalledInventoryItem>
    {
        public InventoryManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId,
            OASISDNA,
            typeof(InventoryItemType),
            HolonType.InventoryItem,
            HolonType.InstalledInventoryItem,
            "Inventory",
            "InventoryId",
            "InventoryName",
            "InventoryType",
            "inventoryitem",
            "oasis_inventoryitems",
            "InventoryDNA.json",
            "InventoryDNAJSON")
        { }

        public InventoryManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(InventoryItemType),
            HolonType.InventoryItem,
            HolonType.InstalledInventoryItem,
            "Inventory",
            "InventoryId",
            "InventoryName",
            "InventoryType",
            "inventoryitem",
            "oasis_inventoryitems",
            "InventoryDNA.json",
            "InventoryDNAJSON")
        { }
    }
}