
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class HolochainBaseDataObjectLoadedEventArgs : EventArgs
    {
        public IHolochainBaseDataObject HolochainBaseDataObject { get; set; }
    }

    public class HolochainBaseDataObjectSavedEventArgs : EventArgs
    {
        public IHolochainBaseDataObject HolochainBaseDataObject { get; set; }
    }

    public class ZomeErrorEventArgs : EventArgs
    {
        public string EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }
        public HoloNETErrorEventArgs HoloNETErrorDetails { get; set; }
    }
}
