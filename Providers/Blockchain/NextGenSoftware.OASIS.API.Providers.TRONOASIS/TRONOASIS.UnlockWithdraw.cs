using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS
{
    public partial class TRONOASIS
    {
        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                if (_idBoundEscrow != null)
                    return await ExecuteIdBoundTokenEscrowAsync(request.LockId, request.TokenAddress,
                        request.EscrowContractAddress, request.UnlockedByAvatarId, null, null, 0, true);
                if (request.UnlockedByAvatarId == Guid.Empty)
                    throw new ArgumentException("UnlockedByAvatarId is required for depositor signing-wallet resolution.");
                if (string.IsNullOrWhiteSpace(request.TokenAddress))
                    throw new ArgumentException("A TRC20 token contract address is required.");
                if (string.IsNullOrWhiteSpace(_escrowContractAddress))
                    throw new InvalidOperationException("A separate EscrowContractAddress must be configured.");
                NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.AggregateTokenEscrowRequest.Validate(
                    0, request.LockId, request.EscrowContractAddress, _escrowContractAddress);
                var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(request.UnlockedByAvatarId,
                    Core.Enums.ProviderType.TRONOASIS, showPrivateKeys: true);
                if (wallet.IsError || wallet.Result == null || string.IsNullOrWhiteSpace(wallet.Result.PrivateKey))
                    throw new InvalidOperationException("Could not resolve the depositor's TRON signing wallet.");
                if (!IsProviderActivated)
                {
                    var activation = await ActivateProviderAsync();
                    if (activation.IsError) throw new InvalidOperationException(activation.Message);
                }
                var hash = await ExecuteRuntimeAsync(client => client.UnlockEscrowAsync(wallet.Result.WalletAddress,
                    _escrowContractAddress, request.TokenAddress, wallet.Result.PrivateKey));
                result.Result = new TransactionResponse { TransactionResult = hash };
                result.Message = "Owned TRC20 escrow withdrawal confirmed by solidified receipt and returned amount.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking TRON escrow: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                if (string.IsNullOrWhiteSpace(request.WalletAddress))
                    throw new ArgumentException("A TRON wallet address is required.");
                var balance = await ExecuteNativeRuntimeAsync(client => client.GetNativeBalanceAsync(request.WalletAddress));
                result.Result = (double)balance; // Shared interface exposes double; SDK conversion retains exact decimal SUN.
                result.Message = "Native TRX balance read from the solidified account.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting TRON balance: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                if (string.IsNullOrWhiteSpace(request.WalletAddress)) throw new ArgumentException("Wallet address is required.");
                result.Result = await ExecuteNativeRuntimeAsync(async client =>
                {
                    var history = await new TronGridTokenHistoryClient(_httpClient, client.ExpectedGenesisBlockId)
                        .ReadNativeAsync(request.WalletAddress, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    return (IList<IWalletTransaction>)(await client.ReadWalletTransactionsAsync(request.WalletAddress,
                        history.Select(record => record.GetProperty("txID").GetString()))).ToList();
                });
                result.Message = "Indexed confirmed native history verified through SDK receipts; native hashes and exact SUN values are provided separately from OASIS IDs and display amounts.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading verified TRON wallet history: {ex.Message}", ex); }
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return GenerateKeyPairAsync().Result;
        }

        public Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                // Key generation is local cryptography, not funded account activation.
                var ecKey = EthECKey.GenerateKey();
                var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
                var publicKey = Convert.ToHexString(ecKey.GetPubKey()).ToLowerInvariant();
                var tronAddress = new TronNet.TronECKey(privateKey, TronNet.TronNetwork.MainNet).GetPublicAddress();

                result.Result = new KeyPairAndWallet()
                {                     
                    PrivateKey = privateKey,
                    PublicKey = publicKey,
                    WalletAddressLegacy = tronAddress
                };

                result.IsError = false;
                result.Message = "TRON secp256k1 key pair and Base58Check address generated locally; no on-chain account has been activated.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return Task.FromResult(result);
        }

        private static (string PublicKey, string PrivateKey) DeriveTronMnemonic(string phrase)
        {
            var mnemonic = new NBitcoin.Mnemonic(phrase, NBitcoin.Wordlist.English);
            if (!mnemonic.IsValidChecksum)
                throw new ArgumentException("Invalid BIP39 mnemonic checksum.", nameof(phrase));
            var privateKey = mnemonic.DeriveExtKey().Derive(new NBitcoin.KeyPath("m/44'/195'/0'/0/0")).PrivateKey.ToBytes();
            var keyHex = Convert.ToHexString(privateKey).ToLowerInvariant();
            return (Convert.ToHexString(new Nethereum.Signer.EthECKey(keyHex).GetPubKey()).ToLowerInvariant(), keyHex);
        }



        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                token.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(accountAddress))
                    throw new ArgumentException("Account address is required.", nameof(accountAddress));
                result.Result = await ExecuteNativeRuntimeAsync(client => client.GetNativeBalanceAsync(accountAddress, token), token);
                result.Message = "Native TRX account balance retrieved from the solidity node.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting account balance: {ex.Message}", ex);
            }
            return result;
        }

        public Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                token.ThrowIfCancellationRequested();
                var phrase = new NBitcoin.Mnemonic(NBitcoin.Wordlist.English, NBitcoin.WordCount.Twelve).ToString();
                var key = DeriveTronMnemonic(phrase);
                result.Result = (key.PublicKey, key.PrivateKey, phrase);
                result.Message = "TRON key pair generated locally; the on-chain account is not activated.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating account: {ex.Message}", ex);
            }
            return Task.FromResult(result);
        }

        public Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                token.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(seedPhrase))
                    throw new ArgumentException("A valid BIP39 mnemonic is required.", nameof(seedPhrase));
                result.Result = DeriveTronMnemonic(seedPhrase);
                result.Message = "TRON key pair restored locally using m/44'/195'/0'/0/0.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring account: {ex.Message}", ex);
            }
            return Task.FromResult(result);
        }

    }
}
