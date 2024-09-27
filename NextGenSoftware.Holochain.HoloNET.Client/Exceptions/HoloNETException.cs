using System;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public class HoloNETException : Exception
    {
        public Uri EndPoint { get; set; }

        public HoloNETException()
        {

        }

        public HoloNETException(string message) : base(message)
        {

        }

        public HoloNETException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public HoloNETException(string message, Exception innerException, Uri endPoint) : base(message, innerException)
        {
            this.EndPoint = endPoint;
        }
    }
}