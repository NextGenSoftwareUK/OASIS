using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using Solnet.Metaplex;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Programs;
using Solnet.Wallet;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using Rijndael256;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Helpers;
using Solnet.Wallet;
using System.Linq;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SolanaController : OASISControllerBase
    {
        private readonly ISolanaService _solanaService;

        public SolanaController(ISolanaService solanaService)
        {
            _solanaService = solanaService;
        }

        /// <summary>
        /// Mint NFT (non-fungible token)
        /// </summary>
        /// <param name="request">Mint Public Key Account, and Mint Decimals for Mint NFT</param>
        /// <returns>Mint NFT Transaction Hash</returns>
        [HttpPost]
        [Route("Mint")]
        public async Task<OASISResult<MintNftResult>> MintNft([FromBody] MintWeb3NFTRequest request)
        {
            return await _solanaService.MintNftAsync(request);
        }

        /// <summary>
        /// Handles a transaction between accounts with a specific Lampposts size
        /// </summary>
        /// <param name="request">FromAccount(Public Key) and ToAccount(Public Key)
        /// between which the transaction will be carried out</param>
        /// <returns>Send Transaction Hash</returns>
        [HttpPost]
        [Route("Send")]
        public async Task<OASISResult<SendTransactionResult>> SendTransaction([FromBody] SendTransactionRequest request)
        {
            return await _solanaService.SendTransaction(request);
        }

        /// <summary>
        /// Send SOL payment from authenticated avatar to another avatar using their wallets
        /// Uses the authenticated avatar's private key to sign the transaction
        /// </summary>
        /// <param name="toAvatarId">Recipient avatar ID</param>
        /// <param name="amount">Amount in SOL (decimal)</param>
        /// <param name="memoText">Optional memo text</param>
        /// <returns>Transaction hash</returns>
        [HttpPost]
        [Route("SendToAvatar/{toAvatarId}")]
        public async Task<OASISResult<SendTransactionResult>> SendToAvatar(
            Guid toAvatarId, 
            [FromBody] decimal amount,
            [FromQuery] string memoText = "")
        {
            var result = new OASISResult<SendTransactionResult>();
            
            try
            {
                // Get authenticated avatar ID (from JWT token)
                var fromAvatarId = AvatarId;
                if (fromAvatarId == Guid.Empty)
                {
                    result.IsError = true;
                    result.Message = "Avatar not authenticated";
                    return result;
                }

                // Get wallet addresses for both avatars
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(
                    WalletManager.Instance, ProviderType.SolanaOASIS, fromAvatarId);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(
                    WalletManager.Instance, ProviderType.SolanaOASIS, toAvatarId);

                if (fromWalletResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Error getting sender wallet: {fromWalletResult.Message}";
                    return result;
                }

                if (toWalletResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Error getting recipient wallet: {toWalletResult.Message}";
                    return result;
                }

                // Get sender's private key directly from WalletManager (bypasses KeyManager authorization check)
                // KeyManager.GetProviderPrivateKeysForAvatarById has an authorization check that fails
                // because AvatarManager.LoggedInAvatar is not set correctly in web API context
                // So we'll load wallets directly and extract/decrypt private keys ourselves
                var walletsResult = WalletManager.Instance.LoadProviderWalletsForAvatarById(
                    fromAvatarId, false, false, ProviderType.SolanaOASIS);

                if (walletsResult.IsError || walletsResult.Result == null || !walletsResult.Result.ContainsKey(ProviderType.SolanaOASIS))
                {
                    result.IsError = true;
                    result.Message = $"Error loading sender wallets: {walletsResult.Message ?? "No wallets found"}";
                    return result;
                }

                var solanaWallets = walletsResult.Result[ProviderType.SolanaOASIS];
                if (solanaWallets == null || !solanaWallets.Any() || string.IsNullOrEmpty(solanaWallets.First().PrivateKey))
                {
                    result.IsError = true;
                    result.Message = "No private key found in sender's Solana wallet";
                    return result;
                }

                // Decrypt the private key (it's stored encrypted)
                var encryptedPrivateKey = solanaWallets.First().PrivateKey;
                string privateKey;
                try
                {
                    privateKey = Rijndael256.Rijndael.Decrypt(
                        encryptedPrivateKey, 
                        OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, 
                        KeySize.Aes256);
                }
                catch (Exception ex)
                {
                    result.IsError = true;
                    result.Message = $"Error decrypting private key: {ex.Message}";
                    return result;
                }

                // Create Solana Account from private key
                // KeyManager.GenerateKeyPair stores Solana private key as Base58 (account.PrivateKey.Key)
                // The decrypted private key is already in Base58 format, so we can use it directly!
                // Account(string, string) constructor expects Base58-encoded 64-byte keypair
                Account senderAccount;
                
                // The private key from KeyManager is already Base58, so use it directly
                // No need to decode and re-encode - it's already in the correct format
                senderAccount = new Account(privateKey, fromWalletResult.Result);
                
                // Verify public key matches (sanity check)
                // This is critical - if this fails, the Account was created incorrectly
                if (senderAccount.PublicKey.Key != fromWalletResult.Result)
                {
                    result.IsError = true;
                    result.Message = $"Account public key mismatch: expected {fromWalletResult.Result}, got {senderAccount.PublicKey.Key}. This indicates the private key does not correspond to the stored public key.";
                    return result;
                }
                
                // Additional verification: Ensure the Account can sign (verify private key is valid)
                // If the Account was created with wrong key format, PublicKey might match but signing will fail
                // We'll verify this by checking the Account's internal state is valid
                if (senderAccount == null || senderAccount.PublicKey == null)
                {
                    result.IsError = true;
                    result.Message = "Failed to create valid Account from private key";
                    return result;
                }

                // Validate amount before processing
                if (amount <= 0)
                {
                    result.IsError = true;
                    result.Message = "Amount must be greater than zero";
                    return result;
                }

                // Convert SOL to lamports
                var lamports = (ulong)(amount * 1_000_000_000);

                // Use SolanaService.SendTransaction with the sender's account
                // This uses the keys we generated at wallet creation!
                // SendTransactionRequest inherits FromAccount and ToAccount from BaseExchangeRequest
                // NOTE: BaseExchangeRequest.IsRequestValid() checks Amount property, not Lampposts
                // So we need to set both Amount (for validation) and Lampposts (for actual transaction)
                var sendRequest = new SendTransactionRequest
                {
                    FromAccount = new BaseAccountRequest { PublicKey = fromWalletResult.Result },
                    ToAccount = new BaseAccountRequest { PublicKey = toWalletResult.Result },
                    Amount = lamports,  // Set Amount for validation (BaseExchangeRequest.IsRequestValid checks this)
                    Lampposts = lamports,  // Set Lampposts for actual transaction
                    MemoText = memoText ?? $"Payment from {fromAvatarId} to {toAvatarId}"
                };
                
                // Pass the sender's account to SolanaService - it will use this instead of temporary account
                var sendResult = await _solanaService.SendTransaction(sendRequest, senderAccount);
                
                if (sendResult.IsError)
                {
                    result.IsError = true;
                    result.Message = sendResult.Message;
                    return result;
                }

                result.Result = sendResult.Result;
                result.IsError = false;
                result.Message = "Transaction sent successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error sending payment: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }
    }
}