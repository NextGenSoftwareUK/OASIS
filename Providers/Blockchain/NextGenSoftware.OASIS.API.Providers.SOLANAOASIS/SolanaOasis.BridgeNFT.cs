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
    public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
    {
        var result = new OASISResult<BridgeTransactionStatus>();
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

            if (string.IsNullOrWhiteSpace(transactionHash))
            {
                OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                return result;
            }

            // Check transaction status using RPC
            var transactionResult = await _rpcClient.GetTransactionAsync(transactionHash);
            
            if (!transactionResult.WasSuccessful)
            {
                // Transaction might not be found or failed
                result.Result = BridgeTransactionStatus.NotFound;
                result.IsError = false;
                return result;
            }

            if (transactionResult.Result != null)
            {
                var transaction = transactionResult.Result;
                if (transaction.Meta != null && transaction.Meta.Error != null)
                {
                    result.Result = BridgeTransactionStatus.Canceled;
                }
                else if (transaction.Meta != null && transaction.Meta.Error == null)
                {
                    // Check if transaction is finalized
                    if (transaction.Slot > 0)
                    {
                        result.Result = BridgeTransactionStatus.Completed;
                    }
                    else
                    {
                        result.Result = BridgeTransactionStatus.Pending;
                    }
                }
                else
                {
                    result.Result = BridgeTransactionStatus.Pending;
                }
            }
            else
            {
                result.Result = BridgeTransactionStatus.NotFound;
            }

            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting transaction status: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
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

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                return result;
            }

            // Use LockNFTAsync internally for withdrawal
            var lockRequest = new LockWeb3NFTRequest
            {
                NFTTokenAddress = nftTokenAddress,
                Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : CreateDeterministicGuid($"{ProviderType.Value}:nft:{nftTokenAddress}"),
                LockedByAvatarId = Guid.Empty // Would be passed in a real implementation
            };

            var lockResult = await LockNFTAsync(lockRequest);
            if (lockResult.IsError || lockResult.Result == null)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = lockResult.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {lockResult.Message}");
                return result;
            }

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = lockResult.Result.TransactionResult ?? string.Empty,
                IsSuccessful = !lockResult.IsError,
                Status = BridgeTransactionStatus.Pending
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
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

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(receiverAccountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address and receiver address are required");
                return result;
            }

            // For deposit, we mint a wrapped NFT on the destination chain
            // In production, you would retrieve NFT metadata from sourceTransactionHash
            var mintRequest = new MintWeb3NFTRequest
            {
                SendToAddressAfterMinting = receiverAccountAddress,
                // Additional metadata would be retrieved from source chain via sourceTransactionHash
                // This is a simplified implementation
            };

            var mintResult = await MintNFTAsync(mintRequest);
            if (mintResult.IsError || mintResult.Result == null)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = mintResult.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                OASISErrorHandling.HandleError(ref result, $"Failed to deposit/mint NFT: {mintResult.Message}");
                return result;
            }

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = mintResult.Result.TransactionResult ?? string.Empty,
                IsSuccessful = !mintResult.IsError,
                Status = BridgeTransactionStatus.Pending
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error depositing NFT: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }


    public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
    {
        try
        {
            if (string.IsNullOrEmpty(outputFolder))
                return false;

            string solanaFolder = Path.Combine(outputFolder, "Solana");
            if (!Directory.Exists(solanaFolder))
                Directory.CreateDirectory(solanaFolder);

            if (!string.IsNullOrEmpty(nativeSource))
            {
                File.WriteAllText(Path.Combine(solanaFolder, "lib.rs"), nativeSource);
                return true;
            }

            if (celestialBody == null)
                return true;

            var sb = new StringBuilder();
            sb.AppendLine("// Auto-generated by SolanaOASIS.NativeCodeGenesis");
            sb.AppendLine("use anchor_lang::prelude::*;");
            sb.AppendLine();
            sb.AppendLine($"declare_id!(\"YourProgramIdHere\");");
            sb.AppendLine();
            sb.AppendLine($"#[program]");
            sb.AppendLine($"pub mod {celestialBody.Name?.ToSnakeCase() ?? "oapp"} {{");
            sb.AppendLine("    use super::*;");
            sb.AppendLine();

            var zomes = celestialBody.CelestialBodyCore?.Zomes;
            if (zomes != null)
            {
                foreach (var zome in zomes)
                {
                    if (zome?.Children == null) continue;

                    foreach (var holon in zome.Children)
                    {
                        if (holon == null || string.IsNullOrWhiteSpace(holon.Name)) continue;

                        var holonTypeName = holon.Name.ToPascalCase();
                        var holonVarName = holon.Name.ToSnakeCase();

                        sb.AppendLine($"    // {holonTypeName} Account");
                        sb.AppendLine($"    #[account]");
                        sb.AppendLine($"    pub struct {holonTypeName}Account {{");
                        sb.AppendLine("        pub id: String,");
                        sb.AppendLine("        pub name: String,");
                        sb.AppendLine("        pub description: String,");
                        sb.AppendLine("    }");
                        sb.AppendLine();

                        sb.AppendLine($"    // Create {holonTypeName}");
                        sb.AppendLine($"    pub fn create_{holonVarName}(ctx: Context<Create{holonTypeName}>, id: String, name: String, description: String) -> Result<()> {{");
                        sb.AppendLine($"        let {holonVarName} = &mut ctx.accounts.{holonVarName};");
                        sb.AppendLine($"        {holonVarName}.id = id;");
                        sb.AppendLine($"        {holonVarName}.name = name;");
                        sb.AppendLine($"        {holonVarName}.description = description;");
                        sb.AppendLine("        Ok(())");
                        sb.AppendLine("    }");
                        sb.AppendLine();

                        sb.AppendLine($"    #[derive(Accounts)]");
                        sb.AppendLine($"    pub struct Create{holonTypeName}<'info> {{");
                        sb.AppendLine($"        #[account(init, payer = user, space = 8 + 32 + 4 + 32 + 4 + 32)]");
                        sb.AppendLine($"        pub {holonVarName}: Account<'info, {holonTypeName}Account>,");
                        sb.AppendLine("        #[account(mut)]");
                        sb.AppendLine("        pub user: Signer<'info>,");
                        sb.AppendLine("        pub system_program: Program<'info, System>,");
                        sb.AppendLine("    }");
                        sb.AppendLine();
                    }
                }
            }

            sb.AppendLine("}");
            File.WriteAllText(Path.Combine(solanaFolder, "lib.rs"), sb.ToString());
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }
}
