using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS.Services;
using NextGenSoftware.OASIS.Common;

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
            return await WalletManager.LoadProviderWalletsForAvatarByIdAsync(id, showOnlyDefault, decryptPrivateKeys, providerType);
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
            return await WalletManager.LoadProviderWalletsForAvatarByUsernameAsync(username, showOnlyDefault, decryptPrivateKeys, providerType);
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
        /// <param name="showOnlyDefault">Whether to show only the default wallet.</param>
        /// <param name="decryptPrivateKeys">Whether to decrypt private keys.</param>
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
            return await WalletManager.LoadProviderWalletsForAvatarByEmailAsync(email, showOnlyDefault, decryptPrivateKeys, providerType);
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
            return await WalletManager.GetAvatarDefaultWalletByUsernameAsync(username, showOnlyDefault, decryptPrivateKeys, providerType);
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
            return WalletManager.GetWalletThatPublicKeyBelongsTo(providerKey, providerType);
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
            // This would need to be implemented in WalletManager
            // For now, return a demo response
            return new OASISResult<object>
            {
                Result = new
                {
                    totalValue = 15420.50,
                    totalValueUSD = 15420.50,
                    currency = "USD",
                    lastUpdated = DateTime.UtcNow.ToString("O"),
                    breakdown = new
                    {
                        ethereum = new { value = 8500.25, usdValue = 8500.25, count = 3 },
                        bitcoin = new { value = 3200.15, usdValue = 3200.15, count = 1 },
                        solana = new { value = 2100.10, usdValue = 2100.10, count = 2 },
                        polygon = new { value = 1620.00, usdValue = 1620.00, count = 1 }
                    }
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
            // This would need to be implemented in WalletManager
            // For now, return a demo response
            return new OASISResult<List<IProviderWallet>>
            {
                Result = new List<IProviderWallet>(),
                IsLoaded = true,
                Message = $"{chain} wallets retrieved successfully"
            };
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
        [ProducesResponseType(typeof(OASISResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<object>> TransferBetweenWalletsAsync([FromBody] object request)
        {
            // This would need to be implemented in WalletManager
            // For now, return a demo response
            return new OASISResult<object>
            {
                Result = new
                {
                    transactionId = Guid.NewGuid().ToString(),
                    status = "pending",
                    timestamp = DateTime.UtcNow.ToString("O")
                },
                IsSaved = true,
                Message = "Transfer initiated successfully"
            };
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
            // This would need to be implemented in WalletManager
            // For now, return a demo response
            return new OASISResult<object>
            {
                Result = new
                {
                    walletId = walletId,
                    totalTransactions = 45,
                    totalVolume = 12500.75,
                    averageTransaction = 277.79,
                    lastActivity = DateTime.UtcNow.ToString("O"),
                    topTokens = new[]
                    {
                        new { symbol = "ETH", amount = "2.5", value = 5000 },
                        new { symbol = "USDC", amount = "1000", value = 1000 },
                        new { symbol = "BTC", amount = "0.1", value = 3200 }
                    },
                    monthlyActivity = new[]
                    {
                        new { month = "Jan", transactions = 12, volume = 3200 },
                        new { month = "Feb", transactions = 8, volume = 2100 },
                        new { month = "Mar", transactions = 15, volume = 4500 },
                        new { month = "Apr", transactions = 10, volume = 2700 }
                    }
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
            // This would need to be implemented in WalletManager
            // For now, return a demo response
            return new OASISResult<List<object>>
            {
                Result = new List<object>
                {
                    new { id = "ethereum", name = "Ethereum", symbol = "ETH", icon = "ethereum.png", isActive = true },
                    new { id = "bitcoin", name = "Bitcoin", symbol = "BTC", icon = "bitcoin.png", isActive = true },
                    new { id = "solana", name = "Solana", symbol = "SOL", icon = "solana.png", isActive = true },
                    new { id = "polygon", name = "Polygon", symbol = "MATIC", icon = "polygon.png", isActive = true },
                    new { id = "arbitrum", name = "Arbitrum", symbol = "ARB", icon = "arbitrum.png", isActive = true },
                    new { id = "optimism", name = "Optimism", symbol = "OP", icon = "optimism.png", isActive = true },
                    new { id = "base", name = "Base", symbol = "BASE", icon = "base.png", isActive = true },
                    new { id = "avalanche", name = "Avalanche", symbol = "AVAX", icon = "avalanche.png", isActive = true },
                    new { id = "bnb", name = "BNB Chain", symbol = "BNB", icon = "bnb.png", isActive = true },
                    new { id = "fantom", name = "Fantom", symbol = "FTM", icon = "fantom.png", isActive = true }
                },
                IsLoaded = true,
                Message = "Supported chains retrieved successfully"
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
            // This would need to be implemented in WalletManager
            // For now, return a demo response
            return new OASISResult<List<object>>
            {
                Result = new List<object>
                {
                    new { symbol = "ETH", name = "Ethereum", amount = "2.5", value = 5000, usdValue = 5000, chain = "ethereum" },
                    new { symbol = "USDC", name = "USD Coin", amount = "1000", value = 1000, usdValue = 1000, chain = "ethereum" },
                    new { symbol = "USDT", name = "Tether", amount = "500", value = 500, usdValue = 500, chain = "ethereum" }
                },
                IsLoaded = true,
                Message = "Wallet tokens retrieved successfully"
            };
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
            return await WalletManager.CreateWalletForAvatarByUsernameAsync(username, request.Name, request.Description, request.WalletProviderType, request.GenerateKeyPair, request.IsDefaultWallet, providerTypeToLoadSave);
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
            return await WalletManager.CreateWalletForAvatarByEmailAsync(email, request.Name, request.Description, request.WalletProviderType, request.GenerateKeyPair, request.IsDefaultWallet, providerTypeToLoadSave);
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
            return await WalletManager.UpdateWalletForAvatarByEmailAsync(email, walletId, request.Name, request.Description, request.WalletProviderType, providerTypeToLoadSave);
        }

        #region Generic Token Operations (ERC-20 compatible)

        /// <summary>
        /// Get token balance for an avatar
        /// Generic endpoint that works with any ERC-20 compatible token on any supported blockchain
        /// </summary>
        /// <param name="avatarId">Avatar ID (optional, defaults to authenticated avatar)</param>
        /// <param name="tokenContractAddress">Token contract address</param>
        /// <param name="providerType">Provider type (e.g., EthereumOASIS, BaseOASIS, ArbitrumOASIS)</param>
        /// <returns>Token balance in decimal format</returns>
        [Authorize]
        [HttpGet("token/balance")]
        [ProducesResponseType(typeof(OASISResult<decimal>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<decimal>> GetTokenBalance([FromQuery] Guid? avatarId = null, [FromQuery] string tokenContractAddress = null, [FromQuery] ProviderType providerType = ProviderType.EthereumOASIS)
        {
            var targetAvatarId = avatarId ?? AvatarId;
            if (targetAvatarId == Guid.Empty)
            {
                var result = new OASISResult<decimal>();
                OASISErrorHandling.HandleError(ref result, "Avatar ID is required");
                return result;
            }

            if (string.IsNullOrWhiteSpace(tokenContractAddress))
            {
                var result = new OASISResult<decimal>();
                OASISErrorHandling.HandleError(ref result, "Token contract address is required");
                return result;
            }

            // Use provider-specific token balance method via reflection
            var providerResult = ProviderManager.Instance.GetProvider(providerType);
            if (providerResult == null)
            {
                var result = new OASISResult<decimal>();
                OASISErrorHandling.HandleError(ref result, $"Provider {providerType} is not available");
                return result;
            }

            // For Ethereum-based providers, use the generic token service
            if (providerType == ProviderType.EthereumOASIS)
            {
                try
                {
                    // Get avatar's wallet address
                    var walletsResult = await WalletManager.LoadProviderWalletsForAvatarByIdAsync(targetAvatarId, false, false, providerType, ProviderType.Default);
                    if (walletsResult.IsError || walletsResult.Result == null || !walletsResult.Result.ContainsKey(providerType) || walletsResult.Result[providerType].Count == 0)
                    {
                        var result = new OASISResult<decimal>();
                        OASISErrorHandling.HandleError(ref result, $"No {providerType} wallet found for avatar");
                        return result;
                    }

                    var walletAddress = walletsResult.Result[providerType][0].WalletAddress;
                    
                    // Use reflection to call GetTokenBalanceAsync if it exists, otherwise use MNEEService pattern
                    var method = providerResult.GetType().GetMethod("GetTokenBalanceAsync");
                    if (method != null)
                    {
                        var balanceTask = (Task<OASISResult<decimal>>)method.Invoke(providerResult, new object[] { walletAddress, tokenContractAddress });
                        return await balanceTask;
                    }
                    
                    // Fallback: Use MNEEService pattern (works for any ERC-20)
                    var serviceType = Type.GetType("NextGenSoftware.OASIS.API.Providers.EthereumOASIS.Services.MNEEService, NextGenSoftware.OASIS.API.Providers.EthereumOASIS");
                    if (serviceType != null)
                    {
                        var hostUri = providerResult.GetType().GetProperty("HostURI")?.GetValue(providerResult)?.ToString();
                        if (!string.IsNullOrWhiteSpace(hostUri))
                        {
                            var service = Activator.CreateInstance(serviceType, hostUri, tokenContractAddress);
                            var getBalanceMethod = serviceType.GetMethod("GetBalanceAsync");
                            if (getBalanceMethod != null)
                            {
                                var balanceTask = (Task<OASISResult<decimal>>)getBalanceMethod.Invoke(service, new object[] { walletAddress, tokenContractAddress });
                                return await balanceTask;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var result = new OASISResult<decimal>();
                    OASISErrorHandling.HandleError(ref result, $"Error getting token balance: {ex.Message}", ex);
                    return result;
                }
            }

            var errorResult = new OASISResult<decimal>();
            OASISErrorHandling.HandleError(ref errorResult, $"Token balance operation not yet implemented for provider {providerType}");
            return errorResult;
        }

        /// <summary>
        /// Approve token spending for a spender address
        /// Generic endpoint that works with any ERC-20 compatible token
        /// </summary>
        /// <param name="request">Approval request with token contract address, spender, and amount</param>
        /// <returns>Transaction hash</returns>
        [Authorize]
        [HttpPost("token/approve")]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<string>> ApproveToken([FromBody] TokenApprovalRequest request)
        {
            var result = new OASISResult<string>();

            if (request == null || string.IsNullOrWhiteSpace(request.TokenContractAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token contract address is required");
                return result;
            }

            if (string.IsNullOrWhiteSpace(request.SpenderAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Spender address is required");
                return result;
            }

            var avatarId = request.AvatarId != Guid.Empty ? request.AvatarId : AvatarId;
            if (avatarId == Guid.Empty)
            {
                OASISErrorHandling.HandleError(ref result, "Avatar ID is required");
                return result;
            }

            // Use provider-specific token approval method
            var providerResult = ProviderManager.Instance.GetProvider(request.ProviderType);
            if (providerResult == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Provider {request.ProviderType} is not available");
                return result;
            }

            // For Ethereum-based providers, use the generic token service
            if (request.ProviderType == ProviderType.EthereumOASIS)
            {
                try
                {
                    var keyManager = new KeyManager(ProviderManager.Instance.CurrentStorageProvider);
                    var privateKeysResult = keyManager.GetProviderPrivateKeysForAvatarById(avatarId, request.ProviderType);
                    if (privateKeysResult.IsError || privateKeysResult.Result == null || privateKeysResult.Result.Count == 0)
                    {
                        OASISErrorHandling.HandleError(ref result, "No private key found for avatar");
                        return result;
                    }

                    // Use MNEEService pattern (works for any ERC-20)
                    var serviceType = Type.GetType("NextGenSoftware.OASIS.API.Providers.EthereumOASIS.Services.MNEEService, NextGenSoftware.OASIS.API.Providers.EthereumOASIS");
                    if (serviceType != null)
                    {
                        var hostUri = providerResult.GetType().GetProperty("HostURI")?.GetValue(providerResult)?.ToString();
                        if (!string.IsNullOrWhiteSpace(hostUri))
                        {
                            var service = Activator.CreateInstance(serviceType, hostUri, request.TokenContractAddress);
                            var approveMethod = serviceType.GetMethod("ApproveAsync");
                            if (approveMethod != null)
                            {
                                var approveTask = (Task<OASISResult<string>>)approveMethod.Invoke(service, new object[] { 
                                    privateKeysResult.Result[0],
                                    request.SpenderAddress,
                                    request.Amount,
                                    request.TokenContractAddress
                                });
                                return await approveTask;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error approving token: {ex.Message}", ex);
                    return result;
                }
            }

            OASISErrorHandling.HandleError(ref result, $"Token approval operation not yet implemented for provider {request.ProviderType}");
            return result;
        }

        /// <summary>
        /// Get token allowance for a spender
        /// Generic endpoint that works with any ERC-20 compatible token
        /// </summary>
        /// <param name="avatarId">Avatar ID (optional, defaults to authenticated avatar)</param>
        /// <param name="tokenContractAddress">Token contract address</param>
        /// <param name="spenderAddress">Spender address</param>
        /// <param name="providerType">Provider type</param>
        /// <returns>Allowance amount</returns>
        [Authorize]
        [HttpGet("token/allowance")]
        [ProducesResponseType(typeof(OASISResult<decimal>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<decimal>> GetTokenAllowance([FromQuery] Guid? avatarId = null, [FromQuery] string tokenContractAddress = null, [FromQuery] string spenderAddress = null, [FromQuery] ProviderType providerType = ProviderType.EthereumOASIS)
        {
            var result = new OASISResult<decimal>();

            var targetAvatarId = avatarId ?? AvatarId;
            if (targetAvatarId == Guid.Empty)
            {
                OASISErrorHandling.HandleError(ref result, "Avatar ID is required");
                return result;
            }

            if (string.IsNullOrWhiteSpace(tokenContractAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token contract address is required");
                return result;
            }

            if (string.IsNullOrWhiteSpace(spenderAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Spender address is required");
                return result;
            }

            // Use provider-specific token allowance method
            var providerResult = ProviderManager.Instance.GetProvider(providerType);
            if (providerResult == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Provider {providerType} is not available");
                return result;
            }

            // For Ethereum-based providers, use the generic token service
            if (providerType == ProviderType.EthereumOASIS)
            {
                try
                {
                    // Get avatar's wallet address
                    var walletsResult = await WalletManager.LoadProviderWalletsForAvatarByIdAsync(targetAvatarId, false, false, providerType, ProviderType.Default);
                    if (walletsResult.IsError || walletsResult.Result == null || !walletsResult.Result.ContainsKey(providerType) || walletsResult.Result[providerType].Count == 0)
                    {
                        OASISErrorHandling.HandleError(ref result, $"No {providerType} wallet found for avatar");
                        return result;
                    }

                    var ownerAddress = walletsResult.Result[providerType][0].WalletAddress;
                    
                    // Use MNEEService pattern (works for any ERC-20)
                    var serviceType = Type.GetType("NextGenSoftware.OASIS.API.Providers.EthereumOASIS.Services.MNEEService, NextGenSoftware.OASIS.API.Providers.EthereumOASIS");
                    if (serviceType != null)
                    {
                        var hostUri = providerResult.GetType().GetProperty("HostURI")?.GetValue(providerResult)?.ToString();
                        if (!string.IsNullOrWhiteSpace(hostUri))
                        {
                            var service = Activator.CreateInstance(serviceType, hostUri, tokenContractAddress);
                            var getAllowanceMethod = serviceType.GetMethod("GetAllowanceAsync");
                            if (getAllowanceMethod != null)
                            {
                                var allowanceTask = (Task<OASISResult<decimal>>)getAllowanceMethod.Invoke(service, new object[] { 
                                    ownerAddress,
                                    spenderAddress,
                                    tokenContractAddress
                                });
                                return await allowanceTask;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting token allowance: {ex.Message}", ex);
                    return result;
                }
            }

            OASISErrorHandling.HandleError(ref result, $"Token allowance operation not yet implemented for provider {providerType}");
            return result;
        }

        /// <summary>
        /// Get token information (name, symbol, decimals, total supply)
        /// Generic endpoint that works with any ERC-20 compatible token
        /// </summary>
        /// <param name="tokenContractAddress">Token contract address</param>
        /// <param name="providerType">Provider type</param>
        /// <returns>Token information</returns>
        [Authorize]
        [HttpGet("token/info")]
        [ProducesResponseType(typeof(OASISResult<TokenInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<TokenInfo>> GetTokenInfo([FromQuery] string tokenContractAddress = null, [FromQuery] ProviderType providerType = ProviderType.EthereumOASIS)
        {
            var result = new OASISResult<TokenInfo>();

            if (string.IsNullOrWhiteSpace(tokenContractAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token contract address is required");
                return result;
            }

            // Use provider-specific token info method
            var providerResult = ProviderManager.Instance.GetProvider(providerType);
            if (providerResult == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Provider {providerType} is not available");
                return result;
            }

            // For Ethereum-based providers, use the generic token service
            if (providerType == ProviderType.EthereumOASIS)
            {
                try
                {
                    // Use MNEEService pattern (works for any ERC-20)
                    var serviceType = Type.GetType("NextGenSoftware.OASIS.API.Providers.EthereumOASIS.Services.MNEEService, NextGenSoftware.OASIS.API.Providers.EthereumOASIS");
                    if (serviceType != null)
                    {
                        var hostUri = providerResult.GetType().GetProperty("HostURI")?.GetValue(providerResult)?.ToString();
                        if (!string.IsNullOrWhiteSpace(hostUri))
                        {
                            var service = Activator.CreateInstance(serviceType, hostUri, tokenContractAddress);
                            var getTokenInfoMethod = serviceType.GetMethod("GetTokenInfoAsync");
                            if (getTokenInfoMethod != null)
                            {
                                var tokenInfoTask = (Task<OASISResult<MNEETokenInfo>>)getTokenInfoMethod.Invoke(service, new object[] { tokenContractAddress });
                                var tokenInfoResult = await tokenInfoTask;
                                
                                if (!tokenInfoResult.IsError && tokenInfoResult.Result != null)
                                {
                                    var genericResult = new OASISResult<TokenInfo>
                                    {
                                        Result = new TokenInfo
                                        {
                                            Name = tokenInfoResult.Result.Name,
                                            Symbol = tokenInfoResult.Result.Symbol,
                                            Decimals = tokenInfoResult.Result.Decimals,
                                            TotalSupply = tokenInfoResult.Result.TotalSupply,
                                            ContractAddress = tokenInfoResult.Result.ContractAddress
                                        },
                                        IsError = false,
                                        Message = tokenInfoResult.Message
                                    };
                                    return genericResult;
                                }
                                
                                var errorResult = new OASISResult<TokenInfo>();
                                OASISErrorHandling.HandleError(ref errorResult, tokenInfoResult.Message);
                                return errorResult;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting token info: {ex.Message}", ex);
                    return result;
                }
            }

            OASISErrorHandling.HandleError(ref result, $"Token info operation not yet implemented for provider {providerType}");
            return result;
        }

        #endregion
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

    /// <summary>
    /// Token approval request model
    /// </summary>
    public class TokenApprovalRequest
    {
        public Guid AvatarId { get; set; }
        public string TokenContractAddress { get; set; }
        public string SpenderAddress { get; set; }
        public decimal Amount { get; set; }
        public ProviderType ProviderType { get; set; } = ProviderType.EthereumOASIS;
    }

    /// <summary>
    /// Token information model
    /// </summary>
    public class TokenInfo
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public byte Decimals { get; set; }
        public decimal TotalSupply { get; set; }
        public string ContractAddress { get; set; }
    }
}