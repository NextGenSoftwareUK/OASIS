using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests
{
    public interface IUpdateWeb4NFTCollectionRequest : IUpdateWeb4NFTCollectionRequestBase
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        //List<string> Web4OASISNFTIds { get; set; }
        //List<IWeb4OASISNFT> Web4OASISNFTs { get; set; }
    }
}