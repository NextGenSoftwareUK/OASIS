
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    [MessagePackObject]
    public class AgentInfoDetailed
    {
        [Key("agent")]
        public byte[] agent { get; set; }

        [Key("signature")]
        public byte[] signature { get; set; }

        [Key("agent_info")]
        public byte[] agent_info { get; set; }
    }
}