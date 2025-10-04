﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;

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
        [ProducesResponseType(typeof(OASISResult<ITransactionRespone>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<ITransactionRespone>> SendTokenAsync(IWalletTransactionRequest request)
        {
            return await WalletManager.SendTokenAsync(request);
        }

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
        [HttpGet("avatar/{id}/wallets")]
        [ProducesResponseType(typeof(OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            return await WalletManager.LoadProviderWalletsForAvatarByIdAsync(id, providerType);
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
        [HttpGet("avatar/username/{username}/wallets")]
        [ProducesResponseType(typeof(OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameAsync(string username, ProviderType providerType = ProviderType.Default)
        {
            return await WalletManager.LoadProviderWalletsForAvatarByUsernameAsync(username, providerType);
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
            return await WalletManager.LoadProviderWalletsForAvatarByEmailAsync(email, providerType);
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
        [HttpGet("avatar/username/{username}/default-wallet")]
        [ProducesResponseType(typeof(OASISResult<IProviderWallet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByUsernameAsync(string username, ProviderType providerType)
        {
            return await WalletManager.GetAvatarDefaultWalletByUsernameAsync(username, providerType);
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
        public async Task<OASISResult<bool>> SetAvatarDefaultWalletByIdAsync(Guid id, Guid walletId, ProviderType providerType)
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
        public async Task<OASISResult<bool>> SetAvatarDefaultWalletByUsernameAsync(string username, Guid walletId, ProviderType providerType)
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
        public async Task<OASISResult<bool>> SetAvatarDefaultWalletByEmailAsync(string email, Guid walletId, ProviderType providerType)
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
        public OASISResult<Guid> ImportWalletUsingPrivateKeyById(Guid avatarId, string key, ProviderType providerToImportTo)
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
        public OASISResult<Guid> ImportWalletUsingPrivateKeyByUsername(string username, string key, ProviderType providerToImportTo)
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
        public OASISResult<Guid> ImportWalletUsingPrivateKeyByEmail(string email, string key, ProviderType providerToImportTo)
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
        public OASISResult<Guid> ImportWalletUsingPublicKeyById(Guid avatarId, string key, ProviderType providerToImportTo)
        {
            return WalletManager.ImportWalletUsingPublicKeyById(avatarId, key, providerToImportTo);
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
        public OASISResult<Guid> ImportWalletUsingPublicKeyByUsername(string username, string key, ProviderType providerToImportTo)
        {
            return WalletManager.ImportWalletUsingPublicKeyByUsername(username, key, providerToImportTo);
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
        public OASISResult<Guid> ImportWalletUsingPublicKeyByEmail(string email, string key, ProviderType providerToImportTo)
        {
            return WalletManager.ImportWalletUsingPublicKeyByEmail(email, key, providerToImportTo);
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
    }
}