using System;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.Runtime;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Objects
{
    public class Runtime : IRuntime
    //TODO: Do we want to use Holon? What was the reason again?! ;-) Think so can be used with Data API and HolonManager?
    {
        public Runtime()
        {
            //this.HolonType = HolonType.InstalledOAPP;
        }

        public RuntimeType RunTimeType { get; set; }

        public string Version { get; set; }

        public string InstalledPath { get; set; }

        public DateTime InstalledOn { get; set; }

        public Guid InstalledBy { get; set; }

        public string InstalledByAvatarUsername { get; set; }
    }
}
