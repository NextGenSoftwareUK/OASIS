using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using LockWeb3TokenRequest = NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.LockWeb3TokenRequest;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Infrastructure.Services.Miden;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Models;
using NextGenSoftware.OASIS.Common;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS
{
    public partial class MidenOASIS
    {
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                _midenService = new MidenService(_apiClient);
                _bridgeService = new MidenBridgeService(_midenService);

                IsProviderActivated = true;
                result.Result = true;
                result.Message = "Miden provider activated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _midenService = null;
            _bridgeService = null;
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        /// <summary>
        /// Gets the bridge service for cross-chain operations
        /// </summary>
        public MidenBridgeService GetBridgeService()
        {
            return _bridgeService;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }


        public async Task<OASISResult<PrivateNote>> CreatePrivateNoteAsync(decimal value, string ownerPublicKey, string assetId = null, string metadata = null)
        {
            var result = new OASISResult<PrivateNote>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_midenService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden service is not initialized");
                    return result;
                }

                result.Result = await _midenService.CreatePrivateNoteAsync(value, ownerPublicKey, assetId, metadata);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }

            return result;
        }

        public async Task<OASISResult<STARKProof>> GenerateSTARKProofAsync(string programHash, object inputs, object outputs)
        {
            var result = new OASISResult<STARKProof>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_midenService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden service is not initialized");
                    return result;
                }

                result.Result = await _midenService.GenerateSTARKProofAsync(programHash, inputs, outputs);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<bool>> VerifySTARKProofAsync(STARKProof proof)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_midenService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden service is not initialized");
                    return result;
                }

                result.Result = await _midenService.VerifySTARKProofAsync(proof);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<PrivateNote>> NullifyNoteAsync(string noteId, STARKProof proof)
        {
            var result = new OASISResult<PrivateNote>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_midenService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden service is not initialized");
                    return result;
                }

                result.Result = await _midenService.NullifyNoteAsync(noteId, proof);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        // Bridge operations for Zcash ↔ Miden
        public async Task<OASISResult<string>> MintOnMidenAsync(string midenAddress, decimal amount, string zcashTxHash, string viewingKey)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_midenService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden service is not initialized");
                    return result;
                }

                var mintResult = await _midenService.MintOnMidenAsync(midenAddress, amount, zcashTxHash, viewingKey);
                if (mintResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, mintResult.Message);
                    return result;
                }
                result.Result = mintResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<string>> LockOnMidenAsync(string midenAddress, decimal amount, string zcashAddress)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_midenService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden service is not initialized");
                    return result;
                }

                var lockResult = await _midenService.LockOnMidenAsync(midenAddress, amount, zcashAddress);
                if (lockResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, lockResult.Message);
                    return result;
                }
                result.Result = lockResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public async Task<OASISResult<string>> ReleaseFromMidenAsync(string midenAddress, decimal amount, string zcashAddress)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_midenService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden service is not initialized");
                    return result;
                }

                var releaseResult = await _midenService.ReleaseFromMidenAsync(midenAddress, amount, zcashAddress);
                if (releaseResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, releaseResult.Message);
                    return result;
                }
                result.Result = releaseResult.Result;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }



        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query all avatars from Miden API
                var apiResult = await _apiClient.GetAsync<List<Avatar>>($"/api/avatars?version={version}");
                
                if (!apiResult.IsError && apiResult.Result != null)
                {
                    result.Result = apiResult.Result.Cast<IAvatar>();
                    result.IsError = false;
                    result.Message = $"Successfully loaded {apiResult.Result.Count} avatars from Miden";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatars from Miden: {apiResult.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatars from Miden: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden provider is not activated");
                    return result;
                }

                // Load avatar from Miden (stored as holon)
                var holon = await LoadHolonAsync(Id);
                if (holon.IsError || holon.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                    return result;
                }

                // Convert holon to avatar
                if (holon.Result is IAvatar avatar)
                {
                    result.Result = avatar;
                }
                else
                {
                    result.Result = ConvertHolonToAvatar(holon.Result);
                }
                result.IsError = false;
                result.Message = "Avatar loaded successfully from Miden";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0) => LoadAvatarAsync(Id, version).Result;

        // Additional required overrides would go here...
        // For now, implementing minimal set for bridge functionality



        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated || _bridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden provider is not activated");
                    return result;
                }

                return await _bridgeService.GetAccountBalanceAsync(accountAddress, token);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting account balance: {ex.Message}", ex);
                return result;
            }
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!IsProviderActivated || _bridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden provider is not activated");
                    return result;
                }

                return await _bridgeService.CreateAccountAsync(token);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating account: {ex.Message}", ex);
                return result;
            }
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                if (!IsProviderActivated || _bridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden provider is not activated");
                    return result;
                }

                return await _bridgeService.RestoreAccountAsync(seedPhrase, token);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring account: {ex.Message}", ex);
                return result;
            }
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated || _bridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden provider is not activated");
                    return result;
                }

                return await _bridgeService.WithdrawAsync(amount, senderAccountAddress, senderPrivateKey);
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
                return result;
            }
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated || _bridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden provider is not activated");
                    return result;
                }

                return await _bridgeService.DepositAsync(amount, receiverAccountAddress);
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
                return result;
            }
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                if (!IsProviderActivated || _bridgeService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden provider is not activated");
                    return result;
                }

                return await _bridgeService.GetTransactionStatusAsync(transactionHash, token);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transaction status: {ex.Message}", ex);
                return result;
            }
        }



        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Miden provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (_midenService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Miden service is not initialized");
                    return result;
                }

                // Miden uses private notes for token transfers
                // Create a private note for the recipient
                var privateNote = await _midenService.CreatePrivateNoteAsync(
                    request.Amount,
                    request.ToWalletAddress,
                    request.FromTokenAddress, // assetId
                    request.MemoText);

                result.Result.TransactionResult = privateNote?.NoteId ?? string.Empty;
                result.IsError = false;
                result.Message = "Token sent successfully on Miden.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
    }
}
