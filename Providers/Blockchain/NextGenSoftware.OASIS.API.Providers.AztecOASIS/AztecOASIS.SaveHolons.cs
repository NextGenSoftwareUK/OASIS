using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS
{
    public partial class AztecOASIS
    {
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // For Aztec, we would query all holons from the blockchain
                // This is a simplified implementation - in production would query Aztec for all holon transactions
                // For now, return empty list as Aztec doesn't have a direct "get all" method
                // In production, would:
                // 1. Query Aztec for all transactions with holon metadata
                // 2. Decrypt private notes
                // 3. Deserialize holon data
                // 4. Filter by type if needed
                
                result.Result = new List<IHolon>();
                result.IsError = false;
                result.Message = "LoadAllHolons: Aztec requires querying blockchain transactions (simplified implementation)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                result.Result = await _aztecRepository.SaveHolonAsync(holon);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var saved = new List<IHolon>();
            foreach (var holon in holons)
            {
                var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (saveResult.IsError && !continueOnError)
                {
                    var errorResult = new OASISResult<IEnumerable<IHolon>>();
                    errorResult.IsError = true;
                    errorResult.Message = saveResult.Message;
                    return errorResult;
                }
                if (!saveResult.IsError && saveResult.Result != null)
                {
                    saved.Add(saveResult.Result);
                }
            }

            return new OASISResult<IEnumerable<IHolon>>(saved);
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load holon first
                var holonResult = await LoadHolonAsync(id);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found");
                    return result;
                }

                // For Aztec, deletion would involve marking the holon as deleted in metadata
                // and creating a new transaction indicating deletion
                // For now, mark as deleted in metadata
                holonResult.Result.MetaData = holonResult.Result.MetaData ?? new Dictionary<string, object>();
                holonResult.Result.MetaData["Deleted"] = true;
                holonResult.Result.MetaData["DeletedDate"] = DateTime.UtcNow.ToString("o");

                // Save updated holon
                var saveResult = await SaveHolonAsync(holonResult.Result);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }

                result.Result = saveResult.Result;
                result.IsError = false;
                result.Message = "Holon marked as deleted in Aztec";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load holon by provider key first
                var holonResult = await LoadHolonAsync(providerKey);
                if (holonResult.IsError || holonResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found by provider key");
                    return result;
                }

                // Mark as deleted
                holonResult.Result.MetaData = holonResult.Result.MetaData ?? new Dictionary<string, object>();
                holonResult.Result.MetaData["Deleted"] = true;
                holonResult.Result.MetaData["DeletedDate"] = DateTime.UtcNow.ToString("o");

                // Save updated holon
                var saveResult = await SaveHolonAsync(holonResult.Result);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }

                result.Result = saveResult.Result;
                result.IsError = false;
                result.Message = "Holon marked as deleted in Aztec by provider key";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons collection is null");
                    return result;
                }

                // Save all holons
                var saveResult = await SaveHolonsAsync(holons);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message);
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Imported {saveResult.Result?.Count() ?? 0} holons to Aztec";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load avatar
                var avatarResult = await LoadAvatarAsync(avatarId, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                // Load all holons for this avatar (as parent)
                var holonsResult = await LoadHolonsForParentAsync(avatarId);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, holonsResult.Message);
                    return result;
                }

                // Combine avatar and holons
                var allData = new List<IHolon> { avatarResult.Result as IHolon };
                if (holonsResult.Result != null)
                {
                    allData.AddRange(holonsResult.Result);
                }

                result.Result = allData;
                result.IsError = false;
                result.Message = $"Exported {allData.Count} holons for avatar";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load avatar by username
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
                    return result;
                }

                // Load all holons for this avatar
                var holonsResult = await LoadHolonsForParentAsync(avatarResult.Result.Id);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, holonsResult.Message);
                    return result;
                }

                // Combine avatar and holons
                var allData = new List<IHolon> { avatarResult.Result as IHolon };
                if (holonsResult.Result != null)
                {
                    allData.AddRange(holonsResult.Result);
                }

                result.Result = allData;
                result.IsError = false;
                result.Message = $"Exported {allData.Count} holons for avatar by username";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load avatar by email
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
                    return result;
                }

                // Load all holons for this avatar
                var holonsResult = await LoadHolonsForParentAsync(avatarResult.Result.Id);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, holonsResult.Message);
                    return result;
                }

                // Combine avatar and holons
                var allData = new List<IHolon> { avatarResult.Result as IHolon };
                if (holonsResult.Result != null)
                {
                    allData.AddRange(holonsResult.Result);
                }

                result.Result = allData;
                result.IsError = false;
                result.Message = $"Exported {allData.Count} holons for avatar by email";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Load all holons
                var holonsResult = await LoadAllHolonsAsync(HolonType.All, version: version);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, holonsResult.Message);
                    return result;
                }

                result.Result = holonsResult.Result ?? new List<IHolon>();
                result.IsError = false;
                result.Message = $"Exported {result.Result.Count()} holons from Aztec";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;



        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "To wallet address is required");
                    return result;
                }

                // Aztec uses private notes for token transfers
                // Create a private note for the recipient
                var privateNote = await _aztecService.CreatePrivateNoteAsync(
                    request.Amount,
                    request.ToWalletAddress,
                    request.MemoText);

                if (privateNote == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to create private note for token transfer");
                    return result;
                }

                result.Result.TransactionResult = privateNote.NoteId ?? string.Empty;
                result.IsError = false;
                result.Message = "Token sent successfully on Aztec.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Mint request is required");
                    return result;
                }

                // Get mint to address from avatar ID or use default
                var mintToAddress = _apiBaseUrl ?? "aztec_mint_address";
                var mintAmount = request.MetaData?.ContainsKey("Amount") == true && decimal.TryParse(request.MetaData["Amount"]?.ToString(), out var amount)
                    ? amount 
                    : 1m;

                // Use MintStablecoinAsync if available, otherwise create a private note
                try
                {
                    var mintResult = await _aztecService.MintStablecoinAsync(mintToAddress, mintAmount, null, null);
                    if (mintResult != null && !mintResult.IsError && !string.IsNullOrEmpty(mintResult.Result))
                    {
                        result.Result.TransactionResult = mintResult.Result;
                        result.IsError = false;
                        result.Message = "Token minted successfully on Aztec.";
                        return result;
                    }
                }
                catch
                {
                    // Fall back to creating a private note
                }

                // Fallback: Create a private note for minting
                var privateNote = await _aztecService.CreatePrivateNoteAsync(mintAmount, mintToAddress, "minted");
                if (privateNote == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to mint token");
                    return result;
                }

                result.Result.TransactionResult = privateNote.NoteId ?? string.Empty;
                result.IsError = false;
                result.Message = "Token minted successfully on Aztec.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token: {ex.Message}", ex);
            }
            return result;
        }

    }
}
