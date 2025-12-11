using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.Common;
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
    [ProducesResponseType(typeof(OASISResult<Models.QuoteResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RequestQuote([FromBody] RateRequest request)
    {
        try
        {
            _logger?.LogInformation("Shipox quote request received for merchant: {MerchantId}", request.MerchantId);

            if (request == null)
            {
                return BadRequest(new OASISResult<Models.QuoteResponse>
                {
                    IsError = true,
                    Message = "Request body is required"
                });
            }

            // Validate request
            if (request.MerchantId == Guid.Empty)
            {
                return BadRequest(new OASISResult<Models.QuoteResponse>
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
            return StatusCode(500, new OASISResult<Models.QuoteResponse>
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
    [ProducesResponseType(typeof(OASISResult<Models.ShipmentResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ConfirmShipment([FromBody] OrderRequest request)
    {
        try
        {
            _logger?.LogInformation("Shipox shipment confirmation received for quote: {QuoteId}", request.QuoteId);

            if (request == null)
            {
                return BadRequest(new OASISResult<Models.ShipmentResponse>
                {
                    IsError = true,
                    Message = "Request body is required"
                });
            }

            // Validate request
            if (request.QuoteId == Guid.Empty)
            {
                return BadRequest(new OASISResult<Models.ShipmentResponse>
                {
                    IsError = true,
                    Message = "QuoteId is required"
                });
            }

            if (string.IsNullOrWhiteSpace(request.SelectedCarrier))
            {
                return BadRequest(new OASISResult<Models.ShipmentResponse>
                {
                    IsError = true,
                    Message = "SelectedCarrier is required"
                });
            }

            // Create shipment (reuses ShipmentService from Agent E)
            // Convert OrderRequest to CreateShipmentRequest
            var createRequest = new Services.CreateShipmentRequest
            {
                QuoteId = request.QuoteId,
                SelectedCarrier = request.SelectedCarrier,
                CustomerInfo = request.CustomerInfo
            };
            var result = await _shipmentService.CreateShipmentAsync(createRequest);

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
            return StatusCode(500, new OASISResult<Models.ShipmentResponse>
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
    [ProducesResponseType(typeof(OASISResult<Models.TrackingResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> TrackShipment(string trackingNumber)
    {
        try
        {
            _logger?.LogInformation("Shipox tracking request for: {TrackingNumber}", trackingNumber);

            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                return BadRequest(new OASISResult<Models.TrackingResponse>
                {
                    IsError = true,
                    Message = "Tracking number is required"
                });
            }

            // Get tracking information (reuses ShipmentService from Agent E)
            var shipmentResult = await _shipmentService.GetShipmentByTrackingNumberAsync(trackingNumber);
            
            if (shipmentResult.IsError || shipmentResult.Result == null)
            {
                var notFoundResult = new OASISResult<Models.TrackingResponse>
                {
                    IsError = true,
                    Message = shipmentResult.Message ?? "Tracking information not found"
                };
                return NotFound(notFoundResult);
            }
            
            // Convert Shipment to TrackingResponse
            var shipment = shipmentResult.Result;
            var trackingResponse = new Models.TrackingResponse
            {
                TrackingNumber = shipment.TrackingNumber ?? trackingNumber,
                Status = shipment.Status.ToString(),
                CurrentLocation = null, // Can be populated from carrier API if needed
                EstimatedDelivery = null, // Can be populated from carrier API if needed
                History = shipment.StatusHistory?.Select(h => new Models.TrackingHistoryItem
                {
                    Status = h.Status.ToString(),
                    Timestamp = h.Timestamp,
                    Location = h.Source,
                    Description = $"Status changed to {h.Status}"
                }).ToList() ?? new List<Models.TrackingHistoryItem>()
            };
            
            var result = new OASISResult<Models.TrackingResponse>
            {
                Result = trackingResponse
            };

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
            return StatusCode(500, new OASISResult<Models.TrackingResponse>
            {
                IsError = true,
                Message = $"Internal server error: {ex.Message}",
                Exception = ex
            });
        }
    }
}

