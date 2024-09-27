
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class P2pAgentsDump
    {
        [Key("this_agent_info")]
        public AgentInfoDump this_agent_info { get; set; }

        [Key("this_agent_info")]
        public byte[] this_dna { get; set; } //Option<(DnaHash, KitsuneSpace)>

        [Key("this_agent")]
        public byte[] this_agent { get; set; } //Option<(AgentPubKey, KitsuneAgent)>,

        [Key("this_agent")]
        public AgentInfoDump[] peers { get; set; }
    }
}