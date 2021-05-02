
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.DNA.Enums;

namespace NextGenSoftware.Holochain.HoloNET.Client.Unity
{
    public class HoloNETClient : HoloNETClientBase
    {
        public HoloNETClient(string holochainConductorURI, HolochainVersion version) : base(holochainConductorURI, version)
        {
            //TODO: Add Unity Compat Logger Here (hopefully the Unity NLogger Download/Asset I found)
            // this.Logger = new NLogger();
            this.Logger = new DumbyLogger();
        }
    }
}
