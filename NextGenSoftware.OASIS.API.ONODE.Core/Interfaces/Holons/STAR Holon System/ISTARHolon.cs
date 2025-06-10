using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces
{
    public interface ISTARHolon : IHolon // : IPublishableHolon
    {
        public ISTARHolonDNA STARHolonDNA { get; set; }
        public byte[] PublishedSTARHolon { get; set; }
    }
}