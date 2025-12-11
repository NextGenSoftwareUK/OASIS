using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.Shipox.Models;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.Shipox;

/// <summary>
/// Service for interacting with Shipox's order management system
/// Handles order CRUD operations and carrier aggregation
/// </summary>
public class ShipoxConnectorService
{
    private readonly ShipoxApiClient _apiClient;

    public ShipoxConnectorService(string baseUrl, ISecretVaultService secretVault, Guid merchantId)
    {
        _apiClient = new ShipoxApiClient(baseUrl, secretVault, merchantId);
    }

    /// <summary>
    /// Creates a new order in Shipox
    /// </summary>
    public async Task<OASISResult<ShipoxOrder>> CreateOrderAsync(ShipoxOrderRequest request)
    {
        try
        {
            var result = await _apiClient.PostAsync<ShipoxApiResponse<ShipoxOrder>>("/api/orders", request);
            
            if (result.IsError || result.Result == null)
            {
                return new OASISResult<ShipoxOrder>
                {
                    IsError = true,
                    Message = result.Message ?? "Failed to create order in Shipox"
                };
            }

            if (!result.Result.Success || result.Result.Data == null)
            {
                return new OASISResult<ShipoxOrder>
                {
                    IsError = true,
                    Message = result.Result.Error ?? result.Result.Message ?? "Shipox API returned unsuccessful response"
                };
            }

            return new OASISResult<ShipoxOrder>(result.Result.Data);
        }
        catch (Exception ex)
        {
            return new OASISResult<ShipoxOrder>
            {
                IsError = true,
                Message = $"Exception creating order: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Retrieves an order from Shipox by order ID
    /// </summary>
    public async Task<OASISResult<ShipoxOrder>> GetOrderAsync(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return new OASISResult<ShipoxOrder>
            {
                IsError = true,
                Message = "Order ID is required"
            };
        }

        try
        {
            var result = await _apiClient.GetAsync<ShipoxApiResponse<ShipoxOrder>>($"/api/orders/{orderId}");
            
            if (result.IsError || result.Result == null)
            {
                return new OASISResult<ShipoxOrder>
                {
                    IsError = true,
                    Message = result.Message ?? "Failed to retrieve order from Shipox"
                };
            }

            if (!result.Result.Success || result.Result.Data == null)
            {
                return new OASISResult<ShipoxOrder>
                {
                    IsError = true,
                    Message = result.Result.Error ?? result.Result.Message ?? "Order not found"
                };
            }

            return new OASISResult<ShipoxOrder>(result.Result.Data);
        }
        catch (Exception ex)
        {
            return new OASISResult<ShipoxOrder>
            {
                IsError = true,
                Message = $"Exception retrieving order: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Updates an existing order in Shipox
    /// </summary>
    public async Task<OASISResult<ShipoxOrder>> UpdateOrderAsync(string orderId, ShipoxOrderUpdate update)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return new OASISResult<ShipoxOrder>
            {
                IsError = true,
                Message = "Order ID is required"
            };
        }

        try
        {
            var result = await _apiClient.PutAsync<ShipoxApiResponse<ShipoxOrder>>($"/api/orders/{orderId}", update);
            
            if (result.IsError || result.Result == null)
            {
                return new OASISResult<ShipoxOrder>
                {
                    IsError = true,
                    Message = result.Message ?? "Failed to update order in Shipox"
                };
            }

            if (!result.Result.Success || result.Result.Data == null)
            {
                return new OASISResult<ShipoxOrder>
                {
                    IsError = true,
                    Message = result.Result.Error ?? result.Result.Message ?? "Shipox API returned unsuccessful response"
                };
            }

            return new OASISResult<ShipoxOrder>(result.Result.Data);
        }
        catch (Exception ex)
        {
            return new OASISResult<ShipoxOrder>
            {
                IsError = true,
                Message = $"Exception updating order: {ex.Message}",
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Gets available carriers for an order (carrier aggregation)
    /// </summary>
    public async Task<OASISResult<List<ShipoxCarrier>>> GetAvailableCarriersAsync(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return new OASISResult<List<ShipoxCarrier>>
            {
                IsError = true,
                Message = "Order ID is required"
            };
        }

        try
        {
            var result = await _apiClient.GetAsync<ShipoxApiResponse<List<ShipoxCarrier>>>($"/api/orders/{orderId}/carriers");
            
            if (result.IsError || result.Result == null)
            {
                return new OASISResult<List<ShipoxCarrier>>
                {
                    IsError = true,
                    Message = result.Message ?? "Failed to retrieve carriers from Shipox"
                };
            }

            if (!result.Result.Success || result.Result.Data == null)
            {
                return new OASISResult<List<ShipoxCarrier>>
                {
                    IsError = true,
                    Message = result.Result.Error ?? result.Result.Message ?? "No carriers available"
                };
            }

            return new OASISResult<List<ShipoxCarrier>>(result.Result.Data);
        }
        catch (Exception ex)
        {
            return new OASISResult<List<ShipoxCarrier>>
            {
                IsError = true,
                Message = $"Exception retrieving carriers: {ex.Message}",
                Exception = ex
            };
        }
    }
}

