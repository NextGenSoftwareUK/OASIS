using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface ISTARNFTCollection : ISTARNETHolon
    {
        NFTCollectionType NFTCollectionType { get; set; }
        Guid NFTCollectionId { get; set; }
    }
}