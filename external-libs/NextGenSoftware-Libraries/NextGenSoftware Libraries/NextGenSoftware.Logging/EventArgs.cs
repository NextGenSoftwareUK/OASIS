using System;

namespace NextGenSoftware.Logging
{
    public class LoggingErrorEventArgs : EventArgs
    {
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }
    }
}
