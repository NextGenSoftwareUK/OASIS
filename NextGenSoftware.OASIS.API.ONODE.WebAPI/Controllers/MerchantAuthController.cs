using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

/// <summary>
/// Controller for merchant authentication operations
/// </summary>
[ApiController]
[Route("api/shipexpro/merchant")]
public class MerchantAuthController : ControllerBase
{
    private readonly MerchantAuthService _authService;
    private readonly ILogger<MerchantAuthController> _logger;

    public MerchantAuthController(
        MerchantAuthService authService,
        ILogger<MerchantAuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register a new merchant
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(MerchantAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] MerchantRegistrationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Registering new merchant: {Email}", request.Email);

            var result = await _authService.RegisterAsync(request);

            if (result.IsError)
            {
                _logger.LogWarning("Merchant registration failed: {Message}", result.Message);
                return BadRequest(new { error = result.Message });
            }

            _logger.LogInformation("Merchant registered successfully: {MerchantId}", result.Result.MerchantId);
            return Ok(result.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in Register");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Login merchant and get JWT token
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(MerchantAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] MerchantLoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Merchant login attempt: {Email}", request.Email);

            var result = await _authService.LoginAsync(request);

            if (result.IsError)
            {
                _logger.LogWarning("Merchant login failed: {Message}", result.Message);
                return Unauthorized(new { error = result.Message });
            }

            _logger.LogInformation("Merchant logged in successfully: {MerchantId}", result.Result.MerchantId);
            return Ok(result.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in Login");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Generate a new API key for the authenticated merchant
    /// </summary>
    [HttpPost("apikeys")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateApiKey()
    {
        try
        {
            if (!HttpContext.Items.ContainsKey("MerchantId") ||
                HttpContext.Items["MerchantId"] is not Guid merchantId)
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            _logger.LogInformation("Generating API key for merchant: {MerchantId}", merchantId);

            var result = await _authService.GenerateApiKeyAsync(merchantId);

            if (result.IsError)
            {
                _logger.LogWarning("API key generation failed: {Message}", result.Message);
                return BadRequest(new { error = result.Message });
            }

            return Ok(new { apiKey = result.Result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GenerateApiKey");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

