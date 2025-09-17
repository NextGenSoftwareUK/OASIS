

namespace NextGenSoftware.OASIS.Common
{
    public class OASISEventArgs<T> : EventArgs
    {
        public OASISResult<T> Result { get; set; }
    }

    public class OASISErrorEventArgs<T> : OASISEventArgs<T>
    {
        public string EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception Exception { get; set; }
    }

    public class OASISErrorEventArgs : EventArgs
    {
        public string EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception Exception { get; set; }
    }

    public class OASISWarningEventArgs : EventArgs
    {
        public string EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception Exception { get; set; }
    }

}
