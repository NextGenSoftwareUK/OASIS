
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    [MessagePackObject]
    public class CapGrantAccessAssignedDetails
    {
        [Key("secret")]
        public byte[] secret { get; set; }

        [Key("assignees")]
        public byte[][] assignees { get; set; }
    }
}