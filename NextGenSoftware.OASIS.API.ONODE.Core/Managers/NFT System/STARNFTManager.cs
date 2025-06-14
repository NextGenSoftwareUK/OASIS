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

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class STARNFTManager : STARManagerBase<STARNFT, DownloadedNFT, InstalledNFT>
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
            ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.CreateAsync(avatarId, name, description, nftType, fullPathToNFTSource, null,
                new STARNFT()
                {
                    NFTType = nftType,
                    OASISNFT = OASISNFT
                },
            providerType));
        }

        public OASISResult<ISTARNFT> CreateNFT(
            Guid avatarId,
            string name,
            string description,
            string fullPathToNFTSource,
            NFTType nftType,
            IOASISNFT OASISNFT,
            ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Create(avatarId, name, description, nftType, fullPathToNFTSource, null,
                new STARNFT()
                {
                    NFTType = nftType,
                    OASISNFT = OASISNFT
                },
            providerType));
        }

        public async Task<OASISResult<ISTARNFT>> CreateNFTAsync(
            Guid avatarId,
            string name,
            string description,
            string fullPathToNFTSource,
            NFTType nftType,
            Guid OASISNFTId,
            ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.CreateAsync(avatarId, name, description, nftType, fullPathToNFTSource, null,
                new STARNFT()
                {
                    NFTType = nftType,
                    OASISNFTId = OASISNFTId
                },
            providerType));
        }

        public OASISResult<ISTARNFT> CreateNFT(
            Guid avatarId,
            string name,
            string description,
            string fullPathToNFTSource,
            NFTType nftType,
            Guid OASISNFTId,
            ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Create(avatarId, name, description, nftType, fullPathToNFTSource, null,
                new STARNFT()
                {
                    NFTType = nftType,
                    OASISNFTId = OASISNFTId
                },
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