
namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IInventoryItem
    {
        string Description { get; set; }
        string Name { get; set; }
        int Quantity { get; set; }
        byte[] Image2D { get; set; }
        byte[] Image3D { get; set; }
    }
}