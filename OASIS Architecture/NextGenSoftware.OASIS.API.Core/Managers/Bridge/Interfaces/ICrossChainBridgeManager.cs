using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.Interfaces;

/// <summary>
/// Manager interface for cross-chain bridge operations between different blockchain networks
/// </summary>
public interface ICrossChainBridgeManager
{
    /// <summary>
    /// Creates a cross-chain bridge order (e.g., SOL to XRD swap)
    /// </summary>
    /// <param name="request">The bridge order request details</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>The created order response</returns>
    Task<OASISResult<CreateBridgeOrderResponse>> CreateBridgeOrderAsync(
        CreateBridgeOrderRequest request,
        CancellationToken token = default);
    
    /// <summary>
    /// Checks the status and balance of a bridge order
    /// </summary>
    /// <param name="orderId">The order ID to check</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>The order balance status</returns>
    Task<OASISResult<BridgeOrderBalanceResponse>> CheckOrderBalanceAsync(
        Guid orderId,
        CancellationToken token = default);
    
    /// <summary>
    /// Gets the current exchange rate between two tokens
    /// </summary>
    /// <param name="fromToken">Source token symbol</param>
    /// <param name="toToken">Destination token symbol</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>The exchange rate</returns>
    Task<OASISResult<decimal>> GetExchangeRateAsync(
        string fromToken,
        string toToken,
        CancellationToken token = default);
}

