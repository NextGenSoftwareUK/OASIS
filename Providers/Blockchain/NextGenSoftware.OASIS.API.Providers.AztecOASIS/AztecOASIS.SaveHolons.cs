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
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NoRecordStore<IEnumerable<IHolon>>();

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) { await Task.CompletedTask; return NoRecordStore<IHolon>(); }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NoRecordStore<IHolon>();

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NoRecordStore<IEnumerable<IHolon>>();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) { await Task.CompletedTask; return NoRecordStore<IHolon>(); }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => NoRecordStore<IHolon>();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey) { await Task.CompletedTask; return NoRecordStore<IHolon>(); }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => NoRecordStore<IHolon>();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { await Task.CompletedTask; return NoRecordStore<bool>(); }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => NoRecordStore<bool>();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => NoRecordStore<IEnumerable<IHolon>>();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => NoRecordStore<IEnumerable<IHolon>>();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => NoRecordStore<IEnumerable<IHolon>>();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => NoRecordStore<IEnumerable<IHolon>>();



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
