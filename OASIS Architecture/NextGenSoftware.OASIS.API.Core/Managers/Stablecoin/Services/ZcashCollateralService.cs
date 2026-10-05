using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Stablecoin.Services
{
    /// <summary>
    /// Service for locking/unlocking ZEC collateral on Zcash
    /// </summary>
    public class ZcashCollateralService : IZcashCollateralService
    {
        // Locked collateral address for unlocking (fallback if ReleaseZECAsync needs a specific address)
        private const string LOCKED_COLLATERAL_ADDRESS_TESTNET = "zt1test..."; // Placeholder testnet address

        /// <summary>
        /// Locks ZEC collateral by locking it for the bridge/stablecoin system
        /// Uses ZcashOASIS provider to lock ZEC for bridge operations
        /// </summary>
        public async Task<OASISResult<string>> LockCollateralAsync(
            string zcashAddress, 
            decimal amount, 
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<string>();

            try
            {
                if (string.IsNullOrWhiteSpace(zcashAddress))
                {
                    result.IsError = true;
                    result.Message = "Zcash address is required";
                    return result;
                }

                if (amount <= 0)
                {
                    result.IsError = true;
                    result.Message = "Amount must be greater than zero";
                    return result;
                }

                // Get Zcash provider from ProviderManager
                var zcashProviderBase = ProviderManager.Instance.GetStorageProvider(ProviderType.ZcashOASIS);
                
                if (zcashProviderBase == null)
                {
                    result.IsError = true;
                    result.Message = "ZcashOASIS provider is not registered or not available";
                    return result;
                }

                // Ensure provider is activated
                if (!zcashProviderBase.IsProviderActivated)
                {
                    var activationResult = await zcashProviderBase.ActivateProviderAsync();
                    if (activationResult.IsError)
                    {
                        result.IsError = true;
                        result.Message = $"Failed to activate Zcash provider: {activationResult.Message}";
                        return result;
                    }
                }

                // Use reflection to call LockZECForBridgeAsync method (provider is in separate assembly)
                var providerType = zcashProviderBase.GetType();
                var lockMethod = providerType.GetMethod("LockZECForBridgeAsync", BindingFlags.Public | BindingFlags.Instance);
                
                if (lockMethod == null)
                {
                    result.IsError = true;
                    result.Message = "Zcash provider does not support LockZECForBridgeAsync method";
                    return result;
                }

                // Invoke the method dynamically
                var lockTask = lockMethod.Invoke(zcashProviderBase, new object[] { amount, "Aztec", zcashAddress, null }) as Task<OASISResult<string>>;
                var lockResult = await lockTask;

                if (lockResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to lock ZEC: {lockResult.Message}";
                    return result;
                }

                result.Result = lockResult.Result; // Transaction ID/hash
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error locking ZEC collateral: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Unlocks ZEC collateral by releasing it from the bridge lock
        /// Note: In a real implementation, we'd need the lock transaction hash to release
        /// For now, we use ReleaseZECAsync which requires the lock transaction hash
        /// </summary>
        public async Task<OASISResult<string>> UnlockCollateralAsync(
            string zcashAddress, 
            decimal amount, 
            CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<string>();

            try
            {
                if (string.IsNullOrWhiteSpace(zcashAddress))
                {
                    result.IsError = true;
                    result.Message = "Zcash address is required";
                    return result;
                }

                if (amount <= 0)
                {
                    result.IsError = true;
                    result.Message = "Amount must be greater than zero";
                    return result;
                }

                // Get Zcash provider from ProviderManager
                var zcashProviderBase = ProviderManager.Instance.GetStorageProvider(ProviderType.ZcashOASIS);
                
                if (zcashProviderBase == null)
                {
                    result.IsError = true;
                    result.Message = "ZcashOASIS provider is not registered or not available";
                    return result;
                }

                // Ensure provider is activated
                if (!zcashProviderBase.IsProviderActivated)
                {
                    var activationResult = await zcashProviderBase.ActivateProviderAsync();
                    if (activationResult.IsError)
                    {
                        result.IsError = true;
                        result.Message = $"Failed to activate Zcash provider: {activationResult.Message}";
                        return result;
                    }
                }

                // Use reflection to call CreateShieldedTransactionAsync method
                var providerType = zcashProviderBase.GetType();
                var shieldedTxMethod = providerType.GetMethod("CreateShieldedTransactionAsync", BindingFlags.Public | BindingFlags.Instance);
                
                if (shieldedTxMethod == null)
                {
                    result.IsError = true;
                    result.Message = "Zcash provider does not support CreateShieldedTransactionAsync method";
                    return result;
                }

                // Invoke the method dynamically - returns Task<OASISResult<ShieldedTransaction>>
                var shieldedTxTaskObj = shieldedTxMethod.Invoke(zcashProviderBase, new object[] { LOCKED_COLLATERAL_ADDRESS_TESTNET, zcashAddress, amount, "Unlock ZEC collateral" });
                dynamic shieldedTxTask = shieldedTxTaskObj;
                dynamic shieldedTxResult = await shieldedTxTask;

                if (shieldedTxResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Failed to unlock ZEC: {shieldedTxResult.Message}";
                    return result;
                }

                // Extract transaction hash from result using dynamic or reflection
                var transactionHash = "unlock_completed";
                if (shieldedTxResult.Result != null)
                {
                    try
                    {
                        // Try to access TransactionHash property dynamically
                        transactionHash = shieldedTxResult.Result.TransactionHash?.ToString() ?? transactionHash;
                    }
                    catch
                    {
                        // Fallback to reflection if dynamic fails
                        var resultType = shieldedTxResult.Result.GetType();
                        var hashProperty = resultType.GetProperty("TransactionHash");
                        if (hashProperty != null)
                        {
                            transactionHash = hashProperty.GetValue(shieldedTxResult.Result)?.ToString() ?? transactionHash;
                        }
                    }
                }
                
                result.Result = transactionHash;
                result.IsError = false;
                return result;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Error unlocking ZEC collateral: {ex.Message}", ex);
                return result;
            }
        }
    }
}

