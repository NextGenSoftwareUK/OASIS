using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledRuntime : InstalledSTARNETHolon, IInstalledRuntime
    {
        public InstalledRuntime() : base("RuntimeDNAJSON")
        {
            this.HolonType = HolonType.InstalledRuntime;
        }

        [CustomOASISProperty]
        public bool IsRunning { get; set; }

        [CustomOASISProperty]
        public string StartCommand { get; set; }

        [CustomOASISProperty]
        public string StopCommand { get; set; }

        [CustomOASISProperty]
        public string BuildCommand { get; set; }

        [CustomOASISProperty]
        public string PublishCommand { get; set; }

        [CustomOASISProperty]
        public string InstallCommand { get; set; }

        [CustomOASISProperty]
        public string DeployCommand { get; set; }
    }
}