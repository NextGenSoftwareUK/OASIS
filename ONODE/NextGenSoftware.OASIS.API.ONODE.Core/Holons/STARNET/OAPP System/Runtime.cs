using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class Runtime : STARNETHolon, IRuntime
    {
        public Runtime() : base("RuntimeDNAJSON")
        {
            this.HolonType = HolonType.Runtime;
        }
    }
}