using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS;
using NextGenSoftware.OASIS.API.Providers.MidenOASIS.Infrastructure.Services.Miden;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash;

namespace Zypherpunk.Tests
{
    /// <summary>
    /// Test class for Miden ↔ Zcash bridge operations
    /// Run these tests after setting up Miden and Zcash testnet connections
    /// </summary>
    public class MidenZcashBridgeTests
    {
        private CrossChainBridgeManager _bridgeManager;
        private MidenOASIS _midenProvider;
        private ZcashOASIS _zcashProvider;

        /// <summary>
        /// Initialize providers and bridge manager
        /// </summary>
        public async Task SetupAsync()
        {
            // Initialize Miden provider
            _midenProvider = new MidenOASIS();
            var midenActivateResult = await _midenProvider.ActivateProviderAsync();
            if (midenActivateResult.IsError)
            {
                throw new Exception($"Failed to activate Miden provider: {midenActivateResult.Message}");
            }

            // Initialize Zcash provider
            _zcashProvider = new ZcashOASIS();
            var zcashActivateResult = await _zcashProvider.ActivateProviderAsync();
            if (zcashActivateResult.IsError)
            {
                throw new Exception($"Failed to activate Zcash provider: {zcashActivateResult.Message}");
            }

            // Create bridge services
            // Note: In production, these would be accessed through provider managers
            // For testing, we create them directly
            var zcashRPCClient = new ZcashRPCClient(
                Environment.GetEnvironmentVariable("ZCASH_RPC_URL") ?? "http://localhost:8232",
                Environment.GetEnvironmentVariable("ZCASH_RPC_USER") ?? "user",
                Environment.GetEnvironmentVariable("ZCASH_RPC_PASSWORD") ?? "password"
            );
            
            var midenAPIClient = new MidenAPIClient(
                Environment.GetEnvironmentVariable("MIDEN_API_URL") ?? "https://testnet.miden.xyz",
                Environment.GetEnvironmentVariable("MIDEN_API_KEY")
            );
            
            var midenService = new MidenService(midenAPIClient);
            
            var bridges = new Dictionary<string, IOASISBridge>
            {
                { "ZEC", new ZcashBridgeService(zcashRPCClient) },
                { "MIDEN", new MidenBridgeService(midenService) }
            };

            // Initialize bridge manager
            _bridgeManager = new CrossChainBridgeManager(bridges);
        }

        /// <summary>
        /// Test Zcash → Miden bridge
        /// </summary>
        public async Task<OASISResult<string>> TestZcashToMidenBridgeAsync(
            decimal amount,
            string zcashAddress,
            string midenAddress)
        {
            var result = new OASISResult<string>();

            try
            {
                var request = new CreateBridgeOrderRequest
                {
                    FromToken = "ZEC",
                    ToToken = "MIDEN",
                    Amount = amount,
                    FromAddress = zcashAddress,
                    DestinationAddress = midenAddress,
                    UserId = Guid.NewGuid(),
                    EnableViewingKeyAudit = true,
                    RequireProofVerification = true
                };

                var bridgeResult = await _bridgeManager.CreateBridgeOrderAsync(request);
                if (bridgeResult.IsError)
                {
                    result.IsError = true;
                    result.Message = bridgeResult.Message;
                    return result;
                }

                result.Result = bridgeResult.Result?.TransactionId;
                result.IsError = false;
                result.Message = "Bridge completed successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Test Miden → Zcash bridge
        /// </summary>
        public async Task<OASISResult<string>> TestMidenToZcashBridgeAsync(
            decimal amount,
            string midenAddress,
            string zcashAddress)
        {
            var result = new OASISResult<string>();

            try
            {
                var request = new CreateBridgeOrderRequest
                {
                    FromToken = "MIDEN",
                    ToToken = "ZEC",
                    Amount = amount,
                    FromAddress = midenAddress,
                    DestinationAddress = zcashAddress,
                    UserId = Guid.NewGuid(),
                    RequireProofVerification = true
                };

                var bridgeResult = await _bridgeManager.CreateBridgeOrderAsync(request);
                if (bridgeResult.IsError)
                {
                    result.IsError = true;
                    result.Message = bridgeResult.Message;
                    return result;
                }

                result.Result = bridgeResult.Result?.TransactionId;
                result.IsError = false;
                result.Message = "Bridge completed successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Test STARK proof generation and verification
        /// </summary>
        public async Task<OASISResult<bool>> TestSTARKProofAsync()
        {
            var result = new OASISResult<bool>();

            try
            {
                var proofResult = await _midenProvider.GenerateSTARKProofAsync(
                    programHash: "bridge_program_hash",
                    inputs: new { amount = 1.0m, source = "Zcash", txHash = "test_hash" },
                    outputs: new { noteId = "test_note" }
                );

                if (proofResult.IsError)
                {
                    result.IsError = true;
                    result.Message = $"Proof generation failed: {proofResult.Message}";
                    return result;
                }

                var verifyResult = await _midenProvider.VerifySTARKProofAsync(proofResult.Result);
                if (verifyResult.IsError || !verifyResult.Result)
                {
                    result.IsError = true;
                    result.Message = $"Proof verification failed: {verifyResult.Message}";
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "STARK proof generated and verified successfully";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }

            return result;
        }
    }
}

