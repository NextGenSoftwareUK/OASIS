using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

/// <summary>
/// Controller for Universal Asset Bridge operations.
/// Provides endpoints for cross-chain token swaps, exchange rates, and order management.
/// </summary>
[ApiController]
[Route("api/v1/bridge")]
public class BridgeController : ControllerBase
{
    private readonly ICrossChainBridgeManager _bridgeManager;
    private readonly ILogger<BridgeController> _logger;

    public BridgeController(
        ICrossChainBridgeManager bridgeManager,
        ILogger<BridgeController> logger)
    {
        _bridgeManager = bridgeManager ?? throw new ArgumentNullException(nameof(bridgeManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new cross-chain bridge order (token swap).
    /// Executes atomic swap with automatic rollback on failure.
    /// </summary>
    /// <param name="request">Order creation request with token details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order creation response with transaction details</returns>
    [HttpPost("order/create")]
    [ProducesResponseType(typeof(CreateBridgeOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateBridgeOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating bridge order: {FromToken} → {ToToken}, Amount: {Amount}",
                request.FromToken, request.ToToken, request.Amount);

            var result = await _bridgeManager.CreateBridgeOrderAsync(request, cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Bridge order creation failed: {Message}", result.Message);
                return BadRequest(new { error = result.Message });
            }

            _logger.LogInformation("Bridge order created successfully: {OrderId}", result.Result.OrderId);
            return Ok(result.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in CreateOrder");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Checks the balance and status of an existing bridge order.
    /// </summary>
    /// <param name="orderId">Unique identifier of the order</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order balance and status information</returns>
    [HttpGet("order/{orderId:guid}/balance")]
    [ProducesResponseType(typeof(BridgeOrderBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckOrderBalance(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking balance for order: {OrderId}", orderId);

            var result = await _bridgeManager.CheckOrderBalanceAsync(orderId, cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Order balance check failed: {Message}", result.Message);
                return NotFound(new { error = result.Message });
            }

            return Ok(result.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in CheckOrderBalance");
            return StatusCode(500, new { error = "Internal server error" });
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
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
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
                return BadRequest(new { error = "Both fromToken and toToken are required" });
            }

            _logger.LogInformation("Getting exchange rate: {FromToken}/{ToToken}", fromToken, toToken);

            var result = await _bridgeManager.GetExchangeRateAsync(fromToken, toToken, cancellationToken);

            if (result.IsError)
            {
                _logger.LogWarning("Exchange rate lookup failed: {Message}", result.Message);
                return BadRequest(new { error = result.Message });
            }

            return Ok(new { rate = result.Result, fromToken, toToken });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetExchangeRate");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets supported networks for bridge operations.
    /// </summary>
    /// <returns>List of supported networks</returns>
    [HttpGet("networks")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    public IActionResult GetSupportedNetworks()
    {
        return Ok(new[]
        {
            new { name = "Solana", symbol = "SOL", network = "devnet", status = "active" },
            new { name = "Radix", symbol = "XRD", network = "stokenet", status = "pending" },
            new { name = "Ethereum", symbol = "ETH", network = "sepolia", status = "planned" },
            new { name = "Polygon", symbol = "MATIC", network = "mumbai", status = "planned" }
        });
    }
}




