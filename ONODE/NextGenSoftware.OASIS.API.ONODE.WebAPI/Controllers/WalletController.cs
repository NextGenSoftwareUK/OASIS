using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

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
        [HttpPost("send_token")]
        [ProducesResponseType(typeof(OASISResult<ITransactionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<ISendWeb4TokenResponse>> SendTokenAsync(ISendWeb4TokenRequest request)
        {
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
            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = null;
                try
                {
                    result = await WalletManager.LoadProviderWalletsForAvatarByIdAsync(id, showOnlyDefault, decryptPrivateKeys, false, ProviderType.All, providerType);
                }
                catch
                {
                    // If real data unavailable, use test data
                }

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
            return await WalletManager.LoadProviderWalletsForAvatarByUsernameAsync(username, showOnlyDefault, decryptPrivateKeys, false, ProviderType.All, providerType);
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
            return await WalletManager.LoadProviderWalletsForAvatarByEmailAsync(email, showOnlyDefault, decryptPrivateKeys, false, ProviderType.All, providerType);
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
            return await WalletManager.SaveProviderWalletsForAvatarByUsernameAsync(username, wallets, providerType);
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
            return await WalletManager.SaveProviderWalletsForAvatarByEmailAsync(email, wallets, providerType);
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
            return await WalletManager.GetAvatarDefaultWalletByUsernameAsync(username, showOnlyDefault, decryptPrivateKeys, false, providerType);
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
            return await WalletManager.SetAvatarDefaultWalletByUsernameAsync(username, walletId, providerType);
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
            return await WalletManager.SetAvatarDefaultWalletByEmailAsync(email, walletId, providerType);
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
            return WalletManager.ImportWalletUsingPrivateKeyByUsername(username, key, providerToImportTo);
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
            return WalletManager.ImportWalletUsingPrivateKeyByEmail(email, key, providerToImportTo);
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
            return WalletManager.ImportWalletUsingPublicKeyByUsername(username, key, walletAddress, providerToImportTo);
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
            return WalletManager.ImportWalletUsingPublicKeyByEmail(email, key, walletAddress, providerToImportTo);
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
            var totalResult = await WalletManager.GetTotalBalanceForAllProviderWalletsForAvatarByIdAsync(avatarId);
            if (totalResult.IsError)
                return new OASISResult<object> { IsError = true, Message = totalResult.Message };

            var walletsResult = await WalletManager.LoadProviderWalletsForAvatarByIdAsync(avatarId, false, false, false, ProviderType.All);

            var breakdown = new Dictionary<string, object>();
            if (!walletsResult.IsError && walletsResult.Result != null)
            {
                foreach (var kvp in walletsResult.Result)
                {
                    double chainBalance = kvp.Value?.Sum(w => w.Balance) ?? 0;
                    int count = kvp.Value?.Count ?? 0;
                    if (count > 0)
                        breakdown[kvp.Key.ToString()] = new { value = chainBalance, count };
                }
            }

            return new OASISResult<object>
            {
                Result = new
                {
                    totalValue = totalResult.Result,
                    currency = "USD",
                    lastUpdated = DateTime.UtcNow.ToString("O"),
                    walletCount = walletsResult.Result?.Values.Sum(v => v?.Count ?? 0) ?? 0,
                    breakdown
                },
                IsLoaded = true,
                Message = "Portfolio value retrieved successfully"
            };
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
            if (!Enum.TryParse<ProviderType>(chain, true, out var providerType) || providerType == ProviderType.None || providerType == ProviderType.Default)
                return new OASISResult<List<IProviderWallet>>
                {
                    IsError = true,
                    Message = $"Unknown chain '{chain}'. Use a valid OASIS ProviderType name (e.g. EthereumOASIS, SolanaOASIS, ArbitrumOASIS)."
                };

            return await WalletManager.LoadProviderWalletsForProviderByAvatarIdAsync(avatarId, providerType);
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
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<ISendWeb4TokenResponse>> TransferBetweenWalletsAsync([FromBody] SendWeb4TokenRequest request)
        {
            if (request == null)
                return new OASISResult<ISendWeb4TokenResponse>
                {
                    IsError = true,
                    Message = "The request body is required. Provide: Web3TokenId (guid), Amount (decimal), ToWalletAddress or (ToAvatarId / ToAvatarUsername / ToAvatarEmail), and optionally FromProvider / ToProvider (ProviderType name)."
                };

            return await WalletManager.SendTokenAsync(AvatarId, request);
        }

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
        public async Task<OASISResult<object>> GetWalletAnalyticsAsync(Guid avatarId, Guid walletId)
        {
            var walletResult = await WalletManager.LoadProviderWalletForAvatarByIdAsync(avatarId, walletId);
            if (walletResult.IsError)
                return new OASISResult<object> { IsError = true, Message = walletResult.Message };

            var wallet = walletResult.Result;
            if (wallet == null)
                return new OASISResult<object> { IsError = true, Message = "Wallet not found." };

            var txns = wallet.Transactions ?? new List<IWalletTransaction>();
            int totalTransactions = txns.Count;
            double totalVolume = txns.Sum(t => t.Amount);
            double avgTransaction = totalTransactions > 0 ? totalVolume / totalTransactions : 0;
            DateTime? lastActivity = txns.Count > 0 ? txns.Max(t => t.CreatedDate) : (DateTime?)null;

            var monthlyActivity = txns
                .GroupBy(t => new { t.CreatedDate.Year, t.CreatedDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => (object)new
                {
                    month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    transactions = g.Count(),
                    volume = g.Sum(t => t.Amount)
                })
                .ToList();

            return new OASISResult<object>
            {
                Result = new
                {
                    walletId,
                    walletAddress = wallet.WalletAddress,
                    providerType = wallet.ProviderType.ToString(),
                    balance = wallet.Balance,
                    totalTransactions,
                    totalVolume,
                    averageTransaction = Math.Round(avgTransaction, 4),
                    lastActivity = lastActivity?.ToString("O"),
                    monthlyActivity
                },
                IsLoaded = true,
                Message = "Wallet analytics retrieved successfully"
            };
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
            // Derive the list from the real ProviderType enum, filtering to blockchain providers only.
            // Non-blockchain entries (storage, cloud, IPFS, maps, etc.) are excluded.
            var nonBlockchain = new HashSet<ProviderType>
            {
                ProviderType.None, ProviderType.All, ProviderType.Default,
                ProviderType.MoralisOASIS,
                ProviderType.IPFSOASIS, ProviderType.PinataOASIS,
                ProviderType.HoloOASIS,
                ProviderType.MongoDBOASIS, ProviderType.Neo4jOASIS,
                ProviderType.SQLLiteDBOASIS, ProviderType.SQLServerDBOASIS, ProviderType.OracleDBOASIS,
                ProviderType.GoogleCloudOASIS, ProviderType.AzureStorageOASIS,
                ProviderType.AzureCosmosDBOASIS, ProviderType.AWSOASIS,
                ProviderType.UrbitOASIS, ProviderType.ThreeFoldOASIS, ProviderType.PLANOASIS,
                ProviderType.HoloWebOASIS, ProviderType.SOLIDOASIS,
                ProviderType.ActivityPubOASIS, ProviderType.ScuttlebuttOASIS,
                ProviderType.LocalFileOASIS, ProviderType.GitHubOASIS
            };

            var chains = Enum.GetValues<ProviderType>()
                .Where(p => !nonBlockchain.Contains(p))
                .Select(p => (object)new
                {
                    id = p.ToString(),
                    name = p.ToString().Replace("OASIS", "").Replace("BlockChain", ""),
                    providerType = p.ToString(),
                    isActive = true
                })
                .ToList();

            return new OASISResult<List<object>>
            {
                Result = chains,
                IsLoaded = true,
                Message = $"{chains.Count} blockchain providers supported by OASIS"
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
            var walletResult = await WalletManager.LoadProviderWalletForAvatarByIdAsync(avatarId, walletId);
            if (walletResult.IsError)
                return new OASISResult<List<object>> { IsError = true, Message = walletResult.Message };

            var wallet = walletResult.Result;
            if (wallet == null)
                return new OASISResult<List<object>> { IsError = true, Message = "Wallet not found." };

            // Native chain token derived from the provider type and wallet balance.
            // Individual ERC-20 / SPL balances require a block-explorer integration (future work).
            var nativeTicker = wallet.ProviderType.ToString().Replace("OASIS", "").Replace("BlockChain", "").ToUpper();
            var tokens = new List<object>
            {
                new
                {
                    symbol = nativeTicker,
                    name = nativeTicker + " (native)",
                    amount = wallet.Balance.ToString("F6"),
                    balance = wallet.Balance,
                    chain = wallet.ProviderType.ToString(),
                    walletAddress = wallet.WalletAddress,
                    isNative = true
                }
            };

            return new OASISResult<List<object>>
            {
                Result = tokens,
                IsLoaded = true,
                Message = "Wallet tokens retrieved successfully"
            };
        }

        /// <summary>
        ///     Load all provider wallets for an avatar by ID, filtered to a specific provider (alias path).
        ///     Alias: GET /api/wallet/get-wallets-for-avatar/{avatarId}/{providerType}
        ///     Added to fix 404 reported by Pangea integration testing.
        ///     The primary working path is /api/wallet/avatar/{id}/wallets/false/false.
        /// </summary>
        /// <param name="avatarId">The avatar ID.</param>
        /// <param name="providerType">The provider type name (e.g. SolanaOASIS).</param>
        /// <returns>OASIS result containing the provider wallets or error details.</returns>
        [Authorize]
        [HttpGet("get-wallets-for-avatar/{avatarId}/{providerType}")]
        [ProducesResponseType(typeof(OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> GetWalletsForAvatarAsync(Guid avatarId, string providerType)
        {
            ProviderType parsedProvider = ProviderType.Default;
            if (!string.IsNullOrWhiteSpace(providerType))
                Enum.TryParse(providerType, out parsedProvider);

            return await WalletManager.LoadProviderWalletsForAvatarByIdAsync(avatarId, false, false, false, ProviderType.All, parsedProvider);
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
            if (request == null)
                return new OASISResult<IProviderWallet> { IsError = true, Message = "The request body is required. Please provide a valid JSON body with Name, Description, and optional WalletProviderType, GenerateKeyPair, IsDefaultWallet." };
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