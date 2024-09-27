
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.App.Responses.Objects
{
    [MessagePackObject]
    public struct CloneId
    {
        [Key("role_name")]
        public string role_name { get; set; }
    }
}