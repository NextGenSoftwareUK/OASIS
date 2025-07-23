using NextGenSoftware.OASIS.API.Core.Enums;

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