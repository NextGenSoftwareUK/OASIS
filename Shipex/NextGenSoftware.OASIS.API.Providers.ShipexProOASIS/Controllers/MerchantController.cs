using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Controllers;

/// <summary>
/// Controller for merchant operations
/// Handles merchant profile management linked to OASIS avatars
/// </summary>
[ApiController]
[Route("api/shipexpro/merchant")]
public class MerchantController : ControllerBase
{
    private readonly IShipexProRepository _repository;

    public MerchantController(IShipexProRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Get merchant by avatar ID
    /// GET /api/shipexpro/merchant/by-avatar/{avatarId}
    /// </summary>
    [HttpGet("by-avatar/{avatarId}")]
    [ProducesResponseType(typeof(OASISResult<Merchant>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMerchantByAvatar(string avatarId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(avatarId) || !Guid.TryParse(avatarId, out var avatarGuid))
            {
                return BadRequest(new OASISResult<Merchant>
                {
                    IsError = true,
                    Message = "Valid avatar ID is required"
                });
            }

            // Search for merchant by avatar ID
            var result = await _repository.GetMerchantByAvatarIdAsync(avatarGuid);

            if (result.IsError || result.Result == null)
            {
                return NotFound(new OASISResult<Merchant>
                {
                    IsError = true,
                    Message = result.Message ?? "Merchant not found for this avatar. Please create merchant profile."
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new OASISResult<Merchant>
            {
                IsError = true,
                Message = $"Error retrieving merchant: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Create merchant profile from OASIS avatar
    /// POST /api/shipexpro/merchant/create-from-avatar
    /// </summary>
    [HttpPost("create-from-avatar")]
    [ProducesResponseType(typeof(OASISResult<Merchant>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateMerchantFromAvatar([FromBody] CreateMerchantFromAvatarRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new OASISResult<Merchant>
                {
                    IsError = true,
                    Message = "Request body is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.AvatarId) || !Guid.TryParse(request.AvatarId, out var avatarGuid))
            {
                return BadRequest(new OASISResult<Merchant>
                {
                    IsError = true,
                    Message = "Valid avatar ID is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.CompanyName))
            {
                return BadRequest(new OASISResult<Merchant>
                {
                    IsError = true,
                    Message = "Company name is required"
                });
            }

            // Check if merchant already exists for this avatar
            var existingMerchantResult = await _repository.GetMerchantByAvatarIdAsync(avatarGuid);
            if (!existingMerchantResult.IsError && existingMerchantResult.Result != null)
            {
                return Ok(new OASISResult<Merchant>
                {
                    Result = existingMerchantResult.Result,
                    Message = "Merchant profile already exists for this avatar"
                });
            }

            // Generate API key
            var apiKey = Guid.NewGuid().ToString("N");
            var apiKeyHash = HashApiKey(apiKey);

            // Parse rate limit tier
            if (!Enum.TryParse<RateLimitTier>(request.RateLimitTier ?? "Basic", out var tier))
            {
                tier = RateLimitTier.Basic;
            }

            // Create merchant
            var merchant = new Merchant
            {
                MerchantId = Guid.NewGuid(),
                CompanyName = request.CompanyName,
                ContactInfo = new ContactInfo
                {
                    Email = request.Email ?? "",
                    Phone = request.Phone ?? "",
                    Address = request.Address ?? ""
                },
                ApiKeyHash = apiKeyHash,
                RateLimitTier = tier,
                IsActive = true,
                QuickBooksConnected = false,
                Configuration = new MerchantConfiguration
                {
                    AutoCreateInvoices = false,
                    DefaultCurrency = "USD",
                    TimeZone = "UTC"
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save merchant
            var result = await _repository.SaveMerchantAsync(merchant);

            if (result.IsError)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetMerchantByAvatar), new { avatarId = request.AvatarId }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new OASISResult<Merchant>
            {
                IsError = true,
                Message = $"Error creating merchant: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get merchant by ID
    /// GET /api/shipexpro/merchant/{merchantId}
    /// </summary>
    [HttpGet("{merchantId}")]
    [ProducesResponseType(typeof(OASISResult<Merchant>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMerchant(Guid merchantId)
    {
        try
        {
            var result = await _repository.GetMerchantAsync(merchantId);

            if (result.IsError || result.Result == null)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new OASISResult<Merchant>
            {
                IsError = true,
                Message = $"Error retrieving merchant: {ex.Message}"
            });
        }
    }

    private static string HashApiKey(string apiKey)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(apiKey);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}

/// <summary>
/// Request model for creating merchant from avatar
/// </summary>
public class CreateMerchantFromAvatarRequest
{
    public string AvatarId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? RateLimitTier { get; set; } = "Basic";
}
