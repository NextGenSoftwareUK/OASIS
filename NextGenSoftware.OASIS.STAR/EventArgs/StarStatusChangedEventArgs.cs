
using NextGenSoftware.OASIS.STAR.Enums;

namespace NextGenSoftware.OASIS.STAR.EventArgs
{
    public class StarStatusChangedEventArgs : System.EventArgs
    {
        public StarStatus Status { get; set; }
    }
}
