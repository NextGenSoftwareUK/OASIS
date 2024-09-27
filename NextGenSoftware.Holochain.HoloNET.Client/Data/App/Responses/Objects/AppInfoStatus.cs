
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.App.Responses.Objects
{
    //[MessagePackObject]
    //public enum InstalledAppInfoStatusType
    //{
    //    Paused, //PausedAppReasonType
    //    Disabled,//DisabledAppReasonType
    //    Running
    //}


    //public struct InstalledAppInfoStatus
    //{
    //    [Key("status")]
    //    public string status { get; set; }

    //    [Key("reason")]
    //    public string reason { get; set; }
    //}

    [MessagePackObject]
    public struct AppInfoStatus
    {
        [Key("paused")]
        public object paused { get; set; }

        [Key("disabled")]
        public object disabled { get; set; }

        [Key("running")]
        public object running { get; set; }

        [IgnoreMember]
        public AppInfoStatusEnum Status
        {
            get
            {
                if (paused != null)
                    return AppInfoStatusEnum.Paused;

                else if (disabled != null)
                    return AppInfoStatusEnum.Disabled;

                else if (running != null)
                    return AppInfoStatusEnum.Running;

                else
                    return AppInfoStatusEnum.None;
            }
        }
    }
}