﻿using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.Utilities;
using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT
{
    public interface IOASISGeoSpatialNFT : IOASISNFT
    {
        Guid PlacedByAvatarId { get; set; }
        Guid OriginalOASISNFTId { get; set; }
        EnumValue<ProviderType> GeoNFTMetaDataOffChainProvider { get; set; }
        //EnumValue<ProviderType> OriginalOASISNFTProviderType { get; set; }
        DateTime PlacedOn { get; set; }
        long Lat { get; set; }
        long Long { get; set; }
        bool AllowOtherPlayersToAlsoCollect { get; set; }
        bool PermSpawn { get; set; }
        int GlobalSpawnQuantity { get; set; }
        int PlayerSpawnQuantity { get; set; }
        int RepawnDurationInSeconds { get; set; }
    }
}