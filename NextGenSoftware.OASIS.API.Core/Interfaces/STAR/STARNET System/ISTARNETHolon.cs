
namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ISTARNETHolon : IHolon
    {
        public ISTARNETDNA STARNETDNA { get; set; }
        public byte[] PublishedSTARNETHolon { get; set; }
    }
}