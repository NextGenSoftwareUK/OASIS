using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Controllers;

/// <summary>
/// Controller for shipment operations
/// Handles listing and retrieving shipments
/// </summary>
[ApiController]
[Route("api/shipexpro/shipments")]
public class ShipmentController : ControllerBase
{
    private readonly IShipexProRepository _repository;

    public ShipmentController(IShipexProRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Get shipments by merchant ID with optional filters
    /// GET /api/shipexpro/shipments?merchantId={guid}&status={status}&limit={limit}&offset={offset}
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(OASISResult<List<Shipment>>), 200)]
    public async Task<IActionResult> GetShipments(
        [FromQuery] Guid merchantId,
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        try
        {
            if (merchantId == Guid.Empty)
            {
                return BadRequest(new OASISResult<List<Shipment>>
                {
                    IsError = true,
                    Message = "Merchant ID is required"
                });
            }

            // Get shipments for merchant
            var shipmentsResult = await _repository.GetShipmentsByMerchantIdAsync(merchantId);
            
            if (shipmentsResult.IsError)
            {
                return StatusCode(500, shipmentsResult);
            }

            var shipments = shipmentsResult.Result ?? new List<Shipment>();

            // Filter by status if provided
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ShipmentStatus>(status, out var statusEnum))
            {
                shipments = shipments.Where(s => s.Status == statusEnum).ToList();
            }

            // Apply pagination
            var total = shipments.Count;
            var paged = shipments.Skip(offset).Take(limit).ToList();

            return Ok(new OASISResult<List<Shipment>>
            {
                Result = paged,
                Message = $"Retrieved {paged.Count} of {total} shipments"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new OASISResult<List<Shipment>>
            {
                IsError = true,
                Message = $"Error retrieving shipments: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Get shipment by ID
    /// GET /api/shipexpro/shipments/{shipmentId}
    /// </summary>
    [HttpGet("{shipmentId}")]
    [ProducesResponseType(typeof(OASISResult<Shipment>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetShipment(Guid shipmentId)
    {
        try
        {
            var result = await _repository.GetShipmentAsync(shipmentId);

            if (result.IsError || result.Result == null)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new OASISResult<Shipment>
            {
                IsError = true,
                Message = $"Error retrieving shipment: {ex.Message}"
            });
        }
    }
}
