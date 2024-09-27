
using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests
{
    [MessagePackObject]
    public class AddAgentInfoRequest
    {
        [Key("agent_infos")]
        public object[] agent_infos { get; set; }
        //public AgentInfo[] agent_infos { get; set; }
        //public AgentInfoInner[] agent_infos { get; set; }
    }
}

// https://github.com/holochain/holochain-client-js/blob/main/src/api/admin/types.ts
// export type AgentInfoSigned = unknown;
// export type AddAgentInfoRequest = { agent_infos: Array<AgentInfoSigned> };


