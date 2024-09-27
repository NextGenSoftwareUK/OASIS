using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.WebSocket;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public class HoloNETDataReceivedBaseEventArgs : CallBackBaseEventArgsWithDataAndId
    {
        public string Type { get; set; }
        public HoloNETResponseType HoloNETResponseType { get; set; }
    }

    public class HoloNETDataReceivedBaseBaseEventArgs<T> : HoloNETDataReceivedBaseEventArgs
    {
        public T Response { get; set; }
    }

    public class HoloNETConnectedEventArgs : CallBackBaseEventArgs
    {
        public bool IsConnected { get; set; }
    }

    public class HoloNETDisconnectedEventArgs : CallBackBaseEventArgs
    {
        public bool IsDisconnected { get; set; }
    }

    public class HoloNETErrorEventArgs : EventArgs
    {
        public Uri EndPoint { get; set; }
        public string Reason { get; set; }
        public Exception ErrorDetails { get; set; }
    }

    public class HoloNETDataReceivedEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public bool IsConductorDebugInfo { get; set; }
    }

    public class HoloNETDataSentEventArgs : DataSentEventArgs
    {

    }

    public class ZomeFunctionCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public ZomeFunctionCallBackEventArgs() : base()
        {

        }

        public string Zome { get; set; }
        public string ZomeFunction { get; set; }
        public Dictionary<string, object> ZomeReturnData { get; set; }
        public Dictionary<object, object> RawZomeReturnData { get; set; }
        public string ZomeReturnHash { get; set; }
        public List<Record> Records { get; set; } = new List<Record>();
        public Dictionary<string, string> KeyValuePair { get; set; }
        public string KeyValuePairAsString { get; set; }
    }

    //public class ZomeFunctionCallBackForCollectionEventArgs : HoloNETDataReceivedBaseEventArgs
    //{
    //    public string Zome { get; set; }
    //    public string ZomeFunction { get; set; }
    //    public Dictionary<string, object> ZomeReturnData { get; set; }
    //    public Dictionary<object, object> RawZomeReturnData { get; set; }
    //    public string ZomeReturnHash { get; set; }
    //    public List<EntryData> Entries { get; set; }
    //    public Dictionary<string, string> KeyValuePair { get; set; }
    //    public string KeyValuePairAsString { get; set; }
    //}

    public class AppInfoCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public IAppInfoResponse AppInfoResponse { get; set; }
        public AppInfoStatusEnum AppStatus { get; set; } //TODO: Set
        public string AppStatusReason { get; set; } //TODO: Set
        public IAppManifest AppManifest { get; set; } //TODO: Set
        public string InstalledAppId { get; set; }
        public string DnaHash { get; set; }
        public string AgentPubKey { get; set; }
        public byte[][] CellId { get; set; }
        public CellInfoType CellType { get; set; } //TODO: Set
    }

    public class ReadyForZomeCallsEventArgs : CallBackBaseEventArgs
    {
        public ReadyForZomeCallsEventArgs(Uri endPoint, string dnaHash, string agentPubKey)
        {
            EndPoint = endPoint;
            DnaHash = dnaHash;
            AgentPubKey = agentPubKey;
        }

        public Uri EndPoint { get; set; }
        public string DnaHash { get; set; }
        public string AgentPubKey { get; set; }
    }

    public class HoloNETShutdownEventArgs : CallBackBaseEventArgs
    {
        public HoloNETShutdownEventArgs()
        {

        }

        public HoloNETShutdownEventArgs(Uri endPoint, string dnaHash, string agentPubKey, HolochainConductorsShutdownEventArgs holochainConductorsShutdownEventArgs)
        {
            EndPoint = endPoint;
            DnaHash = dnaHash;
            AgentPubKey = agentPubKey;
            HolochainConductorsShutdownEventArgs = holochainConductorsShutdownEventArgs;
        }

        public HolochainConductorsShutdownEventArgs HolochainConductorsShutdownEventArgs { get; private set; }
        public Uri EndPoint { get; set; }
        public string DnaHash { get; set; }
        public string AgentPubKey { get; set; }
    }


    public class HolochainConductorStartingEventArgs : CallBackBaseEventArgs
    {

    }

    public class HolochainConductorStartedEventArgs : CallBackBaseEventArgs
    {

    }

    public class HolochainConductorsShutdownEventArgs : CallBackBaseEventArgs
    {
        public int NumberOfHolochainExeInstancesShutdown { get; set; }
        public int NumberOfHcExeInstancesShutdown { get; set; }
        public int NumberOfRustcExeInstancesShutdown { get; set; }
        public Uri EndPoint { get; set; }
        public string DnaHash { get; set; }
        public string AgentPubKey { get; set; }
    }

    public class SignalCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public string AgentPubKey { get; set; }
        public string DnaHash { get; set; }
        public SignalType SignalType { get; set; }
        public ISignalData RawSignalData { get;set;}
        public Dictionary<string, object> SignalData { get; set; }
        public string SignalDataAsString { get; set; }
    }

    public class ConductorDebugCallBackEventArgs : CallBackBaseEventArgs
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