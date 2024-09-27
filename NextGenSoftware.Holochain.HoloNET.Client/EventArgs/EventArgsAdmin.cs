using System;
using System.Collections.Generic;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.WebSocket;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public class InstallEnableSignAndAttachHappEventArgs : CallBackBaseEventArgs
    {
        public bool IsSuccess { get; set; }
        public bool IsAgentPubKeyGenerated { get; set; }
        public bool IsAppInstalled { get; set; }
        public bool IsAppEnabled { get; set; }
        public bool IsAppSigned { get; set; }
        public bool IsAppAttached { get; set; }

        public string AgentPubKey { get; set; }
        public string DnaHash { get; set; }
        public byte[][] CellId { get; set; }
        public CellInfoType CellType { get; set; } = CellInfoType.None;
        public AppInfoStatusEnum AppStatus { get; set; }
        public string AppStatusReason { get; set; }
        public IAppManifest AppManifest { get; set; }
        public UInt16? AttachedOnPort { get; set; }

        public AgentPubKeyGeneratedCallBackEventArgs AgentPubKeyGeneratedResult { get; set; }
        public AppInstalledCallBackEventArgs AppInstalledResult { get; set; }
        public AppEnabledCallBackEventArgs AppEnabledResult { get; set; }
        public ZomeCallCapabilityGrantedCallBackEventArgs ZomeCallCapabilityGrantedResult { get; set; }
        public AppInterfaceAttachedCallBackEventArgs AppInterfaceAttachedResult { get; set; }
    }

    public class InstallEnableSignAttachAndConnectToHappEventArgs : InstallEnableSignAndAttachHappEventArgs
    {
        public bool IsAppConnected { get; set; }
        public IHoloNETClientAppAgent HoloNETClientAppAgent { get; set; }
        public HoloNETConnectedEventArgs HoloNETConnectedResult { get; set; }
    }

    public class AppInstalledCallBackEventArgs : AppInfoCallBackEventArgs
    {

    }

    public class AppUninstalledCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public string InstalledAppId { get; set; }
    }

    public class AgentPubKeyGeneratedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public IAppResponse AppResponse { get; set; }
        public string AgentPubKey { get; set; }
    }

    public class AppEnabledCallBackEventArgs : AppInfoCallBackEventArgs
    {
        public object Errors { get; set; }
    }

    public class AppDisabledCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public string InstalledAppId { get; set; }
    }

    public class ZomeCallCapabilityGrantedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        //public AppResponse AppResponse { get; set; }
    }

    public class AppInterfaceAttachedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public UInt16? Port { get; set; }
    }

    public class DnaRegisteredCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public byte[] HoloHash { get; set; }
    }

    public class DnaDefinitionReturnedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public IDnaDefinition DnaDefinition { get; set; }
    }

    public class AppInterfacesListedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public List<ushort> WebSocketPorts { get; set; } = new List<ushort>();
    }

    public class AppsListedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public List<IAppInfo> Apps { get; set; } = new List<IAppInfo>();
    }

    public class DnasListedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public List<byte[]> Dnas { get; set; } = new List<byte[]>();
    }

    public class CellIdsListedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public List<byte[][]> CellIds { get; set; } = new List<byte[][]>();
    }

    public class GetAppInfoCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public IAppInfo AppInfo { get; set; }
    }

    public class StateDumpedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public string DumpedStateJSON { get; set; }
    }

    public class FullStateDumpedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public IFullStateDumpedResponse DumpedState { get; set; }
    }

    public class CoordinatorsUpdatedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
 
    }

    public class AgentInfoReturnedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        //public IAgentInfo AgentInfo { get; set; }
        public AgentInfo AgentInfo { get; set; }
    }

    public class AgentInfoAddedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        //public AgentInfo AgentInfo { get; set; }
    }

    public class CloneCellDeletedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {

    }

    public class StorageInfoReturnedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public IStorageInfoResponse StorageInfoResponse { get; set; }
    }

    public class NetworkMetricsDumpedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public string NetworkMetricsDumpJSON { get; set; }
    }

    public class NetworkStatsDumpedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {
        public string NetworkStatsDumpJSON { get; set; }
    }

    public class RecordsGraftedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {

    }

    public class AdminInterfacesAddedCallBackEventArgs : HoloNETDataReceivedBaseEventArgs
    {

    }
}