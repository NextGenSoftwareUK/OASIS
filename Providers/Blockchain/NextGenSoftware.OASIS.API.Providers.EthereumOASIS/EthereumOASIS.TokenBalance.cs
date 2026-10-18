using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.IO;
using System.Reflection;
using System.Text;
using Nethereum.RPC.Accounts;
// using Nethereum.StandardTokenEIP20; // Commented out - type doesn't exist

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).SendToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).SendTokenAsync(request);
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).MintToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).MintTokenAsync(request);
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).BurnToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).BurnTokenAsync(request);
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).LockToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).LockTokenAsync(request);
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).UnlockToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).UnlockTokenAsync(request);
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).GetAwaiter().GetResult();
        }

        public Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
            =>NextGenSoftware.OASIS.Providers.Shared.Evm.EvmWalletBalance.ReadAsync(request,async ()=>
            {
                if (!IsProviderActivated || Web3Client == null)
                {
                    var active = await ActivateProviderAsync();
                    if(active.IsError) return new OASISResult<Web3>{IsError=true,Message=active.Message,Exception=active.Exception,ErrorCode=active.ErrorCode};
                }
                return new OASISResult<Web3>{Result=Web3Client};
            });

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
            =>NextGenSoftware.OASIS.Providers.Shared.Evm.EvmWalletHistory.ReadAsync(request,async ()=>
            {
                if (!IsProviderActivated || Web3Client == null)
                {
                    var active = await ActivateProviderAsync();
                    if(active.IsError) return new OASISResult<Web3>{IsError=true,Message=active.Message,Exception=active.Exception,ErrorCode=active.ErrorCode};
                }
                return new OASISResult<Web3>{Result=Web3Client};
            });

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return GenerateKeyPairAsync().Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Generate Ethereum key pair using Nethereum
                var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
                var publicKey = ecKey.GetPublicAddress();

                // Use KeyHelper to generate key pair structure
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    keyPair.PrivateKey = privateKey;
                    keyPair.PublicKey = publicKey;
                    keyPair.WalletAddressLegacy = publicKey; // publicKey from GetPublicAddress() is already the Ethereum address
                }

                result.Result = keyPair;
                result.IsError = false;
                result.Message = "Key pair generated successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.SendTokenAsync(ISendWeb3TokenRequest request)
        {
            return await ExecuteErc20MutationAsync(request?.OwnerPrivateKey, request?.FromTokenAddress, request?.ToWalletAddress, request?.Amount ?? 0, false);
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.MintTokenAsync(IMintWeb3TokenRequest request)
        {
            return await MintExistingErc20Async(request);
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            return await ExecuteErc20MutationAsync(request?.OwnerPrivateKey, request?.TokenAddress, null, request?.Amount ?? 0, true);
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.LockTokenAsync(ILockWeb3TokenRequest request)
        {
            return await ExecuteTokenEscrowAsync(request?.LockId ?? Guid.Empty, request?.TokenAddress, request?.EscrowContractAddress,
                request?.LockedByAvatarId ?? Guid.Empty, request?.FromWalletPrivateKey, request?.FromWalletAddress, request?.Amount ?? 0, false);
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            return await ExecuteTokenEscrowAsync(request?.LockId ?? Guid.Empty, request?.TokenAddress, request?.EscrowContractAddress,
                request?.UnlockedByAvatarId ?? Guid.Empty, null, null, 0, true);
        }


        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Account address is required");
                    return result;
                }

                var balance = await Web3Client.Eth.GetBalance.SendRequestAsync(accountAddress);
                result.Result = Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Ethereum account balance: {ex.Message}", ex);
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
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
                var publicKey = ecKey.GetPublicAddress();

                // Ethereum doesn't use seed phrases directly for account creation via Nethereum
                result.Result = (publicKey, privateKey, string.Empty);
                result.IsError = false;
                result.Message = "Ethereum account created successfully. Seed phrase not applicable for direct key generation.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating Ethereum account: {ex.Message}", ex);
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
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(seedPhrase))
                {
                    OASISErrorHandling.HandleError(ref result, "Seed phrase is required");
                    return result;
                }

                // Restore wallet from seed phrase using Nethereum HD wallet
                try
                {
                    var wallet = new Nethereum.HdWallet.Wallet(seedPhrase, null);
                    var account = wallet.GetAccount(0);

                    result.Result = (account.Address, account.PrivateKey);
                    result.IsError = false;
                    result.Message = "Ethereum account restored successfully from seed phrase.";
                }
                catch (Exception walletEx)
                {
                    // If HD wallet fails, try treating seedPhrase as a private key
                    try
                    {
                        var account = new Account(seedPhrase);
                        result.Result = (account.Address, account.PrivateKey);
                        result.IsError = false;
                        result.Message = "Ethereum account restored successfully from private key.";
                    }
                    catch
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to restore account from seed phrase or private key: {walletEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring Ethereum account: {ex.Message}", ex);
            }
            return result;
        }

    }
}
