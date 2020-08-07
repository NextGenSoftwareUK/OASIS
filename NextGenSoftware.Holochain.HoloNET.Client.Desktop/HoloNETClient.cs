
using NextGenSoftware.Holochain.HDK.HoloNET.Client.Core;

namespace NextGenSoftware.Holochain.HDK.HoloNET.Client.Desktop
{
    public class HoloNETClient : HoloNETClientBase
    {
        public HoloNETClient(string holochainURI) : base(holochainURI)
        {
            this.Logger = new NLogger();
        }


    }
}
