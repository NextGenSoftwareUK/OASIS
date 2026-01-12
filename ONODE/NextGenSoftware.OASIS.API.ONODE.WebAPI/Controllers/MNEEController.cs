using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS.Services;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// MNEE stablecoin convenience endpoints
    /// Provides convenience wrappers for MNEE operations (defaults to MNEE contract address)
    /// Uses generic WalletController token operations under the hood
    /// Contract Address: 0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF
    /// 
    /// Note: For generic token operations, use /api/wallet/token/* endpoints with any contract address
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MNEEController : OASISControllerBase
    {
        private WalletManager _walletManager = null;
        private KeyManager _keyManager = null;
        private EthereumOASIS _ethereumProvider = null;

        public WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));
                    _walletManager = new WalletManager(result.Result);
                }
                return _walletManager;
            }
        }

        public KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));
                    _keyManager = new KeyManager(result.Result);
                }
                return _keyManager;
            }
        }

        public EthereumOASIS EthereumProvider
        {
            get
            {
                if (_ethereumProvider == null)
                {
                    var providerResult = ProviderManager.Instance.GetProvider(ProviderType.EthereumOASIS);
                    if (providerResult != null && providerResult is EthereumOASIS)
                    {
                        _ethereumProvider = providerResult as EthereumOASIS;
                    }
                }
                return _ethereumProvider;
            }
        }

        /// <summary>
        /// Get MNEE balance for an avatar (convenience wrapper)
        /// Uses MNEE contract address: 0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF
        /// For generic token balance, use: GET /api/wallet/token/balance?tokenContractAddress={address}
        /// </summary>
        /// <param name="avatarId">Avatar ID (optional, defaults to authenticated avatar)</param>
        /// <returns>MNEE balance in decimal format</returns>
        [Authorize]
        [HttpGet("balance/{avatarId?}")]
        [ProducesResponseType(typeof(OASISResult<decimal>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<decimal>> GetBalance(Guid? avatarId = null)
        {
            var targetAvatarId = avatarId ?? AvatarId;
            if (targetAvatarId == Guid.Empty)
            {
                var result = new OASISResult<decimal>();
                OASISErrorHandling.HandleError(ref result, "Avatar ID is required");
                return result;
            }

            if (EthereumProvider == null)
            {
                var result = new OASISResult<decimal>();
                OASISErrorHandling.HandleError(ref result, "Ethereum provider is not available");
                return result;
            }

            return await EthereumProvider.GetMNEEBalanceForAvatarAsync(targetAvatarId);
        }

        /// <summary>
        /// Transfer MNEE between avatars or to external address (convenience wrapper)
        /// Uses MNEE contract address: 0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF
        /// For generic token transfer, use: POST /api/wallet/send_token with FromTokenAddress set to contract address
        /// </summary>
        /// <param name="request">Transfer request containing from/to avatars and amount</param>
        /// <returns>Transaction hash</returns>
        [Authorize]
        [HttpPost("transfer")]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<string>> Transfer([FromBody] MNEETransferRequest request)
        {
            var result = new OASISResult<string>();

            if (request == null)
            {
                OASISErrorHandling.HandleError(ref result, "Transfer request is required");
                return result;
            }

            if (EthereumProvider == null)
            {
                OASISErrorHandling.HandleError(ref result, "Ethereum provider is not available");
                return result;
            }

            try
            {
                // Use authenticated avatar as sender if not specified
                var fromAvatarId = request.FromAvatarId != Guid.Empty ? request.FromAvatarId : AvatarId;
                
                if (fromAvatarId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, "From avatar ID is required");
                    return result;
                }

                // Get sender's private key
                var senderPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(fromAvatarId, ProviderType.EthereumOASIS);
                if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "No private key found for sender");
                    return result;
                }

                // Determine recipient address
                string toAddress = null;
                if (!string.IsNullOrWhiteSpace(request.ToWalletAddress))
                {
                    toAddress = request.ToWalletAddress;
                }
                else if (request.ToAvatarId.HasValue && request.ToAvatarId.Value != Guid.Empty)
                {
                    var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, ProviderType.EthereumOASIS, request.ToAvatarId.Value);
                    if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                    {
                        OASISErrorHandling.HandleError(ref result, "No Ethereum wallet found for recipient");
                        return result;
                    }
                    toAddress = toWalletResult.Result;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Either ToWalletAddress or ToAvatarId must be provided");
                    return result;
                }

                // Transfer MNEE
                return await EthereumProvider.TransferMNEEAsync(
                    senderPrivateKeysResult.Result[0],
                    toAddress,
                    request.Amount
                );
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error transferring MNEE: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Approve MNEE spending for a spender address (convenience wrapper)
        /// Uses MNEE contract address: 0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF
        /// For generic token approval, use: POST /api/wallet/token/approve with TokenContractAddress set
        /// </summary>
        /// <param name="request">Approval request</param>
        /// <returns>Transaction hash</returns>
        [Authorize]
        [HttpPost("approve")]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<string>> Approve([FromBody] MNEEApprovalRequest request)
        {
            var result = new OASISResult<string>();

            if (request == null)
            {
                OASISErrorHandling.HandleError(ref result, "Approval request is required");
                return result;
            }

            if (EthereumProvider == null)
            {
                OASISErrorHandling.HandleError(ref result, "Ethereum provider is not available");
                return result;
            }

            try
            {
                var avatarId = request.AvatarId != Guid.Empty ? request.AvatarId : AvatarId;
                
                if (avatarId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar ID is required");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(request.SpenderAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Spender address is required");
                    return result;
                }

                // Get avatar's private key
                var privateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, ProviderType.EthereumOASIS);
                if (privateKeysResult.IsError || privateKeysResult.Result == null || privateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "No private key found for avatar");
                    return result;
                }

                return await EthereumProvider.ApproveMNEEAsync(
                    privateKeysResult.Result[0],
                    request.SpenderAddress,
                    request.Amount
                );
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error approving MNEE: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Get MNEE allowance for a spender (convenience wrapper)
        /// Uses MNEE contract address: 0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF
        /// For generic token allowance, use: GET /api/wallet/token/allowance?tokenContractAddress={address}&spenderAddress={spender}
        /// </summary>
        /// <param name="avatarId">Avatar ID (optional, defaults to authenticated avatar)</param>
        /// <param name="spenderAddress">Spender address</param>
        /// <returns>Allowance amount</returns>
        [Authorize]
        [HttpGet("allowance/{avatarId?}")]
        [ProducesResponseType(typeof(OASISResult<decimal>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<decimal>> GetAllowance(Guid? avatarId, [FromQuery] string spenderAddress)
        {
            var result = new OASISResult<decimal>();

            var targetAvatarId = avatarId ?? AvatarId;
            if (targetAvatarId == Guid.Empty)
            {
                OASISErrorHandling.HandleError(ref result, "Avatar ID is required");
                return result;
            }

            if (string.IsNullOrWhiteSpace(spenderAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Spender address is required");
                return result;
            }

            if (EthereumProvider == null)
            {
                OASISErrorHandling.HandleError(ref result, "Ethereum provider is not available");
                return result;
            }

            try
            {
                // Get avatar's wallet address
                var walletsResult = await WalletManager.LoadProviderWalletsForAvatarByIdAsync(targetAvatarId, false, false, ProviderType.EthereumOASIS, ProviderType.Default);
                if (walletsResult.IsError || walletsResult.Result == null || !walletsResult.Result.ContainsKey(ProviderType.EthereumOASIS) || walletsResult.Result[ProviderType.EthereumOASIS].Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "No Ethereum wallet found for avatar");
                    return result;
                }

                var ownerAddress = walletsResult.Result[ProviderType.EthereumOASIS][0].WalletAddress;
                return await EthereumProvider.GetMNEEAllowanceAsync(ownerAddress, spenderAddress);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting allowance: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Get MNEE token information (convenience wrapper)
        /// Uses MNEE contract address: 0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF
        /// For generic token info, use: GET /api/wallet/token/info?tokenContractAddress={address}
        /// </summary>
        /// <returns>Token information (name, symbol, decimals, total supply)</returns>
        [Authorize]
        [HttpGet("token-info")]
        [ProducesResponseType(typeof(OASISResult<MNEETokenInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status400BadRequest)]
        public async Task<OASISResult<MNEETokenInfo>> GetTokenInfo()
        {
            if (EthereumProvider == null)
            {
                var result = new OASISResult<MNEETokenInfo>();
                OASISErrorHandling.HandleError(ref result, "Ethereum provider is not available");
                return result;
            }

            return await EthereumProvider.GetMNEETokenInfoAsync();
        }
    }

    /// <summary>
    /// MNEE transfer request
    /// </summary>
    public class MNEETransferRequest
    {
        public Guid FromAvatarId { get; set; }
        public Guid? ToAvatarId { get; set; }
        public string ToWalletAddress { get; set; }
        public decimal Amount { get; set; }
        public string Memo { get; set; }
    }

    /// <summary>
    /// MNEE approval request
    /// </summary>
    public class MNEEApprovalRequest
    {
        public Guid AvatarId { get; set; }
        public string SpenderAddress { get; set; }
        public decimal Amount { get; set; }
    }
}
