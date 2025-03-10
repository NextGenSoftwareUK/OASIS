using System;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class InstalledRuntime : Holon, IInstalledRuntime //TODO: Do we want to use Holon? What was the reason again?! ;-) Think so can be used with Data API and HolonManager?
    {
        private IRuntimeDNA _RuntimeDNA;

        public InstalledRuntime()
        {
            this.HolonType = HolonType.InstalledOAPPTemplate;
        }

        public IRuntimeDNA RuntimeDNA
        {
            get
            {
                if (_RuntimeDNA == null && MetaData["RUNTIMEDNAJSON"] != null && !string.IsNullOrEmpty(MetaData["RUNTIMEDNAJSON"].ToString()))
                    _RuntimeDNA = JsonSerializer.Deserialize<RuntimeDNA>(MetaData["RUNTIMEDNAJSON"].ToString());

                return _RuntimeDNA;
            }
            set
            {
                _RuntimeDNA = value;
                MetaData["RUNTIMEDNAJSON"] = JsonSerializer.Serialize(RuntimeDNA);
            }
        }

        [CustomOASISProperty]
        public string InstalledPath { get; set; }

        [CustomOASISProperty]
        public DateTime InstalledOn { get; set; }

        [CustomOASISProperty]
        public Guid InstalledBy { get; set; }

        [CustomOASISProperty]
        public string InstalledByAvatarUsername { get; set; }
    }
}