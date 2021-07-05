
using NextGenSoftware.Holochain.HoloNET.Client.Core;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class StarErrorEventArgs : OASIS.API.Core.ZomeErrorEventArgs
    {
        public HoloNETErrorEventArgs HoloNETErrorDetails { get; set; }
    }
}
