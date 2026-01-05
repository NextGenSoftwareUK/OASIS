using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Controllers;

/// <summary>
/// Controller for Shipox UI endpoints
/// Provides quote requests, shipment confirmation, and tracking functionality
/// </summary>
[ApiController]
[Route("api/shipexpro/shipox")]
public class ShipexProShipoxController : ControllerBase
{
    private readonly IRateService _rateService;
    private readonly IShipmentService _shipmentService;
    private readonly ILogger<ShipexProShipoxController>? _logger;

    public ShipexProShipoxController(
        IRateService rateService,
        IShipmentService shipmentService,
        ILogger<ShipexProShipoxController>? logger = null)
    {
        _rateService = rateService ?? throw new ArgumentNullException(nameof(rateService));
        _shipmentService = shipmentService ?? throw new ArgumentNullException(nameof(shipmentService));
        _logger = logger;
    }

    /// <summary>
    /// Request shipping quotes for Shipox UI customers
    /// POST /api/shipexpro/shipox/quote-request
    /// </summary>
    [HttpPost("quote-request")]
    [ProducesResponseType(typeof(OASISResult<QuoteResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RequestQuote([FromBody] RateRequest request)
    {
        try
        {
            _logger?.LogInformation("Shipox quote request received for merchant: {MerchantId}", request.MerchantId);

            if (request == null)
            {
                return BadRequest(new OASISResult<QuoteResponse>
                {
                    IsError = true,
                    Message = "Request body is required"
                });
            }

            // Validate request
            if (request.MerchantId == Guid.Empty)
            {
                return BadRequest(new OASISResult<QuoteResponse>
                {
                    IsError = true,
                    Message = "MerchantId is required"
                });
            }

            // Get rates (reuses RateService from Agent E)
            var result = await _rateService.GetRatesAsync(request);

            if (result.IsError)
            {
                _logger?.LogError("Failed to get rates: {Error}", result.Message);
                return StatusCode(500, result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception in quote request");
            return StatusCode(500, new OASISResult<QuoteResponse>
            {
                IsError = true,
                Message = $"Internal server error: {ex.Message}",
                Exception = ex
            });
        }
    }

    /// <summary>
    /// Confirm shipment after customer selects a quote
    /// POST /api/shipexpro/shipox/confirm-shipment
    /// </summary>
    [HttpPost("confirm-shipment")]
    [ProducesResponseType(typeof(OASISResult<ShipmentResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ConfirmShipment([FromBody] OrderRequest request)
    {
        try
        {
            _logger?.LogInformation("Shipox shipment confirmation received for quote: {QuoteId}", request.QuoteId);

            if (request == null)
            {
                return BadRequest(new OASISResult<ShipmentResponse>
                {
                    IsError = true,
                    Message = "Request body is required"
                });
            }

            // Validate request
            if (request.QuoteId == Guid.Empty)
            {
                return BadRequest(new OASISResult<ShipmentResponse>
                {
                    IsError = true,
                    Message = "QuoteId is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.SelectedCarrier))
            {
                return BadRequest(new OASISResult<ShipmentResponse>
                {
                    IsError = true,
                    Message = "SelectedCarrier is required"
                });
            }

            // Create shipment (reuses ShipmentService from Agent E)
            var result = await _shipmentService.CreateShipmentAsync(request);

            if (result.IsError)
            {
                _logger?.LogError("Failed to create shipment: {Error}", result.Message);
                return StatusCode(500, result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception in shipment confirmation");
            return StatusCode(500, new OASISResult<ShipmentResponse>
            {
                IsError = true,
                Message = $"Internal server error: {ex.Message}",
                Exception = ex
            });
        }
    }

    /// <summary>
    /// Track shipment by tracking number
    /// GET /api/shipexpro/shipox/track/{trackingNumber}
    /// </summary>
    [HttpGet("track/{trackingNumber}")]
    [ProducesResponseType(typeof(OASISResult<TrackingResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> TrackShipment(string trackingNumber)
    {
        try
        {
            _logger?.LogInformation("Shipox tracking request for: {TrackingNumber}", trackingNumber);

            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                return BadRequest(new OASISResult<TrackingResponse>
                {
                    IsError = true,
                    Message = "Tracking number is required"
                });
            }

            // Get tracking information (reuses ShipmentService from Agent E)
            var result = await _shipmentService.GetTrackingAsync(trackingNumber);

            if (result.IsError)
            {
                if (result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(result);
                }
                _logger?.LogError("Failed to get tracking: {Error}", result.Message);
                return StatusCode(500, result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception in tracking request");
            return StatusCode(500, new OASISResult<TrackingResponse>
            {
                IsError = true,
                Message = $"Internal server error: {ex.Message}",
                Exception = ex
            });
        }
    }
}

