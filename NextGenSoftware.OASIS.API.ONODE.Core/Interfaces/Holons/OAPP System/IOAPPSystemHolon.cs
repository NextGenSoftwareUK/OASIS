using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces
{
    public interface IOAPPSystemHolon : IHolon // : IPublishableHolon
    {
        public IOAPPSystemHolonDNA OAPPSystemHolonDNA { get; set; }
        public byte[] PublishedOAPPSystemHolon { get; set; }
    }
}