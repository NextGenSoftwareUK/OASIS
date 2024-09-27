
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [MessagePackObject]
    public class DnaStorageInfo
    {
        [Key("authored_data_size")]
        public int authored_data_size { get; set; }

        [Key("authored_data_size_on_disk")]
        public int authored_data_size_on_disk { get; set; }

        [Key("dht_data_size")]
        public int dht_data_size { get; set; }

        [Key("dht_data_size_on_disk")]
        public int dht_data_size_on_disk { get; set; }

        [Key("cache_data_size")]
        public int cache_data_size { get; set; }

        [Key("cache_data_size_on_disk")]
        public int cache_data_size_on_disk { get; set; }

        [Key("used_by")]
        public string used_by { get; set; } //InstalledAppId
    }
}
