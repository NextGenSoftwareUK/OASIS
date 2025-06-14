using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IInventoryItem : ISTARHolon
    {
        byte[] Image2D { get; set; }
        byte[] Object3D { get; set; }
        InventoryItemType InventoryItemType { get; set; }
    }
}