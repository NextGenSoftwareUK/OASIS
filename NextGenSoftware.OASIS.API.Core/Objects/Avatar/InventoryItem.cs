using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class InventoryItem : STARNETHolon, IInventoryItem
    {
        public InventoryItem()
        {
            this.HolonType = HolonType.InventoryItem;
        }

        public byte[] Image2D { get; set; }
        public byte[] Object3D { get; set; }
        public InventoryItemType InventoryItemType { get; set; }
    }
}