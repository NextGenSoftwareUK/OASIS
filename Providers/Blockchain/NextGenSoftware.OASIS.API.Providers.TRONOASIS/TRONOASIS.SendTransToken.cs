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
    public partial class TRONOASIS : Core.Interfaces.IOASISNativeFeeReceiptProvider
    {
        public Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
            => SendAvatarTokenAsync(fromAvatarUsername, toAvatarUsername, amount, token, byEmail: false);

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                TronNativeTransferClient.NativeUnits(amount);
                if (string.IsNullOrWhiteSpace(fromAvatarEmail) || string.IsNullOrWhiteSpace(toAvatarEmail))
                    throw new ArgumentException("Sender and recipient email addresses are required.");
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate TRON provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Get wallet addresses for both avatars by email
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, fromAvatarEmail);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, toAvatarEmail);
                
                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref response, "Failed to get wallet addresses for avatars by email");
                    return response;
                }
                
                var fromWalletAddress = fromWalletResult.Result;
                var toWalletAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromWalletAddress) || string.IsNullOrEmpty(toWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref response, "Unable to get wallet addresses for avatars by email");
                    return response;
                }

                return await SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, string.Empty);
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending TRX transaction on TRON: {ex.Message}");
            }
            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
            => SendAvatarTokenAsync(fromAvatarEmail, toAvatarEmail, amount, token, byEmail: true);

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        private async Task<OASISResult<ITransactionResponse>> SendAvatarTokenAsync(string sender, string recipient,
            decimal amount, string tokenAddress, bool byEmail)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
                if (string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(recipient))
                    throw new ArgumentException("Sender and recipient avatar identifiers are required.");
                if (string.IsNullOrWhiteSpace(tokenAddress))
                    throw new ArgumentException("An explicit TRC20 token contract address is required.");
                if (WalletManager == null) throw new InvalidOperationException("Avatar wallet manager is unavailable.");
                var senderAvatar = byEmail ? await AvatarManager.Instance.LoadAvatarByEmailAsync(sender)
                    : await AvatarManager.Instance.LoadAvatarAsync(sender);
                var recipientAvatar = byEmail ? await AvatarManager.Instance.LoadAvatarByEmailAsync(recipient)
                    : await AvatarManager.Instance.LoadAvatarAsync(recipient);
                if (senderAvatar.IsError || senderAvatar.Result == null || recipientAvatar.IsError || recipientAvatar.Result == null)
                    throw new InvalidOperationException("Could not resolve sender and recipient avatar identities.");
                // The ID lookup decrypts a detached wallet; never filter/decrypt the stored avatar wallet in place.
                var from = await WalletManager.GetAvatarDefaultWalletByIdAsync(senderAvatar.Result.Id,
                    Core.Enums.ProviderType.TRONOASIS, showPrivateKeys: true);
                var to = await WalletManager.GetAvatarDefaultWalletByIdAsync(recipientAvatar.Result.Id,
                    Core.Enums.ProviderType.TRONOASIS);
                if (from.IsError || from.Result == null || string.IsNullOrWhiteSpace(from.Result.PrivateKey)
                    || to.IsError || to.Result == null)
                    throw new InvalidOperationException("Could not resolve the sender signing wallet and recipient TRON wallet.");
                return await SendTokenAsync(new Core.Objects.Wallet.Requests.SendWeb3TokenRequest
                {
                    FromWalletAddress = from.Result.WalletAddress,
                    ToWalletAddress = to.Result.WalletAddress,
                    FromTokenAddress = tokenAddress,
                    Amount = amount,
                    OwnerPrivateKey = from.Result.PrivateKey,
                    OwnerPublicKey = from.Result.PublicKey
                });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending avatar TRC20 token: {ex.Message}", ex);
                return result;
            }
        }

        public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            // Delegate to SendTransactionByIdAsync (which uses default wallets)
            return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
        }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse());
            try
            {
                if (request == null) throw new ArgumentNullException(nameof(request));
                if (request.Amount <= 0) throw new ArgumentOutOfRangeException(nameof(request.Amount));
                var native = string.Equals(request.FromTokenAddress, "TRX", StringComparison.OrdinalIgnoreCase);
                if (native) TronNativeTransferClient.NativeUnits(request.Amount);
                if (!string.IsNullOrWhiteSpace(request.OwnerSeedPhrase))
                    throw new ArgumentException("TRON token transfer requires an explicit private signing key; seed phrases are not accepted.");
                if (string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                    throw new ArgumentException("A real sender private signing key is required for TRON token transfer.");
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrEmpty(request.FromWalletAddress) || string.IsNullOrEmpty(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "FromWalletAddress and ToWalletAddress are required");
                    return result;
                }

                // TRON TRC20 token transfer using TRON API
                var tokenAddress = string.IsNullOrEmpty(request.FromTokenAddress) ? _contractAddress ?? "" : request.FromTokenAddress;
                if (string.IsNullOrEmpty(tokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                var hash = native
                    ? await ExecuteRuntimeAsync(client => client.SendAsync(request.FromWalletAddress, request.ToWalletAddress,
                        request.Amount, request.OwnerPrivateKey, request.MemoText, publicKey: request.OwnerPublicKey))
                    : await ExecuteRuntimeAsync(client => client.SendTokenAsync(request.FromWalletAddress, request.ToWalletAddress,
                        tokenAddress, request.Amount, request.OwnerPrivateKey, request.MemoText, request.OwnerPublicKey));
                result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse { TransactionResult = hash };
                result.Message = native ? "TRX transfer confirmed by solidified native receipt." : "TRC20 transfer confirmed by solidified contract receipt.";
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
            var result = new OASISResult<ITransactionResponse>(new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse());
            try
            {
                if (request == null) throw new ArgumentNullException(nameof(request));
                if (request.Amount <= 0) throw new ArgumentOutOfRangeException(nameof(request.Amount));
                if (request.MintedByAvatarId == Guid.Empty) throw new ArgumentException("MintedByAvatarId is required for signing-wallet resolution.");
                if (string.IsNullOrWhiteSpace(_contractAddress)) throw new ArgumentException("A configured TRC20 mint-capable contract address is required.");
                if (request.ProviderType != null && request.ProviderType.Value != Core.Enums.ProviderType.TRONOASIS)
                    throw new ArgumentException("Mint provider must be TRONOASIS.");
                var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(request.MintedByAvatarId,
                    Core.Enums.ProviderType.TRONOASIS, showPrivateKeys: true);
                if (wallet.IsError || wallet.Result == null || string.IsNullOrWhiteSpace(wallet.Result.PrivateKey))
                    throw new InvalidOperationException("Could not resolve the avatar's TRON minter signing wallet.");
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
                        return result;
                    }
                }

                var hash = await ExecuteRuntimeAsync(client => client.MintTokenAsync(wallet.Result.WalletAddress, _contractAddress,
                    request.Amount, wallet.Result.PrivateKey, request.MemoText, wallet.Result.PublicKey));
                result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse { TransactionResult = hash };
                result.Message = "TRC20 mint confirmed by solidified contract receipt.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                if (request.Amount <= 0) throw new ArgumentOutOfRangeException(nameof(request.Amount));
                if (string.IsNullOrWhiteSpace(request.TokenAddress) || string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                    throw new ArgumentException("Token contract and holder signing key are required.");
                if (!string.IsNullOrWhiteSpace(request.OwnerSeedPhrase))
                    throw new ArgumentException("Recover a signing key explicitly before burning; seed/key ambiguity is not supported.");
                if (!IsProviderActivated)
                {
                    var activation = await ActivateProviderAsync();
                    if (activation.IsError) throw new InvalidOperationException(activation.Message);
                }
                var hash = await ExecuteRuntimeAsync(client => client.BurnTokenAsync(request.TokenAddress,
                    request.Amount, request.OwnerPrivateKey, publicKey: request.OwnerPublicKey));
                result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse { TransactionResult = hash };
                result.Message = "Requested TRC20 burn confirmed by solidified contract receipt.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token: {ex.Message}", ex);
            }
            return result;
        }

        private void EnsureNativeRuntime()
        {
            if (_nativeTransfers != null) return;
            _ownedRuntime = new TronSdkRuntime(new Uri(_fullNodeGrpcEndpoint), new Uri(_solidityNodeGrpcEndpoint),
                _genesisBlockId, Environment.GetEnvironmentVariable(_apiKeyEnvironmentVariable), _contractFeeLimitSun);
            _nativeTransfers = _ownedRuntime.NativeTransfers;
            _nativeSigningKeyResolver = address => Task.FromResult(Environment.GetEnvironmentVariable(_signingKeyEnvironmentVariable));
        }

        public async Task<OASISResult<System.Text.Json.JsonElement>> GetNativeFeeReceiptAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<System.Text.Json.JsonElement>();
            if (!System.Text.RegularExpressions.Regex.IsMatch(transactionHash ?? "", "\\A[0-9a-fA-F]{64}\\z"))
            {
                result.IsError = true; result.Message = "Exact TRON transaction ID required."; return result;
            }
            await _runtimeLifecycle.WaitAsync(token);
            try
            {
                // SDK read only: do not activate the provider's charge-bearing storage probe.
                EnsureNativeRuntime();
                result.Result = await _nativeTransfers.GetNativeFeeReceiptAsync(transactionHash, token);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
            finally { _runtimeLifecycle.Release(); }
            return result;
        }

        private async Task<T> ExecuteRuntimeAsync<T>(Func<TronNativeTransferClient, Task<T>> operation)
        {
            await _runtimeLifecycle.WaitAsync();
            try
            {
                if (!IsProviderActivated || _nativeTransfers == null)
                    throw new InvalidOperationException("The configured TRON SDK runtime is no longer active.");
                return await operation(_nativeTransfers);
            }
            finally { _runtimeLifecycle.Release(); }
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                if (string.IsNullOrWhiteSpace(request.TokenAddress) || string.IsNullOrWhiteSpace(request.FromWalletAddress)
                    || string.IsNullOrWhiteSpace(request.FromWalletPrivateKey))
                    throw new ArgumentException("Token contract, depositor address and private signing key are required.");
                if (string.IsNullOrWhiteSpace(_escrowContractAddress))
                    throw new InvalidOperationException("A separate EscrowContractAddress must be configured.");
                if (!IsProviderActivated)
                {
                    var activation = await ActivateProviderAsync();
                    if (activation.IsError) throw new InvalidOperationException(activation.Message);
                }
                var hash = await ExecuteRuntimeAsync(client => client.LockEscrowAsync(request.FromWalletAddress,
                    _escrowContractAddress, request.TokenAddress, request.FromWalletPrivateKey));
                result.Result = new TransactionResponse { TransactionResult = hash };
                result.Message = "Spendable TRC20 balance deposited into owned escrow and confirmed by solidified receipt.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking TRON escrow: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

    }
}
