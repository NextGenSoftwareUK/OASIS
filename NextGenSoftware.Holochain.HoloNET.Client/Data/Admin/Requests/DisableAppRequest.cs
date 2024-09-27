
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests
{
    [MessagePackObject]
    public class DisableAppRequest
    {
        [Key("installed_app_id")]
        public string installed_app_id { get; set; }
    }
}