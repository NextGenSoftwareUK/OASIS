
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects
{
    [MessagePackObject]
    public class DnaManifest
    {
        /// <summary>
        ///  Currently one "1" is supported
        /// </summary>
        [Key("manifest_version")]
        public string manifest_version { get; set; }

        /// <summary>
        /// The friendly "name" of a Holochain DNA.
        /// </summary>
        [Key("name")]
        public string name { get; set; }

        /// <summary>
        /// A network seed for uniquifying this DNA.
        /// </summary>
        [Key("network_seed")]
        public string network_seed { get; set; }

        /// <summary>
        /// Any arbitrary application properties can be included in this object.
        /// </summary>
        [Key("properties")]
        public dynamic properties { get; set; }

        /// <summary>
        /// An array of zomes associated with your DNA.  The order is significant: it determines initialization order.
        /// </summary>
        [Key("zomes")]
        public ZomeManifest[] zomes { get; set; }
    }
}