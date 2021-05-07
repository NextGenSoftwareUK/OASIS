
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class InventoryItem : IInventoryItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
    }
}