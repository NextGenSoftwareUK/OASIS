using System;

namespace NextGenSoftware.WebSocket
{
    public class WebSocketException : Exception
    {
        public Uri EndPoint { get; set; }

        public WebSocketException()
        {

        }

        public WebSocketException(string message) : base(message)
        {

        }

        public WebSocketException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public WebSocketException(string message, Exception innerException, Uri endPoint) : base(message, innerException)
        {
            this.EndPoint = endPoint;
        }
    }

    
}
