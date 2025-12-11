using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Controllers;

/// <summary>
/// Controller for receiving webhooks from iShip and Shipox
/// Processes webhooks asynchronously and returns 200 OK immediately
/// </summary>
[ApiController]
[Route("api/shipexpro/webhooks")]
public class ShipexProWebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<ShipexProWebhookController>? _logger;

    public ShipexProWebhookController(
        IWebhookService webhookService,
        ILogger<ShipexProWebhookController>? logger = null)
    {
        _webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
        _logger = logger;
    }

    /// <summary>
    /// Receives webhooks from iShip
    /// POST /api/shipexpro/webhooks/iship
    /// </summary>
    [HttpPost("iship")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ProcessIShipWebhook()
    {
        try
        {
            // Read raw payload
            string payload;
            using (var reader = new System.IO.StreamReader(Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            _logger?.LogInformation("Received iShip webhook: {PayloadLength} bytes", payload?.Length ?? 0);

            // Get signature from header (iShip typically uses X-iShip-Signature or similar)
            var signature = Request.Headers["X-iShip-Signature"].ToString();
            if (string.IsNullOrWhiteSpace(signature))
            {
                signature = Request.Headers["X-Signature"].ToString();
            }

            // Get source IP
            var sourceIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Process webhook asynchronously (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await _webhookService.ProcessIShipWebhookAsync(payload, signature, sourceIP);
                    if (result.IsError)
                    {
                        _logger?.LogError("Failed to process iShip webhook: {Error}", result.Message);
                    }
                    else
                    {
                        _logger?.LogInformation("Successfully processed iShip webhook");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Exception processing iShip webhook");
                }
            });

            // Return 200 OK immediately (don't wait for processing)
            return Ok(new { message = "Webhook received" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception receiving iShip webhook");
            // Still return 200 to prevent retries from iShip
            return Ok(new { message = "Webhook received", error = "Processing error logged" });
        }
    }

    /// <summary>
    /// Receives webhooks from Shipox
    /// POST /api/shipexpro/webhooks/shipox
    /// </summary>
    [HttpPost("shipox")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ProcessShipoxWebhook()
    {
        try
        {
            // Read raw payload
            string payload;
            using (var reader = new System.IO.StreamReader(Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            _logger?.LogInformation("Received Shipox webhook: {PayloadLength} bytes", payload?.Length ?? 0);

            // Get signature from header (Shipox typically uses X-Shipox-Signature or similar)
            var signature = Request.Headers["X-Shipox-Signature"].ToString();
            if (string.IsNullOrWhiteSpace(signature))
            {
                signature = Request.Headers["X-Signature"].ToString();
            }

            // Get source IP
            var sourceIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Process webhook asynchronously (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await _webhookService.ProcessShipoxWebhookAsync(payload, signature, sourceIP);
                    if (result.IsError)
                    {
                        _logger?.LogError("Failed to process Shipox webhook: {Error}", result.Message);
                    }
                    else
                    {
                        _logger?.LogInformation("Successfully processed Shipox webhook");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Exception processing Shipox webhook");
                }
            });

            // Return 200 OK immediately (don't wait for processing)
            return Ok(new { message = "Webhook received" });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception receiving Shipox webhook");
            // Still return 200 to prevent retries from Shipox
            return Ok(new { message = "Webhook received", error = "Processing error logged" });
        }
    }
}




