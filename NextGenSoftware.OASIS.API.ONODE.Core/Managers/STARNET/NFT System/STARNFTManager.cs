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

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class STARNFTManager : STARNETManagerBase<STARNFT, DownloadedNFT, InstalledNFT>, ISTARNFTManager
    {
        public STARNFTManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId,
            OASISDNA,
            typeof(STARNFT),
            HolonType.STARNFT,
            HolonType.InstalledNFT,
            "NFT",
            "NFTId",
            "NFTName",
            "NFTType",
            "nft",
            "oasis_nfts",
            "NFTDNA.json",
            "NFTDNAJSON")
        { }

        public STARNFTManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(STARNFT),
            HolonType.NFT,
            HolonType.InstalledNFT,
            "NFT",
            "NFTId",
            "NFTName",
            "NFTType",
            "nft",
            "oasis_nfts",
            "NFTDNA.json",
            "NFTDNAJSON")
        { }

        public async Task<OASISResult<ISTARNFT>> CreateNFTAsync(
            Guid avatarId,
            string name,
            string description,
            string fullPathToNFTSource,
            NFTType nftType,
            IOASISNFT OASISNFT,
            bool checkIfSourcePathExists = true,
            ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.CreateAsync(avatarId, name, description, nftType, fullPathToNFTSource, null,
                new STARNFT()
                {
                    NFTType = nftType,
                    OASISNFT = OASISNFT
                }, checkIfSourcePathExists: checkIfSourcePathExists,
            providerType));
        }

        public OASISResult<ISTARNFT> CreateNFT(
            Guid avatarId,
            string name,
            string description,
            string fullPathToNFTSource,
            NFTType nftType,
            IOASISNFT OASISNFT,
            bool checkIfSourcePathExists = true,
            ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Create(avatarId, name, description, nftType, fullPathToNFTSource, null,
                new STARNFT()
                {
                    NFTType = nftType,
                    OASISNFT = OASISNFT
                }, checkIfSourcePathExists: checkIfSourcePathExists,
            providerType));
        }

        public async Task<OASISResult<ISTARNFT>> CreateNFTAsync(
            Guid avatarId,
            string name,
            string description,
            string fullPathToNFTSource,
            NFTType nftType,
            Guid OASISNFTId,
            bool checkIfSourcePathExists = true,
            ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.CreateAsync(avatarId, name, description, nftType, fullPathToNFTSource, null,
                new STARNFT()
                {
                    NFTType = nftType,
                    OASISNFTId = OASISNFTId
                }, checkIfSourcePathExists: checkIfSourcePathExists,
            providerType));
        }

        public OASISResult<ISTARNFT> CreateNFT(
            Guid avatarId,
            string name,
            string description,
            string fullPathToNFTSource,
            NFTType nftType,
            Guid OASISNFTId,
            bool checkIfSourcePathExists = true,
            ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Create(avatarId, name, description, nftType, fullPathToNFTSource, null,
                new STARNFT()
                {
                    NFTType = nftType,
                    OASISNFTId = OASISNFTId
                }, checkIfSourcePathExists: checkIfSourcePathExists,
            providerType));
        }

        private OASISResult<ISTARNFT> ProcessResult(OASISResult<STARNFT> operationResult)
        {
            OASISResult<ISTARNFT> result = new OASISResult<ISTARNFT>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }
    }
}