using System;

namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    public class HoloNETException : Exception
    {
        public string EndPoint { get; set; }

        public HoloNETException()
        {

        }

        public HoloNETException(string message) : base(message)
        {

        }

        public HoloNETException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public HoloNETException(string message, Exception innerException, string endPoint) : base(message, innerException)
        {
            this.EndPoint = endPoint;
        }
    }
}