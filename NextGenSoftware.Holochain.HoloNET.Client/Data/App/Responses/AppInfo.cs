using MessagePack;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using System.Collections.Generic;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class AppInfo : IAppInfo
    {
        [Key("installed_app_id")]
        public string installed_app_id { get; set; }

        [Key("cell_info")]
        public Dictionary<string, List<CellInfo>> cell_info { get; set; }

        [Key("agent_pub_key")]
        public byte[] agent_pub_key { get; set; }

        [IgnoreMember]
        public string AgentPubKey { get; set; }

        [IgnoreMember]
        public string DnaHash { get; set; }

        [IgnoreMember]
        public byte[][] CellId { get; set; }

        [Key("status")]
        public Dictionary<string, object> status { get; set; }
        //public AppInfoStatus status { get; set; }
        //public object status { get; set; }

        [IgnoreMember]
        public AppInfoStatusEnum AppStatus
        {
            get
            {
                if (status != null)
                {
                    if (status.ContainsKey("running"))
                        return AppInfoStatusEnum.Running;

                    else if (status.ContainsKey("paused"))
                    {
                        Dictionary<object, object> pausedDict = status["paused"] as Dictionary<object, object>;

                        if (pausedDict != null && pausedDict.ContainsKey("reason"))
                        {
                            Dictionary<object, object> reasonDict = pausedDict["reason"] as Dictionary<object, object>;

                            if (reasonDict != null && reasonDict.ContainsKey("error"))
                                AppStatusReason = reasonDict["error"].ToString();
                        }

                        return AppInfoStatusEnum.Paused;
                    }

                    else if (status.ContainsKey("disabled"))
                    {
                        Dictionary<object, object> disabledDict = status["disabled"] as Dictionary<object, object>;

                        if (disabledDict != null && disabledDict.ContainsKey("reason"))
                        {
                            Dictionary<object, object> reasonDict = disabledDict["reason"] as Dictionary<object, object>;

                            if (reasonDict != null && reasonDict.ContainsKey("never_started"))
                                AppStatusReason = "Never Started";

                            else if (reasonDict != null && reasonDict.ContainsKey("user"))
                                AppStatusReason = "User";

                            else if (reasonDict != null && reasonDict.ContainsKey("error"))
                                AppStatusReason = reasonDict["error"].ToString();
                        }

                        return AppInfoStatusEnum.Disabled;
                    }
                }

                return AppInfoStatusEnum.None;
            }
        }

        [IgnoreMember]
        public string AppStatusReason { get; private set; }


        [Key("manifest")]
        public AppManifest manifest { get; set; }
    }
}