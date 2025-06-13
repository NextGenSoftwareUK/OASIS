using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledInventoryItem : InstalledSTARHolon, IInstalledInventoryItem
    {
        public InstalledInventoryItem() : base("InventoryItemDNAJSON")
        {
            this.HolonType = HolonType.InstalledInventoryItem;
        }
    }
}