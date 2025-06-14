
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IOAPP : ISTARHolon
    {
        public OAPPType OAPPType { get; set; }
    }
}