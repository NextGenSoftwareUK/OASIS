using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IInventoryItem : ISTARNETHolon
    {
        byte[] Image2D { get; set; }
        Uri Image2DURI { get; set; }
        byte[] Object3D { get; set; }
        Uri Object3DURI { get; set; }
        //InventoryItemType InventoryItemType { get; set; }
    }
}