using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface ISTARGeoNFTCollection : ISTARNETHolon
    {
        NFTCollectionType GeoNFTCollectionType { get; set; }
        Guid GeoNFTCollectionId { get; set; }
    }
}