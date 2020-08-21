
using NextGenSoftware.Holochain.HoloNET.Client.Core;

namespace NextGenSoftware.Holochain.HoloNET.Client.Desktop
{
    public class HoloNETClient : HoloNETClientBase
    {
        public HoloNETClient(string holochainConductorURI) : base(holochainConductorURI)
        {
            this.Logger = new NLogger();
        }


    }
}
