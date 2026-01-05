using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using Newtonsoft.Json.Linq;
using NextGenSoftware.WebSocket;

namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    public class HoloNETErrorEventArgs : EventArgs
    {
        public string EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }
    }

    public class HoloNETDataReceivedEventArgs : DataReceivedEventArgs
    {
        public bool IsConductorDebugInfo { get; set; }
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
        public SignalsCallBackEventArgs(string id, string endPoint, bool isCallSuccessful, string rawJSONData, SignalTypes signalType, string name, JToken args, WebSocketReceiveResult webSocketResult)
            : base(id, endPoint, isCallSuccessful, rawJSONData, webSocketResult)
        {
            this.SignalType = signalType;
            this.Name = name;
            this.Arguments = args;
        }

        public enum SignalTypes
        {
            User
        }

        //TODO: Check Signals Return Data And Add Properties Here
       public SignalTypes SignalType { get; set; }
       public string Name { get; set; }
       public JToken Arguments { get; set; }
    }

    public class ConductorDebugCallBackEventArgs 
    {
        public string Type { get; set; }
        public int NumberHeldEntries { get; set; }
        public int NumberHeldAspects { get; set; }
        public int NumberPendingValidations { get; set; }
        public int NumberDelayedValidations { get; set; }
        public int NumberRunningZomeCalls { get; set; }
        public bool Offline { get; set; }
        public string EndPoint { get; set; }
        public string RawJSONData { get; set; }
        public WebSocketReceiveResult WebSocketResult { get; set; }
    }
}