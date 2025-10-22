using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public interface IInstalledGeoNFTCollection : ISTARNETHolon
    {
        NFTType NFTCollectionType { get; set; }
    }
}