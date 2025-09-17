using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedZomeMetaDataDNA : DownloadedSTARNETHolon//, IDownloadedZome
    {
        public DownloadedZomeMetaDataDNA() : base("ZomeMetaDataDNAJSON")
        {
            this.HolonType = HolonType.DownloadedZomeMetaDataDNA;
        }
    }
}