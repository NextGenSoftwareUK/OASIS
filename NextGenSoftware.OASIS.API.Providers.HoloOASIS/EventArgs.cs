using NextGenSoftware.OASIS.API.Core;
using System;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class ProfileLoadedEventArgs : EventArgs
    {
        public IProfile Profile { get; set; }
    }

    public class ProfileSavedEventArgs : EventArgs
    {
        public string ProfileEntryHash { get; set; }
    }

    public class ErrorEventArgs : EventArgs
    {
        public string EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }

        public Holochain.HoloNET.Client.Core.ErrorEventArgs HoloNETErrorDetails { get; set; }
    }
}
