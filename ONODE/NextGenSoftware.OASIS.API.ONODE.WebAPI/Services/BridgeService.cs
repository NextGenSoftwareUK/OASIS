using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;
using Solnet.Rpc;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;

/// <summary>
/// Service for managing cross-chain bridge operations in OASIS WebAPI.
/// Wraps the CrossChainBridgeManager and provides a service layer for controllers.
/// </summary>
public class BridgeService
{
    private readonly ICrossChainBridgeManager _bridgeManager;
    private readonly ILogger<BridgeService> _logger;

    public BridgeService(ILogger<BridgeService> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        try
        {
            // Initialize Solana bridge
            var solanaRpcUrl = configuration["SolanaBridgeOptions:RpcUrl"] ?? "https://api.devnet.solana.com";
            var solanaTechPrivateKey = configuration["SolanaBridgeOptions:TechnicalAccountPrivateKey"];
            
            _logger.LogInformation("Initializing Solana bridge with RPC: {RpcUrl}", solanaRpcUrl);
            
            var rpcClient = ClientFactory.GetClient(solanaRpcUrl);
            
            // Generate temporary technical account for testing
            // TODO: Load from configuration once we have persistent technical accounts
            var mnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
            var wallet = new Wallet(mnemonic);
            var solanaTechnicalAccount = wallet.Account;
            
            _logger.LogInformation("Solana technical account: {PublicKey}", solanaTechnicalAccount.PublicKey);
            _logger.LogWarning("Using temporary Solana technical account (regenerated each startup)");
            
            var solanaBridge = new SolanaBridgeService(solanaTechnicalAccount, rpcClient);
            
            // TODO: Initialize Radix bridge when RadixOASIS is fixed
            // For now, we'll use null and the manager will handle it gracefully
            
            // Create bridge manager
            // Note: Once RadixOASIS is ready, add it as second parameter
            _bridgeManager = new CrossChainBridgeManager(
                solanaBridge: solanaBridge,
                radixBridge: solanaBridge  // Temporary: using Solana for both until Radix is ready
            );
            
            _logger.LogInformation("Bridge service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize bridge service");
            throw;
        }
    }

    public async Task<OASISResult<CreateBridgeOrderResponse>> CreateOrderAsync(
        CreateBridgeOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _bridgeManager.CreateBridgeOrderAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bridge order");
            var result = new OASISResult<CreateBridgeOrderResponse>();
            result.IsError = true;
            result.Message = $"Error creating order: {ex.Message}";
            return result;
        }
    }

    public async Task<OASISResult<BridgeOrderBalanceResponse>> CheckBalanceAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _bridgeManager.CheckOrderBalanceAsync(orderId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking order balance");
            var result = new OASISResult<BridgeOrderBalanceResponse>();
            result.IsError = true;
            result.Message = $"Error checking balance: {ex.Message}";
            return result;
        }
    }

    public async Task<OASISResult<decimal>> GetExchangeRateAsync(
        string fromToken,
        string toToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _bridgeManager.GetExchangeRateAsync(fromToken, toToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exchange rate");
            var result = new OASISResult<decimal>();
            result.IsError = true;
            result.Message = $"Error getting exchange rate: {ex.Message}";
            return result;
        }
    }
}

