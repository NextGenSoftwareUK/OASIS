//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using NextGenSoftware.OASIS.API.Core.Managers.Bridge;
//using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
//using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services;
//using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Starknet;
//using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
//using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
//using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash;
//using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec;
//using NextGenSoftware.OASIS.API.Providers.EthereumOASIS.Infrastructure.Services.Ethereum;
//using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Bridges;
//using Nethereum.Web3;
//using Nethereum.Web3.Accounts;
//using NextGenSoftware.OASIS.Common;
//using Solnet.Rpc;
//using Solnet.Wallet;
//using Solnet.Wallet.Bip39;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;

///// <summary>
///// Service for managing cross-chain bridge operations in OASIS WebAPI.
///// Wraps the CrossChainBridgeManager and provides a service layer for controllers.
///// </summary>
//public class BridgeService
//{
//    private readonly ICrossChainBridgeManager _bridgeManager;
//    private readonly StarknetAtomicSwapManager _atomicSwapManager;
//    private readonly ILogger<BridgeService> _logger;

//    public BridgeService(ILogger<BridgeService> logger, IConfiguration configuration)
//    {
//        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
//        try
//        {
//            var solanaRpcUrl = configuration["SolanaBridgeOptions:RpcUrl"] ?? "https://api.devnet.solana.com";
//            var solanaTechPrivateKey = configuration["SolanaBridgeOptions:TechnicalAccountPrivateKey"];
            
//            _logger.LogInformation("Initializing Solana bridge with RPC: {RpcUrl}", solanaRpcUrl);
            
//            var rpcClient = ClientFactory.GetClient(solanaRpcUrl);
            
//            // Generate temporary technical account for testing
//            // TODO: Load from configuration once we have persistent technical accounts
//            var mnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
//            var wallet = new Wallet(mnemonic);
//            var solanaTechnicalAccount = wallet.Account;
            
//            _logger.LogInformation("Solana technical account: {PublicKey}", solanaTechnicalAccount.PublicKey);
//            _logger.LogWarning("Using temporary Solana technical account (regenerated each startup)");
            
//            var solanaBridge = new SolanaBridgeService(solanaTechnicalAccount, rpcClient);
            
//            // Initialize Zcash bridge
//            // Default to testnet port (18232). Once local zcashd build completes, use:
//            // http://localhost:18232 with credentials from ~/.zcash/zcash.conf
//            // For public testnet endpoints, configure via appsettings.json
//            var zcashRpcUrl = configuration["ZcashBridge:RpcUrl"] ?? "http://localhost:18232";
//            var zcashRpcUser = configuration["ZcashBridge:RpcUser"] ?? "oasis";
//            var zcashRpcPassword = configuration["ZcashBridge:RpcPassword"] ?? "Uppermall1!";
//            var zcashBridge = new ZcashBridgeService(new ZcashRPCClient(zcashRpcUrl, zcashRpcUser, zcashRpcPassword));
//            _logger.LogInformation("Zcash bridge initialized with RPC: {RpcUrl}", zcashRpcUrl);

//            // Initialize Aztec bridge - REAL TESTNET CONNECTION, NO MOCKS
//            var aztecNodeUrl = configuration["AztecBridge:NodeUrl"] ?? "https://aztec-testnet-fullnode.zkv.xyz";
//            var aztecPxeUrl = configuration["AztecBridge:PxeUrl"] ?? "https://aztec-testnet-fullnode.zkv.xyz";
            
//            _logger.LogInformation("Aztec bridge connecting to REAL testnet node: {NodeUrl}", aztecNodeUrl);
//            _logger.LogInformation("Aztec bridge PXE URL: {PxeUrl}", aztecPxeUrl);
            
//            // Create real Aztec testnet client - NO MOCKS
//            var aztecTestnetClient = new AztecTestnetClient(aztecNodeUrl, aztecPxeUrl);
//            var aztecApiClient = new AztecAPIClient(aztecNodeUrl); // Keep for backward compatibility
//            var aztecCliService = new AztecCLIService(aztecNodeUrl); // For real transactions via CLI
//            var aztecBridge = new AztecBridgeService(aztecApiClient, aztecTestnetClient, aztecCliService);
            
//            _logger.LogInformation("Aztec bridge initialized with REAL testnet connection and CLI service - NO MOCKS");

//            var starknetRpcUrl = configuration["StarknetBridge:RpcUrl"] ?? "https://alpha4.starknet.io";
//            var starknetNetwork = configuration["StarknetBridge:Network"] ?? "alpha-goerli";
//            _logger.LogInformation("Starknet bridge connecting to {RpcUrl} on {Network}", starknetRpcUrl, starknetNetwork);
//            var starknetHttpClient = new HttpClient { BaseAddress = new Uri(starknetRpcUrl) };
//            var starknetRpcClient = new StarknetRpcClient(starknetHttpClient, starknetRpcUrl);
//            var starknetBridge = new StarknetBridge(starknetNetwork, starknetRpcClient);

//            // Initialize Ethereum bridge
//            var ethereumRpcUrl = configuration["EthereumBridge:RpcUrl"] ?? "http://testchain.nethereum.com:8545";
//            var ethereumNetwork = configuration["EthereumBridge:Network"] ?? "sepolia";
//            _logger.LogInformation("Ethereum bridge connecting to {RpcUrl} on {Network}", ethereumRpcUrl, ethereumNetwork);
            
//            // Create a temporary test account for Ethereum bridge (in production, load from secure config)
//            // Using a placeholder private key for testing - in production, load from secure storage
//            var ethereumTestPrivateKey = configuration["EthereumBridge:TechnicalAccountPrivateKey"] ?? "0x0000000000000000000000000000000000000000000000000000000000000001";
//            var ethereumTestAccount = new Nethereum.Web3.Accounts.Account(ethereumTestPrivateKey);
//            var ethereumWeb3 = new Web3(ethereumTestAccount, ethereumRpcUrl);
//            var ethereumBridge = new EthereumBridgeService(
//                ethereumWeb3,
//                ethereumTestAccount,
//                useTestnet: ethereumNetwork.Contains("sepolia") || ethereumNetwork.Contains("testnet"));
//            _logger.LogInformation("Ethereum bridge initialized with test account: {Address}", ethereumTestAccount.Address);

//            var bridgeMap = new Dictionary<string, IOASISBridge>(StringComparer.OrdinalIgnoreCase)
//            {
//                { "SOL", solanaBridge },
//                { "ETH", ethereumBridge }, // Add Ethereum support
//                { "XRD", solanaBridge }, // Placeholder until Radix bridge is ready
//                { "ZEC", zcashBridge },
//                { "AZTEC", aztecBridge },
//                { "STARKNET", starknetBridge }
//            };

//            _bridgeManager = new CrossChainBridgeManager(
//                bridgeMap,
//                viewingKeyAuditService: new ViewingKeyAuditService(),
//                proofVerificationService: new ProofVerificationService(logger),
//                mpcExecutionService: new MpcExecutionService());
            
//            // Initialize atomic swap manager
//            _atomicSwapManager = new StarknetAtomicSwapManager(_bridgeManager);
            
//            _logger.LogInformation("Bridge service initialized successfully");
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Failed to initialize bridge service");
//            throw;
//        }
//    }

//    public async Task<OASISResult<CreateBridgeOrderResponse>> CreateOrderAsync(
//        CreateBridgeOrderRequest request,
//        CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            return await _bridgeManager.CreateBridgeOrderAsync(request, cancellationToken);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error creating bridge order");
//            var result = new OASISResult<CreateBridgeOrderResponse>();
//            result.IsError = true;
//            result.Message = $"Error creating order: {ex.Message}";
//            return result;
//        }
//    }

//    public async Task<OASISResult<BridgeOrderBalanceResponse>> CheckBalanceAsync(
//        Guid orderId,
//        CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            return await _bridgeManager.CheckOrderBalanceAsync(orderId, cancellationToken);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error checking order balance");
//            var result = new OASISResult<BridgeOrderBalanceResponse>();
//            result.IsError = true;
//            result.Message = $"Error checking balance: {ex.Message}";
//            return result;
//        }
//    }

//    public async Task<OASISResult<decimal>> GetExchangeRateAsync(
//        string fromToken,
//        string toToken,
//        CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            return await _bridgeManager.GetExchangeRateAsync(fromToken, toToken, cancellationToken);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error getting exchange rate");
//            var result = new OASISResult<decimal>();
//            result.IsError = true;
//            result.Message = $"Error getting exchange rate: {ex.Message}";
//            return result;
//        }
//    }

//    public Task<OASISResult<bool>> RecordViewingKeyAsync(ViewingKeyAuditEntry entry, CancellationToken cancellationToken = default)
//    {
//        return _bridgeManager.RecordViewingKeyAsync(entry, cancellationToken);
//    }

//    public Task<OASISResult<bool>> VerifyProofAsync(ProofVerificationRequest request, CancellationToken cancellationToken = default)
//    {
//        return _bridgeManager.VerifyProofAsync(request.ProofPayload, request.ProofType, cancellationToken);
//    }

//    /// <summary>
//    /// Creates a Starknet ↔ Zcash atomic swap
//    /// </summary>
//    public async Task<OASISResult<AtomicSwapStatusResponse>> CreateAtomicSwapAsync(
//        AtomicSwapRequest request,
//        CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            return await _atomicSwapManager.CreateAtomicSwapAsync(request, cancellationToken);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error creating atomic swap");
//            var result = new OASISResult<AtomicSwapStatusResponse>();
//            result.IsError = true;
//            result.Message = $"Error creating atomic swap: {ex.Message}";
//            return result;
//        }
//    }

//    /// <summary>
//    /// Gets the status of an atomic swap by bridge ID
//    /// </summary>
//    public async Task<OASISResult<AtomicSwapStatusResponse>> GetAtomicSwapStatusAsync(
//        string bridgeId,
//        CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            return await _atomicSwapManager.GetSwapStatusAsync(bridgeId, cancellationToken);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error getting atomic swap status");
//            var result = new OASISResult<AtomicSwapStatusResponse>();
//            result.IsError = true;
//            result.Message = $"Error getting swap status: {ex.Message}";
//            return result;
//        }
//    }

//    /// <summary>
//    /// Gets atomic swap history for a user
//    /// </summary>
//    public async Task<OASISResult<AtomicSwapHistoryResponse>> GetAtomicSwapHistoryAsync(
//        Guid userId,
//        CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            return await _atomicSwapManager.GetSwapHistoryAsync(userId, cancellationToken);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error getting atomic swap history");
//            var result = new OASISResult<AtomicSwapHistoryResponse>();
//            result.IsError = true;
//            result.Message = $"Error getting swap history: {ex.Message}";
//            return result;
//        }
//    }
//}

