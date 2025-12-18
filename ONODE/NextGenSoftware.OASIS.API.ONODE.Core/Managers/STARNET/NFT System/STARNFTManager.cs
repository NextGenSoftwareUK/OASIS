using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    /// Note: You can choose to use the base STARNETDNA (and other base classes) or extend them and inject them in here...
    //public class STARNFTManager : STARNETManagerBase<STARNFT, DownloadedNFT, InstalledNFT, NFTDNA>, ISTARNFTManager
    public class STARNFTManager : STARNETManagerBase<STARNFT, DownloadedNFT, InstalledNFT, STARNETDNA>, ISTARNFTManager
    //public class STARNFTManager : STARNETManagerBase<STARNFT, DownloadedNFT, InstalledNFT, STARNETDNA>, ISTARNFTManager
    {
        public STARNFTManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(NFTType),
            HolonType.Web5NFT,
            HolonType.InstalledNFT,
            "Web5 NFT",
            //"NFTId",
            "STARNETHolonId",
            "NFTName",
            "NFTType",
            "onft",
            "oasis_nfts",
            "STARNFTDNA.json",
            "STARNFTDNAJSON")
        { }

        public STARNFTManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(STARNFT),
            HolonType.Web5NFT,
            HolonType.InstalledNFT,
            "Web5 NFT",
            //"NFTId",
            "STARNETHolonId",
            "NFTName",
            "NFTType",
            "onft",
            "oasis_nfts",
            "STARNFTDNA.json",
            "STARNFTDNAJSON")
        { }

        //public async Task<OASISResult<ISTARNFT>> CreateNFTAsync(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToNFTSource,
        //    NFTType nftType,
        //    IOASISNFT OASISNFT,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, nftType, fullPathToNFTSource, null, null, null,
        //        new STARNFT()
        //        {
        //            NFTType = nftType,
        //            OASISNFT = OASISNFT
        //        }, null, checkIfSourcePathExists,
        //    providerType));
        //}

        //public OASISResult<ISTARNFT> CreateNFT(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToNFTSource,
        //    NFTType nftType,
        //    IOASISNFT OASISNFT,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(base.Create(avatarId, name, description, nftType, fullPathToNFTSource, null,
        //        new STARNFT()
        //        {
        //            NFTType = nftType,
        //            OASISNFT = OASISNFT
        //        }, null, checkIfSourcePathExists: checkIfSourcePathExists,
        //    providerType));
        //}

        //public async Task<OASISResult<ISTARNFT>> CreateNFTAsync(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToNFTSource,
        //    NFTType nftType,
        //    Guid OASISNFTId,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, nftType, fullPathToNFTSource, null, null, null,
        //        new STARNFT()
        //        {
        //            NFTType = nftType,
        //            OASISNFTId = OASISNFTId
        //        }, null, checkIfSourcePathExists: checkIfSourcePathExists,
        //    providerType));
        //}

        //public OASISResult<ISTARNFT> CreateNFT(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToNFTSource,
        //    NFTType nftType,
        //    Guid OASISNFTId,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(base.Create(avatarId, name, description, nftType, fullPathToNFTSource, null,
        //        new STARNFT()
        //        {
        //            NFTType = nftType,
        //            OASISNFTId = OASISNFTId
        //        }, null, checkIfSourcePathExists: checkIfSourcePathExists,
        //    providerType));
        //}

        //private OASISResult<ISTARNFT> ProcessResult(OASISResult<STARNFT> operationResult)
        //{
        //    OASISResult<ISTARNFT> result = new OASISResult<ISTARNFT>();
        //    result.Result = operationResult.Result;
        //    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
        //    return result;
        //}
    }
}