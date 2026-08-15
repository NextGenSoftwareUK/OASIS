using Ipfs;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public partial class NFTManager
    {
        public async Task<OASISResult<IWeb4NFT>> ImportWeb3NFTAsync(IImportWeb3NFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The request is required. Please provide a valid IImportWeb3NFTRequest.";
                return result;
            }
            string errorMessage = "Error occured in ImportWeb3NFT in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                result.Result = CreateWeb4NFT(request);

                //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                if (request.OffChainProvider.Value == ProviderType.None)
                    request.OffChainProvider.Value = ProviderType.MongoDBOASIS;

                OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(CreateWeb4NFTMetaDataHolon(result.Result, request), request.ImportedByAvatarId, true, true, 0, true, false, request.OffChainProvider.Value);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    List<IWeb3NFT> web3NFTs = new List<IWeb3NFT>();

                    foreach (IWeb3NFT webNFT in result.Result.Web3NFTs)
                        web3NFTs.Add(webNFT);

                    result.Message = FormatSuccessMessage(request, result, web3NFTs, responseFormatType);
                }
                else
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the {request.OffChainProvider.Name}. Reason: {saveHolonResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> ImportWeb3NFT(IImportWeb3NFTRequest request, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The request is required. Please provide a valid IImportWeb3NFTRequest.";
                return result;
            }
            string errorMessage = "Error occured in ImportWeb3NFT in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                result.Result = CreateWeb4NFT(request);

                //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                if (request.OffChainProvider.Value == ProviderType.None)
                    request.OffChainProvider.Value = ProviderType.MongoDBOASIS;

                OASISResult<IHolon> saveHolonResult = Data.SaveHolon(CreateWeb4NFTMetaDataHolon(result.Result, request), request.ImportedByAvatarId, true, true, 0, true, false, request.OffChainProvider.Value);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    List<IWeb3NFT> web3NFTs = new List<IWeb3NFT>();

                    foreach (IWeb3NFT webNFT in result.Result.Web3NFTs)
                        web3NFTs.Add(webNFT);

                    result.Message = FormatSuccessMessage(request, result, web3NFTs, responseFormatType);
                }
                else
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the {request.OffChainProvider.Name}. Reason: {saveHolonResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> ImportWeb4NFTAsync(Guid importedByAvatarId, string fullPathToOASISNFTJsonFile, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            return await ImportWeb4NFTAsync(importedByAvatarId, JsonConvert.DeserializeObject<IWeb4NFT>(await File.ReadAllTextAsync(fullPathToOASISNFTJsonFile)));
        }

        public async Task<OASISResult<IWeb4NFT>> ImportWeb4NFTAsync(Guid importedByAvatarId, IWeb4NFT OASISNFT, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in ImportWeb4NFTAsync in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                if (OASISNFT.OffChainProvider.Value == ProviderType.None)
                    OASISNFT.OffChainProvider.Value = ProviderType.MongoDBOASIS;

                IHolon holon = CreateWeb4NFTMetaDataHolon(OASISNFT);
                holon.MetaData["NFT.ImportedOn"] = DateTime.Now;
                holon.MetaData["NFT.ImportedBy"] = importedByAvatarId;

                OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(holon, importedByAvatarId, true, true, 0, true, false, providerType);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                    result.Message = FormatSuccessMessage(result, importedByAvatarId, responseFormatType);
                else
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the {OASISNFT.OffChainProvider.Name}. Reason: {saveHolonResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> ExportWeb4NFTAsync(Guid OASISNFTId, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4NFT> exportResult = await LoadWeb4NftAsync(OASISNFTId, providerType);

            if (exportResult != null && exportResult.Result != null && !exportResult.IsError)
            {
                return await ExportWeb4NFTAsync(exportResult.Result, fullPathToExportTo, providerType, responseFormatType);
            }
            else
                return exportResult;
        }

        public async Task<OASISResult<IWeb4NFT>> ExportWeb4NFTAsync(IWeb4NFT OASISNFT, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            await File.WriteAllTextAsync(fullPathToExportTo, JsonConvert.SerializeObject(OASISNFT, Formatting.Indented));
            return new OASISResult<IWeb4NFT>(OASISNFT);
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ImportWeb4GeoNFTAsync(Guid importedByAvatarId, string fullPathToOASISGeoNFTJsonFile, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            return await ImportWeb4GeoNFTAsync(importedByAvatarId, JsonConvert.DeserializeObject<IWeb4GeoSpatialNFT>(await File.ReadAllTextAsync(fullPathToOASISGeoNFTJsonFile)));
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ImportWeb4GeoNFTAsync(Guid importedByAvatarId, IWeb4GeoSpatialNFT OASISGeoNFT, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in ImportOASISGeoNFTAsync in NFTManager. Reason:";
            IAvatar currentAvatar = null;

            try
            {
                //Default to Mongo for storing the OASIS NFT meta data if none is specified.
                if (OASISGeoNFT.OffChainProvider.Value == ProviderType.None)
                    OASISGeoNFT.OffChainProvider.Value = ProviderType.MongoDBOASIS;

                IHolon holon = CreateWeb4GeoSpatialNFTMetaDataHolon(OASISGeoNFT);
                holon.MetaData["GEONFT.OriginalOASISNFT.ImportedOn"] = DateTime.Now;
                holon.MetaData["GEONFT.OriginalOASISNFT.ImportedBy"] = importedByAvatarId;

                OASISResult<IHolon> saveHolonResult = await Data.SaveHolonAsync(holon, importedByAvatarId, true, true, 0, true, false, providerType);

                if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                {
                    result.Result = OASISGeoNFT;
                    result.Message = FormatSuccessMessage(result, importedByAvatarId, responseFormatType);
                }
                else
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving metadata holon to the {OASISGeoNFT.OffChainProvider.Name}. Reason: {saveHolonResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ExportWeb4GeoNFTAsync(Guid OASISGeoNFTId, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4GeoSpatialNFT> exportResult = await LoadWeb4GeoNftAsync(OASISGeoNFTId, providerType);

            if (exportResult != null && exportResult.Result != null && !exportResult.IsError)
                return await ExportWeb4GeoNFTAsync(exportResult.Result, fullPathToExportTo, providerType, responseFormatType);
            else
                return exportResult;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> ExportWeb4GeoNFTAsync(IWeb4GeoSpatialNFT OASISGeoNFT, string fullPathToExportTo, ProviderType providerType = ProviderType.Default, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            await File.WriteAllTextAsync(fullPathToExportTo, JsonConvert.SerializeObject(OASISGeoNFT, Formatting.Indented));
            return new OASISResult<IWeb4GeoSpatialNFT>(OASISGeoNFT);
        }

        public OASISResult<bool> IsNFTStandardTypeValid(IMintWeb4NFTRequest request, string errorMessage = "")
        {
            return IsNFTStandardTypeValid(request.NFTStandardType.Value, request.OnChainProvider.Value, errorMessage);
        }

        public OASISResult<bool> IsNFTStandardTypeValid(NFTStandardType NFTStandardType, ProviderType onChainProvider, string errorMessage = "")
        {
            OASISResult<bool> result = new OASISResult<bool>();

            if (NFTStandardType == NFTStandardType.SPL && onChainProvider != ProviderType.SolanaOASIS)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} NFTStandardType is set to SPL but OnChainProvider is not set to SolanaOASIS! Please make sure you set the OnChainProvider to SolanaOASIS when minting SPL NFTs.");
                return result;
            }

            if (NFTStandardType != NFTStandardType.SPL && onChainProvider == ProviderType.SolanaOASIS)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} OnChainProvider is set to SolanaOASIS but NFTStandardType is not set to SPL! Please make sure you set the NFTStandardType to SPL when minting NFTs on SolanaOASIS.");
                return result;
            }

            if ((NFTStandardType == NFTStandardType.ERC721 || NFTStandardType == NFTStandardType.ERC1155) && !ProviderManager.Instance.IsProviderEVMBlockchain(onChainProvider))
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} When selecting NFTStandardType ERC721 or ERC1155 then the OnChainProvider needs to be set to a supported EVM chain such as ArbitrumOASIS, EthereumOASIS, PolygonOASIS & BaseOASIS.");
                return result;
            }

            return result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadWeb3NftAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
            string errorMessage = "Error occured in LoadWeb3NftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonAsync(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb3NFT> LoadWeb3Nft(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
            string errorMessage = "Error occured in LoadWeb3Nft in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadHolon(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadWeb3NftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
            string errorMessage = "Error occured in LoadWeb3NftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonByMetaDataAsync("NFT.Hash", onChainNftHash, HolonType.Web3NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb3NFT> LoadWeb3Nft(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb3NFT> result = new OASISResult<IWeb3NFT>();
            string errorMessage = "Error occured in LoadWeb3Nft in NFTManager. Reason:";

            try
            {
                //result = DecodeNFTMetaData(Data.LoadHolonByCustomKey(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeNFTMetaData(Data.LoadHolonByMetaData("NFT.Hash", onChainNftHash, HolonType.Web3NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> LoadWeb4NftAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in LoadWeb4NftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonAsync(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb4NFT> LoadWeb4Nft(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in LoadWeb4Nft in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(Data.LoadHolon(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //TODO: Need to refactor this because it needs to check all the child web nfts to find the matching hash...
        public async Task<OASISResult<IWeb4NFT>> LoadWeb4NftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in LoadWeb4NftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeNFTMetaData(await Data.LoadHolonByMetaDataAsync("NFT.Hash", onChainNftHash, HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);

            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //TODO: Need to refactor this because it needs to check all the child web nfts to find the matching hash...
        public OASISResult<IWeb4NFT> LoadWeb4Nft(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            string errorMessage = "Error occured in LoadWeb4Nft in NFTManager. Reason:";

            try
            {
                //result = DecodeNFTMetaData(Data.LoadHolonByCustomKey(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeNFTMetaData(Data.LoadHolonByMetaData("NFT.Hash", onChainNftHash, HolonType.Web4NFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4GeoSpatialNFT>> LoadWeb4GeoNftAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in LoadWeb4GeoNftAsync in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(await Data.LoadHolonAsync(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IWeb4GeoSpatialNFT> LoadWeb4GeoNft(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in LoadWeb4GeoNft in NFTManager. Reason:";

            try
            {
                result = DecodeGeoNFTMetaData(Data.LoadHolon(id, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //TODO: Need to refactor this because it needs to check all the child web nfts to find the matching hash...
        public async Task<OASISResult<IWeb4GeoSpatialNFT>> LoadWeb4GeoNftAsync(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in LoadWeb4GeoNftAsync in NFTManager. Reason:";

            try
            {
                //result = DecodeGeoNFTMetaData(await Data.LoadHolonByCustomKeyAsync(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeGeoNFTMetaData(await Data.LoadHolonByMetaDataAsync("NFT.Hash", onChainNftHash, HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        //TODO: Need to refactor this because it needs to check all the child web nfts to find the matching hash...
        public OASISResult<IWeb4GeoSpatialNFT> LoadWeb4GeoNft(string onChainNftHash, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4GeoSpatialNFT> result = new OASISResult<IWeb4GeoSpatialNFT>();
            string errorMessage = "Error occured in LoadWeb4GeoNft in NFTManager. Reason:";

            try
            {
                //result = DecodeGeoNFTMetaData(Data.LoadHolonByCustomKey(onChainNftHash, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
                result = DecodeGeoNFTMetaData(Data.LoadHolonByMetaData("NFT.Hash", onChainNftHash, HolonType.Web4GeoNFT, true, true, 0, true, false, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IWeb3NFT>>> LoadAllWeb3NFTsForAvatarAsync(Guid avatarId, Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in LoadAllWeb3NFTsForAvatarAsync in NFTManager. Reason:";

            try
            {
                if (parentWeb4NFTId != Guid.Empty)
                {
                    result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync(new Dictionary<string, string>()
                    {
                        { "NFT.MintedByAvatarId", avatarId.ToString() },
                        { "NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString() }
                    }, MetaKeyValuePairMatchMode.All, HolonType.Web3NFT, providerType: providerType), result, errorMessage);
                }
                else
                    result = DecodeNFTMetaData(await Data.LoadHolonsByMetaDataAsync("NFT.MintedByAvatarId", avatarId.ToString(), HolonType.Web3NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

        public OASISResult<IEnumerable<IWeb3NFT>> LoadAllWeb3NFTsForAvatar(Guid avatarId, Guid parentWeb4NFTId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IWeb3NFT>> result = new OASISResult<IEnumerable<IWeb3NFT>>();
            string errorMessage = "Error occured in LoadAllWeb3NFTsForAvatar in NFTManager. Reason:";

            try
            {
                if (parentWeb4NFTId != Guid.Empty)
                {
                    result = DecodeNFTMetaData(Data.LoadHolonsByMetaData(new Dictionary<string, string>()
                    {
                        { "NFT.MintedByAvatarId", avatarId.ToString() },
                        { "NFT.ParentWeb4NFTId", parentWeb4NFTId.ToString() }
                    }, MetaKeyValuePairMatchMode.All, HolonType.Web3NFT, providerType: providerType), result, errorMessage);
                }
                else
                    result = DecodeNFTMetaData(Data.LoadHolonsByMetaData("NFT.MintedByAvatarId", avatarId.ToString(), HolonType.Web3NFT, true, true, 0, true, false, 0, HolonType.All, 0, providerType), result, errorMessage);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

    }
}
