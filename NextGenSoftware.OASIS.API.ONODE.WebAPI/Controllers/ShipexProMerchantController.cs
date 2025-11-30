using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

/// <summary>
/// Controller for merchant API endpoints (rates, quotes, orders)
/// </summary>
[ApiController]
[Route("api/shipexpro/merchant")]
public class ShipexProMerchantController : ControllerBase
{
    private readonly IShipexProRepository _repository;
    private readonly RateService? _rateService; // Will be implemented by Agent E
    private readonly ShipmentService? _shipmentService; // Will be implemented by Agent E
    private readonly ILogger<ShipexProMerchantController> _logger;

    public ShipexProMerchantController(
        IShipexProRepository repository,
        ILogger<ShipexProMerchantController> logger,
        RateService? rateService = null,
        ShipmentService? shipmentService = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rateService = rateService;
        _shipmentService = shipmentService;
    }

    /// <summary>
    /// Get shipping rates for a shipment
    /// </summary>
    [HttpPost("rates")]
    [ProducesResponseType(typeof(QuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRates([FromBody] RateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get merchant ID from context (set by auth middleware)
            if (!HttpContext.Items.ContainsKey("MerchantId") ||
                HttpContext.Items["MerchantId"] is not Guid merchantId)
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            // Ensure merchant ID matches
            request.MerchantId = merchantId;

            _logger.LogInformation("Rate request for merchant {MerchantId}", merchantId);

            // Validate request
            if (request.Weight <= 0)
            {
                return BadRequest(new { error = "Weight must be greater than 0" });
            }

            if (request.Dimensions.Length <= 0 || request.Dimensions.Width <= 0 || request.Dimensions.Height <= 0)
            {
                return BadRequest(new { error = "All dimensions must be greater than 0" });
            }

            // Call RateService (will be implemented by Agent E)
            // For now, return mock data if service is not available
            QuoteResponse quoteResponse;
            
            if (_rateService != null)
            {
                var result = await _rateService.GetRatesAsync(request);
                if (result.IsError)
                {
                    _logger.LogWarning("Rate service error: {Message}", result.Message);
                    return BadRequest(new { error = result.Message });
                }
                quoteResponse = result.Result;
            }
            else
            {
                // Mock response until Agent E implements RateService
                _logger.LogWarning("RateService not available, returning mock data");
                quoteResponse = GenerateMockQuoteResponse(request);
            }

            return Ok(quoteResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetRates");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get quote details by quote ID
    /// </summary>
    [HttpGet("quotes/{quoteId:guid}")]
    [ProducesResponseType(typeof(Quote), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetQuote(Guid quoteId)
    {
        try
        {
            // Get merchant ID from context
            if (!HttpContext.Items.ContainsKey("MerchantId") ||
                HttpContext.Items["MerchantId"] is not Guid merchantId)
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            _logger.LogInformation("Getting quote {QuoteId} for merchant {MerchantId}", quoteId, merchantId);

            var result = await _repository.GetQuoteAsync(quoteId);

            if (result.IsError || result.Result == null)
            {
                return NotFound(new { error = "Quote not found" });
            }

            // Verify merchant owns this quote
            if (result.Result.MerchantId != merchantId)
            {
                return Unauthorized(new { error = "Unauthorized to access this quote" });
            }

            return Ok(result.Result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetQuote");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new shipping order from a quote
    /// </summary>
    [HttpPost("orders")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get merchant ID from context
            if (!HttpContext.Items.ContainsKey("MerchantId") ||
                HttpContext.Items["MerchantId"] is not Guid merchantId)
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            _logger.LogInformation("Creating order for merchant {MerchantId} with quote {QuoteId}", merchantId, request.QuoteId);

            // Get quote
            var quoteResult = await _repository.GetQuoteAsync(request.QuoteId);
            if (quoteResult.IsError || quoteResult.Result == null)
            {
                return BadRequest(new { error = "Quote not found" });
            }

            var quote = quoteResult.Result;

            // Verify merchant owns quote
            if (quote.MerchantId != merchantId)
            {
                return Unauthorized(new { error = "Unauthorized to use this quote" });
            }

            // Check if quote is expired
            if (quote.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new { error = "Quote has expired" });
            }

            // Create order
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                MerchantId = merchantId,
                QuoteId = request.QuoteId,
                SelectedCarrier = request.SelectedCarrier,
                Status = "Pending",
                CustomerInfo = request.CustomerInfo,
                CreatedAt = DateTime.UtcNow
            };

            var saveResult = await _repository.SaveOrderAsync(order);
            if (saveResult.IsError)
            {
                _logger.LogWarning("Failed to save order: {Message}", saveResult.Message);
                return BadRequest(new { error = saveResult.Message });
            }

            // If ShipmentService is available, create shipment
            if (_shipmentService != null)
            {
                var shipmentRequest = new CreateShipmentRequest
                {
                    QuoteId = request.QuoteId,
                    SelectedCarrier = request.SelectedCarrier,
                    CustomerInfo = request.CustomerInfo
                };

                var shipmentResult = await _shipmentService.CreateShipmentAsync(shipmentRequest);
                if (!shipmentResult.IsError && shipmentResult.Result != null)
                {
                    order.ShipmentId = shipmentResult.Result.ShipmentId;
                    order.TrackingNumber = shipmentResult.Result.TrackingNumber;
                    order.Status = "Created";
                    order.UpdatedAt = DateTime.UtcNow;
                    await _repository.UpdateOrderAsync(order);
                }
            }

            var response = new OrderResponse
            {
                OrderId = order.OrderId,
                QuoteId = order.QuoteId,
                ShipmentId = order.ShipmentId,
                SelectedCarrier = order.SelectedCarrier,
                Status = order.Status,
                TrackingNumber = order.TrackingNumber,
                CustomerInfo = order.CustomerInfo,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in CreateOrder");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get order details by order ID
    /// </summary>
    [HttpGet("orders/{orderId:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOrder(Guid orderId)
    {
        try
        {
            // Get merchant ID from context
            if (!HttpContext.Items.ContainsKey("MerchantId") ||
                HttpContext.Items["MerchantId"] is not Guid merchantId)
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            _logger.LogInformation("Getting order {OrderId} for merchant {MerchantId}", orderId, merchantId);

            var result = await _repository.GetOrderAsync(orderId);

            if (result.IsError || result.Result == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            // Verify merchant owns this order
            if (result.Result.MerchantId != merchantId)
            {
                return Unauthorized(new { error = "Unauthorized to access this order" });
            }

            var order = result.Result;
            var response = new OrderResponse
            {
                OrderId = order.OrderId,
                QuoteId = order.QuoteId,
                ShipmentId = order.ShipmentId,
                SelectedCarrier = order.SelectedCarrier,
                Status = order.Status,
                TrackingNumber = order.TrackingNumber,
                CustomerInfo = order.CustomerInfo,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetOrder");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update an existing order (before shipment is created)
    /// </summary>
    [HttpPut("orders/{orderId:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrder(Guid orderId, [FromBody] OrderRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get merchant ID from context
            if (!HttpContext.Items.ContainsKey("MerchantId") ||
                HttpContext.Items["MerchantId"] is not Guid merchantId)
            {
                return Unauthorized(new { error = "Unauthorized" });
            }

            _logger.LogInformation("Updating order {OrderId} for merchant {MerchantId}", orderId, merchantId);

            var orderResult = await _repository.GetOrderAsync(orderId);
            if (orderResult.IsError || orderResult.Result == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            var order = orderResult.Result;

            // Verify merchant owns this order
            if (order.MerchantId != merchantId)
            {
                return Unauthorized(new { error = "Unauthorized to update this order" });
            }

            // Only allow updates if shipment hasn't been created
            if (order.ShipmentId.HasValue)
            {
                return BadRequest(new { error = "Cannot update order after shipment is created" });
            }

            // Update order
            order.SelectedCarrier = request.SelectedCarrier;
            order.CustomerInfo = request.CustomerInfo;
            order.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _repository.UpdateOrderAsync(order);
            if (updateResult.IsError)
            {
                _logger.LogWarning("Failed to update order: {Message}", updateResult.Message);
                return BadRequest(new { error = updateResult.Message });
            }

            var response = new OrderResponse
            {
                OrderId = order.OrderId,
                QuoteId = order.QuoteId,
                ShipmentId = order.ShipmentId,
                SelectedCarrier = order.SelectedCarrier,
                Status = order.Status,
                TrackingNumber = order.TrackingNumber,
                CustomerInfo = order.CustomerInfo,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in UpdateOrder");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Generate mock quote response (temporary until RateService is implemented)
    /// </summary>
    private QuoteResponse GenerateMockQuoteResponse(RateRequest request)
    {
        var quoteId = Guid.NewGuid();
        var baseRate = 10.00m + (request.Weight * 2.5m);
        var markup = baseRate * 0.20m; // 20% markup

        return new QuoteResponse
        {
            QuoteId = quoteId,
            Quotes = new List<QuoteOption>
            {
                new QuoteOption
                {
                    Carrier = "UPS",
                    CarrierRate = baseRate,
                    ClientPrice = baseRate + markup,
                    MarkupAmount = markup,
                    EstimatedDays = 3,
                    ServiceName = "Ground"
                },
                new QuoteOption
                {
                    Carrier = "FedEx",
                    CarrierRate = baseRate + 2.00m,
                    ClientPrice = (baseRate + 2.00m) + (baseRate + 2.00m) * 0.20m,
                    MarkupAmount = (baseRate + 2.00m) * 0.20m,
                    EstimatedDays = 2,
                    ServiceName = "Express"
                }
            },
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
    }
}

// Temporary models until Agent E implements services
internal class RateService
{
    public Task<OASISResult<QuoteResponse>> GetRatesAsync(RateRequest request)
    {
        throw new NotImplementedException("RateService will be implemented by Agent E");
    }
}

internal class ShipmentService
{
    public Task<OASISResult<ShipmentResponse>> CreateShipmentAsync(CreateShipmentRequest request)
    {
        throw new NotImplementedException("ShipmentService will be implemented by Agent E");
    }
}

internal class CreateShipmentRequest
{
    public Guid QuoteId { get; set; }
    public string SelectedCarrier { get; set; } = string.Empty;
    public CustomerInfo CustomerInfo { get; set; } = new();
}

internal class ShipmentResponse
{
    public Guid ShipmentId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

