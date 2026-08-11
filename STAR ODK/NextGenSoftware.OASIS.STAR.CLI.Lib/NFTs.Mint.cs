using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class NFTs : STARNETUIBase<STARNFT, DownloadedNFT, InstalledNFT, STARNETDNA>
    {
        public async Task<OASISResult<IWeb4NFT>> ImportNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();

            if (mintParams is string existingFile && File.Exists(existingFile))
            {
                try
                {
                    OASISResult<IWeb4NFT> importResult = await NFTCommon.NFTManager.ImportWeb4NFTAsync(STAR.BeamedInAvatar.Id, existingFile);
                    if (importResult != null && importResult.Result != null && !importResult.IsError)
                    {
                        CLIEngine.ShowSuccessMessage(importResult.Message);
                        result.Result = importResult.Result;
                        result.Message = importResult.Message;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, importResult?.Message ?? "WEB4 import failed.");
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error importing WEB4 OASIS NFT: {ex.Message}", ex);
                }

                return result;
            }

            bool isWeb3 = false;

            if (mintParams != null)
                bool.TryParse(mintParams.ToString(), out isWeb3);

            if (isWeb3)
            {
                if (CLIEngine.GetConfirmation("Do you wish to import a WEB3 JSON MetaData file & then mint and wrap in a WEB4 OASIS NFT or import an existing minted NFT's token address and wrap in a WEB4 OASIS NFT? Press 'Y' for JSON File or 'N' for Token Address."))
                {
                    //WEB3 NFT Import from JSON MetaData file
                    string jsonPath = CLIEngine.GetValidFile("Please enter the full path to the JSON MetaData file you wish to import: ");

                    IMintWeb4NFTRequest request = await NFTCommon.GenerateNFTRequestAsync(jsonPath);

                    CLIEngine.ShowWorkingMessage("Minting WEB4 OASIS NFT...");
                    OASISResult<IWeb4NFT> nftResult = await STAR.OASISAPI.NFTs.MintNftAsync(request);
         
                    if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                    {
                        CLIEngine.ShowSuccessMessage(nftResult.Message);
                        result.Result = nftResult.Result;
                    }
                    else
                    {
                        string msg = nftResult != null ? nftResult.Message : "";
                        CLIEngine.ShowErrorMessage($"Error Occured: {msg}");
                    }
                }
                else
                {
                    // Import Web3 NFT functionality
                    try
                    {
                        IImportWeb3NFTRequest request = await NFTCommon.GenerateImportNFTRequestAsync();
                        CLIEngine.ShowWorkingMessage("Importing WEB3 NFT...");

                        var importResult = await NFTCommon.NFTManager.ImportWeb3NFTAsync(request);

                        if (importResult != null && importResult.Result != null && !importResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage(importResult.Message);
                            result.Result = importResult.Result;
                            result.Message = importResult.Message;
                        }
                        else
                        {
                            string msg = importResult != null ? importResult.Message : "";
                            CLIEngine.ShowErrorMessage($"Failed to import WEB3 NFT: {msg}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.IsError = true;
                        result.Message = $"Error importing WEB3 NFT: {ex.Message}";
                        CLIEngine.ShowErrorMessage($"Error importing WEB3 NFT: {ex.Message}");
                    }
                }
            }
            else
            {
                // WEB4 OASIS NFT Import
                try
                {
                    string filePath = CLIEngine.GetValidFile("Please enter the full path to the WEB4 OASIS NFT file you wish to import: ");

                    OASISResult<IWeb4NFT> importResult = await NFTCommon.NFTManager.ImportWeb4NFTAsync(STAR.BeamedInAvatar.Id, filePath);

                    if (importResult != null && importResult.Result != null && !importResult.IsError)
                    {
                        CLIEngine.ShowSuccessMessage(importResult.Message);
                        result.Result = importResult.Result;
                        result.Message = importResult.Message;
                    }
                    else
                    {
                        string msg = importResult != null ? importResult.Message : "";
                        CLIEngine.ShowErrorMessage($"Failed to import WEB4 OASIS NFT: {msg}");
                    }
                }
                catch (Exception ex)
                {
                    result.IsError = true;
                    result.Message = $"Error importing WEB4 OASIS NFT: {ex.Message}";
                    CLIEngine.ShowErrorMessage($"Error importing WEB4 OASIS NFT: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>Non-interactive WEB3 path: mint from a <see cref="MintWeb4NFTRequest"/> JSON file (same shape as <c>nft mint</c>).</summary>
        public async Task<OASISResult<IWeb4NFT>> ImportNFTWeb3MintFromJsonFileAsync(string jsonPath)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            if (string.IsNullOrWhiteSpace(jsonPath) || !File.Exists(jsonPath))
            {
                OASISErrorHandling.HandleError(ref result, "JSON file path is missing or does not exist.");
                return result;
            }

            try
            {
                string json = File.ReadAllText(jsonPath);
                MintWeb4NFTRequest request = JsonConvert.DeserializeObject<MintWeb4NFTRequest>(json);
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not deserialize MintWeb4NFTRequest from JSON.");
                    return result;
                }

                if (request.MintedByAvatarId == Guid.Empty)
                    request.MintedByAvatarId = STAR.BeamedInAvatar.Id;

                CLIEngine.ShowWorkingMessage("Minting WEB4 OASIS NFT from JSON...");
                OASISResult<IWeb4NFT> nftResult = await STAR.OASISAPI.NFTs.MintNftAsync(request);

                if (nftResult != null && nftResult.Result != null && !nftResult.IsError)
                {
                    CLIEngine.ShowSuccessMessage(nftResult.Message);
                    result.Result = nftResult.Result;
                    result.Message = nftResult.Message;
                }
                else
                    OASISErrorHandling.HandleError(ref result, nftResult?.Message ?? "Mint failed.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting from WEB3 JSON file: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>Non-interactive WEB3 path: import an already-minted token using <see cref="ImportWeb3NFTRequest"/> JSON.</summary>
        public async Task<OASISResult<IWeb4NFT>> ImportNFTWeb3TokenFromJsonFileAsync(string jsonPath)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            if (string.IsNullOrWhiteSpace(jsonPath) || !File.Exists(jsonPath))
            {
                OASISErrorHandling.HandleError(ref result, "JSON file path is missing or does not exist.");
                return result;
            }

            try
            {
                string json = File.ReadAllText(jsonPath);
                ImportWeb3NFTRequest request = JsonConvert.DeserializeObject<ImportWeb3NFTRequest>(json);
                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not deserialize ImportWeb3NFTRequest from JSON.");
                    return result;
                }

                request.ImportedByAvatarId = STAR.BeamedInAvatar.Id;
                CLIEngine.ShowWorkingMessage("Importing WEB3 NFT from JSON...");
                OASISResult<IWeb4NFT> importResult = await NFTCommon.NFTManager.ImportWeb3NFTAsync(request);

                if (importResult != null && importResult.Result != null && !importResult.IsError)
                {
                    CLIEngine.ShowSuccessMessage(importResult.Message);
                    result.Result = importResult.Result;
                    result.Message = importResult.Message;
                }
                else
                    OASISErrorHandling.HandleError(ref result, importResult?.Message ?? "WEB3 token import failed.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing WEB3 NFT from JSON: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> ExportNFTNonInteractiveAsync(string idOrName, string destinationFilePath, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();
            OASISResult<IWeb4NFT> NFTResult = await FindWeb4NFTAsync("export", idOrName, providerType: providerType);
            if (NFTResult == null || NFTResult.Result == null || NFTResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured loading WEB4 NFT in ExportNFTNonInteractiveAsync. Reason: {NFTResult?.Message}");
                return result;
            }

            OASISResult<IWeb4NFT> exportResult = await NFTCommon.NFTManager.ExportWeb4NFTAsync(NFTResult.Result.Id, destinationFilePath);
            if (exportResult != null && exportResult.Result != null && !exportResult.IsError)
            {
                CLIEngine.ShowSuccessMessage(exportResult.Message);
                result.Result = exportResult.Result;
                result.Message = exportResult.Message;
            }
            else
                OASISErrorHandling.HandleError(ref result, exportResult?.Message ?? "Export failed.");

            return result;
        }

        public async Task<OASISResult<IWeb4NFT>> ExportNFTAsync(object mintParams = null)
        {
            OASISResult<IWeb4NFT> result = new OASISResult<IWeb4NFT>();

            try
            {
                OASISResult<IWeb4NFT> NFTResult = await FindWeb4NFTAsync("export");

                if (NFTResult == null || NFTResult.Result == null || NFTResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error occured loading WEB4 NFT in ExportNFTAsync method. Reason: {NFTResult.Message}");
                    return result;
                }

                string filePath = CLIEngine.GetValidFile("Please enter the full path to where you would like to export the WEB4 OASIS NFT file: ");
                OASISResult<IWeb4NFT> exportResult = await NFTCommon.NFTManager.ExportWeb4NFTAsync(NFTResult.Result.Id, filePath);

                if (exportResult != null && exportResult.Result != null && !exportResult.IsError)
                {
                    CLIEngine.ShowSuccessMessage(exportResult.Message);
                    result.Result = exportResult.Result;
                    result.Message = exportResult.Message;
                }
                else
                {
                    string msg = exportResult != null ? exportResult.Message : "";
                    CLIEngine.ShowErrorMessage($"Failed to export WEB4 OASIS NFT: {msg}");
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error exporting WEB4 OASIS NFT: {ex.Message}";
                CLIEngine.ShowErrorMessage($"Error importing WEB4 OASIS NFT: {ex.Message}");
            }

            return result;
        }
    }
}
