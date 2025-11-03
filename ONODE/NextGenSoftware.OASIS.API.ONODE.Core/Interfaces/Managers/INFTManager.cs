using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public interface INFTManager
    {
        IPFSOASIS IPFS { get; }
        PinataOASIS Pinata { get; }

        Task<OASISResult<IWeb4OASISGeoNFTCollection>> AddOASISGeoNFTToCollectionAsync(Guid collectionId, Guid OASISGeoNFTId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISGeoNFTCollection>> AddOASISGeoNFTToCollectionAsync(Guid collectionId, IWeb4OASISGeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISNFTCollection>> AddOASISNFTToCollectionAsync(Guid collectionId, Guid OASISNFTId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISNFTCollection>> AddOASISNFTToCollectionAsync(Guid collectionId, IWeb4OASISNFT OASISNFT, ProviderType providerType = ProviderType.Default);
        string CreateMetaDataJson(IMintWeb4NFTTRequest request, NFTStandardType NFTStandardType);
        Task<OASISResult<IWeb4OASISNFTCollection>> CreateOASISNFTCollectionAsync(ICreateWeb4NFTCollectionRequest createOASISNFTCollectionRequest, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISGeoNFTCollection>> CreateWeb4OASISGeoNFTCollectionAsyc(ICreateWeb4GeoNFTCollectionRequest createWeb4OASISGeoNFTCollectionRequest, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteOASISGeoNFTAsync(Guid avatarId, Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteOASISNFTAsync(Guid avatarId, Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteOASISNFTCollectionAsync(Guid avatarId, Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<bool>> DeleteWeb4OASISGeoNFTCollectionAsync(Guid avatarId, Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISGeoSpatialNFT>> ExportOASISGeoNFTAsync(Guid OASISGeoNFTId, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<IWeb4OASISGeoSpatialNFT>> ExportOASISGeoNFTAsync(IWeb4OASISGeoSpatialNFT OASISGeoNFT, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<IWeb4OASISNFT>> ExportOASISNFTAsync(Guid OASISNFTId, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<IWeb4OASISNFT>> ExportOASISNFTAsync(IWeb4OASISNFT OASISNFT, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        OASISResult<IOASISNFTProvider> GetNFTProvider(ProviderType providerType);
        Task<OASISResult<IWeb4OASISGeoSpatialNFT>> ImportOASISGeoNFTAsync(Guid importedByAvatarId, IWeb4OASISGeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<IWeb4OASISGeoSpatialNFT>> ImportOASISGeoNFTAsync(Guid importedByAvatarId, string fullPathToOASISGeoNFTJsonFile, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<IWeb4OASISNFT>> ImportOASISNFTAsync(Guid importedByAvatarId, IWeb4OASISNFT OASISNFT, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<IWeb4OASISNFT>> ImportOASISNFTAsync(Guid importedByAvatarId, string fullPathToOASISNFTJsonFile, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<IWeb4OASISNFT>> ImportWeb3NFT(IImportWeb3NFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<IWeb4OASISNFT>> ImportWeb3NFTAsync(IImportWeb3NFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        OASISResult<bool> IsNFTStandardTypeValid(IMintWeb4NFTTRequest request, string errorMessage = "");
        OASISResult<bool> IsNFTStandardTypeValid(NFTStandardType NFTStandardType, ProviderType onChainProvider, string errorMessage = "");
        Task<OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>>> LoadAllGeoNFTCollectionsAsync(bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> LoadAllGeoNFTs(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> LoadAllGeoNFTsAsync(ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatarLocation(long latLocation, long longLocation, int radius, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarLocationAsync(long latLocation, long longLocation, int radius, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>> LoadAllGeoNFTsForMintAddress(string mintWalletAddress, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISNFTCollection>>> LoadAllNFTCollectionsAsync(bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<IWeb4OASISNFT>> LoadAllNFTs(ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISNFT>>> LoadAllNFTsAsync(ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<IWeb4OASISNFT>> LoadAllNFTsForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<IWeb4OASISNFT>> LoadAllNFTsForMintAddress(string mintWalletAddress, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISNFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IList<IWeb4OASISGeoSpatialNFT>>> LoadChildGeoNFTsForNFTCollectionAsync(List<string> Web4OASISGeoNFTIds, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IList<IWeb4OASISNFT>>> LoadChildNFTsForNFTCollectionAsync(List<string> Web4OASISNFTIds, ProviderType providerType = ProviderType.Default);
        OASISResult<IWeb4OASISGeoSpatialNFT> LoadGeoNft(Guid id, ProviderType providerType = ProviderType.Default);
        OASISResult<IWeb4OASISGeoSpatialNFT> LoadGeoNft(string onChainNftHash, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISGeoSpatialNFT>> LoadGeoNftAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISGeoSpatialNFT>> LoadGeoNftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>>> LoadGeoNFTCollectionsForAvatarAsync(Guid avatarId, bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IWeb4OASISNFT> LoadNft(Guid id, ProviderType providerType = ProviderType.Default);
        OASISResult<IWeb4OASISNFT> LoadNft(string onChainNftHash, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISNFT>> LoadNftAsync(Guid id, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISNFT>> LoadNftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISNFTCollection>>> LoadNFTCollectionsForAvatarAsync(Guid avatarId, bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISNFTCollection>> LoadOASISNFTCollectionAsync(Guid id, bool loadChildNFTs = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISGeoNFTCollection>> LoadWeb4OASISGeoNFTCollectionAsync(Guid id, bool loadChildGeoNFTs = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IWeb4OASISGeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<IWeb4OASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        OASISResult<IWeb4NFTTransactionRespone> MintNft(IMintWeb4NFTTRequest request, bool isGeoNFT = false, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<IWeb4NFTTransactionRespone>> MintNftAsync(IMintWeb4NFTTRequest request, bool isGeoNFT = false, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<List<IWeb4NFTTransactionRespone>>> MintNFTBatchAsync(List<IMintWeb4NFTTRequest> requests, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        OASISResult<IWeb4OASISGeoSpatialNFT> PlaceGeoNFT(IPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<IWeb4OASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceWeb4GeoSpatialNFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<IWeb4OASISGeoNFTCollection>> RemoveOASISGeoNFTFromCollectionAsync(Guid collectionId, Guid OASISGeoNFTId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISGeoNFTCollection>> RemoveOASISGeoNFTFromCollectionAsync(Guid collectionId, IWeb4OASISGeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISNFTCollection>> RemoveOASISNFTFromCollectionAsync(Guid collectionId, Guid OASISNFTId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISNFTCollection>> RemoveOASISNFTFromCollectionAsync(Guid collectionId, IWeb4OASISNFT OASISNFT, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>> SearchGeoNFTCollections(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISGeoNFTCollection>>> SearchGeoNFTCollectionsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> SearchGeoNFTs(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISGeoSpatialNFT>>> SearchGeoNFTsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<IWeb4OASISNFTCollection>> SearchNFTCollections(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISNFTCollection>>> SearchNFTCollectionsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<IWeb4OASISNFT>> SearchNFTs(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IWeb4OASISNFT>>> SearchNFTsAsync(string searchTerm, Guid avatarId, bool searchOnlyForCurrentAvatar = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IWeb4NFTTransactionRespone> SendNFT(IWeb4NFTWalletTransactionRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<IWeb4NFTTransactionRespone>> SendNFTAsync(IWeb4NFTWalletTransactionRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText);
        Task<OASISResult<IWeb4OASISGeoSpatialNFT>> UpdateOASISGeoNFTAsync(IUpdateWeb4GeoNFTRequest request, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISNFT>> UpdateOASISNFTAsync(IUpdateWeb4NFTRequest request, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISNFTCollection>> UpdateOASISNFTCollectionAsync(IUpdateWeb4NFTCollectionRequest request, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IWeb4OASISGeoNFTCollection>> UpdateWeb4OASISGeoNFTCollectionAsync(IUpdateWeb4OASISGeoNFTCollectionRequest request, ProviderType providerType = ProviderType.Default);
    }
}