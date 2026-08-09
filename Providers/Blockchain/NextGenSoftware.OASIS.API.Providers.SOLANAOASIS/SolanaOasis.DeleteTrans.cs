using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin.RPC;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Rpc.Utilities;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Helpers;
using Newtonsoft.Json;
using NextGenSoftware.Utilities.ExtentionMethods;
using System.Linq;
using System.IO;
using System.Text;
using static Solnet.Programs.TokenProgram;
using static Solnet.Programs.AssociatedTokenAccountProgram;
using static Solnet.Programs.SystemProgram;
using static Solnet.Programs.MemoProgram;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS
{
    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Solana provider: {activateResult.Message}");
                    return result;
                }
            }

            // Load holon first to get the provider key
            var holonResult = await LoadHolonAsync(id);
            if (holonResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon for deletion: {holonResult.Message}");
                return result;
            }

            if (holonResult.Result != null)
            {
                // Delete holon from Solana blockchain using provider key (transaction hash)
                if (holonResult.Result.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.SolanaOASIS) &&
                    holonResult.Result.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.SolanaOASIS, out var providerKey))
                {
                    var deleteSuccess = await _solanaRepository.DeleteAsync(providerKey);
                    if (deleteSuccess)
                    {
                        result.Result = holonResult.Result;
                        result.IsDeleted = true;
                        result.IsError = false;
                        result.Message = "Holon deleted successfully from Solana";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to delete holon from Solana repository");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon does not have a Solana provider key (transaction hash)");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Holon not found for deletion");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting holon from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
    {
        var result = new OASISResult<IHolon>();

        try
        {
            if (await _solanaRepository.DeleteAsync(providerKey))
            {
                result.IsDeleted = true;
                result.DeletedCount = 1;
            }
            else
                result.IsError = true;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message);
        }

        return result;
    }

    OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
    {
        var result = new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Solana provider: {activateResult.Message}");
                    return result;
                }
            }

            var avatarsResult = LoadAllAvatars();
            if (avatarsResult.IsError || avatarsResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                return result;
            }

            var centerLat = geoLat / 1e6d;
            var centerLng = geoLong / 1e6d;
            var nearby = new List<IAvatar>();

            foreach (var avatar in avatarsResult.Result)
            {
                if (avatar.MetaData != null &&
                    avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                    avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                    double.TryParse(latObj?.ToString(), out var lat) &&
                    double.TryParse(lngObj?.ToString(), out var lng))
                {
                    var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                    if (distance <= radiusInMeters)
                        nearby.Add(avatar);
                }
            }

            result.Result = nearby;
            result.IsError = false;
            result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Solana: {ex.Message}", ex);
        }
        return result;
    }

    OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = ActivateProviderAsync().GetAwaiter().GetResult();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Solana provider: {activateResult.Message}");
                    return result;
                }
            }

            var holonsResult = LoadAllHolons(Type);
            if (holonsResult.IsError || holonsResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                return result;
            }

            var centerLat = geoLat / 1e6d;
            var centerLng = geoLong / 1e6d;
            var nearby = new List<IHolon>();

            foreach (var holon in holonsResult.Result)
            {
                if (holon.MetaData != null &&
                    holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                    holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                    double.TryParse(latObj?.ToString(), out var lat) &&
                    double.TryParse(lngObj?.ToString(), out var lng))
                {
                    var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                    if (distance <= radiusInMeters)
                        nearby.Add(holon);
                }
            }

            result.Result = nearby;
            result.IsError = false;
            result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Solana: {ex.Message}", ex);
        }
        return result;
    }


    public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
        string errorMessage = "Error occured in SendTransactionAsync method in SolanaOASIS Provider. Reason: ";

        try
        {
            var solanaTransactionResult = await _solanaService.SendTransaction(new SendTransactionRequest()
            {
                Amount = (ulong)amount,
                MemoText = memoText,
                FromAccount = new BaseAccountRequest()
                {
                    PublicKey = fromWalletAddress
                },
                ToAccount = new BaseAccountRequest()
                {
                    PublicKey = toWalletAddress
                }
            });

            if (solanaTransactionResult.IsError ||
                string.IsNullOrEmpty(solanaTransactionResult.Result.TransactionHash))
            {
                OASISErrorHandling.HandleError(ref result, solanaTransactionResult.Message);
                return result;
            }

            result.Result.TransactionResult = solanaTransactionResult.Result.TransactionHash;
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessage}, {e.Message}", e);
        }

        return result;
    }


    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId,
        decimal amount)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate =
            "Error was occured in SendTransactionByIdAsync method in SolanaOASIS while sending transaction. Reason: ";

        var senderAvatarPublicKeyResult =
            KeyManager.GetProviderPublicKeysForAvatarById(fromAvatarId, Core.Enums.ProviderType.SolanaOASIS);
        var receiverAvatarPublicKeyResult =
            KeyManager.GetProviderPublicKeysForAvatarById(toAvatarId, Core.Enums.ProviderType.SolanaOASIS);

        if (senderAvatarPublicKeyResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, senderAvatarPublicKeyResult.Message),
                senderAvatarPublicKeyResult.Exception);
            return result;
        }

        if (receiverAvatarPublicKeyResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, receiverAvatarPublicKeyResult.Message),
                receiverAvatarPublicKeyResult.Exception);
            return result;
        }

        var senderAvatarPublicKey = senderAvatarPublicKeyResult.Result[0];
        var receiverAvatarPublicKey = receiverAvatarPublicKeyResult.Result[0];
        result = await SendSolanaTransaction(senderAvatarPublicKey, receiverAvatarPublicKey, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, result.Message),
                result.Exception);

        return result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername,
        string toAvatarUsername, decimal amount)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate =
            "Error was occured in SendTransactionByUsernameAsync method in SolanaOASIS while sending transaction. Reason: ";

        var senderAvatarPublicKeyResult =
            KeyManager.GetProviderPublicKeysForAvatarByUsername(fromAvatarUsername,
                Core.Enums.ProviderType.SolanaOASIS);
        var receiverAvatarPublicKeyResult =
            KeyManager.GetProviderPublicKeysForAvatarByUsername(toAvatarUsername,
                Core.Enums.ProviderType.SolanaOASIS);

        if (senderAvatarPublicKeyResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, senderAvatarPublicKeyResult.Message),
                senderAvatarPublicKeyResult.Exception);

            return result;
        }

        if (receiverAvatarPublicKeyResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, receiverAvatarPublicKeyResult.Message),
                receiverAvatarPublicKeyResult.Exception);
            return result;
        }

        var senderAvatarPublicKey = senderAvatarPublicKeyResult.Result[0];
        var receiverAvatarPublicKey = receiverAvatarPublicKeyResult.Result[0];

        result = await SendSolanaTransaction(senderAvatarPublicKey, receiverAvatarPublicKey, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, result.Message),
                result.Exception);

        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername,
        string toAvatarUsername, decimal amount)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail,
        string toAvatarEmail, decimal amount)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate =
            "Error was occured in SendTransactionByEmailAsync method in SolanaOASIS while sending transaction. Reason: ";

        var senderAvatarPublicKeysResult =
            KeyManager.GetProviderPublicKeysForAvatarByEmail(fromAvatarEmail, Core.Enums.ProviderType.SolanaOASIS);
        var receiverAvatarPublicKeyResult =
            KeyManager.GetProviderPublicKeysForAvatarByEmail(toAvatarEmail, Core.Enums.ProviderType.SolanaOASIS);

        if (senderAvatarPublicKeysResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, senderAvatarPublicKeysResult.Message),
                senderAvatarPublicKeysResult.Exception);
            return result;
        }

        if (receiverAvatarPublicKeyResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, receiverAvatarPublicKeyResult.Message),
                receiverAvatarPublicKeyResult.Exception);
            return result;
        }

        var senderAvatarPublicKey = senderAvatarPublicKeysResult.Result[0];
        var receiverAvatarPublicKey = receiverAvatarPublicKeyResult.Result[0];

        result = await SendSolanaTransaction(senderAvatarPublicKey, receiverAvatarPublicKey, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, result.Message),
                result.Exception);

        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail,
        decimal amount)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId,
        decimal amount)
    {
        return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId,
        Guid toAvatarId, decimal amount)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate =
            "Error was occured in SendTransactionByDefaultWallet method in SolanaOASIS while sending transaction. Reason: ";

        var senderAvatarPublicKeysResult =
            await WalletManager.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.SolanaOASIS);
        var receiverAvatarPublicKeyResult =
            await WalletManager.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.SolanaOASIS);

        if (senderAvatarPublicKeysResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, senderAvatarPublicKeysResult.Message),
                senderAvatarPublicKeysResult.Exception);
            return result;
        }

        if (receiverAvatarPublicKeyResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result,
                string.Concat(errorMessageTemplate, receiverAvatarPublicKeyResult.Message),
                receiverAvatarPublicKeyResult.Exception);
            return result;
        }

        var senderAvatarPublicKey = senderAvatarPublicKeysResult.Result.PublicKey;
        var receiverAvatarPublicKey = receiverAvatarPublicKeyResult.Result.PublicKey;
        result = await SendSolanaTransaction(senderAvatarPublicKey, receiverAvatarPublicKey, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, result.Message),
                result.Exception);

        return result;
    }

    private async Task<OASISResult<ITransactionResponse>> SendSolanaTransaction(string fromAddress, string toAddress,
        decimal amount)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate =
            "Error was occured in SendSolanaTransaction method in SolanaOASIS while sending transaction. Reason: ";

        try
        {
            var solanaTransactionResult = await _solanaService.SendTransaction(new SendTransactionRequest()
            {
                Amount = (ulong)amount,
                FromAccount = new BaseAccountRequest()
                {
                    PublicKey = fromAddress
                },
                ToAccount = new BaseAccountRequest()
                {
                    PublicKey = toAddress
                }
            });

            if (solanaTransactionResult.IsError ||
                string.IsNullOrEmpty(solanaTransactionResult.Result.TransactionHash))
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessageTemplate, solanaTransactionResult.Message),
                    solanaTransactionResult.Exception);
                return result;
            }

            result.Result.TransactionResult = solanaTransactionResult.Result.TransactionHash;
            TransactionHelper.CheckForTransactionErrors(ref result);
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessageTemplate, e.Message), e);
        }

        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transaction)
        => SendNFTAsync(transaction).Result;


    public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        OASISResult<IWeb3NFTTransactionResponse> result = new();
        try
        {
            OASISResult<SendTransactionResult> solanaNftTransactionResult =
                await _solanaService.SendNftAsync(transaction as SendWeb3NFTRequest);

            if (solanaNftTransactionResult.IsError ||
                string.IsNullOrEmpty(solanaNftTransactionResult.Result.TransactionHash))
            {
                OASISErrorHandling.HandleError(ref result,
                    solanaNftTransactionResult.Message,
                    solanaNftTransactionResult.Exception);
                return result;
            }

            result.IsError = false;
            result.IsSaved = true;
            result.Result = new Web3NFTTransactionResponse()
            {
                TransactionResult = solanaNftTransactionResult.Result.TransactionHash
            };

            TransactionHelper.CheckForTransactionErrors(ref result);
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true,
        bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
    }

}
