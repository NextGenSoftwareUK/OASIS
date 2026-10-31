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
                    return await DeleteHolonAsync(providerKey);
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
            if(string.IsNullOrWhiteSpace(providerKey)) throw new ArgumentException("A Solana storage signature is required.",nameof(providerKey));
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) throw new InvalidOperationException(activation.Message,activation.Exception);
            }
            var stored = await _solanaRepository.GetAsync<SolanaHolonDto>(providerKey);
            var (deleted,tombstone)=await CreateVerifiedTombstoneAsync<SolanaHolonDto,IHolon>(
                providerKey,stored.Id,stored.Version,record=>record.GetHolon());
            deleted.ProviderUniqueStorageKey[Core.Enums.ProviderType.SolanaOASIS] = tombstone;
            result.Result = deleted;
            result.IsDeleted = true;
            result.DeletedCount = 1;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        => ReadNearbyRecords(geoLat, geoLong, radiusInMeters, () => LoadAllAvatars());

    OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        => ReadNearbyRecords(geoLat, geoLong, radiusInMeters, () => LoadAllHolons(HolonType.All), Type);

    private OASISResult<IEnumerable<T>> ReadNearbyRecords<T>(long latitude, long longitude, int radius,
        Func<OASISResult<IEnumerable<T>>> load, HolonType type = HolonType.All) where T : IHolonBase
    {
        var result = new OASISResult<IEnumerable<T>>();
        try
        {
            var query = new NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordLocationQuery(latitude, longitude, radius);
            if (!Enum.IsDefined(typeof(HolonType), type)) throw new ArgumentOutOfRangeException(nameof(type));
            if (!IsProviderActivated)
            {
                var activated = ActivateProviderAsync().GetAwaiter().GetResult();
                if (activated.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, activated.Message, activated.Exception);
                    result.ErrorCode = activated.ErrorCode;
                    return result;
                }
            }
            var loaded = load();
            if (loaded.IsError) return loaded;
            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>, IEnumerable<T>>(loaded);
            var selected=query.Select(loaded.Result, row => row.IsActive && !row.IsDeleted && (type == HolonType.All || row.HolonType == type));
            result.Result=selected;result.IsLoaded=true;result.LoadedCount=selected.Count;
        }
        catch (Exception cause)
        {
            result.Result = null;
            result.IsLoaded = false;
            result.LoadedCount = 0;
            OASISErrorHandling.HandleError(ref result, cause.Message, cause);
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

    public Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId,
        decimal amount)
        => SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token: null);

    public Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername,
        string toAvatarUsername, decimal amount)
        => SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token: null);

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername,
        string toAvatarUsername, decimal amount)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
    }

    public Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail,
        string toAvatarEmail, decimal amount)
        => SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token: null);

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

    public Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId,
        Guid toAvatarId, decimal amount)
        => SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token: null);



    public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transaction)
        => SendNFTAsync(transaction).Result;


    public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        OASISResult<IWeb3NFTTransactionResponse> result = new();
        try
        {
            var snapshot=new SendWeb3NFTRequest
            {
                FromNFTTokenAddress=transaction.FromNFTTokenAddress,FromWalletAddress=transaction.FromWalletAddress,
                ToWalletAddress=transaction.ToWalletAddress,TokenAddress=transaction.TokenAddress,TokenId=transaction.TokenId,
                Amount=transaction.Amount,MemoText=transaction.MemoText,OwnerPrivateKey=transaction.OwnerPrivateKey,
                OwnerPublicKey=transaction.OwnerPublicKey,OwnerSeedPhrase=transaction.OwnerSeedPhrase
            };
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) throw new InvalidOperationException(activation.Message,activation.Exception);
            }
            OASISResult<SendTransactionResult> solanaNftTransactionResult =
                await _solanaService.SendNftAsync(snapshot);

            if (solanaNftTransactionResult.IsError ||
                string.IsNullOrEmpty(solanaNftTransactionResult.Result?.TransactionHash))
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
