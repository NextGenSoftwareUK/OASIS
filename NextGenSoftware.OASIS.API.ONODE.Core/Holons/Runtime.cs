using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class Runtime : Holon, IRuntime
    {
        private IRuntimeDNA _RuntimeDNA;

        public Runtime()
        {
            this.HolonType = HolonType.Runtime;
        }

        public IRuntimeDNA RuntimeDNA
        {
            get
            {
                if (_RuntimeDNA == null && MetaData["RuntimeDNAJSON"] != null && !string.IsNullOrEmpty(MetaData["RuntimeDNAJSON"].ToString()))
                    _RuntimeDNA = JsonSerializer.Deserialize<RuntimeDNA>(MetaData["RuntimeDNAJSON"].ToString());

                return _RuntimeDNA;
            }
            set
            {
                _RuntimeDNA = value;
                MetaData["RuntimeDNAJSON"] = JsonSerializer.Serialize(value);
            }
        }

        [CustomOASISProperty()]
        public byte[] PublishedRuntime { get; set; }

        [CustomOASISProperty()]
        public string RuntimeName { get; set; }
    }
}