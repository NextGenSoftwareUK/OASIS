using Microsoft.AspNetCore.Mvc;
using ScGen.Lib.Shared.Services.X402;
using ScGen.Lib.Shared.Options;

namespace ScGen.API.Infrastructure.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class CreditsController : ControllerBase
{
    private readonly ICreditsService _creditsService;
    private readonly ILogger<CreditsController> _logger;

    public CreditsController(
        ICreditsService creditsService,
        ILogger<CreditsController> logger)
    {
        _creditsService = creditsService;
        _logger = logger;
    }

    /// <summary>
    /// Get credit balance for a wallet
    /// </summary>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(
        [FromQuery] string walletAddress,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            return BadRequest(new { error = "Wallet address required" });
        }

        int credits = await _creditsService.GetCreditsAsync(walletAddress, ct);
        
        return Ok(new 
        { 
            walletAddress, 
            credits,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Purchase credit pack
    /// </summary>
    [HttpPost("purchase")]
    [ProducesResponseType(typeof(CreditPurchaseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PurchaseCredits(
        [FromBody] PurchaseCreditsRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Credit purchase requested: {Wallet} - {PackSize} credits",
            request.WalletAddress, request.PackSize);

        CreditPurchaseResult result = await _creditsService.PurchaseCreditPackAsync(
            request.WalletAddress,
            request.TransactionSignature,
            request.PackSize,
            ct);

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Credit Purchase Failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get available credit packs
    /// </summary>
    [HttpGet("packs")]
    [ProducesResponseType(typeof(CreditPack[]), StatusCodes.Status200OK)]
    public IActionResult GetCreditPacks()
    {
        return Ok(CreditsService.CREDIT_PACKS);
    }

    /// <summary>
    /// Get credit cost for an operation
    /// </summary>
    [HttpGet("cost")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetCost(
        [FromQuery] string operation = "generate",
        [FromQuery] string blockchain = "Rust")
    {
        int cost = CreditCosts.GetCost(operation, blockchain);
        
        return Ok(new 
        { 
            operation, 
            blockchain, 
            credits = cost,
            equivalentSOL = PricingConfig.GetPrice(operation, blockchain)
        });
    }
}

public record PurchaseCreditsRequest
{
    public required string WalletAddress { get; init; }
    public required string TransactionSignature { get; init; }
    public required int PackSize { get; init; }
}

