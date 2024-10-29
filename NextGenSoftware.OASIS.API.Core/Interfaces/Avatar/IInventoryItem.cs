
namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IInventoryItem : IPublishableHolon
    {
        //int Quantity { get; set; }
        byte[] Image2D { get; set; }
        byte[] Object3D { get; set; }

        //InventoryItemData InventoryItemData { get; set; }
    }
}