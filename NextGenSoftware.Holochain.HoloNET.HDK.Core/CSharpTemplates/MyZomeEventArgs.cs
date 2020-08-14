
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core.CSharpTemplates
{
    public class MyClassLoadedEventArgs : EventArgs
    {
        public IMyClass MyClass { get; set; }
    }

    public class MyClassSavedEventArgs : EventArgs
    {
        public IMyClass MyClass { get; set; }
    }

    public class MyZomeErrorEventArgs : EventArgs
    {
        public string EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }
        public HoloNETErrorEventArgs HoloNETErrorDetails { get; set; }
    }
}
