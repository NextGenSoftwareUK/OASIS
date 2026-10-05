using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class Plugin : STARNETHolon, IPlugin
    {
        public Plugin() : base("PluginDNAJSON")
        {
            this.HolonType = HolonType.Plugin;
        }
    }
}