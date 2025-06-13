using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedInventoryItem : DownloadedSTARHolon, IDownloadedInventoryItem
    {
        public DownloadedInventoryItem() : base("InventoryItemDNAJSON")
        {
            this.HolonType = HolonType.DownloadedInventoryItem;
        }
    }
}