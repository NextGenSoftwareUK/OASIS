using System;

namespace NextGenSoftware.CLI.Engine
{
    public class CLIEngineErrorEventArgs : EventArgs
    {
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }
    }
}
