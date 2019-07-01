using System;
using System.Collections.Generic;
using System.Net.WebSockets;

namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    public class ConnectedEventArgs : EventArgs
    {
        //public ConnectedEventArgs(string endPoint)
        //{
        //    EndPoint = endPoint;
        //}

        public string EndPoint { get; set; }
    }

    public class DisconnectedEventArgs : EventArgs
    {
        //public DisconnectedEventArgs(string endPoint, string reason)
        //{
        //    EndPoint = endPoint;
        //    Reason = reason;
        //}

        public string EndPoint { get; set; }
        public string Reason { get; set; }
    }

    public class HoloNETErrorEventArgs : EventArgs
    {
        //public HoloNETErrorEventArgs(string endPoint, string reason, Exception errorDetails)
        //{
        //    EndPoint = endPoint;
        //    Reason = reason;
        //    ErrorDetails = errorDetails;
        //}

        //public HoloNETErrorEventArgs(string endPoint, string reason)
        //{
        //    EndPoint = endPoint;
        //    Reason = reason;
        //}

        public string EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }
    }

    public class DataReceivedEventArgs : EventArgs
    {
        //public DataReceivedEventArgs(string endPoint, string rawJSONData, WebSocketReceiveResult webSocketResult)
        //{
        //    EndPoint = endPoint;
        //    RawJSONData = rawJSONData;
        //    WebSocketResult = webSocketResult;
        //}

        public string EndPoint { get; set; }
        public string RawJSONData { get; set; }
        public WebSocketReceiveResult WebSocketResult { get; set; }
    }

    public class ZomeFunctionCallBackEventArgs : CallBackBaseEventArgs
    {
        public ZomeFunctionCallBackEventArgs(string id, string endPoint, string instance, string zome, string zomeFunction, bool isCallSuccessful, string rawZomeReturnData, string zomeReturnData, string rawJSONData, WebSocketReceiveResult webSocketResult)
            : base(id, endPoint, isCallSuccessful, rawJSONData, webSocketResult)
        {
            Instance = instance;
            Zome = zome;
            ZomeFunction = zomeFunction;
            RawZomeReturnData = rawZomeReturnData;
            ZomeReturnData = zomeReturnData;
        }

        public string Instance { get; private set; }
        public string Zome { get; private set; }
        public string ZomeFunction { get; private set; }
        public string ZomeReturnData { get; private set; }
        public string RawZomeReturnData { get; private set; }
    }

    public class GetInstancesCallBackEventArgs : CallBackBaseEventArgs
    {
        public GetInstancesCallBackEventArgs(string id, string endPoint, bool isCallSuccessful, string rawJSONData, List<string> instances, string dna, string agent, WebSocketReceiveResult webSocketResult) 
            : base(id, endPoint, isCallSuccessful, rawJSONData, webSocketResult)
        {
            Instances = instances;
            DNA = dna;
            Agent = agent;
        }

        public List<string> Instances { get; private set; }
        public string DNA { get; private set; }

        public string Agent { get; private set; }
    }

    public class SignalsCallBackEventArgs : CallBackBaseEventArgs
    {
        public SignalsCallBackEventArgs(string id, string endPoint, bool isCallSuccessful, string rawJSONData, WebSocketReceiveResult webSocketResult)
            : base(id, endPoint, isCallSuccessful, rawJSONData, webSocketResult)
        {
            
        }

        //TODO: Check Signals Return Data And Add Properties Here
        //public SignalType SignalType { get; set; }
    }

    public abstract class CallBackBaseEventArgs : EventArgs
    {
        public CallBackBaseEventArgs(string id, string endPoint, bool isCallSuccessful, string rawJSONData, WebSocketReceiveResult webSocketResult)
        {
            Id = id;
            EndPoint = endPoint;
            IsCallSuccessful = isCallSuccessful;
            RawJSONData = rawJSONData;
            WebSocketResult = webSocketResult;
        }

        public string Id { get; private set; }
        public string EndPoint { get; private set; }
        public bool IsCallSuccessful { get; private set; }
        public string RawJSONData { get; private set; }
        public WebSocketReceiveResult WebSocketResult { get; private set; }
    }

}
