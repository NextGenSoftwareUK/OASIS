using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Controllers;

/// <summary>
/// Admin controller for viewing webhook history and managing retries
/// </summary>
[ApiController]
[Route("api/shipexpro/admin/webhooks")]
[Authorize] // Require authentication for admin endpoints
public class ShipexProWebhookAdminController : ControllerBase
{
    private readonly IShipexProRepository _repository;
    private readonly WebhookRetryService _retryService;
    private readonly ILogger<ShipexProWebhookAdminController>? _logger;

    public ShipexProWebhookAdminController(
        IShipexProRepository repository,
        WebhookRetryService retryService,
        ILogger<ShipexProWebhookAdminController>? logger = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
        _logger = logger;
    }

    /// <summary>
    /// Get webhook event by ID
    /// GET /api/shipexpro/admin/webhooks/{eventId}
    /// </summary>
    [HttpGet("{eventId}")]
    [ProducesResponseType(typeof(OASISResult<WebhookEvent>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetWebhookEvent(Guid eventId)
    {
        try
        {
            var result = await _repository.GetWebhookEventAsync(eventId);

            if (result.IsError || result.Result == null)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception getting webhook event");
            return StatusCode(500, new OASISResult<WebhookEvent>
            {
                IsError = true,
                Message = $"Internal server error: {ex.Message}",
                Exception = ex
            });
        }
    }

    /// <summary>
    /// Retry a failed webhook event
    /// POST /api/shipexpro/admin/webhooks/{eventId}/retry
    /// </summary>
    [HttpPost("{eventId}/retry")]
    [ProducesResponseType(typeof(OASISResult<bool>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RetryWebhook(Guid eventId)
    {
        try
        {
            var result = await _retryService.RetryWebhookAsync(eventId);

            if (result.IsError)
            {
                if (result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(result);
                }
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception retrying webhook");
            return StatusCode(500, new OASISResult<bool>
            {
                IsError = true,
                Message = $"Internal server error: {ex.Message}",
                Exception = ex
            });
        }
    }

    /// <summary>
    /// Retry all failed webhooks
    /// POST /api/shipexpro/admin/webhooks/retry-all
    /// </summary>
    [HttpPost("retry-all")]
    [ProducesResponseType(typeof(OASISResult<int>), 200)]
    public async Task<IActionResult> RetryAllFailedWebhooks()
    {
        try
        {
            var result = await _retryService.RetryFailedWebhooksAsync();

            if (result.IsError)
            {
                return StatusCode(500, result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception retrying all webhooks");
            return StatusCode(500, new OASISResult<int>
            {
                IsError = true,
                Message = $"Internal server error: {ex.Message}",
                Exception = ex
            });
        }
    }
}

