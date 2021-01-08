
using NextGenSoftware.Holochain.HoloNET.Client.Core;

namespace NextGenSoftware.OASIS.API.STAR
{
    public class StarErrorEventArgs : OASIS.API.Core.ZomeErrorEventArgs
    {
        public HoloNETErrorEventArgs HoloNETErrorDetails { get; set; }
    }
}
