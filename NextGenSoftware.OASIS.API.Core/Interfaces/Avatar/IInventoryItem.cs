using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IInventoryItem : ISTARNETHolon
    {
        byte[] Image2D { get; set; }
        byte[] Object3D { get; set; }
        InventoryItemType InventoryItemType { get; set; }
    }
}