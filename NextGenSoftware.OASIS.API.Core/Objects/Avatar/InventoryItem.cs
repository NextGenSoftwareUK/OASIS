
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class InventoryItem : PublishableHolon, IInventoryItem, IPublishableHolon
    {
        public InventoryItem() 
        {
            HolonType = Enums.HolonType.InventoryItem;
        }

        //public InventoryItemData InventoryItemData { get; set; }

        //public string Name { get; set; }
        //public string Description { get; set; }
        //public int Quantity { get; set; }
        public byte[] Image2D { get; set; }
        public byte[] Object3D { get; set; }
    }

    //public class InventoryItemData
    //{
    //    public byte[] Image2D { get; set; }
    //    public byte[] Object3D { get; set; }
    //}
}