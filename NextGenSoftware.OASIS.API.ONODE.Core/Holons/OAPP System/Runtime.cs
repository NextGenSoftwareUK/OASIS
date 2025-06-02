using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class Runtime : OAPPSystemHolon, IRuntime
    {
        public Runtime() : base("RuntimeDNAJSON")
        {
            this.HolonType = HolonType.Runtime;
        }
    }
}