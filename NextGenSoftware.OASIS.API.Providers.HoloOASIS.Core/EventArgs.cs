using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core
{
    public class ProfileLoadedEventArgs : EventArgs
    {
        public IProfile Profile { get; set; }
        public IHcProfile HcProfile { get; set; }
    }

    public class ProfileSavedEventArgs : EventArgs
    {
        //public string ProfileEntryHash { get; set; }
        public IProfile Profile { get; set; }
        public IHcProfile HcProfile { get; set; }
    }

    public class HoloOASISErrorEventArgs : EventArgs
    {
        public string EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }

        public HoloNETErrorEventArgs HoloNETErrorDetails { get; set; }
    }
}
