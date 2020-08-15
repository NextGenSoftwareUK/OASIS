
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core.CSharpTemplates
{
    public class Test2LoadedEventArgs : EventArgs
    {
        public ITest2 Test2 { get; set; }
    }

    public class Test2SavedEventArgs : EventArgs
    {
        public ITest2 Test2 { get; set; }
    }

    public class Test2ZomeErrorEventArgs : EventArgs
    {
        public string EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }
        public HoloNETErrorEventArgs HoloNETErrorDetails { get; set; }
    }
}
