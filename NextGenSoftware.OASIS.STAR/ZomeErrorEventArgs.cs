
using NextGenSoftware.Holochain.HoloNET.Client.Core;

namespace NextGenSoftware.OASIS.STAR
{
    /*
    public class ZomesLoadedEventArgs : EventArgs
    {
        public List<IZome> Zomes { get; set; }
    }

    public class HolonLoadedEventArgs : EventArgs
    {
        public IHolon Holon { get; set; }
    }

    public class HolonsLoadedEventArgs : EventArgs
    {
        public List<IHolon> Holons { get; set; }
    }

    public class HolonSavedEventArgs : EventArgs
    {
        public IHolon Holon { get; set; }
    }
    */

    public class ZomeErrorEventArgs : OASIS.API.Core.ZomeErrorEventArgs
    {
        //public string EndPoint { get; set; }
        //public string Reason { get; set; }
        //public Exception ErrorDetails { get; set; }
        public HoloNETErrorEventArgs HoloNETErrorDetails { get; set; }
    }
}
