
using NextGenSoftware.WebSocket;

namespace NextGenSoftware.Holochain.HoloNET.Client.Core
{
    public class HoloNETConfig
    {
        public string AgentPubKey { get; set; } = "uhC0kTMixTG0lNZCF4SZfQMGozf2WfjQht7E06_wy3h29-zPpWxPQ";
        public string HoloHash { get; set; } = "uhCAkt_cNGyYJZIp08b2ZzxoE6EqPndRPb_WwjVkM_mOBcFyq7zCw";
        public string FullPathToExternalHolochainConductor { get; set; }
        public string FullPathToHolochainAppDNA { get; set; }
        public int SecondsToWaitForHolochainConductorToStart { get; set; } = 5;
        public bool AutoStartConductor { get; set; } = true;
        public bool AutoShutdownConductor { get; set; } = true;
        public ErrorHandlingBehaviour ErrorHandlingBehaviour { get; set; } = ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent;
    }
}