using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class STARGeoNFTManager : STARNETManagerBase<STARGeoNFT, DownloadedGeoNFT, InstalledGeoNFT, GeoNFTDNA>, ISTARGeoNFTManager
    public class STARGeoNFTManager : STARNETManagerBase<STARGeoNFT, DownloadedGeoNFT, InstalledGeoNFT, STARNETDNA>, ISTARGeoNFTManager
    {
        public STARGeoNFTManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId,
            OASISDNA,
            typeof(STARGeoNFT),
            HolonType.STARGeoNFT,
            HolonType.InstalledGeoNFT,
            "GeoNFT",
            "GeoNFTId",
            "GeoNFTName",
            "GeoNFTType",
            "ogeonft",
            "oasis_geonfts",
            "GeoNFTDNA.json",
            "GeoNFTDNAJSON")
        { }

        public STARGeoNFTManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(STARGeoNFT),
            HolonType.STARGeoNFT,
            HolonType.InstalledGeoNFT,
            "GeoNFT",
            "GeoNFTId",
            "GeoNFTName",
            "GeoNFTType",
            "ogeonft",
            "oasis_geonfts",
            "GeoNFTDNA.json",
            "GeoNFTDNAJSON")
        { }

        //public new async Task<OASISResult<ISTARGeoNFT>> CreateAsync(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToGeoNFTSource,
        //    NFTType nftType,
        //    IOASISGeoSpatialNFT OASISGeoNFT,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, nftType, fullPathToGeoNFTSource, null,
        //        new STARGeoNFT()
        //        {
        //            NFTType = nftType,
        //            GeoNFT = OASISGeoNFT
        //        },
        //    providerType));
        //}

        //public async Task<OASISResult<ISTARGeoNFT>> CreateGeoNFTAsync(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToGeoNFTSource,
        //    NFTType nftType,
        //    IOASISGeoSpatialNFT OASISGeoNFT,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, nftType, fullPathToGeoNFTSource, null,
        //        new STARGeoNFT()
        //        {
        //            NFTType = nftType,
        //            GeoNFT = OASISGeoNFT
        //        },
        //    providerType));
        //}

        //public OASISResult<ISTARGeoNFT> CreateGeoNFT(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToGeoNFTSource,
        //    NFTType nftType,
        //    IOASISGeoSpatialNFT OASISGeoNFT,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(base.Create(avatarId, name, description, nftType, fullPathToGeoNFTSource, null,
        //        new STARGeoNFT()
        //        {
        //            NFTType = nftType,
        //            GeoNFT = OASISGeoNFT
        //        },
        //    providerType));
        //}

        //public async Task<OASISResult<ISTARGeoNFT>> CreateGeoNFTAsync(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToGeoNFTSource,
        //    NFTType nftType,
        //    Guid OASISGeoNFTId,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, nftType, fullPathToGeoNFTSource, null,
        //        new STARGeoNFT()
        //        {
        //            NFTType = nftType,
        //            GeoNFTId = OASISGeoNFTId
        //        },
        //    providerType));
        //}

        //public OASISResult<ISTARGeoNFT> CreateGeoNFT(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToGeoNFTSource,
        //    NFTType nftType,
        //    Guid OASISGeoNFTId,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(base.Create(avatarId, name, description, nftType, fullPathToGeoNFTSource, null,
        //        new STARGeoNFT()
        //        {
        //            NFTType = nftType,
        //            GeoNFTId = OASISGeoNFTId
        //        },
        //    providerType));
        //}

        //private OASISResult<ISTARGeoNFT> ProcessResult(OASISResult<STARGeoNFT> operationResult)
        //{
        //    OASISResult<ISTARGeoNFT> result = new OASISResult<ISTARGeoNFT>();
        //    result.Result = operationResult.Result;
        //    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
        //    return result;
        //}
    }
}