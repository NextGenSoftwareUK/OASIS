using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using NextGenSoftware.Logging.Interfaces;

namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IHoloNETClientBase
    {
        Uri EndPoint { get; set; }
        IHoloNETDNA HoloNETDNA { get; set; }
        bool IsConnecting { get; set; }
        bool IsDisconnecting { get; set; }
        bool IsHoloNETDNALoaded { get; }
        ILogger Logger { get; set; }
        WebSocketState State { get; }
        WebSocket.WebSocket WebSocket { get; set; }

        event HoloNETClientBase.Connected OnConnected;
        event HoloNETClientBase.DataReceived OnDataReceived;
        event HoloNETClientBase.DataSent OnDataSent;
        event HoloNETClientBase.Disconnected OnDisconnected;
        event HoloNETClientBase.Error OnError;
        event HoloNETClientBase.HolochainConductorsShutdownComplete OnHolochainConductorsShutdownComplete;
        event HoloNETClientBase.HolochainConductorStarted OnHolochainConductorStarted;
        event HoloNETClientBase.HolochainConductorStarting OnHolochainConductorStarting;
        event HoloNETClientBase.HoloNETShutdownComplete OnHoloNETShutdownComplete;

        void ClearCache(bool clearPendingRequsts = false);
        HoloNETConnectedEventArgs Connect(string holochainConductorURI = "", bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = false, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateIHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true);
        HoloNETConnectedEventArgs Connect(Uri holochainConductorURI, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateIHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true);
        Task<HoloNETConnectedEventArgs> ConnectAsync(string holochainConductorURI = "", ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateIHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true);
        Task<HoloNETConnectedEventArgs> ConnectAsync(Uri holochainConductorURI, ConnectedCallBackMode connectedCallBackMode = ConnectedCallBackMode.WaitForHolochainConductorToConnect, RetrieveAgentPubKeyAndDnaHashMode retrieveAgentPubKeyAndDnaHashMode = RetrieveAgentPubKeyAndDnaHashMode.Wait, bool retrieveAgentPubKeyAndDnaHashFromConductor = true, bool retrieveAgentPubKeyAndDnaHashFromSandbox = true, bool automaticallyAttemptToRetrieveFromConductorIfSandBoxFails = true, bool automaticallyAttemptToRetrieveFromSandBoxIfConductorFails = true, bool updateIHoloNETDNAWithAgentPubKeyAndDnaHashOnceRetrieved = true);
        byte[] ConvertHoloHashToBytes(string hash);
        string ConvertHoloHashToString(byte[] bytes);
        HoloNETDisconnectedEventArgs Disconnect(ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseHoloNETDNASettings);
        Task<HoloNETDisconnectedEventArgs> DisconnectAsync(DisconnectedCallBackMode disconnectedCallBackMode = DisconnectedCallBackMode.WaitForHolochainConductorToDisconnect, ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseHoloNETDNASettings);
        byte[][] GetCellId();
        byte[][] GetCellId(byte[] DnaHash, byte[] AgentPubKey);
        byte[][] GetCellId(string DnaHash, string AgentPubKey);
        Task<byte[][]> GetCellIdAsync();
        IHoloNETDNA LoadDNA();
        bool SaveDNA();
        void SendHoloNETRequest(byte[] data, HoloNETRequestType requestType, string id = "");
        void SendHoloNETRequest(HoloNETData holoNETData, HoloNETRequestType requestType, string id = "");
        Task SendHoloNETRequestAsync(byte[] data, HoloNETRequestType requestType, string id = "");
        Task SendHoloNETRequestAsync(HoloNETData holoNETData, HoloNETRequestType requestType, string id = "");
        HolochainConductorsShutdownEventArgs ShutDownHolochainConductors(ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseHoloNETDNASettings);
        Task<HolochainConductorsShutdownEventArgs> ShutDownHolochainConductorsAsync(ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseHoloNETDNASettings);
        HoloNETShutdownEventArgs ShutdownHoloNET(ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseHoloNETDNASettings);
        Task<HoloNETShutdownEventArgs> ShutdownHoloNETAsync(DisconnectedCallBackMode disconnectedCallBackMode = DisconnectedCallBackMode.WaitForHolochainConductorToDisconnect, ShutdownHolochainConductorsMode shutdownHolochainConductorsMode = ShutdownHolochainConductorsMode.UseHoloNETDNASettings);
        void StartHolochainConductor();
        Task StartHolochainConductorAsync();
    }
}