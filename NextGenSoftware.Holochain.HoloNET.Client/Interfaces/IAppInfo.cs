using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;
using System.Collections.Generic;

namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IAppInfo
    {
        byte[] agent_pub_key { get; set; }
        string AgentPubKey { get; set; }
        AppInfoStatusEnum AppStatus { get; }
        string AppStatusReason { get; }
        Dictionary<string, List<CellInfo>> cell_info { get; set; }
        byte[][] CellId { get; set; }
        string DnaHash { get; set; }
        string installed_app_id { get; set; }
        AppManifest manifest { get; set; }
        Dictionary<string, object> status { get; set; }
    }
}