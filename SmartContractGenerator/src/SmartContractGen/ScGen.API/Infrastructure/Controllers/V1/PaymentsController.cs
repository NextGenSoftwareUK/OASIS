using Microsoft.AspNetCore.Mvc;
using ScGen.Lib.Shared.DTOs.Requests;
using ScGen.Lib.Shared.DTOs.Responses;
using ScGen.Lib.Shared.Services.X402;

namespace ScGen.API.Infrastructure.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IX402PaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IX402PaymentService paymentService,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Verify x402 payment and get payment token
    /// </summary>
    /// <param name="request">Payment verification request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Payment token for authenticated API calls</returns>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerifyPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyPayment(
        [FromBody] VerifyPaymentRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Payment verification requested: {Signature} for {Operation}/{Blockchain}",
            request.Signature, request.Operation, request.Blockchain);

        Result<VerifyPaymentResponse> result = await _paymentService.VerifyPaymentAsync(request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Payment Verification Failed",
                Detail = result.Error.ToString(),
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get pricing information for operations
    /// </summary>
    /// <param name="operation">Operation type (generate, compile, deploy)</param>
    /// <param name="blockchain">Blockchain/language (Solidity, Rust, Scrypto)</param>
    /// <returns>Pricing information</returns>
    [HttpGet("pricing")]
    [ProducesResponseType(typeof(PricingInfo), StatusCodes.Status200OK)]
    public IActionResult GetPricing(
        [FromQuery] string operation = "generate",
        [FromQuery] string blockchain = "Rust")
    {
        PaymentRequiredResponse paymentInfo = _paymentService.GetPaymentRequired(operation, blockchain);
        return Ok(paymentInfo.Pricing);
    }

    /// <summary>
    /// Get NFT holder count (mock for now)
    /// </summary>
    /// <returns>Number of NFT holders</returns>
    [HttpGet("nft/holders-count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetNFTHolderCount()
    {
        // In production, fetch from Solana blockchain
        // For now, return mock data
        return Ok(new { count = 10000 });
    }
}

