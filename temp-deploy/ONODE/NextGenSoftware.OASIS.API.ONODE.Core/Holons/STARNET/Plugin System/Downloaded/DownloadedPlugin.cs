using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedPlugin : DownloadedSTARNETHolon, IDownloadedPlugin
    {
        public DownloadedPlugin() : base("PluginDNAJSON")
        {
            this.HolonType = HolonType.DownloadedPlugin;
        }
    }
}