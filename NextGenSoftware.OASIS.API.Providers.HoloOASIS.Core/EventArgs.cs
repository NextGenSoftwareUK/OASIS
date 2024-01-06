using System;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class AvatarLoadedEventArgs : EventArgs
    {
        public IAvatar Avatar { get; set; }
        public IHcAvatar HcAvatar { get; set; }
    }

    public class AvatarSavedEventArgs : EventArgs
    {
        //public string AvatarEntryHash { get; set; }
        public IAvatar Avatar { get; set; }
        public IHcAvatar HcAvatar { get; set; }
    }

    public class HoloOASISErrorEventArgs : EventArgs
    {
        public string EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }

        public HoloNETErrorEventArgs HoloNETErrorDetails { get; set; }
    }
}
