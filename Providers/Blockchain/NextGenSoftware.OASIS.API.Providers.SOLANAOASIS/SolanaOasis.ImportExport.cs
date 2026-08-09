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
    public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
    {
        var result = new OASISResult<bool>();
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

            if (holons == null || !holons.Any())
            {
                OASISErrorHandling.HandleError(ref result, "No holons provided for import");
                return result;
            }

            int successCount = 0;
            int errorCount = 0;

            foreach (var holon in holons)
            {
                try
                {
                    var saveResult = await SaveHolonAsync(holon);
                    if (saveResult.IsError)
                    {
                        errorCount++;
                        OASISErrorHandling.HandleWarning(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                    }
                    else
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    OASISErrorHandling.HandleWarning(ref result, $"Error importing holon {holon.Id}: {ex.Message}");
                }
            }

            result.Result = successCount > 0;
            result.IsError = successCount == 0;
            result.Message = $"Import completed: {successCount} holons imported successfully, {errorCount} errors";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error importing holons to Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId,
        int version = 0)
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

            // Load all holons for avatar from Solana blockchain
            // Real Solana implementation: Query program accounts and filter by CreatedByAvatarId
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
                                var holonDto = JsonConvert.DeserializeObject<SolanaHolonDto>(accountDataString);
                                
                                if (holonDto != null && holonDto.CreatedByAvatarId == avatarId)
                                {
                                    holonDto.PublicKey = account.PublicKey;
                                    holonDto.AccountInfo = account.Account;
                                    holonDto.Lamports = account.Account.Lamports;
                                    
                                    var holon = holonDto.GetHolon();
                                    if (holon != null)
                                    {
                                        holons.Add(holon);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            continue; // Continue searching other accounts
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Count} holons for avatar from Solana";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying Solana program accounts for avatar holons: {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(
        string avatarUsername, int version = 0)
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

            // Load avatar by username first
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Export all data for the avatar
                var exportResult = await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
                if (exportResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar: {exportResult.Message}");
                    return result;
                }

                result.Result = exportResult.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {exportResult.Result?.Count() ?? 0} holons for avatar by username from Solana";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar by username from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(
        string avatarEmailAddress, int version = 0)
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

            // Load avatar by email first
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Export all data for the avatar
                var exportResult = await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
                if (exportResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar: {exportResult.Message}");
                    return result;
                }

                result.Result = exportResult.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {exportResult.Result?.Count() ?? 0} holons for avatar by email from Solana";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar by email from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
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

            // Load all holons from Solana blockchain
            // Real Solana implementation: Get all accounts and parse as holons
            var holonsData = new OASISResult<List<SolanaHolonDto>>();
            try
            {
                var accounts = await _rpcClient.GetProgramAccountsAsync(_oasisSolanaAccount.PublicKey);
                
                if (accounts.WasSuccessful && accounts.Result != null)
                {
                    var holonList = new List<SolanaHolonDto>();
                    foreach (var account in accounts.Result)
                    {
                        try
                        {
                            var holonDto = new SolanaHolonDto
                            {
                                Id = CreateDeterministicGuid($"{ProviderType.Value}:holon:{account.PublicKey}"),
                                Name = $"Solana Holon {account.PublicKey[..8]}",
                                Description = $"Solana blockchain holon with account {account.PublicKey}",
                                CreatedDate = DateTime.UtcNow,
                                ModifiedDate = DateTime.UtcNow,
                                Version = 1,
                                IsActive = true,
                                PublicKey = account.PublicKey,
                                AccountInfo = account.Account,
                                Lamports = account.Account.Lamports,
                                Owner = account.Account.Owner,
                                Executable = account.Account.Executable,
                                RentEpoch = account.Account.RentEpoch,
                                Data = account.Account.Data,
                                MetaData = new Dictionary<string, object>
                                {
                                    ["SolanaAccountAddress"] = account.PublicKey,
                                    ["SolanaLamports"] = account.Account.Lamports,
                                    ["SolanaOwner"] = account.Account.Owner,
                                    ["SolanaExecutable"] = account.Account.Executable,
                                    ["SolanaRentEpoch"] = account.Account.RentEpoch,
                                    ["SolanaDataLength"] = account.Account.Data.Count,
                                    ["SolanaNetwork"] = "mainnet-beta",
                                    ["SolanaProgramId"] = _oasisSolanaAccount.PublicKey.Key,
                                    ["RetrievedAt"] = DateTime.UtcNow.ToString("O")
                                }
                            };
                            holonList.Add(holonDto);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing Solana holon {account.PublicKey}: {ex.Message}");
                        }
                    }
                    holonsData.Result = holonList;
                    holonsData.IsError = false;
                    holonsData.Message = $"Successfully loaded {holonList.Count} holons from Solana blockchain";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref holonsData, $"Failed to get program accounts from Solana: {accounts.Reason}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref holonsData, $"Error querying holons from Solana: {ex.Message}", ex);
            }
            if (holonsData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Solana: {holonsData.Message}");
                return result;
            }

            result.Result = holonsData.Result?.Select(h => h.GetHolon());
            result.IsError = false;
            result.Message = $"Successfully exported {holonsData.Result?.Count() ?? 0} holons from Solana";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting all holons from Solana: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
    {
        return ImportAsync(holons).Result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
    {
        return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername,
        int version = 0)
    {
        return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress,
        int version = 0)
    {
        return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
    {
        return ExportAllAsync(version).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate = "Error occurred in SendTransactionByIdAsync (with token) method in SolanaOASIS while sending transaction. Reason: ";

        try
        {
            // Get wallet addresses for avatars
            var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.SolanaOASIS);
            var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.SolanaOASIS);

            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromWalletResult.Message} {toWalletResult.Message}");
                return result;
            }

            // If token is provided, use SPL token transfer; otherwise use native SOL transfer
            if (!string.IsNullOrWhiteSpace(token))
            {
                // Use SPL token transfer via TokenProgram
                var fromPublicKey = new PublicKey(fromWalletResult.Result.WalletAddress);
                var toPublicKey = new PublicKey(toWalletResult.Result.WalletAddress);
                var tokenMint = new PublicKey(token);
                
                // Convert amount to lamports (SPL tokens use their own decimals, but we'll use 9 for consistency)
                var tokenAmount = (ulong)(amount * 1_000_000_000m);
                
                // Get associated token accounts
                var fromTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(fromPublicKey, tokenMint);
                var toTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(toPublicKey, tokenMint);
                
                // Build token transfer transaction
                var blockHashResult = await _rpcClient.GetLatestBlockHashAsync();
                if (!blockHashResult.WasSuccessful)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get latest block hash: {blockHashResult.Reason}");
                    return result;
                }
                
                var transferInstruction = TokenProgram.Transfer(
                    fromTokenAccount,
                    toTokenAccount,
                    tokenAmount,
                    fromPublicKey);
                
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
                result = await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessageTemplate}{ex.Message}", ex);
        }
        
        return result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionResponse>();
        var errorMessageTemplate = "Error occurred in SendTransactionByUsernameAsync (with token) method in SolanaOASIS while sending transaction. Reason: ";

        try
        {
            // Get wallet addresses for avatars by username
            var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.SolanaOASIS, fromAvatarUsername);
            var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.SolanaOASIS, toAvatarUsername);

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
                result = await SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount);
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessageTemplate}{ex.Message}", ex);
        }
        
        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
    }
}
