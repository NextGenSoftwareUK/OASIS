//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

///// <summary>
///// Controller for Universal Asset Bridge operations.
///// Provides endpoints for cross-chain token swaps, exchange rates, and order management.
///// Compatible with Quantum Exchange frontend.
///// </summary>
//[ApiController]
//[Route("api/v1")]
//public class BridgeController : ControllerBase
//{
//    private readonly BridgeService _bridgeService;
//    private readonly ILogger<BridgeController> _logger;

//    public BridgeController(
//        BridgeService bridgeService,
//        ILogger<BridgeController> logger)
//    {
//        _bridgeService = bridgeService ?? throw new ArgumentNullException(nameof(bridgeService));
//        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//    }

//    /// <summary>
//    /// Creates a new cross-chain bridge order (token swap).
//    /// Executes atomic swap with automatic rollback on failure.
//    /// </summary>
//    /// <param name="request">Order creation request with token details</param>
//    /// <param name="cancellationToken">Cancellation token</param>
//    /// <returns>Order creation response with transaction details</returns>
//    [HttpPost("orders")]
//    [ProducesResponseType(typeof(CreateBridgeOrderResponse), StatusCodes.Status200OK)]
//    [ProducesResponseType(StatusCodes.Status400BadRequest)]
//    public async Task<IActionResult> CreateOrder(
//        [FromBody] CreateBridgeOrderRequest request,
//        CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            _logger.LogInformation("Creating bridge order: {FromToken} → {ToToken}, Amount: {Amount}",
//                request.FromToken, request.ToToken, request.Amount);

//            var result = await _bridgeService.CreateOrderAsync(request, cancellationToken);

//            if (result.IsError)
//            {
//                _logger.LogWarning("Bridge order creation failed: {Message}", result.Message);
//                return BadRequest(new { error = result.Message });
//            }

//            _logger.LogInformation("Bridge order created successfully: {OrderId}", result.Result.OrderId);
//            return Ok(result.Result);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Exception in CreateOrder");
//            return StatusCode(500, new { error = "Internal server error" });
//        }
//    }

//    /// <summary>
//    /// Checks the balance and status of an existing bridge order.
//    /// </summary>
//    /// <param name="orderId">Unique identifier of the order</param>
//    /// <param name="cancellationToken">Cancellation token</param>
//    /// <returns>Order balance and status information</returns>
//    [HttpGet("orders/{orderId:guid}/check-balance")]
//    [ProducesResponseType(typeof(BridgeOrderBalanceResponse), StatusCodes.Status200OK)]
//    [ProducesResponseType(StatusCodes.Status404NotFound)]
//    public async Task<IActionResult> CheckOrderBalance(
//        [FromRoute] Guid orderId,
//        CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            _logger.LogInformation("Checking balance for order: {OrderId}", orderId);

//            var result = await _bridgeService.CheckBalanceAsync(orderId, cancellationToken);

//            if (result.IsError)
//            {
//                _logger.LogWarning("Order balance check failed: {Message}", result.Message);
//                return NotFound(new { error = result.Message });
//            }

//            return Ok(result.Result);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Exception in CheckOrderBalance");
//            return StatusCode(500, new { error = "Internal server error" });
//        }
//    }

//    /// <summary>
//    /// Gets the current exchange rate between two tokens.
//    /// Optimized for low-latency real-time rate lookups.
//    /// </summary>
//    /// <param name="fromToken">Source token symbol (e.g., "SOL")</param>
//    /// <param name="toToken">Destination token symbol (e.g., "XRD")</param>
//    /// <param name="cancellationToken">Cancellation token</param>
//    /// <returns>Current exchange rate</returns>
//    [HttpGet("exchange-rate")]
//    [AllowAnonymous]
//    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
//    [ProducesResponseType(StatusCodes.Status400BadRequest)]
//    public async Task<IActionResult> GetExchangeRate(
//        [FromQuery] string fromToken,
//        [FromQuery] string toToken,
//        CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            if (string.IsNullOrWhiteSpace(fromToken) || string.IsNullOrWhiteSpace(toToken))
//            {
//                return BadRequest(new { error = "Both fromToken and toToken are required" });
//            }

//            _logger.LogInformation("Getting exchange rate: {FromToken}/{ToToken}", fromToken, toToken);

//            var result = await _bridgeService.GetExchangeRateAsync(fromToken, toToken, cancellationToken);

//            if (result.IsError)
//            {
//                _logger.LogWarning("Exchange rate lookup failed: {Message}", result.Message);
//                return BadRequest(new { error = result.Message });
//            }

//            // Return in the format expected by frontend
//            return Ok(new { rate = result.Result, fromToken, toToken, timestamp = DateTime.UtcNow });
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Exception in GetExchangeRate");
//            return StatusCode(500, new { error = "Internal server error" });
//        }
//    }

//    /// <summary>
//    /// Gets supported networks for bridge operations.
//    /// </summary>
//    /// <returns>List of supported networks</returns>
//    [HttpGet("networks")]
//    [AllowAnonymous]
//    [ProducesResponseType(typeof(object[]), StatusCodes.Status200OK)]
//    public IActionResult GetSupportedNetworks()
//    {
//        return Ok(new[]
//        {
//            new { name = "Solana", symbol = "SOL", network = "devnet", status = "active" },
//                new { name = "Radix", symbol = "XRD", network = "stokenet", status = "pending" },
//                new { name = "Zcash", symbol = "ZEC", network = "testnet", status = "active" },
//                new { name = "Aztec", symbol = "AZTEC", network = "sandbox", status = "active" },
//                new { name = "Ethereum", symbol = "ETH", network = "sepolia", status = "planned" },
//                new { name = "Polygon", symbol = "MATIC", network = "mumbai", status = "planned" }
//        });
//    }

//    [HttpPost("orders/private")]
//    public async Task<IActionResult> CreatePrivateOrder(
//        [FromBody] CreateBridgeOrderRequest request,
//        CancellationToken cancellationToken = default)
//    {
//        request.EnableViewingKeyAudit = true;
//        request.RequireProofVerification = true;

//        var result = await _bridgeService.CreateOrderAsync(request, cancellationToken);
//        if (result.IsError)
//        {
//            return BadRequest(new { error = result.Message });
//        }

//        return Ok(result.Result);
//    }

//    [HttpPost("viewing-keys/audit")]
//    public async Task<IActionResult> RecordViewingKey(
//        [FromBody] ViewingKeyAuditEntry entry,
//        CancellationToken cancellationToken = default)
//    {
//        var result = await _bridgeService.RecordViewingKeyAsync(entry, cancellationToken);
//        if (result.IsError)
//        {
//            return BadRequest(new { error = result.Message });
//        }

//        return Ok(new { success = true });
//    }

//    [HttpPost("proofs/verify")]
//    public async Task<IActionResult> VerifyProof(
//        [FromBody] ProofVerificationRequest request,
//        CancellationToken cancellationToken = default)
//    {
//        var result = await _bridgeService.VerifyProofAsync(request, cancellationToken);
//        if (result.IsError || !result.Result)
//        {
//            return BadRequest(new { error = result.Message });
//        }

//        return Ok(new { success = true });
//    }

//    /// <summary>
//    /// Creates a Starknet ↔ Zcash atomic swap
//    /// </summary>
//    /// <param name="request">Atomic swap request</param>
//    /// <param name="cancellationToken">Cancellation token</param>
//    /// <returns>Swap status response</returns>
//    [HttpPost("bridge/atomic-swap")]
//    [ProducesResponseType(typeof(AtomicSwapStatusResponse), StatusCodes.Status200OK)]
//    [ProducesResponseType(StatusCodes.Status400BadRequest)]
//    public async Task<IActionResult> CreateAtomicSwap(
//        [FromBody] AtomicSwapRequest request,
//        CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            _logger.LogInformation("Creating atomic swap: {FromChain} → {ToChain}, Amount: {Amount}",
//                request.FromChain, request.ToChain, request.Amount);

//            var result = await _bridgeService.CreateAtomicSwapAsync(request, cancellationToken);

//            if (result.IsError)
//            {
//                _logger.LogWarning("Atomic swap creation failed: {Message}", result.Message);
//                return BadRequest(new { error = result.Message, isError = true });
//            }

//            _logger.LogInformation("Atomic swap created successfully: {BridgeId}", result.Result.BridgeId);
//            return Ok(result.Result);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Exception in CreateAtomicSwap");
//            return StatusCode(500, new { error = "Internal server error", isError = true });
//        }
//    }

//    /// <summary>
//    /// Gets the status of an atomic swap by bridge ID
//    /// </summary>
//    /// <param name="bridgeId">Bridge/swap ID</param>
//    /// <param name="cancellationToken">Cancellation token</param>
//    /// <returns>Swap status response</returns>
//    [HttpGet("bridge/atomic-swap/status/{bridgeId}")]
//    [ProducesResponseType(typeof(AtomicSwapStatusResponse), StatusCodes.Status200OK)]
//    [ProducesResponseType(StatusCodes.Status404NotFound)]
//    public async Task<IActionResult> GetAtomicSwapStatus(
//        [FromRoute] string bridgeId,
//        CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            _logger.LogInformation("Getting atomic swap status: {BridgeId}", bridgeId);

//            var result = await _bridgeService.GetAtomicSwapStatusAsync(bridgeId, cancellationToken);

//            if (result.IsError)
//            {
//                _logger.LogWarning("Atomic swap status check failed: {Message}", result.Message);
//                return NotFound(new { error = result.Message, isError = true });
//            }

//            return Ok(result.Result);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Exception in GetAtomicSwapStatus");
//            return StatusCode(500, new { error = "Internal server error", isError = true });
//        }
//    }

//    /// <summary>
//    /// Gets atomic swap history for the current user
//    /// </summary>
//    /// <param name="userId">User ID (optional, defaults to authenticated user)</param>
//    /// <param name="cancellationToken">Cancellation token</param>
//    /// <returns>Swap history response</returns>
//    [HttpGet("bridge/atomic-swap/history")]
//    [ProducesResponseType(typeof(AtomicSwapHistoryResponse), StatusCodes.Status200OK)]
//    [ProducesResponseType(StatusCodes.Status400BadRequest)]
//    public async Task<IActionResult> GetAtomicSwapHistory(
//        [FromQuery] Guid? userId,
//        CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            // If userId not provided, try to get from authenticated user
//            // For now, require userId in query string
//            if (!userId.HasValue)
//            {
//                return BadRequest(new { error = "userId query parameter is required", isError = true });
//            }

//            _logger.LogInformation("Getting atomic swap history for user: {UserId}", userId);

//            var result = await _bridgeService.GetAtomicSwapHistoryAsync(userId.Value, cancellationToken);

//            if (result.IsError)
//            {
//                _logger.LogWarning("Atomic swap history retrieval failed: {Message}", result.Message);
//                return BadRequest(new { error = result.Message, isError = true });
//            }

//            return Ok(result.Result);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Exception in GetAtomicSwapHistory");
//            return StatusCode(500, new { error = "Internal server error", isError = true });
//        }
//    }
//}

