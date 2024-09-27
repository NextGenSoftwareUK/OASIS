
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    [MessagePackObject]
    public class CapGrantAccessAssigned
    {
        [Key("Assigned")]
        public CapGrantAccessAssignedDetails Assigned { get; set; }
    }
}