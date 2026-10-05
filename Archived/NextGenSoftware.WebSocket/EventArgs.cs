using System;
using System.Net.WebSockets;

namespace NextGenSoftware.WebSocket
{
    public class ConnectedEventArgs : EventArgs
    {
        public Uri EndPoint { get; set; }
    }

    public class DisconnectedEventArgs : EventArgs
    {
        public Uri EndPoint { get; set; }
        public string Reason { get; set; }
    }

    public class WebSocketErrorEventArgs : EventArgs
    {
        public Uri EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }
    }

    public class DataReceivedEventArgs : CallBackWithDataReceivedBaseEventArgs
    {

    }

    public class DataSentEventArgs : CallBackWithDataBaseEventArgs
    {

    }

    public abstract class CallBackBaseEventArgs : EventArgs
    {
        public bool IsError { get; set; }
        public bool IsWarning { get; set; }
        public string Message { get; set; }
    }

    public abstract class CallBackWithDataReceivedBaseEventArgs : CallBackWithDataBaseEventArgs
    {
        public WebSocketReceiveResult WebSocketResult { get; set; }
    }

    public abstract class CallBackWithDataBaseEventArgs : CallBackBaseEventArgs
    {
        public CallBackWithDataBaseEventArgs()
        {

        }

        //public CallBackWithDataBaseEventArgs(Uri endPoint, bool isCallSuccessful, byte[] rawBinaryData, string rawJSONData)
        //{
        //    EndPoint = endPoint;
        //    IsCallSuccessful = isCallSuccessful;
        //    RawJSONData = rawJSONData;
        //    RawBinaryData = rawBinaryData;
        //}

        public CallBackWithDataBaseEventArgs(Uri endPoint, byte[] rawBinaryData, string rawJSONData)
        {
            EndPoint = endPoint;
            RawJSONData = rawJSONData;
            RawBinaryData = rawBinaryData;
        }

        public Exception Exception { get; set; }
        public Uri EndPoint { get; set; }
        public string RawJSONData { get; set; }
        public byte[] RawBinaryData { get; set; }
        public string RawBinaryDataAsString { get; set; }
        public string RawBinaryDataDecoded { get; set; }
    }

    public abstract class CallBackBaseEventArgsWithId : CallBackWithDataBaseEventArgs
    {
        public CallBackBaseEventArgsWithId() : base()
        {
          
        }

        public string Id { get; set; }
    }

    public abstract class CallBackBaseEventArgsWithDataAndId : CallBackWithDataReceivedBaseEventArgs
    {
        public CallBackBaseEventArgsWithDataAndId() : base()
        {

        }

        public string Id { get; set; }
    }
}