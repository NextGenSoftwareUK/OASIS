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

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate = "Error occurred in SendTransactionByEmailAsync (with token) method in SolanaOASIS while sending transaction. Reason: ";

        try
        {
            // Get wallet addresses for avatars by email
            var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.SolanaOASIS, fromAvatarEmail);
            var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.SolanaOASIS, toAvatarEmail);

            if (fromWalletResult.IsError || toWalletResult.IsError || string.IsNullOrWhiteSpace(fromWalletResult.Result) || string.IsNullOrWhiteSpace(toWalletResult.Result))
            {
                OASISErrorHandling.HandleError(ref result, "Error getting wallet addresses for avatars");
                return result;
            }

            var fromWallet = fromWalletResult.Result;
            var toWallet = toWalletResult.Result;

            // If token is provided, use SPL token transfer; otherwise use native SOL transfer
            if (!string.IsNullOrWhiteSpace(token))
            {
                var fromPublicKey = new PublicKey(fromWallet);
                var toPublicKey = new PublicKey(toWallet);
                var tokenMint = new PublicKey(token);
                var tokenAmount = (ulong)(amount * 1_000_000_000m);
                
                var fromTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(fromPublicKey, tokenMint);
                var toTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(toPublicKey, tokenMint);
                
                var blockHashResult = await _rpcClient.GetLatestBlockHashAsync();
                if (!blockHashResult.WasSuccessful)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get latest block hash: {blockHashResult.Reason}");
                    return result;
                }
                
                var transferInstruction = TokenProgram.Transfer(fromTokenAccount, toTokenAccount, tokenAmount, fromPublicKey);
                
                byte[] tx = new TransactionBuilder()
                    .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                    .SetFeePayer(fromPublicKey)
                    .AddInstruction(transferInstruction)
                    .Build(_oasisSolanaAccount);
                
                var sendResult = await _rpcClient.SendTransactionAsync(tx);
                if (!sendResult.WasSuccessful)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send token transaction: {sendResult.Reason}");
                    return result;
                }
                
                result.Result = new TransactionResponse { TransactionResult = sendResult.Result };
                result.IsError = false;
                result.Message = "Token transaction sent successfully";
            }
            else
            {
                // Use native SOL transfer (delegate to existing method)
                result = await SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount);
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessageTemplate}{ex.Message}", ex);
        }
        
        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
    {
        return MintNFTAsync(transation).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(
        IMintWeb3NFTRequest transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        OASISResult<IWeb3NFTTransactionResponse> result = new(new Web3NFTTransactionResponse());

        try
        {
            OASISResult<MintNftResult> solanaNftTransactionResult
                = await _solanaService.MintNftAsync(transaction as MintWeb3NFTRequest);

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

            Web3NFT Web3NFT = new Web3NFT()
            {
                MintTransactionHash = solanaNftTransactionResult.Result.TransactionHash,
                NFTTokenAddress = solanaNftTransactionResult.Result.MintAccount,
                OASISMintWalletAddress = _oasisSolanaAccount.PublicKey,
                JSONMetaDataURL = transaction.JSONMetaDataURL,
                Symbol = transaction.Symbol
            };

            //OASISResult<IWeb4OASISNFT> oasisNFT = await LoadOnChainNFTDataAsync(solanaNftTransactionResult.Result.MintAccount);

            //if (oasisNFT != null && oasisNFT.Result != null && !oasisNFT.IsError)
            //{
            //    oasisNFT.Result.NFTTokenAddress = solanaNftTransactionResult.Result.MintAccount;
            //    oasisNFT.Result.MintTransactionHash = solanaNftTransactionResult.Result.TransactionHash;
            //    oasisNFT.Result.OASISMintWalletAddress = _oasisSolanaAccount.PublicKey;
            //    Web4OASISNFT = (Web4OASISNFT)oasisNFT.Result;
            //}

            //This is now handled by NFTManager! ;-)
            //if (!string.IsNullOrEmpty(transaction.SendToAddressAfterMinting))
            //{
            //    OASISResult<IWeb4NFTTransactionRespone> sendNftResult = await SendNFTAsync(new NFTWalletTransactionRequest()
            //    {
            //        FromWalletAddress = _oasisSolanaAccount.PublicKey,
            //        ToWalletAddress = transaction.SendToAddressAfterMinting,
            //        TokenAddress = solanaNftTransactionResult.Result.MintAccount,
            //        Amount = 1
            //    });
            //    if (sendNftResult.IsError)
            //    {
            //        OASISErrorHandling.HandleWarning(ref result,
            //            $"Error occured sending minted NFT to {transaction.SendToAddressAfterMinting}. Reason: {sendNftResult.Message}");
            //    }
            //    else
            //        result.Result.SendNFTTransactionResult = sendNftResult.Result.TransactionResult;
            //}

            result.Result.Web3NFT = Web3NFT;
            result.Result.TransactionResult = solanaNftTransactionResult.Result.TransactionHash;
            result.Result.VerifyCollectionTransactionHash = solanaNftTransactionResult.Result.VerifyCollectionTransactionHash;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
    {
        return BurnNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        OASISResult<IWeb3NFTTransactionResponse> result = new(new Web3NFTTransactionResponse());

        try
        {
            OASISResult<BurnNftResult> solanaNftTransactionResult = await _solanaService.BurnNftAsync(request);

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

            //Web3NFT Web3NFT = new Web3NFT()
            //{
            //    MintTransactionHash = solanaNftTransactionResult.Result.TransactionHash,
            //    NFTTokenAddress = solanaNftTransactionResult.Result.MintAccount,
            //    OASISMintWalletAddress = _oasisSolanaAccount.PublicKey,
            //};

            //result.Result.Web3NFT = Web3NFT;
            result.Result.TransactionResult = solanaNftTransactionResult.Result.TransactionHash;

        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
    {
        return LockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        OASISResult<IWeb3NFTTransactionResponse> result = new(new Web3NFTTransactionResponse());

        try
        {
            // Lock NFT by transferring it to a bridge pool address or locking contract
            // For Solana, this typically involves transferring to a program-owned account
            var bridgePoolAddress = _oasisSolanaAccount.PublicKey.Key;
            
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = string.Empty, // Would be retrieved from request in real implementation
                ToWalletAddress = bridgePoolAddress,
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1 // NFTs are typically 1:1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.IsSaved = true;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
    {
        return UnlockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        OASISResult<IWeb3NFTTransactionResponse> result = new(new Web3NFTTransactionResponse());

        try
        {
            // Unlock NFT by transferring it back from bridge pool to original owner
            // For Solana, this involves transferring from program-owned account back to user
            var bridgePoolAddress = _oasisSolanaAccount.PublicKey.Key;
            
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = bridgePoolAddress,
                ToWalletAddress = string.Empty, // Would be retrieved from request in real implementation
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to unlock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.IsSaved = true;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
    {
        return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
    }

    public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
    {
        OASISResult<IWeb3NFT> result = new();

        try
        {
            OASISResult<GetNftResult> response =
                await _solanaService.LoadNftAsync(new(nftTokenAddress));

            result.IsLoaded = true;
            result.IsError = false;

            if (response.IsLoaded)
                result.Result = response.Result.ToOasisNft();
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error occured in SolanaOASIS Provider. Reason: {e.Message}");
        }

        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> MintOnChainCollectionNFT(IMintOnChainCollectionNFTRequest request)
    {
        return MintOnChainCollectionNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintOnChainCollectionNFTAsync(IMintOnChainCollectionNFTRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        OASISResult<IWeb3NFTTransactionResponse> result = new(new Web3NFTTransactionResponse());

        try
        {
            OASISResult<CreateCollectionNftResult> createResult =
                await _solanaService.CreateCollectionNftAsync(request.Title, request.Symbol, request.JSONMetaDataURL, request.InitialSize, request.FreezeMetadata == true);

            if (createResult.IsError || createResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, createResult.Message, createResult.Exception);
                return result;
            }

            result.IsError = false;
            result.IsSaved = true;
            result.Result.Web3NFT = new Web3NFT
            {
                NFTTokenAddress = createResult.Result.CollectionMintAddress,
                MintTransactionHash = createResult.Result.TransactionHash
            };
            result.Result.TransactionResult = createResult.Result.TransactionHash;
            result.Result.VerifyCollectionTransactionHash = createResult.Result.SetCollectionSizeTransactionHash;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public OASISResult<string> SetCollectionSize(string collectionMintAddress, ulong size)
    {
        return SetCollectionSizeAsync(collectionMintAddress, size).Result;
    }

    public async Task<OASISResult<string>> SetCollectionSizeAsync(string collectionMintAddress, ulong size)
    {
        OASISResult<string> result = new();

        try
        {
            result.Result = await _solanaService.SetCollectionSizeAsync(collectionMintAddress, size);
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, e.Message, e);
        }

        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey,
        string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
        int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
        bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
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

            // Query holons by metadata from Solana program
            // Real Solana implementation: Query program accounts and filter by metadata
            var holons = new List<IHolon>();
            try
            {
                var accounts = await _rpcClient.GetProgramAccountsAsync(_oasisSolanaAccount.PublicKey);
                
                if (accounts.WasSuccessful && accounts.Result != null)
                {
                    foreach (var account in accounts.Result)
                    {
                        try
                        {
                            var accountData = account.Account.Data;
                            if (accountData != null && accountData.Count > 0)
                            {
                                var accountDataString = accountData is IReadOnlyList<byte> bytes ? Encoding.UTF8.GetString(bytes.ToArray()) : string.Join("", (IEnumerable<string>)accountData);
                                var holonDto = JsonConvert.DeserializeObject<Entities.Models.SolanaHolonDto>(accountDataString);
                                
                                if (holonDto != null)
                                {
                                    holonDto.PublicKey = account.PublicKey;
                                    holonDto.AccountInfo = account.Account;
                                    holonDto.Lamports = account.Account.Lamports;
                                    
                                    var holon = holonDto.GetHolon();
                                    if (holon != null && holon.MetaData != null && 
                                        holon.MetaData.ContainsKey(metaKey) &&
                                        holon.MetaData[metaKey]?.ToString()?.Equals(metaValue, StringComparison.OrdinalIgnoreCase) == true)
                                    {
                                        holons.Add(holon);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!continueOnError) throw;
                            continue;
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Found {holons.Count} holons matching metadata key '{metaKey}' = '{metaValue}'";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying Solana program accounts by metadata: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue,
        HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
        int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,
        int version = 0)
    {
        return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

}
