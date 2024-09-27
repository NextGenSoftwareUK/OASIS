
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    [MessagePackObject]
    public class CapGrantAccessTransferable
    {
        [Key("Transferable")]
        public CapGrantAccessTransferableDetails Transferable { get; set; }
    }
}