
using NextGenSoftware.Holochain.HoloNET.Client.Core;

namespace NextGenSoftware.OASIS.STAR
{
    public class StarErrorEventArgs : ZomeErrorEventArgs
    {
        public HoloNETErrorEventArgs HoloNETErrorDetails { get; set; }
    }
}
