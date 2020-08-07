
using System.Configuration;

namespace NextGenSoftware.Holochain.HDK.HoloNET.Client.Core
{
    public class HoloNETConfig
    {
        public int TimeOutSeconds { get; set; }
        public bool NeverTimeOut { get; set; }
        public int KeepAliveSeconds { get; set; }
        public int ReconnectionAttempts { get; set; }                                                                                                                                             
        public int ReconnectionIntervalSeconds { get; set; }
        public int SendChunkSize { get; set; }
        public int ReceiveChunkSize { get; set; }
        public ErrorHandlingBehaviour ErrorHandlingBehaviour { get; set; }
        //public HolochainConductorBehaviour HolochainConductorBehaviour { get; set; }
        public string FullPathToExternalHolochainConductor { get; set; }
        public string FullPathToHolochainAppDNA { get; set; }
        public int SecondsToWaitForHolochainConductorToStart { get; set; }

        public bool AutoStartConductor { get; set; }
        public bool AutoShutdownConductor { get; set; }
    }

    /*
    public enum HolochainConductorBehaviour
    {
        UseExternalRunningConductor,
        AutoStartExternalConductor
       // UseInternalConductor
    }
    */
    public enum ErrorHandlingBehaviour
    {
        AlwaysThrowExceptionOnError,
        OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent,
        NeverThrowExceptions
    }
}
