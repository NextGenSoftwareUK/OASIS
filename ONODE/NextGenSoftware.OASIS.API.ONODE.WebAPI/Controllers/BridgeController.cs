using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

/// <summary>
/// Controller for Universal Asset Bridge operations.
/// Provides endpoints for cross-chain token swaps, exchange rates, and order management.
/// Compatible with Quantum Exchange frontend.
/// Follows OASIS standards by using BridgeManager from OASIS.API.Core.
/// </summary>
[ApiController]
[Route("api/v1/bridge")]
public class BridgeController : OASISControllerBase
{
    private BridgeManager _bridgeManager = null;
    private readonly ILogger<BridgeController> _logger;

    public BridgeManager BridgeManager
    {
        get
        {
            if (_bridgeManager == null)
            {
                _bridgeManager = BridgeManager.Instance;
            }
            return _bridgeManager;
        }
    }

    public BridgeController(ILogger<BridgeController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new cross-chain bridge order (token swap).
    /// Executes atomic swap with automatic rollback on failure.
    /// </summary>
    /// <param name="request">Order creation request with token details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order creation response with transaction details</returns>
    [HttpPost("orders")]
    [ProducesResponseType(typeof(CreateBridgeOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateBridgeOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return BadRequest(new { error = "The request body is required. Please provide a valid JSON body with FromToken, ToToken, Amount.", isError = true });
        try
        {
            _logger.LogInformation("Creating bridge order: {FromToken} → {ToToken}, Amount: {Amount}",
                request.FromToken, request.ToToken, request.Amount);

            var result = await BridgeManager.CreateBridgeOrderAsync(request, cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Bridge order creation failed: {Message}", result.Message);
                return BadRequest(new { error = result.Message, isError = true });
            }

            _logger.LogInformation("Bridge order created successfully: {OrderId}", result.Result.OrderId);
            return Ok(result.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in CreateOrder");
            return StatusCode(500, new { error = "Internal server error", isError = true });
        }
    }

    /// <summary>
    /// Checks the balance and status of an existing bridge order.
    /// </summary>
    /// <param name="orderId">Unique identifier of the order</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order balance and status information</returns>
    [HttpGet("orders/{orderId:guid}/check-balance")]
    [ProducesResponseType(typeof(BridgeOrderBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckOrderBalance(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking balance for order: {OrderId}", orderId);

            var result = await BridgeManager.CheckOrderBalanceAsync(orderId, cancellationToken);

            // Return test data if setting is enabled and result is null, has error, or result is null
            if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
            {
                return Ok(new BridgeOrderBalanceResponse
                {
                    OrderId = orderId,
                    CurrentBalance = 0,
                    Status = "pending"
                });
            }

            if (result.IsError)
            {
                _logger.LogWarning("Order balance check failed: {Message}", result.Message);
                return NotFound(new { error = result.Message, isError = true });
            }

            return Ok(result.Result);
        }
        catch (Exception ex)
        {
            // Return test data if setting is enabled, otherwise return error
            if (UseTestDataWhenLiveDataNotAvailable)
            {
                return Ok(new BridgeOrderBalanceResponse
                {
                    OrderId = orderId,
                    CurrentBalance = 0,
                    Status = "pending"
                });
            }
            _logger.LogError(ex, "Exception in CheckOrderBalance");
            return StatusCode(500, new { error = "Internal server error", isError = true });
        }
    }

    /// <summary>
    /// Gets the current exchange rate between two tokens.
    /// Optimized for low-latency real-time rate lookups.
    /// </summary>
    /// <param name="fromToken">Source token symbol (e.g., "SOL")</param>
    /// <param name="toToken">Destination token symbol (e.g., "XRD")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current exchange rate</returns>
    [HttpGet("exchange-rate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetExchangeRate(
        [FromQuery] string fromToken,
        [FromQuery] string toToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fromToken) || string.IsNullOrWhiteSpace(toToken))
            {
                return BadRequest(new { error = "Both fromToken and toToken are required", isError = true });
            }

            _logger.LogInformation("Getting exchange rate: {FromToken}/{ToToken}", fromToken, toToken);

            var result = await BridgeManager.GetExchangeRateAsync(fromToken, toToken, cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Exchange rate lookup failed: {Message}", result.Message);
                return BadRequest(new { error = result.Message, isError = true });
            }

            // Return in the format expected by frontend
            return Ok(new { rate = result.Result, fromToken, toToken, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetExchangeRate");
            return StatusCode(500, new { error = "Internal server error", isError = true });
        }
    }

    /// <summary>
    /// Gets supported networks for bridge operations.
    /// </summary>
    /// <returns>List of supported networks</returns>
    [HttpGet("networks")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object[]), StatusCodes.Status200OK)]
    public IActionResult GetSupportedNetworks()
    {
        return Ok(new[]
        {
            new { name = "Solana", symbol = "SOL", network = "devnet", status = "active" },
            new { name = "Radix", symbol = "XRD", network = "stokenet", status = "pending" },
            new { name = "Zcash", symbol = "ZEC", network = "testnet", status = "active" },
            new { name = "Aztec", symbol = "AZTEC", network = "sandbox", status = "active" },
            new { name = "Ethereum", symbol = "ETH", network = "sepolia", status = "active" },
            new { name = "Polygon", symbol = "MATIC", network = "mumbai", status = "active" },
            new { name = "Miden", symbol = "MIDEN", network = "testnet", status = "active" },
            new { name = "Starknet", symbol = "STARKNET", network = "testnet", status = "active" }
        });
    }

    /// <summary>
    /// Creates a private bridge order with viewing key audit and proof verification enabled.
    /// </summary>
    [HttpPost("orders/private")]
    [ProducesResponseType(typeof(CreateBridgeOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePrivateOrder(
        [FromBody] CreateBridgeOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return BadRequest(new { error = "The request body is required. Please provide a valid JSON body with FromToken, ToToken, Amount.", isError = true });
        try
        {
            request.EnableViewingKeyAudit = true;
            request.RequireProofVerification = true;

            var result = await BridgeManager.CreateBridgeOrderAsync(request, cancellationToken);
            if (result.IsError)
            {
                return BadRequest(new { error = result.Message, isError = true });
            }

            return Ok(result.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in CreatePrivateOrder");
            return StatusCode(500, new { error = "Internal server error", isError = true });
        }
    }

    /// <summary>
    /// Records a viewing key for auditability/compliance.
    /// </summary>
    [HttpPost("viewing-keys/audit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordViewingKey(
        [FromBody] ViewingKeyAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        if (entry == null)
            return BadRequest(new { error = "The request body is required. Please provide a valid viewing key audit entry.", isError = true });
        try
        {
            var result = await BridgeManager.RecordViewingKeyAsync(entry, cancellationToken);
            if (result.IsError)
            {
                return BadRequest(new { error = result.Message, isError = true });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in RecordViewingKey");
            return StatusCode(500, new { error = "Internal server error", isError = true });
        }
    }

    /// <summary>
    /// Verifies a submitted zero-knowledge proof payload.
    /// </summary>
    [HttpPost("proofs/verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyProof(
        [FromBody] ProofVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return BadRequest(new { error = "The request body is required. Please provide a valid JSON body with ProofPayload and ProofType.", isError = true });
        try
        {
            var result = await BridgeManager.VerifyProofAsync(request.ProofPayload, request.ProofType, cancellationToken);
            if (result.IsError || !result.Result)
            {
                return BadRequest(new { error = result.Message, isError = true });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in VerifyProof");
            return StatusCode(500, new { error = "Internal server error", isError = true });
        }
    }

    // Note: Atomic swap methods would be implemented in BridgeManager if needed
    // For now, these can be added later when atomic swap functionality is fully integrated
}

