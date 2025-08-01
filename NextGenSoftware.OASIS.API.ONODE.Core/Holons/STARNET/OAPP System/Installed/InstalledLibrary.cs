using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledLibrary : InstalledSTARNETHolon, IInstalledRuntime
    {
        public InstalledLibrary() : base("LibraryDNAJSON")
        {
            this.HolonType = HolonType.InstalledLibrary;
        }
    }
}