
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core.CSharpTemplates
{
    public class TestLoadedEventArgs : EventArgs
    {
        public ITest Test { get; set; }
    }

    public class TestSavedEventArgs : EventArgs
    {
        public ITest Test { get; set; }
    }

    public class MyZomeErrorEventArgs : EventArgs
    {
        public string EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }
        public HoloNETErrorEventArgs HoloNETErrorDetails { get; set; }
    }
}
