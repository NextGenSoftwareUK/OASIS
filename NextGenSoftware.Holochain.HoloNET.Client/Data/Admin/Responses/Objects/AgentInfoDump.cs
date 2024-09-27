
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class AgentInfoDump
    {
        [Key("kitsune_agent")]
        public KitsuneAgent kitsune_agent { get; set; }

        [Key("kitsune_agent")]
        public KitsuneSpace kitsune_space { get; set; }

        [Key("dump")]
        public string dump { get; set; }
    }
}