
using NextGenSoftware.OASIS.STAR.Enums;

namespace NextGenSoftware.OASIS.STAR.EventArgs
{
    public class StarStatusChangedEventArgs : System.EventArgs
    {
        public StarStatus Status { get; set; }
        public StarStatusMessageType MessageType { get; set; }
        public string Message { get; set; }
        
    }
}