using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedLibrary : DownloadedSTARNETHolon, IDownloadedLibrary
    {
        public DownloadedLibrary() : base("LibraryDNAJSON")
        {
            this.HolonType = HolonType.DownloadedLibrary;
        }
    }
}