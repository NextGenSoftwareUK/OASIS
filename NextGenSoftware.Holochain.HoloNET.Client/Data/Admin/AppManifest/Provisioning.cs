
using MessagePack;

namespace NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.AppManifest
{
    [MessagePackObject]
    public class Provisioning
    {
        [Key("strategy")]
        public string strategy { get; set; }

        [IgnoreMember]
        public ProvisioningStrategyType StrategyType { get; set; }

        [Key("deferred")]
        public bool deferred { get; set; }
    }
}