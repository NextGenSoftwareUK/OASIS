using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash
{
    public class ZcashBridgeService : IZcashBridgeService, IOASISBridge
    {
        private readonly ZcashRPCClient _rpcClient;
        private readonly string _bridgePoolAddress;

        public ZcashBridgeService(ZcashRPCClient rpcClient)
        {
            _rpcClient = rpcClient;
            _bridgePoolAddress = Environment.GetEnvironmentVariable("ZCASH_BRIDGE_POOL_ADDRESS") ?? "zt1bridgepool";
        }

        #region IOASISBridge Implementation

        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(accountAddress))
            {
                var result = new OASISResult<decimal>();
                result.IsError = true;
                result.Message = "Address is required";
                return result;
            }

            return await _rpcClient.GetBalanceAsync(accountAddress);
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            // Zcash RPC can generate new addresses but does not return private keys via RPC for security reasons.
            // We'll return the address and note that the private key/seed must be managed in the local wallet.
            var addressResult = await _rpcClient.GetNewAddressAsync();
            if (addressResult.IsError)
            {
                var result = new OASISResult<(string, string, string)>();
                result.IsError = true;
                result.Message = addressResult.Message;
                return result;
            }

            var message = "Private key/seed management must be handled by the node wallet. Use z_exportwallet for backups.";
            return new OASISResult<(string, string, string)>((addressResult.Result, string.Empty, string.Empty))
            {
                Message = message
            };
        }

        public Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            // Not supported via RPC without wallet import operations.
            var result = new OASISResult<(string, string)>();
            result.IsError = true;
            result.Message = "Zcash RPC does not support restoring accounts via API. Import wallet manually.";
            return Task.FromResult(result);
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            _ = senderPrivateKey; // Zcash daemon signs transactions internally
            var tx = await _rpcClient.SendShieldedTransactionAsync(senderAccountAddress ?? _bridgePoolAddress, _bridgePoolAddress, amount, "withdrawal");
            if (tx.IsError)
            {
                var errorResult = new OASISResult<BridgeTransactionResponse>();
                errorResult.IsError = true;
                errorResult.Message = tx.Message;
                return errorResult;
            }

            return new OASISResult<BridgeTransactionResponse>(new BridgeTransactionResponse(
                tx.Result,
                tx.Result,
                true,
                "Zcash withdrawal initiated",
                BridgeTransactionStatus.Pending));
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var tx = await _rpcClient.SendShieldedTransactionAsync(_bridgePoolAddress, receiverAccountAddress, amount, "deposit");
            if (tx.IsError)
            {
                var errorResult = new OASISResult<BridgeTransactionResponse>();
                errorResult.IsError = true;
                errorResult.Message = tx.Message;
                return errorResult;
            }

            return new OASISResult<BridgeTransactionResponse>(new BridgeTransactionResponse(
                tx.Result,
                tx.Result,
                true,
                "Zcash deposit initiated",
                BridgeTransactionStatus.Pending));
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var tx = await _rpcClient.GetTransactionAsync(transactionHash);
            if (tx.IsError)
            {
                var errorResult = new OASISResult<BridgeTransactionStatus>();
                errorResult.IsError = true;
                errorResult.Message = tx.Message;
                return errorResult;
            }

            return new OASISResult<BridgeTransactionStatus>(BridgeTransactionStatus.Completed);
        }

        #endregion

        #region Legacy Lock Helper

        public async Task<string> LockZECForBridgeAsync(
            decimal amount,
            string destinationChain,
            string destinationAddress,
            string viewingKey = null)
        {
            var bridgeAddress = GetBridgeAddressForChain(destinationChain);
            var memo = $"{destinationChain}:{destinationAddress}";

            var txResult = await _rpcClient.SendShieldedTransactionAsync(
                null,
                bridgeAddress,
                amount,
                memo
            );

            if (txResult.IsError)
            {
                throw new Exception($"Failed to lock ZEC for bridge: {txResult.Message}");
            }

            if (!string.IsNullOrEmpty(viewingKey))
            {
                await StoreViewingKeyForTransactionAsync(txResult.Result, viewingKey, destinationChain, destinationAddress);
            }

            return txResult.Result;
        }

        #endregion

        #region Stablecoin Integration

        /// <summary>
        /// Release ZEC that was previously locked for bridge operations
        /// Used for stablecoin redemption
        /// </summary>
        public async Task<OASISResult<string>> ReleaseZECAsync(string lockTxHash, decimal amount, string destinationAddress)
        {
            var result = new OASISResult<string>();
            try
            {
                // Verify the lock transaction exists and is valid
                var txResult = await _rpcClient.GetTransactionAsync(lockTxHash);
                if (txResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Lock transaction not found: {txResult.Message}";
                    return result;
                }

                // Release ZEC from bridge pool to destination address
                var releaseTx = await _rpcClient.SendShieldedTransactionAsync(
                    _bridgePoolAddress,
                    destinationAddress,
                    amount,
                    $"Release from lock: {lockTxHash}"
                );

                if (releaseTx.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to release ZEC: {releaseTx.Message}";
                    return result;
                }

                result.Result = releaseTx.Result;
                result.IsError = false;
                result.Message = "ZEC released successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }

            return result;
        }

        #endregion

        private string GetBridgeAddressForChain(string chain)
        {
            return chain switch
            {
                "Aztec" => Environment.GetEnvironmentVariable("ZCASH_AZTEC_BRIDGE_ADDRESS") ?? "zt1testaztec",
                "Miden" => Environment.GetEnvironmentVariable("ZCASH_MIDEN_BRIDGE_ADDRESS") ?? "zt1testmiden",
                "Solana" => Environment.GetEnvironmentVariable("ZCASH_SOLANA_BRIDGE_ADDRESS") ?? "zt1testsolana",
                _ => throw new ArgumentException($"Unsupported destination chain: {chain}")
            };
        }

        private async Task StoreViewingKeyForTransactionAsync(
            string txId,
            string viewingKey,
            string destinationChain,
            string destinationAddress)
        {
            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = $"Bridge Viewing Key: {txId}",
                Description = $"Viewing key for bridge transaction to {destinationChain}",
                HolonType = Core.Enums.HolonType.Default,
                ProviderMetaData = new System.Collections.Generic.Dictionary<Core.Enums.ProviderType, System.Collections.Generic.Dictionary<string, string>>
                {
                    {
                        Core.Enums.ProviderType.ZcashOASIS,
                        new System.Collections.Generic.Dictionary<string, string>
                        {
                            { "TransactionId", txId },
                            { "ViewingKey", viewingKey },
                            { "DestinationChain", destinationChain },
                            { "DestinationAddress", destinationAddress },
                            { "Purpose", "Auditability" }
                        }
                    }
                }
            };

            await HolonManager.Instance.SaveHolonAsync(holon);
        }
    }
}

