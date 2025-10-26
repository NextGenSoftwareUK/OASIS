using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IDownloadedGeoNFTCollection : ISTARNETHolon
    {
        NFTCollectionType NFTCollectionType { get; set; }
    }
}