using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IDownloadedNFTCollection : ISTARNETHolon
    {
        NFTCollectionType NFTCollectionType { get; set; }
    }
}