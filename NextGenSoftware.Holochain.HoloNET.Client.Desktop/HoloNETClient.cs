
using NextGenSoftware.Holochain.HoloNET.Client.Core;

namespace NextGenSoftware.Holochain.HoloNET.Client.Desktop
{
    public class HoloNETClient : HoloNETClientBase
    {
        public HoloNETClient(string holochainConductorURI, HolochainVersion version) : base(holochainConductorURI, version, new NLogger())
        {

        }
    }
}
