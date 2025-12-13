using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Database;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Manager for cross-chain bridge operations following OASIS standards.
    /// Wraps CrossChainBridgeManager and provides bridge functionality through OASIS providers.
    /// </summary>
    public class BridgeManager : OASISManager
    {
        private static BridgeManager _instance = null;
        private ICrossChainBridgeManager _crossChainBridgeManager;
        private readonly Dictionary<string, IOASISBlockchainStorageProvider> _providerBridgeMap;

        public static BridgeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var storageProvider = ProviderManager.Instance.CurrentStorageProvider;
                    _instance = new BridgeManager(storageProvider);
                }
                return _instance;
            }
        }

        public BridgeManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) 
            : base(OASISStorageProvider, OASISDNA)
        {
            _providerBridgeMap = new Dictionary<string, IOASISBlockchainStorageProvider>();
            InitializeBridgeManager();
        }

        private void InitializeBridgeManager()
        {
            try
            {
                // Get all blockchain storage providers from ProviderManager
                var providers = ProviderManager.Instance.GetAllBlockchainProviders();

                // Map providers to their token symbols
                var bridgeMap = new Dictionary<string, IOASISBridge>();

                foreach (var provider in providers)
                {
                    var tokenSymbol = GetTokenSymbolForProvider(provider.ProviderType.Value);
                    if (!string.IsNullOrEmpty(tokenSymbol))
                    {
                        // Create a bridge adapter that wraps the provider
                        var bridgeAdapter = new ProviderBridgeAdapter(provider);
                        bridgeMap[tokenSymbol] = bridgeAdapter;
                        _providerBridgeMap[tokenSymbol] = provider;
                    }
                }

                // Initialize CrossChainBridgeManager with the bridge map
                _crossChainBridgeManager = new CrossChainBridgeManager(
                    bridgeMap,
                    exchangeRateService: new CoinGeckoExchangeRateService(),
                    repository: null, // Can be configured later
                    viewingKeyAuditService: new ViewingKeyAuditService(),
                    proofVerificationService: new ProofVerificationService(),
                    mpcExecutionService: new MpcExecutionService());
            }
            catch (Exception ex)
            {
                var errorResult = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref errorResult, 
                    $"Error initializing BridgeManager: {ex.Message}", ex);
            }
        }

        private string GetTokenSymbolForProvider(ProviderType providerType)
        {
            return providerType switch
            {
                ProviderType.SolanaOASIS => "SOL",
                ProviderType.EthereumOASIS => "ETH",
                ProviderType.RadixOASIS => "XRD",
                ProviderType.ZcashOASIS => "ZEC",
                ProviderType.AztecOASIS => "AZTEC",
                ProviderType.MidenOASIS => "MIDEN",
                ProviderType.StarknetOASIS => "STARKNET",
                ProviderType.PolygonOASIS => "MATIC",
                ProviderType.ArbitrumOASIS => "ARB",
                ProviderType.OptimismOASIS => "OP",
                ProviderType.BNBChainOASIS => "BNB",
                ProviderType.AvalancheOASIS => "AVAX",
                ProviderType.NEAROASIS => "NEAR",
                ProviderType.SuiOASIS => "SUI",
                ProviderType.AptosOASIS => "APT",
                ProviderType.CardanoOASIS => "ADA",
                ProviderType.PolkadotOASIS => "DOT",
                ProviderType.BitcoinOASIS => "BTC",
                _ => null
            };
        }

        /// <summary>
        /// Creates a cross-chain bridge order (e.g., SOL to XRD swap)
        /// </summary>
        public async Task<OASISResult<CreateBridgeOrderResponse>> CreateBridgeOrderAsync(
            CreateBridgeOrderRequest request,
            CancellationToken token = default)
        {
            var result = new OASISResult<CreateBridgeOrderResponse>();
            
            try
            {
                if (_crossChainBridgeManager == null)
                {
                    result.IsError = true;
                    result.Message = "Bridge manager not initialized";
                    return result;
                }

                return await _crossChainBridgeManager.CreateBridgeOrderAsync(request, token);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, 
                    $"Error creating bridge order: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Checks the status and balance of a bridge order
        /// </summary>
        public async Task<OASISResult<BridgeOrderBalanceResponse>> CheckOrderBalanceAsync(
            Guid orderId,
            CancellationToken token = default)
        {
            var result = new OASISResult<BridgeOrderBalanceResponse>();
            
            try
            {
                if (_crossChainBridgeManager == null)
                {
                    result.IsError = true;
                    result.Message = "Bridge manager not initialized";
                    return result;
                }

                return await _crossChainBridgeManager.CheckOrderBalanceAsync(orderId, token);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, 
                    $"Error checking order balance: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Gets the current exchange rate between two tokens
        /// </summary>
        public async Task<OASISResult<decimal>> GetExchangeRateAsync(
            string fromToken,
            string toToken,
            CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            
            try
            {
                if (_crossChainBridgeManager == null)
                {
                    result.IsError = true;
                    result.Message = "Bridge manager not initialized";
                    return result;
                }

                return await _crossChainBridgeManager.GetExchangeRateAsync(fromToken, toToken, token);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, 
                    $"Error getting exchange rate: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Records a viewing key for auditability/compliance
        /// </summary>
        public async Task<OASISResult<bool>> RecordViewingKeyAsync(
            ViewingKeyAuditEntry entry, 
            CancellationToken token = default)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                if (_crossChainBridgeManager == null)
                {
                    result.IsError = true;
                    result.Message = "Bridge manager not initialized";
                    return result;
                }

                return await _crossChainBridgeManager.RecordViewingKeyAsync(entry, token);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, 
                    $"Error recording viewing key: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Verifies a submitted zero-knowledge proof payload
        /// </summary>
        public async Task<OASISResult<bool>> VerifyProofAsync(
            string proofPayload, 
            string proofType, 
            CancellationToken token = default)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                if (_crossChainBridgeManager == null)
                {
                    result.IsError = true;
                    result.Message = "Bridge manager not initialized";
                    return result;
                }

                return await _crossChainBridgeManager.VerifyProofAsync(proofPayload, proofType, token);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, 
                    $"Error verifying proof: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Gets a bridge provider for a specific token symbol
        /// </summary>
        public IOASISBlockchainStorageProvider GetBridgeProvider(string tokenSymbol)
        {
            if (string.IsNullOrWhiteSpace(tokenSymbol))
                return null;

            var key = tokenSymbol.ToUpperInvariant();
            return _providerBridgeMap.TryGetValue(key, out var provider) ? provider : null;
        }

        /// <summary>
        /// Creates a cross-chain NFT bridge order (e.g., NFT from Solana to Ethereum)
        /// </summary>
        public async Task<OASISResult<CreateBridgeOrderResponse>> CreateNFTBridgeOrderAsync(
            CreateNFTBridgeOrderRequest request,
            CancellationToken token = default)
        {
            var result = new OASISResult<CreateBridgeOrderResponse>();
            
            try
            {
                if (_crossChainBridgeManager == null)
                {
                    result.IsError = true;
                    result.Message = "Bridge manager not initialized";
                    return result;
                }

                return await _crossChainBridgeManager.CreateNFTBridgeOrderAsync(request, token);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, 
                    $"Error creating NFT bridge order: {ex.Message}", ex);
                return result;
            }
        }
    }

    /// <summary>
    /// Adapter that wraps IOASISBlockchainStorageProvider to implement IOASISBridge
    /// This allows providers to be used as bridges without implementing IOASISBridge directly
    /// </summary>
    internal class ProviderBridgeAdapter : IOASISBridge
    {
        private readonly IOASISBlockchainStorageProvider _provider;

        public ProviderBridgeAdapter(IOASISBlockchainStorageProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            return await _provider.GetAccountBalanceAsync(accountAddress, token);
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            return await _provider.CreateAccountAsync(token);
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            return await _provider.RestoreAccountAsync(seedPhrase, token);
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            return await _provider.WithdrawAsync(amount, senderAccountAddress, senderPrivateKey);
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            return await _provider.DepositAsync(amount, receiverAccountAddress);
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            return await _provider.GetTransactionStatusAsync(transactionHash, token);
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
        {
            // NFT bridge methods are on IOASISNFTProvider, not IOASISBlockchainStorageProvider
            // This adapter wraps IOASISBlockchainStorageProvider, so we can't directly access NFT methods
            // In a real implementation, you'd need to get the NFT provider separately
            return new OASISResult<BridgeTransactionResponse>
            {
                IsError = true,
                Message = "NFT bridge operations must be called directly on IOASISNFTProvider implementations, not through ProviderBridgeAdapter"
            };
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
        {
            // NFT bridge methods are on IOASISNFTProvider, not IOASISBlockchainStorageProvider
            // This adapter wraps IOASISBlockchainStorageProvider, so we can't directly access NFT methods
            // In a real implementation, you'd need to get the NFT provider separately
            return new OASISResult<BridgeTransactionResponse>
            {
                IsError = true,
                Message = "NFT bridge operations must be called directly on IOASISNFTProvider implementations, not through ProviderBridgeAdapter"
            };
        }
    }
}

