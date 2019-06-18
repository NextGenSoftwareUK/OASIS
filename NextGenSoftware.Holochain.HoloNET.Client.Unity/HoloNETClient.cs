
using NextGenSoftware.Holochain.HoloNET.Client.Core;

namespace NextGenSoftware.Holochain.HoloNET.Client.Unity
{
    public class HoloNETClient : HoloNETClientBase
    {
        public HoloNETClient(string holochainURI) : base(holochainURI)
        {
            //TODO: Add Unity Compat Logger Here (hopefully the Unity NLogger Download/Asset I found)
            // this.Logger = new NLogger();
            this.Logger = new DumbyLogger();
        }


    }
}
