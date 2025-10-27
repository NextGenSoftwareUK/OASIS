using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public interface IInstalledNFTCollection : ISTARNETHolon
    {
        NFTCollectionType NFTCollectionType { get; set; }
    }
}