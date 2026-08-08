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
        public async Task<OASISResult<IWeb4NFT>> MintNftAsync(IMintWeb4NFTRequest request, bool isGeoNFT = false, ResponseFormatType responseFormatType = ResponseFormatType.FormattedText)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            if (request == null)
            {
                result.IsError = true;
                result.Message = "The request is required. Please provide a valid IMintWeb4NFTRequest.";
                return result;
            }
            string errorMessage = "Error occured in MintNftAsync in NFTManager. Reason:";

            try
            {
                if (request.Web3NFTs == null || request.Web3NFTs != null && request.Web3NFTs.Count < request.NumberToMint)
                {
                    if (request.Web3NFTs == null)
                        request.Web3NFTs = new List<IMintWeb3NFTRequest>();

                    for (int i = 0; i <= (request.NumberToMint - request.Web3NFTs.Count); i++)
                        request.Web3NFTs.Add(new MintWeb3NFTRequest());
                }

                if (request.Web3NFTs != null && request.Web3NFTs.Count > 0)
                {
                    int i = 0;
                    foreach (IMintWeb3NFTRequest web3Request in request.Web3NFTs)
                    {
                        i++;

                        if (web3Request.NumberToMint == 0)
                            web3Request.NumberToMint = 1;
                        //web3Request.NumberToMint = request.NumberToMint;

                        IMintWeb4NFTRequest originalMintWeb4NFTRequest = CloneWeb4NFTRequest(request);
                        result = await MintWeb3NFTsAsync(result, originalMintWeb4NFTRequest, web3Request, null, isGeoNFT, responseFormatType, i == request.Web3NFTs.Count);
                    }
                }
                //else
                //    result = await MintWeb3NFTsAsync(result, request, null, null, isGeoNFT, responseFormatType, true);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }

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

    }
}