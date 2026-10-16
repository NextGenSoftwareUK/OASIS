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
    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
    {
        return LockTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in LockTokenAsync method in SolanaOASIS. Reason: ";

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
            if (_solanaService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Solana service is not initialized");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                string.IsNullOrWhiteSpace(request.FromWalletPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                return result;
            }

            // Lock token by transferring to bridge pool
            var bridgePoolAddress = _oasisSolanaAccount.PublicKey.Key;
            var sendRequest = new SendWeb3TokenRequest
            {
                FromTokenAddress = request.TokenAddress,
                FromWalletPrivateKey = request.FromWalletPrivateKey,
                ToWalletAddress = bridgePoolAddress,
                Amount = 1m // Will get actual balance in SendTokenAsync
            };

            return await SendTokenAsync(sendRequest);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
    {
        return UnlockTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in UnlockTokenAsync method in SolanaOASIS. Reason: ";

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
            if (_solanaService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Solana service is not initialized");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address is required");
                return result;
            }

            // Get recipient address from KeyManager using UnlockedByAvatarId
            var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.SolanaOASIS, request.UnlockedByAvatarId);
            if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                return result;
            }

            // Unlock token by transferring from bridge pool to recipient
            var bridgePoolPrivateKey = Convert.ToBase64String(_oasisSolanaAccount.PrivateKey.KeyBytes);
            var sendRequest = new SendWeb3TokenRequest
            {
                FromTokenAddress = request.TokenAddress,
                FromWalletPrivateKey = bridgePoolPrivateKey,
                ToWalletAddress = toWalletResult.Result,
                Amount = 1m // Will get actual balance in SendTokenAsync
            };

            return await SendTokenAsync(sendRequest);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
    {
        return GetBalanceAsync(request).Result;
    }

    public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
    {
        OASISResult<double> result = new OASISResult<double>();
        DateTimeOffset date = DateTimeOffset.UtcNow;

        try
        {
            OASISResult<decimal> solResult = await _solanaService.GetAccountBalanceAsync(request);

            if (solResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result,
                    solResult.Message,
                    solResult.Exception);

                return result;
            }
            else
                result.Result = Convert.ToDouble(solResult.Result);
        }
        catch (Exception e)
        {
            OASISErrorHandling.HandleError(ref result, $"Unknown error occured: {e}");
        }

        return result;
    }

    public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
    {
        return GetTransactionsAsync(request).GetAwaiter().GetResult();
    }

    public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
    {
        var result = new OASISResult<IList<IWalletTransaction>>();
        string errorMessage = "Error in GetTransactionsAsync method in SolanaOASIS. Reason: ";

        try
        {
            if(request==null || string.IsNullOrWhiteSpace(request.WalletAddress)) throw new ArgumentException("Wallet address is required.");
            if(request.FromBlock.HasValue && request.ToBlock.HasValue && request.FromBlock>request.ToBlock)
                throw new ArgumentException("The inclusive slot range is reversed.");
            var address=new PublicKey(request.WalletAddress);
            if(address.KeyBytes.Length!=32) throw new ArgumentException("Invalid Solana wallet address.");
            var transactions=new System.Collections.Generic.List<IWalletTransaction>();
            var seen=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            string before=null;
            while(true)
            {
                var page=await _rpcClient.GetSignaturesForAddressAsync(address.Key,1000,before,null,Solnet.Rpc.Types.Commitment.Finalized);
                if(!page.WasSuccessful) throw new InvalidOperationException($"Solana history RPC failed: {page.Reason}");
                if(page.Result==null) throw new InvalidDataException("Solana history RPC returned no result.");
                bool belowRange=false;
                foreach(var item in page.Result)
                {
                    if(string.IsNullOrWhiteSpace(item.Signature) || !seen.Add(item.Signature))
                        throw new InvalidDataException("Solana history contains an empty or repeated signature.");
                    if(request.FromBlock.HasValue && item.Slot<request.FromBlock.Value) {belowRange=true;continue;}
                    if(request.ToBlock.HasValue && item.Slot>request.ToBlock.Value) continue;
                    transactions.Add(new WalletTransaction {
                        NativeTransactionId=item.Signature,NativeOperation="account transaction (amount not decoded)",
                        NativeExecutionResult=item.Error==null ? "succeeded":"failed",
                        NativeError=item.Error==null ? null:JsonConvert.SerializeObject(item.Error),
                        CreatedDate=item.BlockTime.HasValue ? DateTimeOffset.FromUnixTimeSeconds(checked((long)item.BlockTime.Value)).UtcDateTime:default,
                        Description=$"Finalized Solana slot {item.Slot}; transfer amounts and counterparties are not decoded."
                    });
                }
                if(page.Result.Count<1000 || belowRange) break;
                before=page.Result[page.Result.Count-1].Signature;
            }
            result.Result=transactions;
            result.Message=$"Retrieved {transactions.Count} finalized account transactions; transfer amounts are not decoded.";
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,errorMessage+cause.Message,cause);}
        return result;
    }

    public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
    {
        return GenerateKeyPairAsync().Result;
    }

    public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
    {
        var result = new OASISResult<IKeyPairAndWallet>();
        string errorMessage = "Error in GenerateKeyPairAsync method in SolanaOASIS. Reason: ";

        try
        {
            //if (!IsProviderActivated)
            //{
            //    OASISErrorHandling.HandleError(ref result, "Solana provider is not activated");
            //    return result;
            //}

            // Generate a new Solana wallet using Solnet.Wallet SDK (production-ready)
            var mnemonic = new Solnet.Wallet.Bip39.Mnemonic(Solnet.Wallet.Bip39.WordList.English, Solnet.Wallet.Bip39.WordCount.Twelve);
            var wallet = new Solnet.Wallet.Wallet(mnemonic);
            var account = wallet.Account;

            // Create key pair structure using Solana SDK values directly
            //var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
            //if (keyPair != null)
            //{
            //    keyPair.PrivateKey = Convert.ToBase64String(account.PrivateKey.KeyBytes);
            //    keyPair.PublicKey = account.PublicKey.Key;
            //    keyPair.WalletAddressLegacy = account.PublicKey.Key;
            //}

            result.Result = new KeyPairAndWallet()
            {
                PrivateKey = account.PrivateKey.Key,
                PublicKey = account.PublicKey.Key,
                WalletAddressLegacy = account.PublicKey.Key
            };
            result.IsError = false;
            result.Message = "Key pair generated successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }




    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();
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

            if (string.IsNullOrWhiteSpace(accountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Account address is required");
                return result;
            }

            var balanceRequest = new GetWeb3WalletBalanceRequest
            {
                WalletAddress = accountAddress
            };

            var balanceResult = await _solanaService.GetAccountBalanceAsync(balanceRequest);
            if (balanceResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, balanceResult.Message, balanceResult.Exception);
                return result;
            }

            result.Result = balanceResult.Result;
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting account balance: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
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

            // Generate a new Solana wallet
            var mnemonic = new Solnet.Wallet.Bip39.Mnemonic(Solnet.Wallet.Bip39.WordList.English, Solnet.Wallet.Bip39.WordCount.Twelve);
            var wallet = new Solnet.Wallet.Wallet(mnemonic);
            var account = wallet.Account;

            result.Result = (
                PublicKey: account.PublicKey.Key,
                PrivateKey: account.PrivateKey.Key,
                SeedPhrase: mnemonic.ToString()
            );
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error creating account: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey)>();
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

            if (string.IsNullOrWhiteSpace(seedPhrase))
            {
                OASISErrorHandling.HandleError(ref result, "Seed phrase is required");
                return result;
            }

            // Restore wallet from seed phrase
            var mnemonic = new Solnet.Wallet.Bip39.Mnemonic(seedPhrase, Solnet.Wallet.Bip39.WordList.English);
            var wallet = new Solnet.Wallet.Wallet(mnemonic);
            var account = wallet.Account;

            result.Result = (
                PublicKey: account.PublicKey.Key,
                PrivateKey: account.PrivateKey.Key
            );
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error restoring account: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
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
            if (_solanaService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Solana service is not initialized");
                return result;
            }

            if (string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Sender account address and private key are required");
                return result;
            }

            if (amount <= 0)
            {
                OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                return result;
            }

            // For bridge withdrawals, we lock the token by transferring to bridge pool
            // Using LockTokenAsync which handles the locking mechanism
            var lockRequest = new LockWeb3TokenRequest
            {
                FromWalletPrivateKey = senderPrivateKey,
                FromWalletAddress = senderAccountAddress,
                TokenAddress = string.Empty // Empty for native SOL
            };

            var lockResult = await LockTokenAsync(lockRequest);
            if (lockResult.IsError || lockResult.Result == null)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = lockResult.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                OASISErrorHandling.HandleError(ref result, $"Failed to lock token for withdrawal: {lockResult.Message}");
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
            OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
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

    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
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

            if (string.IsNullOrWhiteSpace(receiverAccountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Receiver account address is required");
                return result;
            }

            if (amount <= 0)
            {
                OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                return result;
            }

            // For bridge deposits, we send from the OASIS bridge pool to the receiver
            var sendRequest = new SendTransactionRequest
            {
                FromAccount = new BaseAccountRequest { PublicKey = _oasisSolanaAccount.PublicKey.Key },
                ToAccount = new BaseAccountRequest { PublicKey = receiverAccountAddress },
                Amount = (ulong)(amount * 1_000_000_000), // Convert SOL to lamports
                Lampposts = (ulong)(amount * 1_000_000_000), // Convert SOL to lamports
                MemoText = "Bridge Deposit"
            };

            var transactionResult = await _solanaService.SendTransaction(sendRequest);
            if (transactionResult.IsError)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = transactionResult.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                OASISErrorHandling.HandleError(ref result, transactionResult.Message, transactionResult.Exception);
                return result;
            }

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = transactionResult.Result.TransactionHash,
                IsSuccessful = true,
                Status = BridgeTransactionStatus.Pending
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
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

}
