namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IFullStateDumpedResponse
    {
        FullIntegrationStateDump integration_dump { get; set; }
        P2pAgentsDump peer_dump { get; set; }
        SourceChainJsonDump source_chain_dump { get; set; }
    }
}