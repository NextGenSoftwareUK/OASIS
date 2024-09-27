
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    [MessagePackObject]
    public class CoordinatorManifest
    {

        /// <summary>
        /// An array of zomes associated with your DNA.  The order is significant: it determines initialization order.
        /// </summary>
        [Key("zomes")]
        public ZomeManifest[] zomes { get; set; }
    }
}