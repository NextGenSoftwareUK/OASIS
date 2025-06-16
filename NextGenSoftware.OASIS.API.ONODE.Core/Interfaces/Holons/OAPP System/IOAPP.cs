
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IOAPP : ISTARNETHolon
    {
        public OAPPType OAPPType { get; set; }
    }
}