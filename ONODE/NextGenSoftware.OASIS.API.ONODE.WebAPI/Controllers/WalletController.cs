using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Wallet management endpoints for cryptocurrency and digital asset operations.
    /// Provides comprehensive wallet functionality including transactions, balances, and multi-chain support.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : OASISControllerBase
    {
        private WalletManager _walletManager = null;

        public WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;

                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));

                    _walletManager = new WalletManager(result.Result);
                }

                return _walletManager;
            }
        }

        ///// <summary>
        /////     Clear's the KeyManager's internal cache of keys.
        ///// </summary>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("clear_cache")]
        //public OASISResult<bool> ClearCache()
        //{
        //    return WalletManager.ClearCache();
        //}

        /// <summary>
        ///     Send's a given token to the target provider.
        /// </summary>
        /// <param name="request">The wallet transaction request containing token details and recipient information.</param>
        /// <returns>OASIS result containing the transaction response or error details.</returns>
        /// <response code="200">Token sent successfully</response>
        /// <response code="400">Error sending token</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("send-token")]
        [ProducesResponseType(typeof(OASISResult<ISendWeb4TokenResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(OASISResult<ITransactionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<ISendWeb4TokenResponse>> SendTokenAsync([FromBody] SendWeb4TokenRequest request)
        {
            if (request == null)
                return new OASISResult<ISendWeb4TokenResponse> { IsError = true, Message = "A token transfer request is required." };

            if ((request.FromAvatarId != Guid.Empty && request.FromAvatarId != AvatarId)
                || (!string.IsNullOrWhiteSpace(request.FromAvatarUsername) && !string.Equals(request.FromAvatarUsername, Avatar?.Username, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(request.FromAvatarEmail) && !string.Equals(request.FromAvatarEmail, Avatar?.Email, StringComparison.OrdinalIgnoreCase)))
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return new OASISResult<ISendWeb4TokenResponse> { IsError = true, ErrorCode = "AVATAR_OPERATION_FORBIDDEN",
                    Message = "Forbidden. All requested sender avatar selectors must match the authenticated avatar." };
            }
            if (request.FromProvider == null || request.ToProvider == null)
                return new OASISResult<ISendWeb4TokenResponse>
                {
                    IsError = true,
                    Message = "Explicit sender and recipient providers are required for token transfer."
                };
            return await WalletManager.SendTokenAsync(AvatarId, request);
        }

        ///// <summary>
        /////     Load all provider wallets for an avatar by ID.
        ///// </summary>
        ///// <param name="id">The avatar ID.</param>
        ///// <param name="providerType">The provider type to load wallets from.</param>
        ///// <returns>OASIS result containing the provider wallets or error details.</returns>
        ///// <response code="200">Wallets loaded successfully</response>
        ///// <response code="400">Error loading wallets</response>
        ///// <response code="401">Unauthorized - authentication required</response>
        //[Authorize]
        //[HttpGet("avatar/{id}/wallets/{showOnlyDefault}/{decryptPrivateKeys}")]
        //[ProducesResponseType(typeof(OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        //public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> Create(Guid id, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerType = ProviderType.Default)
        //{
        //    return await WalletManager.LoadProviderWalletsForAvatarByIdAsync(id, showOnlyDefault, decryptPrivateKeys, providerType);
        //}

        /// <summary>Get wallet count and default balance for the authenticated avatar.</summary>
        [Authorize]
        [HttpGet("current-avatar/summary")]
        public Task<OASISResult<WalletSummary>> GetCurrentAvatarWalletSummary() =>
            WalletManager.LoadWalletSummaryAsync(AvatarId);

        /// <summary>
        ///     Load all provider wallets for an avatar by ID.
        /// </summary>
        /// <param name="id">The avatar ID.</param>
        /// <param name="providerType">The provider type to load wallets from.</param>
        /// <returns>OASIS result containing the provider wallets or error details.</returns>
        /// <response code="200">Wallets loaded successfully</response>
        /// <response code="400">Error loading wallets</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpGet("avatar/{id}/wallets/{showOnlyDefault}/{decryptPrivateKeys}")]
        [ProducesResponseType(typeof(OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerType = ProviderType.Default)
        {
            if (decryptPrivateKeys && !OwnsWalletAvatar(id))
                return RejectWalletKeyRead<Dictionary<ProviderType, List<IProviderWallet>>>();

            try
            {
                // This used to be wrapped in a bare `catch { }`. With test data disabled (the production
                // default) a failed load left `result` null and the endpoint returned literal `{}` - which is
                // how a successfully created wallet appeared to silently vanish. Let it surface instead: the
                // outer catch turns it into a proper error response.
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result =
                    await WalletManager.LoadProviderWalletsForAvatarByIdAsync(id, showOnlyDefault, decryptPrivateKeys, false, ProviderType.All, providerType);

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>
                    {
                        Result = new Dictionary<ProviderType, List<IProviderWallet>>(),
                        IsError = false,
                        Message = "Wallets loaded successfully (using test data)"
                    };
                }

                return result;
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>
                    {
                        Result = new Dictionary<ProviderType, List<IProviderWallet>>(),
                        IsError = false,
                        Message = "Wallets loaded successfully (using test data)"
                    };
                }
                return new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>
                {
                    IsError = true,
                    Message = $"Error loading wallets: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        ///     Load all provider wallets for an avatar by username.
        /// </summary>
        /// <param name="username">The avatar username.</param>
        /// <param name="providerType">The provider type to load wallets from.</param>
        /// <returns>OASIS result containing the provider wallets or error details.</returns>
        /// <response code="200">Wallets loaded successfully</response>
        /// <response code="400">Error loading wallets</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpGet("avatar/username/{username}/wallets/{showOnlyDefault}/{decryptPrivateKeys}")]
        [ProducesResponseType(typeof(OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameAsync(string username, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerType = ProviderType.Default)
        {
            if (decryptPrivateKeys && !OwnsWalletIdentity(username, Avatar?.Username))
                return RejectWalletKeyRead<Dictionary<ProviderType, List<IProviderWallet>>>();

            return decryptPrivateKeys
                ? await WalletManager.LoadProviderWalletsForAvatarByIdAsync(AvatarId, showOnlyDefault, true, false, ProviderType.All, providerType)
                : await WalletManager.LoadProviderWalletsForAvatarByUsernameAsync(username, showOnlyDefault, false, false, ProviderType.All, providerType);
        }


        /// <summary>
        ///     Load all provider wallets for an avatar by email.
        /// </summary>
        /// <param name="email">The avatar email.</param>
        /// <param name="providerType">The provider type to load wallets from.</param>
        /// <returns>OASIS result containing the provider wallets or error details.</returns>
        /// <response code="200">Wallets loaded successfully</response>
        /// <response code="400">Error loading wallets</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpGet("avatar/email/{email}/wallets")]
        [ProducesResponseType(typeof(OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailAsync(string email, ProviderType providerType = ProviderType.Default)
        {
                        if (email != Avatar?.Email && Avatar?.AvatarType?.Value != AvatarType.Wizard)
                return new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> { IsError = true, Message = "Unauthorized. You can only access your own wallet data." };
            return await WalletManager.LoadProviderWalletsForAvatarByEmailAsync(email);
        }

        /// <summary>
        ///     Load all provider wallets for an avatar by email.
        /// </summary>
        /// <param name="email">The avatar email.</param>
        /// <param name="providerType">The provider type to load wallets from.</param>
        /// <returns>OASIS result containing the provider wallets or error details.</returns>
        /// <response code="200">Wallets loaded successfully</response>
        /// <response code="400">Error loading wallets</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpGet("avatar/email/{email}/wallets/{showOnlyDefault}/{decryptPrivateKeys}/{providerType}")]
        [ProducesResponseType(typeof(OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailAsync(string email, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerType = ProviderType.Default)
        {
            if (decryptPrivateKeys && !OwnsWalletIdentity(email, Avatar?.Email))
                return RejectWalletKeyRead<Dictionary<ProviderType, List<IProviderWallet>>>();

            return decryptPrivateKeys
                ? await WalletManager.LoadProviderWalletsForAvatarByIdAsync(AvatarId, showOnlyDefault, true, false, ProviderType.All, providerType)
                : await WalletManager.LoadProviderWalletsForAvatarByEmailAsync(email, showOnlyDefault, false, false, ProviderType.All, providerType);
        }

        /// <summary>
        ///     Save provider wallets for an avatar by ID.
        /// </summary>
        /// <param name="id">The avatar ID.</param>
        /// <param name="wallets">The wallets to save.</param>
        /// <param name="providerType">The provider type to save wallets to.</param>
        /// <returns>OASIS result indicating success or failure.</returns>
        /// <response code="200">Wallets saved successfully</response>
        /// <response code="400">Error saving wallets</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("avatar/{id}/wallets")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            if (!OwnsWalletAvatar(id))
                return RejectWalletAccess<bool>("Only the authenticated wallet owner can change wallet settings.");

            return await WalletManager.SaveProviderWalletsForAvatarByIdAsync(id, wallets, providerType);
        }

        /// <summary>
        ///     Save provider wallets for an avatar by username.
        /// </summary>
        /// <param name="username">The avatar username.</param>
        /// <param name="wallets">The wallets to save.</param>
        /// <param name="providerType">The provider type to save wallets to.</param>
        /// <returns>OASIS result indicating success or failure.</returns>
        /// <response code="200">Wallets saved successfully</response>
        /// <response code="400">Error saving wallets</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("avatar/username/{username}/wallets")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByUsernameAsync(string username, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            if (!OwnsWalletIdentity(username, Avatar?.Username))
                return RejectWalletAccess<bool>("Only the authenticated wallet owner can change wallet settings.");

            return await WalletManager.SaveProviderWalletsForAvatarByIdAsync(AvatarId, wallets, providerType);
        }

        /// <summary>
        ///     Save provider wallets for an avatar by email.
        /// </summary>
        /// <param name="email">The avatar email.</param>
        /// <param name="wallets">The wallets to save.</param>
        /// <param name="providerType">The provider type to save wallets to.</param>
        /// <returns>OASIS result indicating success or failure.</returns>
        /// <response code="200">Wallets saved successfully</response>
        /// <response code="400">Error saving wallets</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("avatar/email/{email}/wallets")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByEmailAsync(string email, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            if (!OwnsWalletIdentity(email, Avatar?.Email))
                return RejectWalletAccess<bool>("Only the authenticated wallet owner can change wallet settings.");

            return await WalletManager.SaveProviderWalletsForAvatarByIdAsync(AvatarId, wallets, providerType);
        }

        /// <summary>
        ///     Get the default wallet for an avatar by ID.
        /// </summary>
        /// <param name="id">The avatar ID.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>OASIS result containing the default wallet or error details.</returns>
        /// <response code="200">Default wallet retrieved successfully</response>
        /// <response code="400">Error retrieving default wallet</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpGet("avatar/{id}/default-wallet")]
        [ProducesResponseType(typeof(OASISResult<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByIdAsync(Guid id, ProviderType providerType)
        {
            return await WalletManager.GetAvatarDefaultWalletByIdAsync(id, providerType);
        }

        /// <summary>
        ///     Get the default wallet for an avatar by username.
        /// </summary>
        /// <param name="username">The avatar username.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>OASIS result containing the default wallet or error details.</returns>
        /// <response code="200">Default wallet retrieved successfully</response>
        /// <response code="400">Error retrieving default wallet</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpGet("avatar/username/{username}/default-wallet/{showOnlyDefault}/{decryptPrivateKeys}")]
        [ProducesResponseType(typeof(OASISResult<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByUsernameAsync(string username, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerType = ProviderType.Default)
        {
            if (decryptPrivateKeys && !OwnsWalletIdentity(username, Avatar?.Username))
                return RejectWalletKeyRead<IProviderWallet>();

            return decryptPrivateKeys
                ? await WalletManager.GetAvatarDefaultWalletByIdAsync(AvatarId, providerType, showOnlyDefault, true)
                : await WalletManager.GetAvatarDefaultWalletByUsernameAsync(username, showOnlyDefault, false, false, providerType);
        }

        private bool OwnsWalletAvatar(Guid id) => id != Guid.Empty && AvatarId == id;

        private bool OwnsWalletIdentity(string requested, string authenticated) =>
            AvatarId != Guid.Empty && !string.IsNullOrWhiteSpace(authenticated) &&
            string.Equals(requested, authenticated, StringComparison.OrdinalIgnoreCase);

        private OASISResult<T> RejectWalletKeyRead<T>() =>
            RejectWalletAccess<T>("Only the authenticated wallet owner can request private keys.");

        private OASISResult<T> RejectWalletAccess<T>(string message)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return new OASISResult<T>
            {
                IsError = true,
                ErrorCode = "AVATAR_OPERATION_FORBIDDEN",
                Message = message
            };
        }

        /// <summary>
        ///     Get the default wallet for an avatar by email.
        /// </summary>
        /// <param name="email">The avatar email.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>OASIS result containing the default wallet or error details.</returns>
        /// <response code="200">Default wallet retrieved successfully</response>
        /// <response code="400">Error retrieving default wallet</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpGet("avatar/email/{email}/default-wallet")]
        [ProducesResponseType(typeof(OASISResult<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByEmailAsync(string email, ProviderType providerType)
        {
            return await WalletManager.GetAvatarDefaultWalletByEmailAsync(email, providerType);
        }

        /// <summary>
        ///     Set the default wallet for an avatar by ID.
        /// </summary>
        /// <param name="id">The avatar ID.</param>
        /// <param name="walletId">The wallet ID to set as default.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>OASIS result indicating success or failure.</returns>
        /// <response code="200">Default wallet set successfully</response>
        /// <response code="400">Error setting default wallet</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("avatar/{id}/default-wallet/{walletId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<IProviderWallet>> SetAvatarDefaultWalletByIdAsync(Guid id, Guid walletId, ProviderType providerType)
        {
            if (!OwnsWalletAvatar(id))
                return RejectWalletAccess<IProviderWallet>("Only the authenticated wallet owner can change wallet settings.");

            return await WalletManager.SetAvatarDefaultWalletByIdAsync(id, walletId, providerType);
        }

        /// <summary>
        ///     Set the default wallet for an avatar by username.
        /// </summary>
        /// <param name="username">The avatar username.</param>
        /// <param name="walletId">The wallet ID to set as default.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>OASIS result indicating success or failure.</returns>
        /// <response code="200">Default wallet set successfully</response>
        /// <response code="400">Error setting default wallet</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("avatar/username/{username}/default-wallet/{walletId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<IProviderWallet>> SetAvatarDefaultWalletByUsernameAsync(string username, Guid walletId, ProviderType providerType)
        {
            if (!OwnsWalletIdentity(username, Avatar?.Username))
                return RejectWalletAccess<IProviderWallet>("Only the authenticated wallet owner can change wallet settings.");

            return await WalletManager.SetAvatarDefaultWalletByIdAsync(AvatarId, walletId, providerType);
        }

        /// <summary>
        ///     Set the default wallet for an avatar by email.
        /// </summary>
        /// <param name="email">The avatar email.</param>
        /// <param name="walletId">The wallet ID to set as default.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>OASIS result indicating success or failure.</returns>
        /// <response code="200">Default wallet set successfully</response>
        /// <response code="400">Error setting default wallet</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("avatar/email/{email}/default-wallet/{walletId}")]
        [ProducesResponseType(typeof(OASISResult<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<IProviderWallet>> SetAvatarDefaultWalletByEmailAsync(string email, Guid walletId, ProviderType providerType)
        {
            if (!OwnsWalletIdentity(email, Avatar?.Email))
                return RejectWalletAccess<IProviderWallet>("Only the authenticated wallet owner can change wallet settings.");

            return await WalletManager.SetAvatarDefaultWalletByIdAsync(AvatarId, walletId, providerType);
        }

        /// <summary>
        ///     Import a wallet using private key by avatar ID.
        /// </summary>
        /// <param name="avatarId">The avatar ID.</param>
        /// <param name="key">The private key.</param>
        /// <param name="providerToImportTo">The provider type to import to.</param>
        /// <returns>OASIS result containing the wallet ID or error details.</returns>
        /// <response code="200">Wallet imported successfully</response>
        /// <response code="400">Error importing wallet</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("avatar/{avatarId}/import/private-key")]
        [ProducesResponseType(typeof(OASISResult<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public OASISResult<IProviderWallet> ImportWalletUsingPrivateKeyById(Guid avatarId, string key, ProviderType providerToImportTo)
        {
            if (!OwnsWalletAvatar(avatarId))
                return RejectWalletAccess<IProviderWallet>("Only the authenticated wallet owner can change wallet settings.");

            return WalletManager.ImportWalletUsingPrivateKeyById(avatarId, key, providerToImportTo);
        }

        /// <summary>
        ///     Import a wallet using private key by username.
        /// </summary>
        /// <param name="username">The avatar username.</param>
        /// <param name="key">The private key.</param>
        /// <param name="providerToImportTo">The provider type to import to.</param>
        /// <returns>OASIS result containing the wallet ID or error details.</returns>
        /// <response code="200">Wallet imported successfully</response>
        /// <response code="400">Error importing wallet</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("avatar/username/{username}/import/private-key")]
        [ProducesResponseType(typeof(OASISResult<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public OASISResult<IProviderWallet> ImportWalletUsingPrivateKeyByUsername(string username, string key, ProviderType providerToImportTo)
        {
            if (!OwnsWalletIdentity(username, Avatar?.Username))
                return RejectWalletAccess<IProviderWallet>("Only the authenticated wallet owner can change wallet settings.");

            return WalletManager.ImportWalletUsingPrivateKeyById(AvatarId, key, providerToImportTo);
        }

        /// <summary>
        ///     Import a wallet using private key by email.
        /// </summary>
        /// <param name="email">The avatar email.</param>
        /// <param name="key">The private key.</param>
        /// <param name="providerToImportTo">The provider type to import to.</param>
        /// <returns>OASIS result containing the wallet ID or error details.</returns>
        /// <response code="200">Wallet imported successfully</response>
        /// <response code="400">Error importing wallet</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("avatar/email/{email}/import/private-key")]
        [ProducesResponseType(typeof(OASISResult<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public OASISResult<IProviderWallet> ImportWalletUsingPrivateKeyByEmail(string email, string key, ProviderType providerToImportTo)
        {
            if (!OwnsWalletIdentity(email, Avatar?.Email))
                return RejectWalletAccess<IProviderWallet>("Only the authenticated wallet owner can change wallet settings.");

            return WalletManager.ImportWalletUsingPrivateKeyById(AvatarId, key, providerToImportTo);
        }

        /// <summary>
        ///     Import a wallet using public key by avatar ID.
        /// </summary>
        /// <param name="avatarId">The avatar ID.</param>
        /// <param name="key">The public key.</param>
        /// <param name="providerToImportTo">The provider type to import to.</param>
        /// <returns>OASIS result containing the wallet ID or error details.</returns>
        /// <response code="200">Wallet imported successfully</response>
        /// <response code="400">Error importing wallet</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("avatar/{avatarId}/import/public-key")]
        [ProducesResponseType(typeof(OASISResult<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public OASISResult<IProviderWallet> ImportWalletUsingPublicKeyById(Guid avatarId, string key, string walletAddress, ProviderType providerToImportTo)
        {
            if (!OwnsWalletAvatar(avatarId))
                return RejectWalletAccess<IProviderWallet>("Only the authenticated wallet owner can change wallet settings.");

            return WalletManager.ImportWalletUsingPublicKeyById(avatarId, key, walletAddress, providerToImportTo);
        }

        /// <summary>
        ///     Import a wallet using public key by username.
        /// </summary>
        /// <param name="username">The avatar username.</param>
        /// <param name="key">The public key.</param>
        /// <param name="providerToImportTo">The provider type to import to.</param>
        /// <returns>OASIS result containing the wallet ID or error details.</returns>
        /// <response code="200">Wallet imported successfully</response>
        /// <response code="400">Error importing wallet</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("avatar/username/{username}/import/public-key")]
        [ProducesResponseType(typeof(OASISResult<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public OASISResult<IProviderWallet> ImportWalletUsingPublicKeyByUsername(string username, string key, string walletAddress, ProviderType providerToImportTo)
        {
            if (!OwnsWalletIdentity(username, Avatar?.Username))
                return RejectWalletAccess<IProviderWallet>("Only the authenticated wallet owner can change wallet settings.");

            return WalletManager.ImportWalletUsingPublicKeyById(AvatarId, key, walletAddress, providerToImportTo);
        }

        /// <summary>
        ///     Import a wallet using public key by email.
        /// </summary>
        /// <param name="email">The avatar email.</param>
        /// <param name="key">The public key.</param>
        /// <param name="providerToImportTo">The provider type to import to.</param>
        /// <returns>OASIS result containing the wallet ID or error details.</returns>
        /// <response code="200">Wallet imported successfully</response>
        /// <response code="400">Error importing wallet</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("avatar/email/{email}/import/public-key")]
        [ProducesResponseType(typeof(OASISResult<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public OASISResult<IProviderWallet> ImportWalletUsingPublicKeyByEmail(string email, string key, string walletAddress, ProviderType providerToImportTo)
        {
            if (!OwnsWalletIdentity(email, Avatar?.Email))
                return RejectWalletAccess<IProviderWallet>("Only the authenticated wallet owner can change wallet settings.");

            return WalletManager.ImportWalletUsingPublicKeyById(AvatarId, key, walletAddress, providerToImportTo);
        }

        /// <summary>
        ///     Get the wallet that a public key belongs to.
        /// </summary>
        /// <param name="providerKey">The provider key.</param>
        /// <param name="providerType">The provider type.</param>
        /// <returns>OASIS result containing the wallet or error details.</returns>
        /// <response code="200">Wallet found successfully</response>
        /// <response code="400">Error finding wallet</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpGet("find-wallet")]
        [ProducesResponseType(typeof(OASISResult<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public OASISResult<IProviderWallet> GetWalletThatPublicKeyBelongsTo(string providerKey, ProviderType providerType)
        {
            return WalletManager.GetWalletThatPublicKeyBelongsTo(providerKey, false, false, providerType);
        }

        /// <summary>
        ///     Get total portfolio value across all wallets for an avatar.
        /// </summary>
        /// <param name="avatarId">The avatar ID.</param>
        /// <returns>OASIS result containing the portfolio value or error details.</returns>
        /// <response code="200">Portfolio value retrieved successfully</response>
        /// <response code="400">Error retrieving portfolio value</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpGet("avatar/{avatarId}/portfolio/value")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<object>> GetPortfolioValueAsync(Guid avatarId)
        {
            if (!OwnsWalletAvatar(avatarId))
                return RejectWalletAccess<object>("Only the authenticated wallet owner can view its portfolio.");

            var loaded = await WalletManager.LoadProviderWalletsForAvatarByIdAsync(avatarId, showPrivateKeys: false, showSecretWords: false);
            if (loaded.IsError)
                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Dictionary<ProviderType, List<IProviderWallet>>, object>(loaded);
            if (loaded.Result == null)
                return new OASISResult<object> { IsError = true, Message = "The wallet provider returned no wallet groups." };

            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            try
            {
                var wallets = WalletManager.ResolveWalletsForProvider(loaded.Result, ProviderType.All);
                var addresses = wallets.Where(wallet => wallet.ProviderType == ProviderType.SolanaOASIS)
                    .Select(wallet => wallet.WalletAddress).Distinct(StringComparer.Ordinal).ToArray();
                var unpricedWallets = wallets.Count(wallet => wallet.ProviderType != ProviderType.SolanaOASIS);
                var assets = new List<SolanaWalletAsset>();
                if (addresses.Length > 0)
                {
                    var provider = await GetAndActivateProviderAsync(ProviderType.SolanaOASIS, false).WaitAsync(deadline.Token);
                    if (provider.IsError || provider.Result is not SolanaOASIS solana)
                        return new OASISResult<object> { IsError = true, Message = "The Solana wallet provider is unavailable." };

                    var mainnet = await solana.VerifyWalletMainnetAsync(deadline.Token);
                    if (mainnet.IsError)
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, object>(mainnet);
                    if (mainnet.Result)
                    {
                        foreach (var address in addresses)
                        {
                            var balances = await solana.ReadWalletAssetsAsync(address, deadline.Token);
                            if (balances.IsError)
                                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<List<SolanaWalletAsset>, object>(balances);
                            assets.AddRange(balances.Result);
                        }
                    }
                    else unpricedWallets += addresses.Length;
                }

                var prices = await SolanaWalletValuation.ReadAsync(assets, deadline.Token);
                var complete = unpricedWallets == 0 && prices.Unpriced == 0;
                return new OASISResult<object>(new
                {
                    value = complete ? (decimal?)prices.Value : null,
                    pricedValueUsd = prices.Priced > 0 ? (decimal?)prices.Value : null,
                    currency = "USD",
                    complete,
                    unpricedWallets,
                    unpricedAssets = prices.Unpriced
                }) { IsLoaded = true };
            }
            catch (OperationCanceledException) when (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                return new OASISResult<object> { IsError = true, Message = "Portfolio valuation timed out." };
            }
            catch (Exception error) when (error is HttpRequestException or JsonException or OverflowException)
            {
                return new OASISResult<object> { IsError = true, Message = "Portfolio valuation could not be calculated." };
            }
        }

        /// <summary>
        ///     Get wallets by chain for an avatar.
        /// </summary>
        /// <param name="avatarId">The avatar ID.</param>
        /// <param name="chain">The chain name.</param>
        /// <returns>OASIS result containing the wallets or error details.</returns>
        /// <response code="200">Wallets retrieved successfully</response>
        /// <response code="400">Error retrieving wallets</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpGet("avatar/{avatarId}/wallets/chain/{chain}")]
        [ProducesResponseType(typeof(OASISResult<List<IProviderWallet>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<List<IProviderWallet>>> GetWalletsByChainAsync(Guid avatarId, string chain)
        {
            if (!OwnsWalletAvatar(avatarId))
                return RejectWalletAccess<List<IProviderWallet>>("Only the authenticated wallet owner can read wallets by chain.");

            var providerName = chain?.Trim();
            if (string.Equals(providerName, "bnb", StringComparison.OrdinalIgnoreCase))
                providerName = nameof(ProviderType.BNBChainOASIS);
            if (!string.IsNullOrEmpty(providerName) && !providerName.EndsWith("OASIS", StringComparison.OrdinalIgnoreCase))
                providerName += "OASIS";

            if (!Enum.TryParse(providerName, true, out ProviderType provider) ||
                !string.Equals(provider.ToString(), providerName, StringComparison.OrdinalIgnoreCase))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return new OASISResult<List<IProviderWallet>>
                {
                    IsError = true,
                    Message = "The chain must identify an OASIS wallet provider."
                };
            }

            var loaded = await WalletManager.LoadProviderWalletsForAvatarByIdAsync(avatarId,
                showPrivateKeys: false, showSecretWords: false, providerTypeToShowWalletsFor: provider);
            var result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Dictionary<ProviderType, List<IProviderWallet>>, List<IProviderWallet>>(loaded);
            if (loaded.IsError)
                return result;
            if (loaded.Result == null)
            {
                result.IsError = true;
                result.Message = "The wallet provider returned no wallet groups.";
                return result;
            }

            result.Result = loaded.Result.TryGetValue(provider, out var wallets) ? wallets : new List<IProviderWallet>();
            return result;
        }

        /// <summary>
        ///     Transfer tokens between wallets.
        /// </summary>
        /// <param name="request">The transfer request.</param>
        /// <returns>OASIS result containing the transfer response or error details.</returns>
        /// <response code="200">Transfer initiated successfully</response>
        /// <response code="400">Error initiating transfer</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpPost("transfer")]
        [ProducesResponseType(typeof(OASISResult<ISendWeb4TokenResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<ISendWeb4TokenResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public Task<OASISResult<ISendWeb4TokenResponse>> TransferBetweenWalletsAsync([FromBody] SendWeb4TokenRequest request) =>
            SendTokenAsync(request);

        /// <summary>
        ///     Get wallet analytics for an avatar.
        /// </summary>
        /// <param name="avatarId">The avatar ID.</param>
        /// <param name="walletId">The wallet ID.</param>
        /// <returns>OASIS result containing the analytics or error details.</returns>
        /// <response code="200">Analytics retrieved successfully</response>
        /// <response code="400">Error retrieving analytics</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpGet("avatar/{avatarId}/wallet/{walletId}/analytics")]
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<object>> GetWalletAnalyticsAsync(Guid avatarId, Guid walletId, [FromQuery] string before = null)
        {
            if (!OwnsWalletAvatar(avatarId))
                return RejectWalletAccess<object>("Only the authenticated wallet owner can view its activity.");
            if (before != null && (before.Length < 64 || before.Length > 88 ||
                before.Any(character => !"123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz".Contains(character))))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return new OASISResult<object> { IsError = true, Message = "The wallet activity cursor is invalid." };
            }

            var wallet = await ResolveOwnedSolanaWalletAsync<object>(avatarId, walletId, "Wallet activity");
            if (wallet.Error != null)
                return wallet.Error;

            return await wallet.Provider.ReadWalletActivityPageAsync(wallet.Address, before, HttpContext.RequestAborted);
        }

        /// <summary>
        ///     Get supported chains.
        /// </summary>
        /// <returns>OASIS result containing the supported chains or error details.</returns>
        /// <response code="200">Supported chains retrieved successfully</response>
        /// <response code="400">Error retrieving supported chains</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [HttpGet("supported-chains")]
        [ProducesResponseType(typeof(OASISResult<List<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public OASISResult<List<object>> GetSupportedChains()
        {
            var chains = ProviderManager.Instance.GetAllRegisteredProviders()
                .OfType<IOASISBlockchainStorageProvider>()
                .OrderBy(provider => provider.ProviderType.Value.ToString(), StringComparer.Ordinal)
                .Select(provider =>
                {
                    var type = provider.ProviderType.Value;
                    var providerName = type.ToString();
                    const string suffix = "OASIS";
                    var name = providerName.EndsWith(suffix, StringComparison.Ordinal)
                        ? providerName[..^suffix.Length]
                        : providerName;
                    var id = type == ProviderType.BNBChainOASIS ? "bnb" : name.ToLowerInvariant();
                    return (object)new
                    {
                        id,
                        name,
                        symbol = TokenHelper.GetTokenForProvider(type),
                        providerType = providerName,
                        isActive = provider.IsProviderActivated
                    };
                }).ToList();

            return new OASISResult<List<object>>
            {
                Result = chains,
                IsLoaded = true,
                LoadedCount = chains.Count
            };
        }

        /// <summary>
        ///     Get wallet tokens for an avatar.
        /// </summary>
        /// <param name="avatarId">The avatar ID.</param>
        /// <param name="walletId">The wallet ID.</param>
        /// <returns>OASIS result containing the tokens or error details.</returns>
        /// <response code="200">Tokens retrieved successfully</response>
        /// <response code="400">Error retrieving tokens</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpGet("avatar/{avatarId}/wallet/{walletId}/tokens")]
        [ProducesResponseType(typeof(OASISResult<List<object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<List<object>>> GetWalletTokensAsync(Guid avatarId, Guid walletId)
        {
            var wallet = await ResolveOwnedSolanaWalletAsync<List<object>>(avatarId, walletId, "Token balance enumeration");
            if (wallet.Error != null)
                return wallet.Error;

            var balances = await wallet.Provider.ReadWalletAssetsAsync(wallet.Address, HttpContext.RequestAborted);
            var result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<List<SolanaWalletAsset>, List<object>>(balances);
            result.Result = balances.Result?.Cast<object>().ToList();
            return result;
        }

        private async Task<(SolanaOASIS Provider, string Address, OASISResult<T> Error)> ResolveOwnedSolanaWalletAsync<T>(
            Guid avatarId, Guid walletId, string operation)
        {
            if (!OwnsWalletAvatar(avatarId))
                return (null, null, RejectWalletAccess<T>("Only the authenticated wallet owner can view these wallet details."));

            var loaded = await WalletManager.LoadProviderWalletsForAvatarByIdAsync(avatarId, showPrivateKeys: false, showSecretWords: false);
            if (loaded.IsError)
                return (null, null, OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<Dictionary<ProviderType, List<IProviderWallet>>, T>(loaded));

            var matching = WalletManager.ResolveWalletsForProvider(loaded.Result, ProviderType.All)
                .Where(wallet => walletId != Guid.Empty && (wallet.WalletId == walletId || wallet.Id == walletId)).Take(2).ToArray();
            if (matching.Length != 1)
            {
                Response.StatusCode = matching.Length == 0 ? StatusCodes.Status404NotFound : StatusCodes.Status409Conflict;
                return (null, null, new OASISResult<T>
                {
                    IsError = true,
                    Message = matching.Length == 0 ? "Wallet not found." : "The wallet identity is ambiguous."
                });
            }

            var wallet = matching[0];
            if (wallet.ProviderType != ProviderType.SolanaOASIS)
                return (null, null, NotImplementedResult<T>(operation + " for this wallet network"));

            var provider = await GetAndActivateProviderAsync(wallet.ProviderType, false);
            if (provider.IsError || provider.Result is not SolanaOASIS solana)
            {
                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return (null, null, new OASISResult<T> { IsError = true, Message = "The Solana wallet provider is unavailable." });
            }

            return (solana, wallet.WalletAddress, null);
        }

        /// <summary>
        ///     Create a new wallet for an avatar by ID.
        /// </summary>
        /// <param name="avatarId">The avatar ID.</param>
        /// <param name="name">The wallet name.</param>
        /// <param name="description">The wallet description.</param>
        /// <param name="walletProviderType">The wallet provider type.</param>
        /// <param name="generateKeyPair">Whether to generate a key pair.</param>
        /// <param name="isDefaultWallet">Whether this should be the default wallet.</param>
        /// <param name="providerTypeToLoadSave">The provider type to load/save from.</param>
        /// <returns>OASIS result containing the created wallet or error details.</returns>
        [Authorize]
        [HttpPost("avatar/{avatarId}/create-wallet")]
        [ProducesResponseType(typeof(OASISResult<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IProviderWallet>> CreateWalletForAvatarByIdAsync(Guid avatarId, [FromBody] CreateWalletRequest request, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            if (avatarId != AvatarId && Avatar?.AvatarType?.Value != AvatarType.Wizard)
                return new OASISResult<IProviderWallet> { IsError = true, Message = "Unauthorized. You can only create wallets for your own avatar." };
            if (request == null)
                return new OASISResult<IProviderWallet> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name, Description, and optional WalletProviderType, GenerateKeyPair, IsDefaultWallet." };
            if ((request.ShowPrivateKey || request.ShowSecretRecoveryPhase) && !OwnsWalletAvatar(avatarId))
                return RejectWalletKeyRead<IProviderWallet>();
            return await WalletManager.CreateWalletForAvatarByIdAsync(avatarId, request.Name, request.Description, request.WalletProviderType, request.GenerateKeyPair, request.IsDefaultWallet, request.ShowSecretRecoveryPhase, request.ShowPrivateKey, providerTypeToLoadSave);
        }

        /// <summary>
        ///     Create a new wallet for an avatar by username.
        /// </summary>
        /// <param name="username">The avatar username.</param>
        /// <param name="request">The wallet creation request.</param>
        /// <param name="providerTypeToLoadSave">The provider type to load/save from.</param>
        /// <returns>OASIS result containing the created wallet or error details.</returns>
        [Authorize]
        [HttpPost("avatar/username/{username}/create-wallet")]
        [ProducesResponseType(typeof(OASISResult<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IProviderWallet>> CreateWalletForAvatarByUsernameAsync(string username, [FromBody] CreateWalletRequest request, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            if (username != Avatar?.Username && Avatar?.AvatarType?.Value != AvatarType.Wizard)
                return new OASISResult<IProviderWallet> { IsError = true, Message = "Unauthorized. You can only create wallets for your own avatar." };
            if (request == null)
                return new OASISResult<IProviderWallet> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name, Description, and optional WalletProviderType, GenerateKeyPair, IsDefaultWallet." };
            return await WalletManager.CreateWalletForAvatarByUsernameAsync(username, request.Name, request.Description, request.WalletProviderType, request.GenerateKeyPair, request.IsDefaultWallet, false, false, providerTypeToLoadSave);
        }

        /// <summary>
        ///     Create a new wallet for an avatar by email.
        /// </summary>
        /// <param name="email">The avatar email.</param>
        /// <param name="request">The wallet creation request.</param>
        /// <param name="providerTypeToLoadSave">The provider type to load/save from.</param>
        /// <returns>OASIS result containing the created wallet or error details.</returns>
        [Authorize]
        [HttpPost("avatar/email/{email}/create-wallet")]
        [ProducesResponseType(typeof(OASISResult<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IProviderWallet>> CreateWalletForAvatarByEmailAsync(string email, [FromBody] CreateWalletRequest request, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            if (email != Avatar?.Email && Avatar?.AvatarType?.Value != AvatarType.Wizard)
                return new OASISResult<IProviderWallet> { IsError = true, Message = "Unauthorized. You can only create wallets for your own avatar." };
            if (request == null)
                return new OASISResult<IProviderWallet> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name, Description, and optional WalletProviderType, GenerateKeyPair, IsDefaultWallet." };
            return await WalletManager.CreateWalletForAvatarByEmailAsync(email, request.Name, request.Description, request.WalletProviderType, request.GenerateKeyPair, request.IsDefaultWallet, false, false, providerTypeToLoadSave);
        }

        /// <summary>
        ///     Update a wallet for an avatar by ID.
        /// </summary>
        /// <param name="avatarId">The avatar ID.</param>
        /// <param name="walletId">The wallet ID to update.</param>
        /// <param name="request">The wallet update request.</param>
        /// <param name="providerTypeToLoadSave">The provider type to load/save from.</param>
        /// <returns>OASIS result containing the updated wallet or error details.</returns>
        [Authorize]
        [HttpPut("avatar/{avatarId}/wallet/{walletId}")]
        [ProducesResponseType(typeof(OASISResult<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IProviderWallet>> UpdateWalletForAvatarByIdAsync(Guid avatarId, Guid walletId, [FromBody] UpdateWalletRequest request, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            if (avatarId != AvatarId && Avatar?.AvatarType?.Value != AvatarType.Wizard)
                return new OASISResult<IProviderWallet> { IsError = true, Message = "Unauthorized. You can only update your own wallet." };
            if (request == null)
                return new OASISResult<IProviderWallet> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name, Description, and optional WalletProviderType." };
            return await WalletManager.UpdateWalletForAvatarByIdAsync(avatarId, walletId, request.Name, request.Description, request.WalletProviderType, providerTypeToLoadSave);
        }

        /// <summary>
        ///     Update a wallet for an avatar by username.
        /// </summary>
        /// <param name="username">The avatar username.</param>
        /// <param name="walletId">The wallet ID to update.</param>
        /// <param name="request">The wallet update request.</param>
        /// <param name="providerTypeToLoadSave">The provider type to load/save from.</param>
        /// <returns>OASIS result containing the updated wallet or error details.</returns>
        [Authorize]
        [HttpPut("avatar/username/{username}/wallet/{walletId}")]
        [ProducesResponseType(typeof(OASISResult<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IProviderWallet>> UpdateWalletForAvatarByUsernameAsync(string username, Guid walletId, [FromBody] UpdateWalletRequest request, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            if (username != Avatar?.Username && Avatar?.AvatarType?.Value != AvatarType.Wizard)
                return new OASISResult<IProviderWallet> { IsError = true, Message = "Unauthorized. You can only update your own wallet." };
            if (request == null)
                return new OASISResult<IProviderWallet> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name, Description, and optional WalletProviderType." };
            return await WalletManager.UpdateWalletForAvatarByUsernameAsync(username, walletId, request.Name, request.Description, request.WalletProviderType, providerTypeToLoadSave);
        }

        /// <summary>
        ///     Update a wallet for an avatar by email.
        /// </summary>
        /// <param name="email">The avatar email.</param>
        /// <param name="walletId">The wallet ID to update.</param>
        /// <param name="request">The wallet update request.</param>
        /// <param name="providerTypeToLoadSave">The provider type to load/save from.</param>
        /// <returns>OASIS result containing the updated wallet or error details.</returns>
        [Authorize]
        [HttpPut("avatar/email/{email}/wallet/{walletId}")]
        [ProducesResponseType(typeof(OASISResult<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<IProviderWallet>> UpdateWalletForAvatarByEmailAsync(string email, Guid walletId, [FromBody] UpdateWalletRequest request, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            if (email != Avatar?.Email && Avatar?.AvatarType?.Value != AvatarType.Wizard)
                return new OASISResult<IProviderWallet> { IsError = true, Message = "Unauthorized. You can only update your own wallet." };
            if (request == null)
                return new OASISResult<IProviderWallet> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name, Description, and optional WalletProviderType." };
            return await WalletManager.UpdateWalletForAvatarByEmailAsync(email, walletId, request.Name, request.Description, request.WalletProviderType, providerTypeToLoadSave);
        }
    }

    /// <summary>
    /// Create wallet request model
    /// </summary>
    public class CreateWalletRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ProviderType WalletProviderType { get; set; }
        public bool GenerateKeyPair { get; set; } = true;
        public bool IsDefaultWallet { get; set; } = false;
        public bool ShowSecretRecoveryPhase { get; set; }
        public bool ShowPrivateKey { get; set; }
    }

    /// <summary>
    /// Update wallet request model
    /// </summary>
    public class UpdateWalletRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ProviderType WalletProviderType { get; set; }
    }
}
